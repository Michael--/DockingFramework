using System;
using System.Diagnostics;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Reflection;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Globalization;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using IronPython.Hosting;
using IronPython.Runtime;
using Docking.Tools;
using Docking.Widgets;
using Docking.Framework;
using Gtk;
using Docking.Helper;


namespace Docking.Components
{
   // This is a workaround for the problem that we cannot write "blablabla".Localized(this) inside class "ComponentManager",
   // Because that would yield the wrong namespace: not "Docking.Components", but the one of the inherited main application class.
   static class StringLoc
   {
      public static string L(this string s)
      {
         return s.Localized("Docking.Components");
      }
   }

   public class ComponentManager : Gtk.Window, IPersistency, IMessageWriteLine, ICut, ICopy, IPaste
   {

      #region Initialization

      private static int mMainThreadID;

      public bool IsMainThread
      {
         get { return Thread.CurrentThread.ManagedThreadId == mMainThreadID; }
      }

      public readonly Stopwatch Clock; // A global clock. Useful for many purposes. This way you don't need to create own clocks to just measure time intervals.

      public string[] CommandLineArguments;

      public string ApplicationName { get; private set; }

      static ComponentManager()
      {
         PowerDown = false;
      }

      // make sure that you construct this class from the main thread!
      public ComponentManager(string[] args, string application_name, string pythonApplicationObjectName)
      : base(WindowType.Toplevel)
      {
         Clock = new Stopwatch();
         Clock.Start();

         mMainThreadID = Thread.CurrentThread.ManagedThreadId; // make sure that you construct this class from the main thread!

         CommandLineArguments = args;

         ApplicationName = application_name;

         Localization = new Components.Localization(this);
         Localization.SearchForResources(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Languages", "*.resx"));

         AccelGroup = new AccelGroup();
         AddAccelGroup(AccelGroup);

         LicenseGroup = new LicenseGroup() { DefaultState = Components.LicenseGroup.State.ENABLED };
         ComponentFinder = new Docking.Components.ComponentFinder();

         InitPythonEngine(pythonApplicationObjectName);

         MakeWidgetReceiveDropEvents(Toplevel, OnDragDataReceived);

         this.WindowStateEvent += OnWindowStateChanged;
      }

      public Gdk.WindowState WindowState { get; protected set; }

      private void OnWindowStateChanged(object sender, WindowStateEventArgs args)
      {
         WindowState = args.Event.NewWindowState;
      }

      public void SetDockFrame(DockFrame df)
      {
         DockFrame = df;
         DockFrame.DockItemRemoved += HandleDockItemRemoved;
         DockFrame.CreateItem = this.CreateItem;

         DockVisualStyle style = new DockVisualStyle();
         style.PadTitleLabelColor = Styles.PadLabelColor;
         style.PadBackgroundColor = Styles.PadBackground;
         style.InactivePadBackgroundColor = Styles.InactivePadBackground;
         // style.PadTitleHeight = barHeight;
         DockFrame.DefaultVisualStyle = style;

         mNormalStyle = DockFrame.DefaultVisualStyle;
         mSelectedStyle = DockFrame.DefaultVisualStyle.Clone();
         mSelectedStyle.PadBackgroundColor = new Gdk.Color(100, 160, 255);//new Gdk.Color(255, 0, 0);
      }

      public void SetStatusBar(Statusbar sb)
      {
         StatusBar = sb;
      }

      public void SetToolBar(Toolbar tb)
      {
         ToolBar = tb;
      }

      public Menu FindMenu(String path, bool createIfNotExist)
      {
         // the last name is the menu name, all others are menu/sub-menu names
         String[] m = path.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

         // as a minimum 1 submenu name must exist where to append the new entry
         Debug.Assert(m.Length >= 1);

         MenuShell menuShell = MenuBar;
         Menu foundmenu = null;
         System.Collections.IEnumerable children = MenuBar.Children;
         for(int i = 0; i < m.Length; i++)
         {
            foundmenu = SearchMenu(m[i], menuShell, children);
            if (foundmenu == null && createIfNotExist)
               foundmenu = CreateMenu(m[i], menuShell);
            if (foundmenu != null)
            {
               children = foundmenu.Children;
               menuShell = foundmenu;
            }
         }

         return foundmenu;
      }

      // TODO: quickhack to add "Export" menu entries - find a better way to let components add and remove menu items later
      public void AppendExportMenuQuickHack(MenuItem item)
      {
         mExportSubmenu.Append(item);
         MenuBar.ShowAll();
      }

      public void RemoveExportMenuQuickHack(MenuItem item)
      {
         mExportSubmenu.Remove(item);
         MenuBar.ShowAll();
      }

      protected void AppendMenuItem(String path, MenuItem item)
      {
         Menu menu = FindMenu(path, true);
         if (menu != null)
            menu.Append(item);
      }

      protected void SetMenuBar(MenuBar menuBar)
      {
         MenuBar = menuBar;
      }

      public AccelGroup AccelGroup { get; private set; }

      const string CONFIG_ROOT_ELEMENT = "DockingConfiguration";
      const string DEFAULT_LAYOUT_NAME = "Default"; // TODO can we localize this string? Careful, the name is persisted...

      ImageMenuItem m_DeleteLayout;

      /// <summary>
      /// Installs the layout menu, show all existing layouts
      /// and a possibility to add and remove layouts
      /// The main layout is not removeable.
      /// If the main layout name is empty or null "Default" will be used as name.
      /// </summary>
      public void InstallLayoutMenu(string currentlayout)
      {
         m_DeleteLayout = new TaggedLocalizedImageMenuItem("Delete Current Layout");
         m_DeleteLayout.Activated += (object sender, EventArgs e) =>
         {
            if(DockFrame.CurrentLayout != DEFAULT_LAYOUT_NAME)
            {
               ResponseType result = MessageBox.Show(this, MessageType.Question,
                                         ButtonsType.YesNo,
                                         "Are you sure to remove the current layout?".L());

               if(result == ResponseType.Yes)
               {
                  MenuItem nitem = sender as MenuItem;
                  DockFrame.DeleteLayout(DockFrame.CurrentLayout);
                  RemoveMenuItem(nitem.Parent, DockFrame.CurrentLayout);
                  DockFrame.CurrentLayout = DEFAULT_LAYOUT_NAME;
                  CheckMenuItem(nitem.Parent, DockFrame.CurrentLayout);
                  m_DeleteLayout.Sensitive = (DockFrame.CurrentLayout != DEFAULT_LAYOUT_NAME);
               }
            }
         };

         ImageMenuItem newLayout = new TaggedLocalizedImageMenuItem("New Layout...");
         newLayout.Activated += (object sender, EventArgs e) =>
         {
            String newLayoutName = null;
            bool createEmptyLayout = true;

            NewLayout dialog = new NewLayout(this);
            dialog.SetPosition(WindowPosition.CenterOnParent);
            ResponseType response = (ResponseType) dialog.Run();
            if(response == ResponseType.Ok)
            {
               if(dialog.LayoutName.Length > 0)
                  newLayoutName = dialog.LayoutName;
               createEmptyLayout = dialog.EmptyLayout;
            }
            dialog.Destroy();

            if(newLayoutName == null)
               return;

            if(!DockFrame.HasLayout(newLayoutName))
            {
               DockFrame.CreateLayout(newLayoutName, !createEmptyLayout);
               DockFrame.CurrentLayout = newLayoutName;
               AppendLayoutMenu(newLayoutName, false);
               m_DeleteLayout.Sensitive = (DockFrame.CurrentLayout != DEFAULT_LAYOUT_NAME);
            }
         };

         AppendMenuItem(@"Options\Layout", newLayout);
         AppendMenuItem(@"Options\Layout", m_DeleteLayout);
         AppendMenuItem(@"Options\Layout", new SeparatorMenuItem());

         foreach(String s in DockFrame.Layouts)
            AppendLayoutMenu(s, true);

         m_DeleteLayout.Sensitive = (DockFrame.CurrentLayout != DEFAULT_LAYOUT_NAME);
         MenuBar.ShowAll();
      }

      private void CheckMenuItem(object baseMenu, string name)
      {
         recursionWorkaround = true;
         if(baseMenu is Menu)
         {
            Menu bm = baseMenu as Menu;
            foreach(object obj in bm)
            {
               if(obj is CheckMenuItem)
               {
                  CheckMenuItem mi = obj as CheckMenuItem;
                  String label = (mi.Child as Label).Text;

                  if(label == name)
                  {
                     if(!mi.Active)
                        mi.Active = true;
                     recursionWorkaround = false;
                     return;
                  }
               }
            }
         }
         recursionWorkaround = false;
      }

      private void RemoveMenuItem(object baseMenu, string name)
      {
         if(baseMenu is Menu)
         {
            Menu bm = baseMenu as Menu;
            foreach(object obj in bm)
            {
               if(obj is CheckMenuItem)
               {
                  CheckMenuItem mi = obj as CheckMenuItem;
                  String label = (mi.Child as Label).Text;

                  if(label == name)
                  {
                     bm.Remove(mi);
                     bm.ShowAll();
                     return;
                  }
               }
            }
         }
      }

      private void UncheckMenuChildren(object baseMenu, object except)
      {
         recursionWorkaround = true;
         // uncheck all other
         if(baseMenu is Menu)
         {
            foreach(object obj in ((Menu) baseMenu))
            {
               if(obj is CheckMenuItem && obj != except)
               {
                  CheckMenuItem mi = obj as CheckMenuItem;
                  if(mi.Active)
                     mi.Active = false;
               }
            }
         }
         recursionWorkaround = false;
      }

      bool recursionWorkaround = false;

      private void AppendLayoutMenu(String name, bool init)
      {
         CheckMenuItem item = new CheckMenuItem(name);

         item.Activated += (object sender, EventArgs e) =>
         {
            if(recursionWorkaround)
               return;

            CheckMenuItem nitem = sender as CheckMenuItem;
            String label = (nitem.Child as Label).Text;

            // double check
            if(DockFrame.HasLayout(label))
            {
               if(DockFrame.CurrentLayout != label)
               {
                  // uncheck all other
                  UncheckMenuChildren(nitem.Parent, nitem);

                  // before check selected layout
                  DockFrame.CurrentLayout = label;
                  if(!nitem.Active)
                     nitem.Active = true;
                  MessageWriteLine(String.Format("CurrentLayout={0}", label));
                  m_DeleteLayout.Sensitive = (DockFrame.CurrentLayout != DEFAULT_LAYOUT_NAME);
               }
               else
                  if(!nitem.Active)
                  {
                     nitem.Active = true;
                  }
            }
         };
         AppendMenuItem(@"Options\Layout", item);
         if(!init)
            UncheckMenuChildren(item.Parent, item);
         item.Active = (name == DockFrame.CurrentLayout);
         item.ShowAll();
      }

      private void AddLayout(string name, bool copyCurrent)
      {
         if(!DockFrame.HasLayout(name))
            DockFrame.CreateLayout(name, copyCurrent);
      }

      private void DeleteLayout(string name)
      {
         if(name != DockFrame.CurrentLayout)
         {
            DockFrame.DeleteLayout(name);
         }
      }

      private void InstallQuitMenu()
      {
         ImageMenuItem item = new TaggedLocalizedImageMenuItem("Quit");
         item.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.Quit-16.png"));
         item.AddAccelerator("activate", AccelGroup, new AccelKey(Gdk.Key.F4, Gdk.ModifierType.Mod1Mask, AccelFlags.Visible));
         item.AddAccelerator("activate", AccelGroup, new AccelKey(Gdk.Key.Q, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
         item.Activated += OnQuitActionActivated;
         AppendMenuItem("File", item);
      }

      protected void OnQuitActionActivated(object sender, EventArgs args)
      {
         Quit(true);
      }

      public  int                       MaxRecentFiles    = 9;
      private SeparatorMenuItem         mRecentFilesBegin = null;
      private List<TaggedImageMenuItem> mRecentFiles      = new List<TaggedImageMenuItem>();

      public void AddRecentFile(string filename, bool do_update_menu = true)
      {
         if(string.IsNullOrEmpty(filename))
            return;

         string filename_normalized = Platform.AdjustDirectorySeparators(filename);

         RemoveRecentFile(filename_normalized, false);
         var filename_shortened = StringTools.ShrinkPath(filename_normalized, 80);

         TaggedImageMenuItem newitem = new TaggedImageMenuItem(filename_shortened);
         newitem.Tag = filename_normalized; // the FULL filename
         //newitem.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.File-16.png"));
         newitem.Activated += OnRecentFileActivated;
         (newitem.Child as Label).UseUnderline = false;

         mRecentFiles.Insert(0, newitem);
         if(mRecentFiles.Count>MaxRecentFiles)
            mRecentFiles.RemoveRange(MaxRecentFiles, mRecentFiles.Count-MaxRecentFiles);

         if(do_update_menu)
            UpdateRecentFilesMenu();
     }

     public void RemoveRecentFile(string filename, bool do_update_menu = true)
     {
         List<TaggedImageMenuItem> founditems = new List<TaggedImageMenuItem>();

         StringComparison mode = Platform.IsWindows
                               ? StringComparison.InvariantCultureIgnoreCase
                               : StringComparison.InvariantCulture;
         foreach(TaggedImageMenuItem item in mRecentFiles)
            if(((string)item.Tag).Equals(filename, mode))
               founditems.Add(item);

         foreach(TaggedImageMenuItem item in founditems)
            mRecentFiles.Remove(item);

         if(do_update_menu)
            UpdateRecentFilesMenu();
     }

     private void UpdateRecentFilesMenu()
     {
         Menu filemenu = FindMenu("File", true);

         List<Widget> oldstuff = new List<Widget>();
         oldstuff.AddRange(filemenu.Children);

         if(mRecentFilesBegin!=null)
         {
            bool deletionmode = false;
            foreach(Widget w in oldstuff)
            {
               if(w==mRecentFilesBegin)
                  deletionmode = true;
               if(deletionmode)
                  filemenu.Remove(w);
            }
         }
         mRecentFilesBegin = null;

         if(mRecentFiles.Count>0)
         {
            mRecentFilesBegin = new SeparatorMenuItem();
            filemenu.Append(mRecentFilesBegin);
            mRecentFilesBegin.ShowAll();

            foreach(TaggedImageMenuItem r in mRecentFiles)
            {
               filemenu.Append(r);
               r.ShowAll();
             }
         }
      }

      protected void OnRecentFileActivated(object sender, EventArgs args)
      {
         TaggedImageMenuItem item = sender as TaggedImageMenuItem;
         if(item==null)
            return;

         string filename = (string) item.Tag;

         if(!File.Exists(filename))
         {
            if(MessageBox.Show(MessageType.Question, ButtonsType.YesNo,
                               "File '{0}' does not exist. Do you want to remove it from the recent files list?".L(), filename
                              )==ResponseType.Yes)
               RemoveRecentFile(filename);
            return;
         }

         if(!OpenFile(filename))
         {
            if(MessageBox.Show(MessageType.Question, ButtonsType.YesNo,
                               "Opening file '{0}' failed. Maybe no window is open which can handle it. In future, this tool will offer you such a window. Currently it cannot do that yet. Do you want to remove the file from the recent files list?".L(), filename
                              )==ResponseType.Yes)
               RemoveRecentFile(filename);
            return;
         }

         // no call AddToRecentFiles() is necessary here, OpenFile() already takes care of that
      }

      #region cut, copy, paste

      ImageMenuItem mMenuCut, mMenuCopy, mMenuPaste;

      private void InstallEditMenu()
      {
         mMenuCut = new TaggedLocalizedImageMenuItem("Cut");
         mMenuCut.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.Cut-16.png"));
         mMenuCut.Activated += OnCutActivated;
         mMenuCut.Sensitive = false;
         mMenuCut.AddAccelerator("activate", AccelGroup, new AccelKey(Gdk.Key.x, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
         mMenuCut.AddAccelerator("activate", AccelGroup, new AccelKey(Gdk.Key.Delete, Gdk.ModifierType.ShiftMask, AccelFlags.Visible));
         AppendMenuItem("Edit", mMenuCut);

         mMenuCopy = new TaggedLocalizedImageMenuItem("Copy");
         mMenuCopy.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.Copy-16.png"));
         mMenuCopy.Activated += OnCopyActivated;
         mMenuCopy.Sensitive = false;
         mMenuCopy.AddAccelerator("activate", AccelGroup, new AccelKey(Gdk.Key.c, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
         mMenuCopy.AddAccelerator("activate", AccelGroup, new AccelKey(Gdk.Key.Insert, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
         AppendMenuItem("Edit", mMenuCopy);

         mMenuPaste = new TaggedLocalizedImageMenuItem("Paste");
         mMenuPaste.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.Paste-16.png"));
         mMenuPaste.Activated += OnPasteActivated;
         mMenuPaste.Sensitive = false;
         mMenuPaste.AddAccelerator("activate", AccelGroup, new AccelKey(Gdk.Key.v, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
         mMenuPaste.AddAccelerator("activate", AccelGroup, new AccelKey(Gdk.Key.Insert, Gdk.ModifierType.ShiftMask, AccelFlags.Visible));
         AppendMenuItem("Edit", mMenuPaste);
      }

      protected void OnCutActivated(object sender, EventArgs e)
      {
         (this as ICut).Cut();
      }

      protected void OnCopyActivated(object sender, EventArgs e)
      {
         (this as ICopy).Copy();
      }

      protected void OnPasteActivated(object sender, EventArgs e)
      {
         (this as IPaste).Paste();
      }

      void ICut.Cut()
      {
         if(CurrentDockItem != null && CurrentDockItem.Content != null)
         {
            if(CurrentDockItem.Content is ICut)
               (CurrentDockItem.Content as ICut).Cut();
            else
               MessageWriteLine("current component does not implement interface ICut");
         }
      }

      void ICopy.Copy()
      {
         if(CurrentDockItem != null && CurrentDockItem.Content != null)
         {
            if(CurrentDockItem.Content is ICopy)
               (CurrentDockItem.Content as ICopy).Copy();
            else
               MessageWriteLine("current component does not implement interface ICopy");
         }
      }

      void IPaste.Paste()
      {
         if(CurrentDockItem != null && CurrentDockItem.Content != null)
         {
            if(CurrentDockItem.Content is IPaste)
               (CurrentDockItem.Content as IPaste).Paste();
            else
               MessageWriteLine("current component does not implement interface IPaste");
         }
      }

      #endregion

      /// <summary>
      /// Add all component start/create menu entries
      /// </summary>
      protected void AddComponentMenus()
      {
         InstallFileOpenMenu();
         InstallFileSaveConfigMenu();
         InstallExportMenu();
         InstallQuitMenu();
         InstallEditMenu();

         // get all menu entries first
         List<KeyValuePair<string, TaggedLocalizedImageMenuItem>> menu = new List<KeyValuePair<string, TaggedLocalizedImageMenuItem>>();
         foreach(ComponentFactoryInformation cfi in ComponentFinder.ComponentInfos)
         {
            if(cfi.MenuPath == null)
               continue;

            if (!LicenseGroup.IsEnabled(cfi.LicenseGroup))
               continue;

            // the last name is the menu name, all others are menu/sub-menu names
            String[] m = cfi.MenuPath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

            // as a minimum submenu-name & menu-name must exist
            Debug.Assert(m.Length >= 2);

            // build path again
            StringBuilder builder = new StringBuilder();
            for(int i = 0; i < m.Length - 1; i++)
            {
               if(i > 0)
                  builder.Append("\\");
               builder.Append(m[i]);
            }

            // use last entry as menu name and create
            TaggedLocalizedImageMenuItem item = new TaggedLocalizedImageMenuItem(m[m.Length - 1]);
            item.Tag = cfi;
            // TODO: make the menu image visible if you know how
            Gdk.Pixbuf pb = cfi.Icon;
            if(pb != null)
               item.Image = new Image(pb);
            item.Activated += ComponentHandleActivated;

            menu.Add(new KeyValuePair<string, TaggedLocalizedImageMenuItem>(builder.ToString(), item));
         }

         // after collecting sort by path and name before add to menu
         menu.Sort((p1, p2) => (p1.Key + p1.Value.LabelText).CompareTo(p2.Key + p2.Value.LabelText));
         foreach (var kvp in menu)
            AppendMenuItem(kvp.Key, kvp.Value);
         MenuBar.ShowAll();
      }

      private Menu SearchMenu(String name, MenuShell menuShell, System.Collections.IEnumerable children)
      {
         // 1st search menu & return if existing
         foreach(MenuItem mi in children)
         {
            if(mi is TaggedLocalizedMenuItem)
            {
               if((mi as TaggedLocalizedMenuItem).LocalizationKey != null && (mi as TaggedLocalizedMenuItem).LocalizationKey == name)
                  return mi.Submenu as Menu;
            }
            else
               if(mi is TaggedLocalizedImageMenuItem)
               {
                  if((mi as TaggedLocalizedImageMenuItem).LocalizationKey != null && (mi as TaggedLocalizedImageMenuItem).LocalizationKey == name)
                     return mi.Submenu as Menu;
               }
               else
                  if(mi is TaggedLocalizedCheckedMenuItem)
                  {
                     if((mi as TaggedLocalizedCheckedMenuItem).LocalizationKey != null && !(mi as TaggedLocalizedCheckedMenuItem).IgnoreLocalization && (mi as TaggedLocalizedCheckedMenuItem).LocalizationKey == name)
                        return mi.Submenu as Menu;
                  }

            // When we get here, localization hasn't taken place yet (LocalizationKey==null)
            // OR
            // we are dealing with non-localized menu entries.
            // In both cases, the current label text of the menu is the one to look at.

            Label label = (Label) mi.Child;
            if(label != null && label.Text == name)
               return mi.Submenu as Menu;
         }

         return null;
      }

      private Menu CreateMenu(String name, MenuShell menuShell)
      {
         // append new menu
         // todo: currently append at the end, may a dedicated position desired
         Menu menu = new Menu();
         MenuItem menuItem = new TaggedLocalizedMenuItem(name);
         menuItem.Submenu = menu;

         // todo: menu insert position should be overworked
         //       position is dependent of content

         menuShell.Add(menuItem);

         return menu;
      }

      private Menu mLanguageBaseMenu = null;

      protected void InstallLanguageMenu(string baseMenu)
      {
         string[] languages = Localization.AvailableLanguages();
         if(languages == null || languages.Length == 0)
            return;

         foreach(string s in languages)
         {
            string[] split = s.Split(new Char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            if(split.Length < 2)
               continue;

            string code = split[0];
            string name = split[1];

            TaggedLocalizedCheckedMenuItem item = new TaggedLocalizedCheckedMenuItem(name) { IgnoreLocalization = true };
            item.Activated += OnLanguageActivated;
            item.Tag = code;
            AppendMenuItem(string.Format("{0}\\Language", baseMenu, name), item);

            if(mLanguageBaseMenu == null)
               mLanguageBaseMenu = item.Parent as Menu;

            item.Active = Localization.CurrentLanguageCode == code;
         }
      }

      protected void OnLanguageActivated(object sender, EventArgs e)
      {
         if(recursionWorkaround)
            return;

         TaggedLocalizedCheckedMenuItem nitem = sender as TaggedLocalizedCheckedMenuItem;
         string code = nitem.Tag as string;
         SetLanguage(code, false, true);
      }

      protected void SetLanguage(string code, bool enforceLanguageChangedNotification, bool triggerRedraw)
      {
         if(recursionWorkaround)
            return;

         bool result = Localization.SetLanguage(code);
         UncheckMenuChildren(mLanguageBaseMenu, null);
         CheckMenuItem(mLanguageBaseMenu, Localization.CurrentLanguageName);

         if(result || enforceLanguageChangedNotification)
            UpdateLanguage(triggerRedraw);
      }

      public void UpdateLanguage(bool triggerRedraw)
      {
         bool isvis = this.Visible;

         if(isvis && triggerRedraw)
            this.Hide();

         try
         {
            foreach(DockItem item in DockFrame.GetItems())
            {
               if(item.Content != null)
               {
                  Localization.LocalizeControls(item.Content.GetType().Namespace, item.Widget);

                  if(item.Content is ILocalizableComponent)
                  {
                     ILocalizableComponent il = item.Content as ILocalizableComponent;
                     il.LocalizationChanged(item);
                     item.Content.Name = il.Name.Localized(item.Content);
                  }
               }
               item.UpdateTitle();
            }

            Localization.LocalizeMenu(MenuBar);
         }
         catch(Exception e)
         {
            if(isvis && triggerRedraw)
               this.Show();

            throw e;
         }

         // after localization change, the child elements may need re-layouting
         //DockFrame.ResizeChildren(); // TODO this breaks VirtualListView layout, commented it out

         if(isvis && triggerRedraw)
            this.Show();
      }

      #endregion

      #region private properties

      private Statusbar      StatusBar                { get; set; }
      private Toolbar        ToolBar                  { get; set; }
      private MenuBar        MenuBar                  { get; set; }
      private String         ConfigurationFilename    { get; set; }
      private XmlDocument    ConfigurationXmlDocument { get; set; }
      private XmlNode        ConfigurationXmlNode     { get; set; }

      #endregion

      #region public properties

      public DockFrame       DockFrame                { get; private set; }
      public ComponentFinder ComponentFinder          { get; private set; }
      public LicenseGroup    LicenseGroup             { get; private set; }
      public static bool     PowerDown                { get; private set; }
      public Localization    Localization             { get; private set; }

      #endregion

      #region OpenFile

      protected IArchive Archive { get; set; }

      void InstallFileOpenMenu()
      {
         TaggedLocalizedImageMenuItem menuItem = new TaggedLocalizedImageMenuItem("Open...");
         menuItem.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.File-16.png"));
         menuItem.AddAccelerator("activate", AccelGroup, new AccelKey(Gdk.Key.O, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
         menuItem.Activated += (sender, e) =>
         {
            List<FileFilterExt> filters = new List<FileFilterExt>();

            if(Archive!=null && Archive is IFileOpen) // if an archive handler is installed that implements IFileOpen
            {
               List<FileFilterExt> morefilters = (Archive as IFileOpen).SupportedFileTypes();
               filters.AddRange(morefilters);
            }

            foreach(DockItem d in DockFrame.GetItems())
            {
               if(d.Content is IFileOpen)
               {
                  List<FileFilterExt> morefilters = (d.Content as IFileOpen).SupportedFileTypes();
                  #if DEBUG
                  foreach(FileFilterExt f in morefilters)
                     MessageWriteLine("{0} supports opening {1}", d.Content.Name, f.Name);
                  #endif
                  filters.AddRange(morefilters);
               }
            }

            String filename = OpenFileDialog("Open file...", filters);
            if(filename != null)
               OpenFile(filename);
         };
         AppendMenuItem("File", menuItem);
      }

      void InstallFileSaveConfigMenu()
      {
         TaggedLocalizedImageMenuItem menuItem = new TaggedLocalizedImageMenuItem("Save Config As...");
         menuItem.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.Save-16.png"));
         menuItem.AddAccelerator("activate", AccelGroup, new AccelKey(Gdk.Key.S, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
         menuItem.Activated += (sender, e) =>
         {
            string filename = SaveFileDialog("Save Config As...", new List<FileFilterExt>()
            {
               new FileFilterExt("*.xml", "Config File")
            }, ConfigurationFilename);

            if (filename != null)
            {
               ConfigurationFilename = filename;
               SaveConfigurationFile();
            }
         };
         AppendMenuItem("File", menuItem);
      }


      public bool OpenFile(string filename, object archiveHandle = null)
      {
         if (archiveHandle == null && Directory.Exists(filename))
         {
            MessageWriteLine("Opening whole directories like {0} currently isn't implemented".FormatLocalizedWithPrefix("Docking.Components", filename));
            return false;
         }

         List<KeyValuePair<IFileOpen, string>> openers = new List<KeyValuePair<IFileOpen, string>>();

         bool isArchive = archiveHandle == null && Archive != null && Archive.IsArchive(filename);
         int openedArchiveFiles = 0;
         if (isArchive)
         {
            var handle = Archive.Open(filename);
            if (handle != null)
            {
               var files = Archive.GetFileNames(handle);
               foreach (var s in files)
               {
                  if (OpenFile(s, handle))
                     openedArchiveFiles++;
               }
               Archive.Close(handle);
            }
         }

         else
         {
            // search EXISTING instances of components if they can handle this file:
            foreach (DockItem item in DockFrame.GetItems())
            {
               if (item.Content is IFileOpen)
               {
                  IFileOpen opener = (item.Content as IFileOpen);
                  string info = opener.TryOpenFile(filename);
                  if (info != null)
                     openers.Add(new KeyValuePair<IFileOpen, string>(opener, info));
               }
            }

            if (openers.Count <= 0)
            {
               // TODO now search all classes implementing IFileOpen by calling their method IFileOpen.SupportedFileTypes()
               // and let the user instantiate them to handle this file.
               // That's not implemented yet. For now we fail with despair:
               MessageWriteLine("No component is instantiated which can handle file {0}".FormatLocalizedWithPrefix("Docking.Components"), filename);
               return false;
            }
         }


         // open file from archive need optionally copy of file
         if (archiveHandle != null)
         {
            filename = Archive.Extract(archiveHandle, filename);
         }

         if (!File.Exists(filename))
         {
            MessageWriteLine("File {0} does not exist".FormatLocalizedWithPrefix("Docking.Components", filename));
            return false;
         }

         // TODO: consider all and let the user pick which one
         if (!isArchive)
         {
            MessageWriteLine(Localization.Format("Docking.Components.Opening file {0} as {1}..."), filename, openers[0].Value);
            bool success = openers[0].Key.OpenFile(filename);
            MessageWriteLine(success ? "File opened successfully" : "File opening failed");
            if (success)
               AddRecentFile(filename);
            return success;
         }

         // any archive opening
         if (openedArchiveFiles > 0)
            AddRecentFile(filename);

         return openedArchiveFiles > 0;
      }

      public String OpenFolderDialog(string title, string startFolder = null)
      {
         String result = null;

         FileChooserDialogLocalized dlg = new FileChooserDialogLocalized(title, this, FileChooserAction.SelectFolder,
                                              "Select".L(), ResponseType.Accept,
                                              "Cancel".L(), ResponseType.Cancel);

         if(!String.IsNullOrEmpty(startFolder))
            dlg.SetCurrentFolder(startFolder);

         if(RunFileChooserDialogLocalized(dlg, null) == (int) ResponseType.Accept)
            result = dlg.Filename;

         dlg.Destroy();
         return result;
      }

      public String OpenFileDialog(string prompt, FileFilterExt filter = null, string startFolder = null)
      {
         List<FileFilterExt> filters = new List<FileFilterExt>();
         if(filter!=null)
            filters.Add(filter);
         return OpenFileDialog(prompt, filters, startFolder);
      }

      public String OpenFileDialog(string title, List<FileFilterExt> filters, string startFolder = null)
      {
         string result = null;

         FileChooserDialogLocalized dlg = new FileChooserDialogLocalized(title, this, FileChooserAction.Open,
                                              "Open".L(),   ResponseType.Accept,
                                              "Cancel".L(), ResponseType.Cancel);
         if(!String.IsNullOrEmpty(startFolder))
            dlg.SetCurrentFolder(startFolder);

         if(RunFileChooserDialogLocalized(dlg, filters) == (int) ResponseType.Accept)
         {
            result = dlg.Filename;
            AddRecentFile(result);
         }

         dlg.Destroy();
         return result;
      }

      public string[] OpenFilesDialog(string prompt, FileFilterExt filter = null, string startFolder = null)
      {
         List<FileFilterExt> filters = new List<FileFilterExt>();
         if(filter!=null)
            filters.Add(filter);
         return OpenFilesDialog(prompt, filters, startFolder);
      }

      public string[] OpenFilesDialog(string title, List<FileFilterExt> filters, string startFolder = null)
      {
         string[] result = null;

         FileChooserDialogLocalized dlg = new FileChooserDialogLocalized(title, this, FileChooserAction.Open,
                                              "Open".L(),   ResponseType.Accept,
                                              "Cancel".L(), ResponseType.Cancel);
         if(!String.IsNullOrEmpty(startFolder))
            dlg.SetCurrentFolder(startFolder);

         dlg.SelectMultiple = true;

         if(RunFileChooserDialogLocalized(dlg, filters) == (int) ResponseType.Accept)
         {
            result = dlg.Filenames;
            if(result!=null)
               foreach(string filename in result)
                  AddRecentFile(filename);
         }

         dlg.Destroy();
         return result;
      }

      Menu mExportSubmenu = new Menu();

      void InstallExportMenu()
      {
         if(LicenseGroup.IsEnabled("nts"))
         {
            MenuItem menuItem = new TaggedLocalizedImageMenuItem("Export");
            menuItem.Submenu = mExportSubmenu;
            AppendMenuItem("File", menuItem);
         }
      }

      public String SaveFileDialog(string prompt, FileFilterExt filter = null, string currentFilename = null)
      {
         List<FileFilterExt> filters = new List<FileFilterExt>();
         if(filter!=null)
            filters.Add(filter);
         return SaveFileDialog(prompt, filters, currentFilename);
      }

      public String SaveFileDialog(string title, List<FileFilterExt> filters = null, string currentFilename = null)
      {
         string result = null;

         FileChooserDialogLocalized dlg = new FileChooserDialogLocalized(title, this, FileChooserAction.Save,
                                              "Save".L(),   ResponseType.Accept,
                                              "Cancel".L(), ResponseType.Cancel);
         if (currentFilename != null)
         {
            dlg.SetCurrentFolder(System.IO.Path.GetDirectoryName(currentFilename));
            dlg.SetFilename(currentFilename);
         }

         if(RunFileChooserDialogLocalized(dlg, filters) == (int) ResponseType.Accept)
         {
            result = dlg.Filename;

            FileFilter selectedFilter = dlg.Filter;
            if(selectedFilter != null)
            {
               foreach(FileFilterExt f in filters)
               {
                  if(f==selectedFilter)
                  {
                     bool correct_extension_found = false;
                     string firstext = null;
                     foreach(string pattern in f.GetPattern())
                     {
                        string ext = pattern.TrimStart('*');
                        if(firstext==null)
                           firstext = ext;
                        if(result.EndsWith(ext, true, null))
                        {
                           correct_extension_found = true;
                           break;
                        }
                     }
                     if(!correct_extension_found && firstext!=null)
                        result += firstext;
                     break;
                  }
               }
            }

            if(File.Exists(result) &&
               MessageBox.Show(MessageType.Question, ButtonsType.YesNo, "File '{0}' already exists.\nDo you want to overwrite it?", result)!=ResponseType.Yes)
               result = null;
         }

         if(result!=null)
            AddRecentFile(result);
         dlg.Destroy();
         return result;
   }

      private int RunFileChooserDialogLocalized(FileChooserDialogLocalized dlg, List<FileFilterExt> filters)
      {
         dlg.ShowHidden = true;

         if(filters!=null && filters.Count>0)
         {
            if(filters.Count>1)
            {
               FileFilterExt combinedfilter = new FileFilterExt();
               foreach(FileFilterExt filter in filters)
                  foreach(string pattern in filter.GetAdjustedPattern())
                     combinedfilter.AddPattern(pattern);
               combinedfilter.Name = "All Known File Types".L();
               dlg.AddFilter(combinedfilter);
            }

            foreach(FileFilterExt filter in filters)
               dlg.AddFilter(filter);

            dlg.AddFilter(new FileFilterExt("*", "All Files".L()) { Name = "All Files".L() });
         }

         #if false
         // we sadly cannot do this here, because we need the getters inside dlg (like .Filename), and they will no longer have proper contents after .Destroy()
         int result = dlg.Run();
         dlg.Destroy();
         return result;
         #endif

         return dlg.Run();
      }

      const string URL_PREFIX_FILE = "file://";
      const string URL_PREFIX_HTTP = "http://";
      const string URL_PREFIX_HTTPS = "https://";

      public bool OpenURL(string url_)
      {
         string url = System.Uri.UnescapeDataString(url_);
         if(url.StartsWith(URL_PREFIX_FILE))
         {
            string filename = url.Substring(URL_PREFIX_FILE.Length);
            if(Platform.IsWindows)
            {
               // treat how local filenames are encoded on Windows. Example: file:///D:/some/folder/myfile.txt
               if(filename.Length >= 3 &&
                        filename[0] == '/' &&
                  //filename[1]=='C' && // drive letter
                        filename[2] == ':')
               {
                  filename = filename.Substring(1);
               }
               filename = filename.Replace('/', System.IO.Path.DirectorySeparatorChar);
            }
            return OpenFile(filename);
         }
         else
            if(url.StartsWith(URL_PREFIX_HTTP) || url.StartsWith(URL_PREFIX_HTTPS))
            {
               string filename;
               if(url.StartsWith(URL_PREFIX_HTTP))
                  filename = url.Substring(URL_PREFIX_HTTP.Length);
               else
                  if(url.StartsWith(URL_PREFIX_HTTPS))
                     filename = url.Substring(URL_PREFIX_HTTPS.Length);
                  else
                     return false;
               string[] portions = filename.Split('/');
               if(portions.Length < 1)
                  return false;
               filename = portions[portions.Length - 1];
               if(!filename.Contains("."))
                  filename = System.IO.Path.GetFileNameWithoutExtension(System.AppDomain.CurrentDomain.FriendlyName) + " TempFile.tmp";
               filename = System.IO.Path.Combine(System.IO.Path.GetTempPath(), filename);
               if(File.Exists(filename))
               {
                  int i = 2;
                  string newfilename = filename;
                  while(File.Exists(newfilename))
                  {
                     newfilename = System.IO.Path.GetFileNameWithoutExtension(filename) + " (" + i + ")" + System.IO.Path.GetExtension(filename);
                     newfilename = System.IO.Path.Combine(System.IO.Path.GetTempPath(), newfilename);
                     i++;
                  }
                  filename = newfilename;
               }
               WebClient2 www = new WebClient2();
               FileStream file = null;
               try
               {
                  file = File.Create(filename, 10000, FileOptions.DeleteOnClose);
                  www.OpenRead(url).CopyTo(file);
               }
               catch
               {
                  file = null;
               }
               if(file != null)
               {
                  bool result = OpenFile(filename);
                  file.Close(); // will implicitly delete the file, see FileOptions.DeleteOnClose above
                  file = null;
                  return result;
               }
            }
         return false;
      }

      #endregion

      #region drag+drop

      // our own enum to which we map the various MIME types we receive via drag+drop
      enum DragDropDataType
      {
         Unknown,
         Text,
         URL,
         URLList
      }

      private static TargetEntry[] sMapMIMEtoEnum = new TargetEntry[] {
            new TargetEntry("text/plain", 0, (uint)DragDropDataType.Text),    // does not work yet, we don't get drop events for this type currently >:(
            new TargetEntry("STRING", 0, (uint)DragDropDataType.Text),    // does not work yet, we don't get drop events for this type currently >:(
            new TargetEntry("text/x-uri", 0, (uint)DragDropDataType.URL),     // does not work yet, we don't get drop events for this type currently >:(
            new TargetEntry("text/x-moz-url", 0, (uint)DragDropDataType.URL),     // does not work yet, we don't get drop events for this type currently >:(
            new TargetEntry("application/x-bookmark", 0, (uint)DragDropDataType.URL),     // does not work yet, we don't get drop events for this type currently >:(
            new TargetEntry("application/x-mswinurl", 0, (uint)DragDropDataType.URL),     // does not work yet, we don't get drop events for this type currently >:(
            new TargetEntry("_NETSCAPE_URL", 0, (uint)DragDropDataType.URL),     // does not work yet, we don't get drop events for this type currently >:(
            new TargetEntry("text/uri-list", 0, (uint)DragDropDataType.URLList), // THE ONLY THING THAT CURRENTLY WORKS FROM THIS LIST
        };

      private void MakeWidgetReceiveDropEvents(Widget widget, DragDataReceivedHandler callback)
      {
#if false // for debugging to see which events get fired put breakpoints at the return statements
        widget.DragBegin += (sender, args) => { return; };
        widget.DragDataDelete += (sender, args) => { return; };
        widget.DragDataGet += (sender, args) => { return; };
        widget.DragDrop += (sender, args) => { return; };
        widget.DragEnd += (sender, args) => { return; };
        widget.DragFailed += (sender, args) => { return; };
        widget.DragLeave += (sender, args) => { return; };
        widget.DragMotion += (sender, args) => { return; };
#endif

         widget.DragDataReceived += callback;
         Gtk.Drag.DestSet(widget, DestDefaults.All, sMapMIMEtoEnum,
             Gdk.DragAction.Default |
                Gdk.DragAction.Copy |
            //Gdk.DragAction.Move
                Gdk.DragAction.Link
            //Gdk.DragAction.Private
            //Gdk.DragAction.Ask
         );
      }
      // parse text/uri-list byte stream as specified in RFC 2483, see http://www.rfc-editor.org/rfc/rfc2483.txt
      static List<string> ParseURLListRFC2483(byte[] input)
      {
         List<string> result = new List<string>();
         string[] lines = Encoding.UTF8.GetString(input).Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
         foreach(string line in lines)
         {
            if(line.StartsWith("#"))
               continue;
            string s = line.Trim();
            if(s == "\0")
               continue;
            result.Add(s);
         }
         return result;
      }

      void OnDragDataReceived(object sender, DragDataReceivedArgs args)
      {
         bool success = false;
         if(args != null && args.SelectionData != null)
         {
            switch((DragDropDataType) args.Info)
            {
            case DragDropDataType.Text:
            case DragDropDataType.URL:
               {
                  string url = Encoding.UTF8.GetString(args.SelectionData.Data).Trim();
                  success = OpenURL(url);
                  break;
               }
            case DragDropDataType.URLList:
               {
                  List<string> uris = ParseURLListRFC2483(args.SelectionData.Data);
                  foreach(string uri in uris)
                     success |= OpenURL(uri);
                  break;
               }
            }
         }
         if(success)
            Gtk.Drag.Finish(args.Context, success, false, args.Time);
      }

      #endregion

      #region Configuration

      // In case a component has changed its namespace or class name, this function can massage the config to map the old name to the new one.
      protected void RemapComponent(string from, string to)
      {
         XmlNode layouts = ConfigurationXmlNode.SelectSingleNode("layouts");
         if(layouts!=null)
         {
            XmlNodeList nodes = layouts.SelectNodes(@"//item[@id]"); // selects all nodes named "item" with attribute "id"
            foreach(XmlNode node in nodes)
            {
               foreach(XmlAttribute attr in node.Attributes)
               {
                  if(attr.Name=="id")
                  {
                     if(attr.Value==from)
                        attr.Value = to;
                     else if(attr.Value.StartsWith(from+"-"))
                        attr.Value = to+"-"+attr.Value.Substring(from.Length+1);
                  }

               }
            }
         }

         List<XmlNode> todo = new List<XmlNode>();
         foreach(XmlNode node in ConfigurationXmlNode.ChildNodes)
            if(node.Name==from || node.Name.StartsWith(from+"-"))
               todo.Add(node);
         foreach(XmlNode oldnode in todo)
         {
            string newname;
            if(oldnode.Name==from)
               newname = to;
            else if(oldnode.Name.StartsWith(from+"-"))
               newname = to+"-"+oldnode.Name.Substring(from.Length+1);
            else
               throw new InvalidDataException(); // we should never get here
            XmlNode newnode = ConfigurationXmlDocument.CreateNode(XmlNodeType.Element, newname, "");
            newnode.InnerXml = oldnode.InnerXml;
            ConfigurationXmlNode.InsertAfter(newnode, oldnode);
            ConfigurationXmlNode.RemoveChild(oldnode);
         }
      }

      protected virtual void PerformDownwardsCompatibilityTweaksOnConfigurationFile()
      {
      }

      protected void LoadConfigurationFile(String filename)
      {
         ConfigurationFilename = filename;
         ConfigurationXmlDocument = new XmlDocument();

         if(!File.Exists(filename))
         {
            ConfigurationXmlNode = ConfigurationXmlDocument.CreateElement(CONFIG_ROOT_ELEMENT);
            DockFrame.CreateLayout(DEFAULT_LAYOUT_NAME, true);
            return;
         }

         try { ConfigurationXmlDocument.Load(filename); } catch { ConfigurationXmlDocument = new XmlDocument(); }

         ConfigurationXmlNode = ConfigurationXmlDocument.SelectSingleNode(CONFIG_ROOT_ELEMENT);
         if(ConfigurationXmlNode==null)
            ConfigurationXmlNode = ConfigurationXmlDocument.CreateElement(CONFIG_ROOT_ELEMENT);

         PerformDownwardsCompatibilityTweaksOnConfigurationFile();
      }

      protected void LoadLayout()
      {
         // load XML node "layouts" in a memory file
         // we should leave the implementation of the Mono Develop Docking as it is
         // to make it easier to update with newest version
         XmlNode layouts = ConfigurationXmlNode.SelectSingleNode("layouts");
         if(layouts!=null)
         {
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xmlWriter = new XmlTextWriter(ms, System.Text.Encoding.UTF8);
            layouts.WriteTo(xmlWriter);
            xmlWriter.Flush();
            XmlReader xmlReader = new XmlTextReader(new MemoryStream(ms.ToArray()));
            DockFrame.LoadLayouts(xmlReader);
         }
      }

      protected void SaveConfigurationFile()
      {
         ComponentsSave();

         Localization.WriteChangedResourceFiles();

         if (!string.IsNullOrEmpty(ConfigurationFilename))
         {
            try
            {
               ConfigurationXmlDocument.Save(new FileStream(
                  ConfigurationFilename, FileMode.Create, FileAccess.ReadWrite,
                  FileShare.None // open the file exclusively for writing, i.e., prevent other instances of us from interfering
               ));
            }
            catch(Exception e)
            {
               this.MessageWriteLine("Failed to save configuration file '{0}': {1}", ConfigurationFilename, e.ToString());
            }
         }
      }

      private void SaveDockFrameLayoutsToXmlConfigurationObject()
      {
         // save first DockFrame persistence in own (memory) file
         MemoryStream ms = new MemoryStream();
         XmlTextWriter xmlWriter = new XmlTextWriter(ms, System.Text.Encoding.UTF8);
         DockFrame.SaveLayouts(xmlWriter);
         xmlWriter.Flush();

         // re-load as XmlDocument
         XmlDocument doc = new XmlDocument();
         doc.Load(new XmlTextReader(new MemoryStream(ms.ToArray())));

         // select layouts and replace in XmlConfiguration
         // note that a node from other document must imported before use for add/replace
         XmlNode layouts = doc.SelectSingleNode("layouts");
         XmlNode newLayouts = ConfigurationXmlDocument.ImportNode(layouts, true);
         XmlNode oldLayouts = ConfigurationXmlNode.SelectSingleNode("layouts");
         if(oldLayouts != null)
            ConfigurationXmlNode.ReplaceChild(newLayouts, oldLayouts);
         else
            ConfigurationXmlNode.AppendChild(newLayouts);
      }

      private bool mInitialLoadOfComponentsCompleted = false;
      protected List<object> mComponents = new List<object>();

      public void AddComponent(object o)
      {
         Debug.Assert(!mComponents.Contains(o));
         mComponents.Add(o);
         if(mInitialLoadOfComponentsCompleted)
         {
            foreach(var item in mComponents)
            {
               if(item is Component)
                  (item as Component).ComponentAdded(o);
               if(o is Component)
                  (o as Component).ComponentAdded(item);
            }
         }
      }

      public void RemoveComponent(object o)
      {
         Debug.Assert(mComponents.Contains(o));
         mComponents.Remove(o);
         Debug.Assert(!mComponents.Contains(o));
         if(mInitialLoadOfComponentsCompleted)
         {
            foreach(object item in mComponents)
               if(item is Component)
                  (item as Component).ComponentRemoved(o);
         }
      }

      protected void ComponentsLoaded()
      {
         mDisableHandleVisibleChanged = false;

         // startup time measurement
         System.Diagnostics.Stopwatch total = new System.Diagnostics.Stopwatch();
         total.Start();

         // ensure that all components which have .AutoCreate set are present
         List<ComponentFactoryInformation> autocreate = ComponentFinder.GetAutoCreateList(this);
         foreach(ComponentFactoryInformation cfi in autocreate)
         {
            bool found = false;
            foreach(DockItem item in DockFrame.GetItems())
            {
               if(item.Content!=null && item.Content.GetType()==cfi.ComponentType)
               {
                  found = true;
                  break;
               }
            }
            if(!found)
            {
               if (LicenseGroup.IsEnabled(cfi.LicenseGroup))
               {
                  DockItem item = CreateComponent(cfi, false);
                  if (item != null && cfi.HideOnCreate)
                     item.Visible = false;
               }
            }
         }

         // tell all components about load state
         // time for late initialization and/or loading persistence
         foreach(DockItem item in DockFrame.GetItems())
         {
            if(item.Content is Component)
            {
               System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
               w.Start();
               (item.Content as Component).Loaded(item);
               w.Stop();

               if(LicenseGroup.IsEnabled("nts")) // do not output this in customer versions
               {
                  if(w.ElapsedMilliseconds>300) // goal limit: 25, 300 is just to reduce current clutter
                     MessageWriteLine("Invoking IComponent.Loaded() for component {0} took {1:0.00}s", item.Id, w.Elapsed.TotalSeconds);
               }
            }

            if(item.Content is Component)
               (item.Content as Component).VisibilityChanged(item.Content, item.Visible);

            if(item.Content is IPropertyViewer)
               mPropertyInterfaces.Add(item.Content as IPropertyViewer);

            if(item.Content is IScript)
               mScriptInterfaces.Add(item.Content as IScript);

            if(item.Content is Component)
               AddComponent(item.Content as Component);
         }

         mInitialLoadOfComponentsCompleted = true;
         List<object> components = mComponents;
         mComponents = new List<object>();
         foreach(object o in components)
            AddComponent(o);

         foreach(DockItem item in DockFrame.GetItems())
            if(item.Content is Component)
               (item.Content as Component).InitComplete();

         total.Stop();
         if(LicenseGroup.IsEnabled("nts") && total.ElapsedMilliseconds>100)
            MessageWriteLine("ComponentsLoaded() total time = {0}s", total.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture));
      }

      private void ComponentsSave()
      {
         foreach(DockItem item in DockFrame.GetItems())
         {
            if(item.Content is Component)
            {
               (item.Content as Component).Save();
            }
         }
         SaveDockFrameLayoutsToXmlConfigurationObject();
      }

      private void ComponentsRemove()
      {
         foreach(DockItem item in DockFrame.GetItems())
            if(item.Content is Component)
               foreach(DockItem other in DockFrame.GetItems())
                  if(other != item)
                     (item.Content as Component).ComponentRemoved(other);
      }

      protected virtual void LoadPersistency(bool installLayoutMenu) // TODO abolish, replace by implementing IPersistable
      {
         string instance = "MainWindow";
         IPersistency persistency = this as IPersistency;

         int    x           = persistency.LoadSetting(instance, "x",           -9999999);
         int    y           = persistency.LoadSetting(instance, "y",           -9999999);
         int    w           = persistency.LoadSetting(instance, "w",           -9999999);
         int    h           = persistency.LoadSetting(instance, "h",           -9999999);
         string layout      = persistency.LoadSetting(instance, "layout",      "");
         int    windowstate = persistency.LoadSetting(instance, "windowstate", 0);

         if(x!=-9999999 && y!=-9999999 && w!=-9999999 && h!=-9999999)
         {
            this.Resize(w, h);
            this.Move(x, y);
         }
         if((windowstate & (int)Gdk.WindowState.Maximized)!=0)
            this.Maximize();

         AddLayout(DEFAULT_LAYOUT_NAME, false);
         DockFrame.CurrentLayout = !String.IsNullOrEmpty(layout) ? layout : DEFAULT_LAYOUT_NAME;

         if (installLayoutMenu)
            InstallLayoutMenu(layout);

         string dir = LoadSetting(instance, "FileChooserDialogLocalized.InitialFolderToShow", "");
         if(!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            Docking.Widgets.FileChooserDialogLocalized.InitialFolderToShow = dir;
         Docking.Widgets.FileChooserDialogLocalized.InitialW = LoadSetting(instance, "FileChooserDialogLocalized.W", 0);
         Docking.Widgets.FileChooserDialogLocalized.InitialH = LoadSetting(instance, "FileChooserDialogLocalized.H", 0);
         Docking.Widgets.FileChooserDialogLocalized.InitialX = LoadSetting(instance, "FileChooserDialogLocalized.X", 0);
         Docking.Widgets.FileChooserDialogLocalized.InitialY = LoadSetting(instance, "FileChooserDialogLocalized.Y", 0);

         MaxRecentFiles = persistency.LoadSetting(instance, "MaxRecentFiles", 9);
         List<string> recentfiles = persistency.LoadSetting(instance, "RecentFiles", new List<string>());
         recentfiles.Reverse();
         if(recentfiles.Count>0)
         {
            foreach(string filename in recentfiles)
               AddRecentFile(filename, false);
            UpdateRecentFilesMenu();
         }
      }

      protected virtual void SavePersistency() // TODO abolish, replace by implementing IPersistable
      {
         IPersistency persistency = this as IPersistency;
         string instance = "MainWindow";

         persistency.SaveSetting("", "ConfigSavedByVersion", Assembly.GetCallingAssembly().GetName().Version.ToString());

         int x, y, w, h;
         GetPosition(out x, out y);
         GetSize(out w, out h);

         persistency.SaveSetting(instance, "x",           x);
         persistency.SaveSetting(instance, "y",           y);
         persistency.SaveSetting(instance, "w",           w);
         persistency.SaveSetting(instance, "h",           h);
         persistency.SaveSetting(instance, "layout",      DockFrame.CurrentLayout);
         persistency.SaveSetting(instance, "windowstate", (int)WindowState);
         List<string> recentfiles = new List<string>();
         foreach(TaggedImageMenuItem item in mRecentFiles)
            recentfiles.Add((string)item.Tag);

         persistency.SaveSetting(instance, "FileChooserDialogLocalized.InitialFolderToShow", Docking.Widgets.FileChooserDialogLocalized.InitialFolderToShow);
         persistency.SaveSetting(instance, "FileChooserDialogLocalized.W",                   Docking.Widgets.FileChooserDialogLocalized.InitialW);
         persistency.SaveSetting(instance, "FileChooserDialogLocalized.H",                   Docking.Widgets.FileChooserDialogLocalized.InitialH);
         persistency.SaveSetting(instance, "FileChooserDialogLocalized.X",                   Docking.Widgets.FileChooserDialogLocalized.InitialX);
         persistency.SaveSetting(instance, "FileChooserDialogLocalized.Y",                   Docking.Widgets.FileChooserDialogLocalized.InitialY);

         persistency.SaveSetting(instance, "MaxRecentFiles", MaxRecentFiles);
         persistency.SaveSetting(instance, "RecentFiles", recentfiles);
      }

      public bool Quit(bool save_persistency, System.Action thingsToDoBeforeShutdown = null)
      {
         foreach(DockItem item in DockFrame.GetItems())
            if((item.Content!=null) && (item.Content is Component))
               if(!(item.Content as Component).IsCloseOK())
                  return false; // close has been canceled, for example by a dialog prompt which asks for saving an edited document

         // from here on, shutdown activity goes on, so returning false must not happen from here on!

         if(save_persistency)
         {
            SavePersistency();
            SaveConfigurationFile();
            ComponentsRemove();
         }

         foreach(DockItem item in DockFrame.GetItems())
            if((item.Content!=null) && (item.Content is Component))
               (item.Content as Component).Closed();

         PowerDown = true;

         if(thingsToDoBeforeShutdown!=null)
            thingsToDoBeforeShutdown();

         Application.Quit();
         return true;
      }

      protected void OnDeleteEvent(object sender, DeleteEventArgs a)
      {
         Quit(true);
         a.RetVal = true;
      }

      #endregion

      #region Binary Persistency

      // TODO It does not really make sense to but binary blobs into XML... this way the file is not really editable/parsable anymore. Suggestion: Prefer using IPersistency.
      /// <summary>
      /// Load an object from persistence.
      /// The optional parameter 'item' can be used to identify the proper DockItem instance.
      /// </summary>
      public object LoadObject(String elementName, Type t, DockItem item)
      {
         String pimpedElementName = elementName;
         if(item != null)
            pimpedElementName += "_" + item.Id.ToString();

         if(ConfigurationXmlNode == null || pimpedElementName == null)
            return null;

         XmlNode element = ConfigurationXmlNode.SelectSingleNode(pimpedElementName);
         if(element == null)
            return null;

         // deserialize new method
         XmlNode node = element.SelectSingleNode(t.Name + "_FMT");
         if(node != null)
         {
            MemoryStream formattedStream = new MemoryStream();
            byte[] data = FromHexString(node.InnerText);
            formattedStream.Write(data, 0, data.Length);
            formattedStream.Flush();
            formattedStream.Seek(0, SeekOrigin.Begin);

            try
            {
               System.Runtime.Serialization.IFormatter formatter = new BinaryFormatter();
               object result = (object) formatter.Deserialize(formattedStream);
               return result;
            }
            catch
            {
               return null;
            }
         }

         // deserialize old method, only necessary to read old persistence, can be removed in some weeks
         node = element.SelectSingleNode(t.Name);
         if(node == null)
            return null;

         MemoryStream ms = new MemoryStream();
         XmlTextWriter xmlWriter = new XmlTextWriter(ms, System.Text.Encoding.UTF8);

         node.WriteTo(xmlWriter);
         xmlWriter.Flush();
         XmlReader xmlReader = new XmlTextReader(new MemoryStream(ms.ToArray()));

         try
         {
            // TODO: construction of XmlSerializer(type) needs a lot of time totally unexpected, an optimization rergarding persistence could be necessary ...
            XmlSerializer serializer = new XmlSerializer(t);
            return serializer.Deserialize(xmlReader);
         }
         catch
         {
            return null;
         }
      }
      // hexdump hex dump (copied from LittleHelper, need in also in other context)
      // TODO this code is misplaced in this class, move it to somewhere else, e.g. "Tools"
      public static String ToHexString(byte[] ar)
      {
         StringBuilder result = new StringBuilder();
         for(int i = 0; i < ar.Length; i++)
            result.Append(BitConverter.ToString(ar, i, 1));
         return result.ToString();
      }
      // Byte array from hexdump string
      // TODO this code is misplaced in this class, move it to somewhere else, e.g. "Tools"
      public static Byte[] FromHexString(String s)
      {
         if(s == null || (s.Length % 2) != 0)
            return null;
         Byte[] bytes = new Byte[s.Length / 2];
         for(int i = 0; i < s.Length / 2; i++)
            bytes[i] = Convert.ToByte(s.Substring(i * 2, 2), 16);
         return bytes;
      }

      /// <summary>
      /// Save an object to persistence.
      /// The optional paranmeter should be used only loading from threads to identify correct DockItem
      /// </summary>
      public void SaveObject(String elementName, object obj, DockItem item)
      {
         String pimpedElementName = elementName;
         if(item != null)
            pimpedElementName += "_" + item.Id.ToString();

         // replace in managed persistence
         XmlNode newNode = ConfigurationXmlDocument.CreateElement(pimpedElementName);

         // add serialized data
         MemoryStream formattedStream = new MemoryStream();
         System.Runtime.Serialization.IFormatter formatter = new BinaryFormatter();
         formatter.Serialize(formattedStream, obj);
         formattedStream.Flush();
         string serializedAsHex = ToHexString(formattedStream.GetBuffer());
         XmlNode importNode = ConfigurationXmlDocument.CreateElement(obj.GetType().Name + "_FMT");
         importNode.InnerText = serializedAsHex;
         newNode.AppendChild(importNode);

         // need new base node if started without old config
         if(ConfigurationXmlNode == null)
         {
            ConfigurationXmlNode = ConfigurationXmlDocument.CreateElement(CONFIG_ROOT_ELEMENT);
            ConfigurationXmlDocument.AppendChild(ConfigurationXmlNode);
         }
         XmlNode oldNode = ConfigurationXmlNode.SelectSingleNode(pimpedElementName);
         if(oldNode != null)
            ConfigurationXmlNode.ReplaceChild(newNode, oldNode);
         else
            ConfigurationXmlNode.AppendChild(newNode);
      }

      #endregion

      #region IPersistency

      #region save

      public void SaveSetting(string instance, string key, string val)
      {
         if(ConfigurationXmlNode == null)
            return;

         List<string> portions = new List<string>();
         if(!string.IsNullOrEmpty(instance))
            portions.AddRange(instance.Split('/'));
         portions.Add(key);
         if(portions.Count<=0)
            return;

         XmlNode N = null;
         XmlNode parent = ConfigurationXmlNode;
         foreach(string p in portions)
         {
            try
            {
               N = parent.SelectSingleNode(p);
            }
            catch
            {
               N = null;
            }
            if(N == null)
            {
               N = ConfigurationXmlDocument.CreateElement(p);
               parent.AppendChild(N);
            }
            parent = N;
         }
         N.InnerText = val ?? ""; // note that this does XML-Escaping, for example > becomes &gt; , so you do not have to care what is inside 'val'; anything can be stored
      }

      public void SaveSetting(string instance, string key, List<string> val)
      {
         int count = val == null ? 0 : val.Count;
         SaveSetting(instance, key + ".Count", count);
         for(int i = 0; i < count; i++)
         {
            SaveSetting(instance, key + "." + i, val[i]);
         }
      }

      public void SaveSetting(string instance, string key, List<bool> val)
      {
         int count = val == null ? 0 : val.Count;
         SaveSetting(instance, key + ".Count", count);
         for(int i = 0; i < count; i++)
         {
            SaveSetting(instance, key + "." + i, val[i]);
         }
      }

      public void SaveSetting(string instance, string key, UInt32 val)
      {
         SaveSetting(instance, key, val.ToString(CultureInfo.InvariantCulture));
      }

      public void SaveSetting(string instance, string key, Int32 val)
      {
         SaveSetting(instance, key, val.ToString(CultureInfo.InvariantCulture));
      }

      public void SaveSetting(string instance, string key, double val)
      {
         SaveSetting(instance, key, val.ToString(CultureInfo.InvariantCulture));
      }

      public void SaveSetting(string instance, string key, bool val)
      {
         SaveSetting(instance, key, val.ToString(CultureInfo.InvariantCulture));
      }

      public void SaveSetting(string instance, string key, System.Drawing.Color val)
      {
         SaveSetting(instance, key, ColorConverter.Color_to_RGBAString(val));
      }

      public void SaveColumnWidths(string instance, Gtk.TreeView treeview)
      {
         StringBuilder widths = new StringBuilder();
         foreach(TreeViewColumn col in treeview.Columns)
         {

            int w = col.Width;
            if(widths.Length > 0)
               widths.Append(";");
            string title = (col is TreeViewColumnLocalized) ? (col as TreeViewColumnLocalized).LocalizationKey : col.Title;
            title = Regex.Replace(title, "[=;]", "");
            if(!string.IsNullOrEmpty(title))
               widths.Append(title).Append("=").Append(col.Width);
         }
         SaveSetting(instance, treeview.Name + ".ColumnWidths", widths.ToString());
      }

      #endregion

      #region load

      public string LoadSetting(string instance, string key, string defaultval)
      {
         if(ConfigurationXmlNode == null)
            return defaultval;

         List<string> portions = new List<string>();
         if(!string.IsNullOrEmpty(instance))
            portions.AddRange(instance.Split('/'));
         portions.Add(key);
         if(portions.Count <= 0)
            return defaultval;

         try
         {
            XmlNode N = null;
            XmlNode parent = ConfigurationXmlNode;
            foreach(string p in portions)
            {
               N = parent.SelectSingleNode(p);
               if(N == null)
                  return defaultval;
               parent = N;
            }
            return N.InnerText;
         }
         catch
         {
            return defaultval;
         }
      }

      public List<string> LoadSetting(string instance, string key, List<string> defaultval)
      {
         int count = LoadSetting(instance, key + ".Count", -1);
         if(count < 0)
            return defaultval;
         List<string> result = new List<string>();
         for(int i = 0; i < count; i++)
            result.Add(LoadSetting(instance, key + "." + i, ""));
         return result;
      }

      public List<bool> LoadSetting(string instance, string key, List<bool> defaultval)
      {
         int count = LoadSetting(instance, key + ".Count", -1);
         if(count < 0)
            return defaultval;
         List<bool> result = new List<bool>();
         for(int i = 0; i < count; i++)
            result.Add(LoadSetting(instance, key + "." + i, false));
         return result;
      }

      public UInt32 LoadSetting(string instance, string key, UInt32 defaultval)
      {
         string s = LoadSetting(instance, key, "");
         UInt32 result;
         return (s != "" && UInt32.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
                ? result : defaultval;
      }

      public Int32 LoadSetting(string instance, string key, Int32 defaultval)
      {
         string s = LoadSetting(instance, key, "");
         Int32 result;
         return (s != "" && Int32.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
                ? result : defaultval;
      }

      public double LoadSetting(string instance, string key, double defaultval)
      {
         string s = LoadSetting(instance, key, "");
         double result;
         return (s != "" && Double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                ? result : defaultval;
      }

      public bool LoadSetting(string instance, string key, bool defaultval)
      {
         string s = LoadSetting(instance, key, "").ToLowerInvariant();
         if(s == "true")
            return true;
         else
            if(s == "false")
               return false;
            else
               return defaultval;
      }

      public System.Drawing.Color LoadSetting(string instance, string key, System.Drawing.Color defaultval)
      {
         string s = LoadSetting(instance, key, "");
         System.Drawing.Color result;
         return (s != "" && ColorConverter.RGBAString_to_Color(s, out result))
                ? result : defaultval;
      }

      public void LoadColumnWidths(string instance, Gtk.TreeView treeview)
      {
         string columnwidths = LoadSetting(instance, treeview.Name + ".ColumnWidths", "");
         string[] all = columnwidths.Split(';');
         foreach(string s in all)
         {
            string[] one = s.Split('=');
            if(one.Length == 2)
            {
               int width;
               if(Int32.TryParse(one[1], out width))
               {
                  if(width < 5) // quickfix: make sure no columns become invisible
                     width = 5;
                  foreach(TreeViewColumn col in treeview.Columns)
                  {
                     string title = (col is TreeViewColumnLocalized) ? (col as TreeViewColumnLocalized).LocalizationKey : col.Title;
                     title = Regex.Replace(title, "[=;]", "");
                     if(!string.IsNullOrEmpty(title) && title.ToLowerInvariant()==one[0].ToLowerInvariant())
                        col.SetWidth(width);
                  }
               }
            }
         }
      }

      #endregion

      #endregion

      #region Docking

      /// <summary>
      /// Searches for requested type in all available components DLLs
      /// </summary>
      public Type[] SearchForTypes(Type search)
      {
         return ComponentFinder.SearchForTypes(search);
      }

      private void ComponentHandleActivated(object sender, EventArgs e)
      {
         TaggedLocalizedImageMenuItem menuItem = sender as TaggedLocalizedImageMenuItem;
         ComponentFactoryInformation cfi = menuItem.Tag as ComponentFactoryInformation;
         CreateComponent(cfi, true);
      }

      public DockItem CreateComponent(string typename, bool initCalls)
      {
         ComponentFactoryInformation cfi = ComponentFinder.FindComponent(typename);
         return cfi==null ? null : CreateComponent(cfi, initCalls);
      }

      public DockItem CreateComponent(ComponentFactoryInformation cfi, bool initCalls)
      {
         if (!LicenseGroup.IsEnabled(cfi.LicenseGroup))
            return null;

         String name = cfi.ComponentType.ToString();

         // Find already existing - potentially invisible - instances for activation.
         // We'll only create a new instance if no such is found.
         if(!cfi.MultiInstance)
         {
            DockItem di = DockFrame.GetItem(name);
            if(di==null)
               di = DockFrame.GetItem(name+"-1"); // this can occur when a component's setting MultiInstance is enabled/disabled during debugging
            if(di!=null)
            {
               if(!di.Visible)
                  di.Visible = true;
               return di;
            }
            
         }
         else
         {
            DockItem[] dis = DockFrame.GetItemsContainingSubstring(name); // TODO make a better, more precise match, see TODO there
            foreach(DockItem di in dis)
            {
               if(!di.Visible)
               {
                  di.Visible = true;
                  return di;
               }
            }

            // adjust the name string so it becomes a new, unique instance we'll create below
            int instance = 0;
            do
            {
               instance++;
               name = cfi.ComponentType.ToString() + "-" + instance.ToString();
            }
            while(DockFrame.GetItem(name)!=null);
         }

         // when we get here, a new instance of the desired component needs to be created

         DockItem item = CreateItem(cfi, name);
         if(item==null)
         {
            MessageWriteLine("ERROR: cannot instantiate component " + name);
            return null;
         }

         Localization.LocalizeControls(item.Content.GetType().Namespace, item.Widget);

         item.DefaultVisible = false;
         //item.DrawFrame = true;
         item.Visible = true;

         // call initialization of new created component
         if(initCalls)
         {
            if (item.Content is Component)
            {
               var component = item.Content as Component;
               component.Loaded(item);
               AddComponent(component);
               component.InitComplete();
            }

            if (item.Content is IPropertyViewer)
               mPropertyInterfaces.Add(item.Content as IPropertyViewer);

            if (item.Content is IScript)
               mScriptInterfaces.Add(item.Content as IScript);
         }
         return item;
      }

      private void HandleDockItemRemoved(DockItem item)
      {
         if(item.Content is IPropertyViewer)
            mPropertyInterfaces.Remove(item.Content as IPropertyViewer);

         // tell all other about current item changed if it the removed component
         if(CurrentDockItem == item)
         {
            // care all IPropertyViewers
            foreach(IPropertyViewer ip in mPropertyInterfaces)
               ip.SetObject(null);

            // care all IScript Widgets
            foreach(IScript isc in mScriptInterfaces)
               isc.SetScript(null, null);

            CurrentDockItem = null;
            foreach(DockItem other in DockFrame.GetItems())
            {
               if(other != item && other.Content is Component)
                  (other.Content as Component).FocusChanged(null);
            }
         }

         // tell all other about removed component
         foreach(DockItem other in DockFrame.GetItems())
         {
            if(other != item && other.Content is Component)
               (other.Content as Component).ComponentRemoved(item.Content);
         }

         // remove from message dictionary
         if(item.Content is IMessage)
            mMessage.Remove(item.Id);

         // tell component about it instance itself has been removed from dock container
         if(item.Content is Component)
            (item.Content as Component).ComponentRemoved(item.Content);

         item.Widget.Destroy();
      }

      bool mDisableHandleVisibleChanged = true; // startup lock

      void HandleVisibleChanged(object sender, EventArgs e)
      {
         if(mDisableHandleVisibleChanged)
            return;

         DockItem item = sender as DockItem;
         if(item.Content is Component)
            (item.Content as Component).VisibilityChanged(item, item.Visible);
      }

      /// <summary>
      /// Create new item. This may be triggered by
      /// - a user interaction via the "View" menu
      /// - loading a persistence
      /// - running a Python script
      /// DO NOT INVOKE THIS METHOD from outside the component manager.
      /// If you want to create a component from outside, use ComponentManager.CreateComponent() instead!
      /// </summary>
      private DockItem CreateItem(ComponentFactoryInformation cfi, String name)
      {
         Component component = cfi.CreateInstance(this);
         if(component==null)
            return null;

         DockItem item = DockFrame.AddItem(name);
         AddSelectNotifier(item, component);
         AddSelectNotifier(item, item.TitleTab);
         item.Content = component;
         if(item.Content is ILocalizableComponent)
            item.Content.Name = (item.Content as ILocalizableComponent).Name.Localized(item.Content);
         item.UpdateTitle();
         item.Icon = cfi.Icon;
         item.DefaultVisible = false;
         item.VisibleChanged += HandleVisibleChanged;

         item.Behavior = DockItemBehavior.Normal;
         /*if(cfi.CloseOnHide)*/ // No "if CloseOnHide" here anymore. "CloseOnHide" is now the implicit, implemented default UNLESS the user has explicitly set "PreventClosing" (see there).
         if(!cfi.PreventClosing)
            item.Behavior |= DockItemBehavior.CloseOnHide;

         if(item.Content is IMessage)
         {
            mMessage.Add(item.Id, item.Content as IMessage);

            // push all queued messages
            foreach(String m in mMessageQueue)
               (item.Content as IMessage).WriteLine(m);
         }

         return item;
      }

      /// <summary>
      /// Create new item, called from persistence
      /// DO NOT INVOKE THIS METHOD from outside the component manager.
      /// If you want to create a component from outside, use ComponentManager.CreateComponent() instead!
      /// </summary>
      private DockItem CreateItem(string id)
      {
         String[] m = id.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
         if(m.Length == 0)
            return null;
         String typename = m[0];

         ComponentFactoryInformation cfi = ComponentFinder.FindComponent(typename);
         if(cfi == null)
            return null;

         if (!LicenseGroup.IsEnabled(cfi.LicenseGroup))
            return null;

         return CreateItem(cfi, id);
      }

      #endregion

      #region Select current DockItem

      /// <summary>
      /// Relation between any object and its parent DockItem
      /// Need to find fast the DockItem for any user selection of an object
      /// Objects are normally widgets and similar
      /// </summary>
      Dictionary<object, DockItem> mSelectRelation = new Dictionary<object, DockItem>();

      public DockItem CurrentDockItem { get; protected set; }

      DockVisualStyle mNormalStyle, mSelectedStyle;
      List<IPropertyViewer> mPropertyInterfaces = new List<IPropertyViewer>();
      List<IScript> mScriptInterfaces = new List<IScript>();

      /// <summary>
      /// Adds events for any child widget to find out which
      /// DockItem is selected by the user.
      /// Care the current selected DockItem, color the title bar.
      /// </summary>
      private void AddSelectNotifier(DockItem item, Widget w)
      {
         if(mSelectRelation.ContainsKey(w))
            return;
         mSelectRelation.Add(w, item);
         w.CanFocus = true;
         w.Events |= Gdk.EventMask.FocusChangeMask;
         w.FocusGrabbed += (object sender, EventArgs e) =>
         {
            SelectCurrentEvent(sender);
         };

         w.Events |= Gdk.EventMask.ButtonPressMask;
         w.ButtonPressEvent += (object sender, ButtonPressEventArgs args) =>
         {
            SelectCurrentEvent(sender);
         };

         if(w is Gtk.Container)
         {
            foreach(Widget xw in ((Gtk.Container) w))
            {
               AddSelectNotifier(item, xw);
            }
         }
#if false // TODO: could be needed
            if (w is TreeView)
            {
                foreach (TreeViewColumn twc in ((TreeView)w).Columns)
                {
                    twc.Clicked += (object sender, EventArgs e) =>
                    {
                        MessageWriteLine("{0} Test TreeViewColumn.Clicked {1}", qwe++, sender);
                    };
                }
            }
#endif
      }

      void SelectCurrentEvent(object item)
      {
         DockItem select;
         if(mSelectRelation.TryGetValue(item, out select))
         {
            if(CurrentDockItem != select)
            {
               if(CurrentDockItem != null)
               {
                  CurrentDockItem.TitleTab.VisualStyle = mNormalStyle;
                  CurrentDockItem.Widget.VisualStyle = mNormalStyle;
                  CurrentDockItem.Widget.QueueDraw();
               }
               CurrentDockItem = select;
               CurrentDockItem.TitleTab.VisualStyle = mSelectedStyle;
               CurrentDockItem.Widget.VisualStyle = mSelectedStyle;
               CurrentDockItem.Widget.QueueDraw();

               mMenuCut.Sensitive = CurrentDockItem.Content is ICut;
               mMenuCopy.Sensitive = CurrentDockItem.Content is ICopy;
               mMenuPaste.Sensitive = CurrentDockItem.Content is IPaste;

               // notify all IPropertyViewers
               if(!(CurrentDockItem.Content is IPropertyViewer))
               {
                  foreach(IPropertyViewer ip in mPropertyInterfaces)
                     ip.SetObject(null);
               }
               // notify all IScript Widgets
               if(!(CurrentDockItem.Content is IScript))
               {
                  foreach(IScript isc in mScriptInterfaces)
                     isc.SetScript(null, null);
               }

               // notify all other components that the current item changed
               foreach(DockItem other in DockFrame.GetItems())
               {
                  if(other != item && other.Content is Component)
                     (other.Content as Component).FocusChanged(CurrentDockItem.Content);
               }
            }
         }
      }

      #endregion

      #region Status Bar

      uint mStatusBarUniqueId = 0;
      // helper to generate unique IDs
      /// <summary>
      /// Push message to the statusbar, return unique ID used to pop message
      /// </summary>
      public uint PushStatusbar(String txt)
      {
         uint id = mStatusBarUniqueId++;
         if(StatusBar!=null)
            StatusBar.Push(id, txt);
         return id;
      }

      /// <summary>
      /// Pop a message from the statusbar.
      /// </summary>
      public void PopStatusbar(uint id)
      {
         if(StatusBar!=null)
            StatusBar.Pop(id);
      }

      #endregion

      #region Toolbar

      public void AddToolItem(ToolItem item)
      {
         if(ToolBar==null)
            return;

         item.Show();
         ToolBar.Insert(item, -1);
      }

      public void RemoveToolItem(ToolItem item)
      {
         if(ToolBar==null)
            return;
         ToolBar.Remove(item);
      }

      #endregion

      TextWriter LogFile = null;

      protected bool SetLogFile(string filename, bool clobber)
      {
         if(filename!=null && filename.Length>=0)
         {
            try
            {
               LogFile = new StreamWriter(filename, clobber, new UTF8Encoding(false));
            }
            catch(Exception e)
            {
               string msg = String.Format("cannot open log file '{0}' for writing: {1}", filename, e);
               System.Console.Error.WriteLine(msg);
               System.Console.Error.Flush();
               MessageWriteLine(msg);
               LogFile = null;
               return false;
            }
            MessageWriteLine("=== {0} === {1} ===============================================================================",
                             DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), this.Title);
         }
         return true;
      }

      #region IMessageWriteLine

      public void MessageWriteLine(String format, params object[] args)
      {
         if(format == null)
            return;

         if(PowerDown)
            return;

         Gtk.Application.Invoke(delegate
         {
            MessageWriteLineWithoutInvoke(format, args);
         });
      }

      protected void MessageWriteLineWithoutInvoke(String format, params object[] args)
      {
         if(format == null)
            return;

         if(PowerDown)
            return;

         if(LogFile!=null)
         {
            LogFile.WriteLine(format, args);
            LogFile.Flush();
         }

         foreach(KeyValuePair<string, IMessage> kvp in mMessage)
            kvp.Value.WriteLine(format, args);

         // queue all messages for new not yet existing receiver
         // todo: may should store only some messages to avoid memory leak ?
         mMessageQueue.Add(String.Format(format, args));
      }

      List<String> mMessageQueue = new List<string>();
      Dictionary<string, IMessage> mMessage = new Dictionary<string, IMessage>();

      #endregion

      #region Python

      private ScriptEngine ScriptEngine { get; set; }

      private ScriptScope ScriptScope { get; set; }

      private void InitPythonEngine(string pythonBaseVariableName)
      {
         ScriptEngine = Python.CreateEngine();
         ScriptScope = ScriptEngine.CreateScope();

         // override import
         //ScriptScope scope = IronPython.Hosting.Python.GetBuiltinModule(ScriptEngine);
         //scope.SetVariable("__import__", new ImportDelegate(DoPythonModuleImport));

         // access to this using "ComponentManager"
         m_ScriptingInstance = new ComponentManagerScripting(this);
         ScriptScope.SetVariable(pythonBaseVariableName, m_ScriptingInstance);

         try
         {
            // add Python commands like "message(...)"
            Execute(ReadResource("cm.py").Replace("[INSTANCE]", pythonBaseVariableName));
         }
         catch(Exception e)
         {
            MessageWriteLine("Error in cm.py:\n"+e.ToString());
         }
      }

      delegate object ImportDelegate(CodeContext context, string moduleName, PythonDictionary globals, PythonDictionary locals, PythonTuple tuple);

      protected object DoPythonModuleImport(CodeContext context, string moduleName, PythonDictionary globals, PythonDictionary locals, PythonTuple tuple)
      {
         // test, may useful to import py from embedded resource
#if false
            string py = ReadResource(moduleName);
            if (py != null)
            {
                //var scope = Execute(py);
                //ScriptSource source = ScriptEngine.CreateScriptSourceFromString(py);
                //ScriptScope scope = ScriptEngine.CreateScope();
                var scope = ScriptScope;
                ScriptEngine.Execute(py, scope);
                Microsoft.Scripting.Runtime.Scope ret = Microsoft.Scripting.Hosting.Providers.HostingHelpers.GetScope(scope);
                ScriptScope.SetVariable(moduleName, ret);
                return ret;
            }
            else
            {   // fall back on the built-in method
                return IronPython.Modules.Builtin.__import__(context, moduleName);
            }
#else
         return IronPython.Modules.Builtin.__import__(context, moduleName);
#endif
      }

      public String ReadResource(String id)
      {
         Assembly asm = System.Reflection.Assembly.GetCallingAssembly();
         System.IO.Stream s = asm.GetManifestResourceStream(id);
         if(s == null)
            return null;
         System.IO.StreamReader reader = new System.IO.StreamReader(s);
         if(reader == null)
            return null;
         return reader.ReadToEnd();
      }

      public CompiledCode Compile(String code)
      {
         ScriptSource source = ScriptEngine.CreateScriptSourceFromString(code, SourceCodeKind.AutoDetect);
         return source.Compile();
      }

      public dynamic Execute(CompiledCode compiled)
      {
         return compiled.Execute(ScriptScope);
      }

      public dynamic Execute(String code)
      {
         CompiledCode compiled = Compile(code);
         try   { return compiled.Execute(ScriptScope); }
         catch { return null;                          }
      }

      public dynamic ExecuteFile(String filename)
      {
         string code = File.ReadAllText(filename, Encoding.UTF8);
         return Execute(code);
      }

      ComponentManagerScripting m_ScriptingInstance;

      /// <summary>
      /// Adapter class encapsulate access to Docking.Components.ComponentManager
      /// </summary>
      public class ComponentManagerScripting
      {
         public ComponentManagerScripting(ComponentManager cm)
         {
            ComponentManager = cm;
         }

         private ComponentManager ComponentManager { get; set; }

         /// <summary>
         /// exit application immediately
         /// </summary>
         public void Quit()
         {
            ComponentManager.Quit(true);
         }

         /// <summary>
         /// set the visibility of the main window
         /// </summary>
         public bool Visible
         {
            get { return ComponentManager.Visible;  }
            set { ComponentManager.Visible = value; }
         }

         /// <summary>
         /// Write a message to the message window (if exist)
         /// </summary>
         public void MessageWriteLine(String message)
         {
            ComponentManager.MessageWriteLine(message);
         }

         /// <summary>
         /// Opens the file.
         /// </summary>
         public bool OpenFile(string filename)
         {
            return ComponentManager.OpenFile(filename);
         }

         /// <summary>
         /// Opens the file dialog.
         /// </summary>
         public String OpenFileDialog(string prompt)
         {
            return ComponentManager.OpenFileDialog(prompt);
         }

         /// <summary>
         /// Gets a simple component instance identification string.
         /// This normally is identical to the component window title.
         /// For non-multi-instance components, this normally is a human-readable text like "Map Explorer".
         /// For multi-instance components, this at the end has a number, for example "Map Explorer 2".
         /// </summary>
         private string GetComponentIdentifier(DockItem item)
         {
            if(item==null)
               return null;
            Component comp = item.Content as Component;
            if(comp==null)
               return null;

            string name = comp is ILocalizableComponent
                        ? (comp as ILocalizableComponent).Name
                        : item.Content.Name;

            return item.InstanceIndex>1
                 ? (name + " " + item.InstanceIndex)
                 : name;
         }

         /// lists all available component types which you can instantiate using CreateComponent()
         public List<string> ListComponentTypes()
         {
            List<string> result = new List<string>();
            foreach(ComponentFactoryInformation info in ComponentManager.ComponentFinder.ComponentInfos)
               result.Add(info.ComponentType.ToString());
            return result;
         }

         /// Creates a new component instance. The given parameter must be one of the available types returned by ListAvailableComponentTypes().
         /// Returned is the unique instance identification string.
         public string CreateComponent(string s)
         {
            foreach(ComponentFactoryInformation info in ComponentManager.ComponentFinder.ComponentInfos)
            {
               if(info.ComponentType.ToString()==s)
               {
                  DockItem item = ComponentManager.CreateComponent(info, true);
                  return GetComponentIdentifier(item);
               }
            }
            return null;
         }

         /// <summary>
         /// Returns a list of all currently instantiated components (including hidden ones).
         /// </summary>
         public List<string> ListInstances()
         {
            List<string> result = new List<string>();
            foreach(DockItem item in ComponentManager.DockFrame.GetItems())
            {
               if(item.Content==null)
                  continue;
               Component component = item.Content as Component;
               if(component==null || component.GetScriptingInstance()==null)
                  continue;

               string cid = GetComponentIdentifier(item);
               if(cid!=null)
                  result.Add(cid);
            }
            return result;
         }

         /// <summary>
         /// Returns a specific component instance, identified by its brief, unique instance identifier string.
         /// </summary>
         public object GetInstance(string identifier)
         {
            foreach(DockItem item in ComponentManager.DockFrame.GetItems())
            {
               if(item.Content==null)
                  continue;
               Component component = item.Content as Component;
               if(component==null || component.GetScriptingInstance()==null)
                  continue;

               string cid = GetComponentIdentifier(item);
               if(cid != null && cid == identifier)
                  return component.GetScriptingInstance();
            }
            return null;
         }

         /// <summary>
         /// Get an array with the names of all available python scripting objects
         /// </summary>
         /// <returns></returns>
         public string[] GetInstances()
         {
            List<string> result = new List<string>();
            foreach(DockItem item in ComponentManager.DockFrame.GetItems())
            {
               if(item.Content == null)
                  continue;
               Component component = item.Content as Component;
               if(component.GetScriptingInstance() == null)
                  continue;

               string cid = GetComponentIdentifier(item);
               if(cid != null)
                  result.Add(cid);
            }
            return result.ToArray();
         }
      }

      #endregion

      // TODO this currently lacks the OS's window decoration (icon, window title text, buttons)
      public Gdk.Pixbuf Screenshot()
      {
         return Gdk.Pixbuf.FromDrawable(GdkWindow, GdkWindow.Colormap, 0, 0, 0, 0, (this as Gtk.Widget).Allocation.Width, (this as Gtk.Widget).Allocation.Height);
      }
   }
}

