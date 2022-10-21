using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Globalization;
using Docking.Tools;
using Docking.Widgets;
using Docking.Framework;
using Docking.Framework.Tools;
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

   public class ComponentManager : Gtk.Window, IMessageWriteLine, ICut, ICopy, IPaste
   {

      #region Initialization

      public bool IsMainThread
      {
         get { return GtkDispatcher.Instance.IsMainThread; }
      }

      public readonly Stopwatch Clock; // A global clock. Useful for many purposes. This way you don't need to create own clocks to just measure time intervals.

      public string[] CommandLineArguments;

      public PythonScriptEngine ScriptEngine { get; private set; }

      public string ApplicationName { get; private set; }

      public ComponentManager()
      : this(new List<string>().ToArray())
      {}

      public ComponentManager(string[] args)
      : this(args, "en-US", Assembly.GetEntryAssembly().GetName().Name, null)
      {}

      // make sure that you construct this class from the main thread!
      public ComponentManager(string[] args, string default_language, string application_name, string pythonApplicationObjectName = null)
      : base(WindowType.Toplevel)
      {
         Clock = new Stopwatch();
         Clock.Start();

         CommandLineArguments = args;

         this.Title = ApplicationName = application_name;
         Settings1  = new TGSettings();

         Settings1.TweakConfigurationFileAction = PerformDownwardsCompatibilityTweaksOnConfigurationFile;

         Localization = new Components.Localization(default_language, this);
         Localization.SearchForResources(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Languages", "*.resx"));

         AccelGroup = new AccelGroup();
         AddAccelGroup(AccelGroup);

         LicenseGroup    = new LicenseGroup();
         ComponentFinder = ComponentFinderHelper.Instance;
         ScriptEngine    = new PythonScriptEngine();

         if (!string.IsNullOrEmpty(pythonApplicationObjectName))
         {
            ScriptEngine.Initialize(pythonApplicationObjectName, this);
         }

         MakeWidgetReceiveDropEvents(Toplevel, OnDragDataReceived);

         this.WindowStateEvent += OnWindowStateChanged;

         GtkDispatcher.Instance.RegisterShutdownHandler(QuitInternal);

         MessageBox.Init(this);
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
         DockFrame.DefaultVisualStyle = style;

         mNormalStyle = DockFrame.DefaultVisualStyle;
         mSelectedStyle = DockFrame.DefaultVisualStyle.Clone();
         mSelectedStyle.PadBackgroundColor = new Gdk.Color(100, 160, 255);
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

         if (m.Length <= 0)
            return null;

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

      public Menu FindOrCreateExportSubmenu(String text)
      {
         foreach (TaggedImageMenuItem m in mExportSubmenu.Children)
         {
            if (m.LabelText == text)
               return m.Submenu as Menu;
         }

         var submenuItem = new TaggedLocalizedImageMenuItem(text);
         submenuItem.Submenu = new Menu();
         AppendExportMenuQuickHack(submenuItem);
         return submenuItem.Submenu as Menu;
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
            if(DockFrame.CurrentLayout != TGSettings.DEFAULT_LAYOUT_NAME)
            {
               ResponseType result = MessageBox.Show(this, MessageType.Question,
                                         ButtonsType.YesNo,
                                         "Are you sure to remove the current layout?".L());

               if(result == ResponseType.Yes)
               {
                  MenuItem nitem = sender as MenuItem;
                  DockFrame.DeleteLayout(DockFrame.CurrentLayout);
                  RemoveMenuItem(nitem.Parent, DockFrame.CurrentLayout);
                  DockFrame.CurrentLayout = TGSettings.DEFAULT_LAYOUT_NAME;
                  CheckMenuItem(nitem.Parent, DockFrame.CurrentLayout);
                  m_DeleteLayout.Sensitive = (DockFrame.CurrentLayout != TGSettings.DEFAULT_LAYOUT_NAME);
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
               m_DeleteLayout.Sensitive = (DockFrame.CurrentLayout != TGSettings.DEFAULT_LAYOUT_NAME);
            }
         };

         AppendMenuItem(@"Options\Layout", newLayout);
         AppendMenuItem(@"Options\Layout", m_DeleteLayout);
         AppendMenuItem(@"Options\Layout", new SeparatorMenuItem());

         foreach(String s in DockFrame.Layouts)
            AppendLayoutMenu(s, true);

         m_DeleteLayout.Sensitive = (DockFrame.CurrentLayout != TGSettings.DEFAULT_LAYOUT_NAME);
         MenuBar.ShowAll();
      }


      TaggedLocalizedCheckedMenuItem mTA_Proven;
      TaggedLocalizedCheckedMenuItem mTA_Smooth;
      TaggedLocalizedCheckedMenuItem mTA_Active;

      public void InstallTabAlgorithmMenu(string baseMenu)
      {
         mTA_Proven = new TaggedLocalizedCheckedMenuItem(DockFrame.TabAlgorithm.Proven.ToString()) { IgnoreLocalization = true };
         mTA_Smooth = new TaggedLocalizedCheckedMenuItem(DockFrame.TabAlgorithm.Smooth.ToString()) { IgnoreLocalization = true };
         mTA_Active = new TaggedLocalizedCheckedMenuItem(DockFrame.TabAlgorithm.Active.ToString()) { IgnoreLocalization = true };
         mTA_Proven.Activated += (o, e) =>
         {
            if (IsActive)
            {
               mTA_Smooth.Active = false;
               mTA_Active.Active = false;
               DockFrame.ChangeTabAlgorithm(DockFrame.TabAlgorithm.Proven);
            }
         };
         mTA_Smooth.Activated += (o, e) =>
         {
            if (IsActive)
            {
               mTA_Proven.Active = false;
               mTA_Active.Active = false;
               DockFrame.ChangeTabAlgorithm(DockFrame.TabAlgorithm.Smooth);
            }
         };
         mTA_Active.Activated += (o, e) =>
         {
            if (IsActive)
            {
               mTA_Proven.Active = false;
               mTA_Smooth.Active = false;
               DockFrame.ChangeTabAlgorithm(DockFrame.TabAlgorithm.Active);
            }
         };
         AppendMenuItem(string.Format("{0}\\Tab Algorithm", baseMenu), mTA_Proven);
         AppendMenuItem(string.Format("{0}\\Tab Algorithm", baseMenu), mTA_Smooth);
         AppendMenuItem(string.Format("{0}\\Tab Algorithm", baseMenu), mTA_Active);
         UpdateTabAlgorithmMenu();
      }

      void UpdateTabAlgorithmMenu()
      {
         if (mTA_Proven==null || mTA_Smooth==null || mTA_Active==null)
            return;

         mTA_Proven.Active = DockFrame.TabType == DockFrame.TabAlgorithm.Proven;
         mTA_Smooth.Active = DockFrame.TabType == DockFrame.TabAlgorithm.Smooth;
         mTA_Active.Active = DockFrame.TabType == DockFrame.TabAlgorithm.Active;
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
                  m_DeleteLayout.Sensitive = (DockFrame.CurrentLayout != TGSettings.DEFAULT_LAYOUT_NAME);
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
         if(DockFrame!=null && !DockFrame.HasLayout(name))
            DockFrame.CreateLayout(name, copyCurrent);
      }

      private void DeleteLayout(string name)
      {
         if(name != DockFrame.CurrentLayout)
         {
            DockFrame.DeleteLayout(name);
         }
      }

      private void InstallQuitMenu(bool minimalistic = false)
      {
         if(Settings1.IsReadonly)
         {
            {
               ImageMenuItem item = new TaggedLocalizedImageMenuItem("Quit Without Saving Config (Config is Read-Only)");
               item.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.Quit-16.png"));
               item.AddAccelerator("activate", AccelGroup, new AccelKey(Gdk.Key.F4, Gdk.ModifierType.Mod1Mask, AccelFlags.Visible));
               item.AddAccelerator("activate", AccelGroup, new AccelKey(Gdk.Key.Q, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
               item.Activated += OnQuitAndDoNotSaveConfigActionActivated;
               AppendMenuItem("File", item);
            }
         }
         else
         {
            if(!minimalistic)
            {
               ImageMenuItem item = new TaggedLocalizedImageMenuItem("Quit Without Saving Config");
               item.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.Quit-16.png"));
               item.Activated += OnQuitAndDoNotSaveConfigActionActivated;
               AppendMenuItem("File", item);
            }

            {
               ImageMenuItem item = new TaggedLocalizedImageMenuItem("Quit");
               item.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.Quit-16.png"));
               item.AddAccelerator("activate", AccelGroup, new AccelKey(Gdk.Key.F4, Gdk.ModifierType.Mod1Mask, AccelFlags.Visible));
               item.AddAccelerator("activate", AccelGroup, new AccelKey(Gdk.Key.Q, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
               item.Activated += OnQuitActionActivated;
               AppendMenuItem("File", item);
            }
         }
      }

      protected void OnQuitAndDoNotSaveConfigActionActivated(object sender, EventArgs args)
      {
         Quit(false);
      }

      protected void OnQuitActionActivated(object sender, EventArgs args)
      {
         Quit(true);
      }

      public  int                       MaxRecentFiles    = 9;
      private SeparatorMenuItem         mRecentFilesBegin = null;
      private List<TaggedImageMenuItem> mRecentLogFiles   = new List<TaggedImageMenuItem>();
      private List<TaggedImageMenuItem> mRecentDbFiles    = new List<TaggedImageMenuItem>();
      private List<TaggedImageMenuItem> mRecentMiscFiles  = new List<TaggedImageMenuItem>();

      private enum FileType { DLT, NDS, MISC }

      private FileType checkFileType(String filename)
      {
         FileInfo fi = new FileInfo(filename);
         String ext = fi.Extension.ToLower();
         if (".dlt" == ext) return FileType.DLT;
         if (".nds" == ext) return FileType.NDS;
         return FileType.MISC;
      }

      public void AddRecentFile(string filename, bool do_update_menu = true)
      {
         if (string.IsNullOrEmpty(filename))
            return;

         string filename_normalized = Platform.AdjustDirectorySeparators(filename);
         FileType ftype = checkFileType(filename);

         RemoveRecentFile(filename_normalized, ftype, false);
         var filename_shortened = StringTools.ShrinkPath(filename_normalized, 80);

         TaggedImageMenuItem newitem = new TaggedImageMenuItem(filename_shortened);
         newitem.Tag = filename_normalized; // the FULL filename
         //newitem.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.File-16.png"));
         newitem.Activated += OnRecentFileActivated;
         (newitem.Child as Label).UseUnderline = false;

         List<TaggedImageMenuItem> refList = findRecentList(ftype);

         refList.Insert(0, newitem);
         if (refList.Count > MaxRecentFiles)
            refList.RemoveRange(MaxRecentFiles, refList.Count - MaxRecentFiles);

         if (do_update_menu)
            UpdateRecentFilesMenu();
      }

      private List<TaggedImageMenuItem> findRecentList(FileType ftype)
      {
         List<TaggedImageMenuItem> refList = null;
         switch (ftype)
         {
            case FileType.DLT: refList = mRecentLogFiles; break;
            case FileType.NDS: refList = mRecentDbFiles; break;
            case FileType.MISC: refList = mRecentMiscFiles; break;
         }

         return refList;
      }


      private void RemoveRecentFile(string filename)
      {
         FileType ftype = checkFileType(filename);
         RemoveRecentFile(filename, ftype, true);
      }


      private void RemoveRecentFile(string filename, FileType ftype,  bool do_update_menu = true)
      {
         List<TaggedImageMenuItem> founditems = new List<TaggedImageMenuItem>();
         List<TaggedImageMenuItem> refList = findRecentList(ftype);

         StringComparison mode = Platform.IsWindows
                               ? StringComparison.InvariantCultureIgnoreCase
                               : StringComparison.InvariantCulture;
         foreach(TaggedImageMenuItem item in refList)
            if(((string)item.Tag).Equals(filename, mode))
               founditems.Add(item);

         foreach(TaggedImageMenuItem item in founditems)
            refList.Remove(item);

         if(do_update_menu)
            UpdateRecentFilesMenu();
      }

     private void UpdateRecentFilesMenu()
     {
         Menu filemenu = FindMenu("File", true);

         // step 1/2: completely remove all old recent file menu entries

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

         // step 2/2: reconstruct recent files menu

         if(mRecentLogFiles.Count>0)
         {
            SeparatorMenuItem sep1 = new SeparatorMenuItem();
            filemenu.Append(sep1);
            sep1.ShowAll();
            if(mRecentFilesBegin==null)
               mRecentFilesBegin = sep1;
            foreach(TaggedImageMenuItem r in mRecentLogFiles)
            {
               filemenu.Append(r);
               r.ShowAll();
            }
         }

         if(mRecentDbFiles.Count>0)
         {
            SeparatorMenuItem sep2 = new SeparatorMenuItem();
            filemenu.Append(sep2);
            sep2.ShowAll();
            if(mRecentFilesBegin==null)
               mRecentFilesBegin = sep2;
            foreach (TaggedImageMenuItem r in mRecentDbFiles)
            {
               filemenu.Append(r);
               r.ShowAll();
            }
         }

         if(mRecentMiscFiles.Count>0)
         {
            SeparatorMenuItem sep3 = new SeparatorMenuItem();
            filemenu.Append(sep3);
            sep3.ShowAll();
            if(mRecentFilesBegin==null)
               mRecentFilesBegin = sep3;
            foreach (TaggedImageMenuItem r in mRecentMiscFiles)
            {
               filemenu.Append(r);
               r.ShowAll();
            }
         }
      }

      protected void OnRecentFileActivated(object sender, EventArgs args)
      {
         var item = sender as TaggedImageMenuItem;
         if(item==null)
         {
            return;
         }

         string filename = (string) item.Tag;
         if(!File.Exists(filename))
         {
            if(MessageBox.Show(MessageType.Question, ButtonsType.YesNo,
                               "File '{0}' does not exist. Do you want to remove it from the recent files list?".L(), filename )==ResponseType.Yes)
            {
               RemoveRecentFile(filename);
            }
            return;
         }

         if(!OpenFile(filename))
         {
            if(MessageBox.Show(MessageType.Question, ButtonsType.YesNo,
                               "Opening file '{0}' failed. Do you want to remove the file from the recent files list?".L(), filename )==ResponseType.Yes)
            {
               RemoveRecentFile(filename);
            }
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

      protected void SearchLoadAndInitializeComponentsFromDLLs(bool minimalistic = false)
      {
         this.ComponentFinder.SearchForComponents();
         AddComponentMenus(minimalistic);
      }

      /// <summary>
      /// Add all component start/create menu entries
      /// </summary>
      private void AddComponentMenus(bool minimalistic = false)
      {
         InstallFileOpenMenu();

         if(!minimalistic)
         {
            InstallFileSaveConfigMenu();
            InstallExportMenu();
         }
         InstallQuitMenu(minimalistic);
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
            var item = new TaggedLocalizedImageMenuItem(m[m.Length - 1]);
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

            var item = new TaggedLocalizedCheckedMenuItem(name) { IgnoreLocalization = true };
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

         var nitem = sender as TaggedLocalizedCheckedMenuItem;
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
            if(DockFrame!=null)
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

      internal TGSettings Settings1 { get; set; }

      public IPersistency Persistence {get{return Settings1 as IPersistency;}}

      #endregion

      #region public properties

      public DockFrame       DockFrame                { get; private set; }
      public ComponentFinder ComponentFinder          { get; private set; }
      public LicenseGroup    LicenseGroup             { get; private set; }

      public Localization    Localization             { get; private set; }
      public bool            OperateInBatchMode       { get; set;         }
      public bool PowerDown
      {
         get { return GtkDispatcher.Instance.IsShutdown; }
      }

      // default font for all languages except Arabic and Hebrew
      // @see DEFAULT_FONT_ARAB
      // @see DEFAULT_FONT_HEBR
      public static string DEFAULT_FONT
      {
         get
         {
            string osid = Docking.Tools.Platform.OSIDString.ToLowerInvariant();
            if(Docking.Tools.Platform.IsWindows)
            {
               // "Microsoft YaHei" is a sans-serif CJK font, installed by default on Windows 7 or higher.
               // It also is the default font for the user interface on Chinese Win7.
               // The font file contains all 20,902 original CJK Unified Ideographs code points specified in Unicode,
               // plus approximately 80 code points defined by the Standardization Administration of China.
               // It supports GBK character set, with localized glyphs.
               // https://en.wikipedia.org/wiki/Microsoft_YaHei
               // https://www.microsoft.com/typography/Fonts/family.aspx?FID=350
               // Therefore it is the best candidate to use here.
               // Fortunately, this font also properly supports European characters like ���� etc., so we can use the same for CHN and non-CHN :)
               // (We may not rely on "Office XYZ installed" here etc. -
               // If the Win7 standard fonts are not enough, TG will have to deliver its own, which we currently try to avoid.)
               // https://en.wikipedia.org/wiki/List_of_typefaces_included_with_Microsoft_Windows
               // https://en.wikipedia.org/wiki/List_of_CJK_fonts
               return "Microsoft YaHei";
            }
            else if(osid.Contains("ubuntu"))
            {
               // "Noto Mono" is a font coming by default with Ubuntu 18.04 LTS
               // https://packages.ubuntu.com/bionic/fonts-noto-mono
               return "Noto Mono";
            }
            else
            {
               // Sadly, not all Linuxes come with Chinese fonts preinstalled.
               // "Noto" is a free font by Google aiming to provide ALL Unicode characters.
               // http://en.wikipedia.org/wiki/Noto_Sans
               // http://en.wikipedia.org/wiki/Noto_Sans_CJK
               // https://www.google.com/get/noto/help/cjk
               // To get it, install the package containing it, for example on ArchLinux, that is
               //    sudo pacman -S noto-fonts-cjk
               return "Noto Sans CJK SC";
            }
         }
      }

      // default font for Arabic
      // @see DEFAULT_FONT
      // @see DEFAULT_FONT_HEBR
      public static string DEFAULT_FONT_ARAB
      {
         get
         {
            string osid = Docking.Tools.Platform.OSIDString.ToLowerInvariant();
            if(Docking.Tools.Platform.IsWindows)
            {
               // https://en.wikipedia.org/wiki/List_of_typefaces_included_with_Microsoft_Windows
               return "Arial";
            }
            else
            {
               return DEFAULT_FONT;
            }
         }
      }

      // default font for Hebrew
      // @see DEFAULT_FONT
      // @see DEFAULT_FONT_ARAB
      public static string DEFAULT_FONT_HEBR
      {
         get
         {
            string osid = Docking.Tools.Platform.OSIDString.ToLowerInvariant();
            if(Docking.Tools.Platform.IsWindows)
            {
               // https://en.wikipedia.org/wiki/List_of_typefaces_included_with_Microsoft_Windows
               return "Arial";
            }
            else
            {
               return DEFAULT_FONT;
            }
         }
      }

      #endregion

      #region OpenFile

      protected IArchive Archive { get; set; }

      void InstallFileOpenMenu()
      {
         var menuItem = new TaggedLocalizedImageMenuItem("Open...");
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
         var menuItem = new TaggedLocalizedImageMenuItem("Save Config As...");
         menuItem.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.Save-16.png"));
         menuItem.AddAccelerator("activate", AccelGroup, new AccelKey(Gdk.Key.S, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
         menuItem.Activated += (sender, e) =>
         {
            string filename = SaveFileDialog("Save Config As...", new List<FileFilterExt>()
            {
               new FileFilterExt("*.xml", "Config File")
            }, Settings1.ConfigurationFilename);

            if (filename != null)
            {
               Settings1.ConfigurationFilename = filename;
               SaveConfigurationFile();
            }
         };
         AppendMenuItem("File", menuItem);
      }


      public bool OpenFile(string filename, bool synchronous = false, object archiveHandle = null)
      {
         if (archiveHandle == null && Directory.Exists(filename))
         {
            MessageBox.Show("Opening whole directories like\n'{0}'\nis not supported. Please choose individual files instead.".FormatLocalizedWithPrefix("Docking.Components", filename));
            return false;
         }

         List<Component> existing_components = new List<Component>();

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
                  if (OpenFile(s, synchronous, handle))
                     openedArchiveFiles++;
               }
               Archive.Close(handle);
            }
         }
         else
         {
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
            /*if (!isArchive)
            {
               MessageWriteLine(Localization.Format("Docking.Components.Opening file {0} as {1}..."), filename, openers[0].Value);
               bool success = openers[0].Key.OpenFile(filename);
               MessageWriteLine(success ? "File opened successfully" : "File opening failed");

            }**/

            // search already created instances of components if they can handle this file:
            foreach (DockItem item in DockFrame.GetItems())
            {
               Component c = item.Content as Component;
               if (c is IFileOpen && (c as IFileOpen).CanOpenFile(filename))
               {
                  existing_components.Add(c);
               }
            }

            // search for available components
            List<ComponentFactoryInformation> available_components = new List<ComponentFactoryInformation>();
            foreach (ComponentFactoryInformation cfi in ComponentFinder.ComponentInfos)
            {
               if (typeof(IFileOpen).IsAssignableFrom(cfi.ComponentType) && cfi.SupportsFile(filename) && LicenseGroup.IsEnabled(cfi.LicenseGroup))
               {
                  // check if this is a single instance type and an instance is already open
                  var ec = existing_components.Where(x => x.ComponentInfo.ComponentType==cfi.ComponentType && !cfi.MultiInstance);
                  if (ec.Count() == 0)
                     available_components.Add(cfi);
               }
            }

            if(available_components.Count + existing_components.Count > 1) // show a dialog and let the user choose
            {
               bool success = true;

               // create a dialog and update the internal component model
               var dlg = new ComponentSelectorDialog(this, available_components, existing_components);
               int result = dlg.Run();
               dlg.GetSelectedComponents(ref available_components, ref existing_components);
               dlg.Hide();
               //dlg.Destroy();

               // show to let the user choose how to open that file
               if(result==(int)ResponseType.Ok)
               {
                  // create a instance for each selected component
                  foreach( ComponentFactoryInformation cfi in available_components )
                  {
                     var component = CreateComponent(cfi, true ).Content;
                     if (component is IFileOpen)
                     {
                        MessageWriteLine(Localization.Format("Docking.Components.Opening file {0} in component {1}..."), filename, cfi.Name );
                        success &= (component as IFileOpen).OpenFile(filename, synchronous);
                        MessageWriteLine(success ? "File opened successfully" : "File opening failed");
                     }
                  }

                  // forward this file to all selected instances
                  foreach (Component component in existing_components)
                  {
                     MessageWriteLine(Localization.Format("Docking.Components.Opening file {0} in component {1}..."), filename, component.Name);
                     success &= (component as IFileOpen).OpenFile(filename, synchronous);
                     MessageWriteLine(success ? "File opened successfully" : "File opening failed");
                  }

                  if (success)
                  {
                     AddRecentFile(filename);
                  }
                  return success;
               }
            }
            else if(existing_components.Count==1 && available_components.Count==0) // use an existing component
            {
               MessageWriteLine(Localization.Format("Docking.Components.Opening file {0} in component {1}..."), filename, existing_components[0].Name);
               bool success = (existing_components[0] as IFileOpen).OpenFile(filename, synchronous);
               if (success)
               {
                  AddRecentFile(filename);
               }
               MessageWriteLine(success ? "File opened successfully" : "File opening failed");
               return success;

            }
            else if(existing_components.Count==0 && available_components.Count==1) // instantiate a new component
            {
               var component = CreateComponent(available_components[0], true).Content;
               if (component is IFileOpen)
               {
                  MessageWriteLine(Localization.Format("Docking.Components.Opening file {0} in component {1}..."), filename, available_components[0].Name);
                  bool success = (component as IFileOpen).OpenFile(filename, synchronous);
                  if (success)
                  {
                     AddRecentFile(filename);
                  }
                  MessageWriteLine(success ? "File opened successfully" : "File opening failed");
                  return success;
               }
            }
            else // no component available at all
            {
               MessageBox.Show("Could not find any component which can handle file '{0}'".FormatLocalizedWithPrefix("Docking.Components"), filename);
               return false;
            }
         }

         // any archive opening
         if (openedArchiveFiles > 0)
            AddRecentFile(filename);

         return openedArchiveFiles > 0;
      }

      public String OpenFolderDialog(string title, string startFolder = null)
      {
         String result = null;

         var dlg = new FileChooserDialogLocalized(title, this, FileChooserAction.SelectFolder,
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

         var dlg = new FileChooserDialogLocalized(title, this, FileChooserAction.Open,
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

         var dlg = new FileChooserDialogLocalized(title, this, FileChooserAction.Open,
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
         if(LicenseGroup.IsEnabled("neusoft"))
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

         var dlg = new FileChooserDialogLocalized(title, this, FileChooserAction.Save,
                                              "Save".L(),   ResponseType.Accept,
                                              "Cancel".L(), ResponseType.Cancel);
         if (currentFilename != null)
         {
            var dirname = System.IO.Path.GetDirectoryName(currentFilename);
            if (dirname != null && dirname.Length > 0)
               dlg.SetCurrentFolderUri(dirname);
            var filename = System.IO.Path.GetFileName(currentFilename);
            if (filename != null && filename.Length > 0)
               dlg.CurrentName = filename;
         }

         if (RunFileChooserDialogLocalized(dlg, filters) == (int) ResponseType.Accept)
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
                  filename[0]=='/' &&
                //filename[1]=='C' && // drive letter
                  filename[2]==':')
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
            if(s == "\0" || string.IsNullOrEmpty(s))
               continue;
            result.Add(s);
         }
         return result;
      }

      void OnDragDataReceived(object sender, DragDataReceivedArgs args)
      {
         bool ok = false;
         if(args != null && args.SelectionData != null)
         {
            switch((DragDropDataType) args.Info)
            {
            case DragDropDataType.Text:
            case DragDropDataType.URL:
            case DragDropDataType.URLList:
               {
                  List<string> uris = ParseURLListRFC2483(args.SelectionData.Data);
                  foreach(string uri in uris)
                     ok = OpenURL(uri);
                  break;
               }
            }
            Gtk.Drag.Finish(args.Context, true, false, args.Time);
         }
      }

      #endregion

      #region Configuration

      // In case a component has changed its namespace or class name, this function can massage the config to map the old name to the new one.
      protected void RemapComponent(string from, string to)
      {
         Settings1.RemapComponent(from, to);
      }

      protected virtual void PerformDownwardsCompatibilityTweaksOnConfigurationFile()
      {
         //nothing to do here
      }

      protected void LoadConfigurationFile(String filename = null)
      {
         Settings1.LoadConfigurationFile(filename);
      }

      protected void LoadLayout()
      {
         string instance = "MainWindow";

         if (DockFrame != null)
         {
            DockFrame.TabType = (DockFrame.TabAlgorithm)Settings1.LoadSetting(instance, "TabAlgorithm", (int)DockFrame.TabAlgorithm.Proven);
         }
         UpdateTabAlgorithmMenu();

         Settings1.LoadLayout(DockFrame);
      }

      private bool mInitialLoadOfComponentsCompleted = false;
      protected List<object> mComponents = new List<object>();

      public void AddComponent(object o)
      {
         if (!mComponents.Contains(o))
            mComponents.Add(o);

         if(mInitialLoadOfComponentsCompleted)
         {
            foreach(var item in mComponents)
            {
               if(item==o) // objects do not need to be notified of their own birth :)
                  continue;
               if(item is Component)
                  (item as Component).ComponentAdded(o);
               if(o is Component)
                  (o as Component).ComponentAdded(item);
            }
         }
      }

      public void RemoveComponent(object o)
      {
         if(mComponents.Contains(o))
            mComponents.Remove(o);

         if(mInitialLoadOfComponentsCompleted)
            foreach(object item in mComponents)
               if(item!=o && (item is Component)) // // objects do not need to be notified of their own death :)
                  (item as Component).ComponentRemoved(o);
      }

      // This function is an ugly workaround for GTK:
      // After modifying a menu, you need to call .ShowAll() on its root element to properly make all menus and submenus show up.
      // Without this call, some of them may not be visible.
      protected void ModifyingTheMenuIsFinished()
      {
         MenuBar.ShowAll();
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
         if(DockFrame!=null)
         {
             foreach(DockItem item in DockFrame.GetItems())
             {
                if (item.Content is Component)
                {
                   var component = item.Content as Component;
                   System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
                   w.Start();
                   var allComponents = CollectAllComponentsOfType<Component>(component);
                   var allIPersistable = CollectAllComponentsOfType<IPersistable>(component);

                   foreach (var c in allComponents)
                      c.DockItem = item;
                   foreach (var c in allIPersistable)
                   {
                      try
                      {
                         c.LoadFrom(Settings1);
                      }
                      catch (Exception e)
                      {
                         MessageWriteLine("{0}.LoadFrom() Exception:{1}", c.GetType(), e);
                      }
                   }

                   w.Stop();

                   #if DEBUG
                   {
                      if (w.ElapsedMilliseconds > 300) // goal limit: 25, 300 is just to reduce current clutter
                         MessageWriteLine("Invoking IComponent.Loaded() for component {0} took {1:0.00}s", item.Id, w.Elapsed.TotalSeconds);
                   }
                   #endif

                   component.VisibilityChanged(item.Content, item.Visible);
                }

                if(item.Content is IPropertyViewer)
                   mPropertyInterfaces.Add(item.Content as IPropertyViewer);

                if(item.Content is IScript)
                   mScriptInterfaces.Add(item.Content as IScript);

                if(item.Content is Component)
                   AddComponent(item.Content as Component);
             }
         }

         mInitialLoadOfComponentsCompleted = true;
         List<object> components = mComponents;
         mComponents = new List<object>();
         foreach(object o in components)
            AddComponent(o);

         if(DockFrame!=null)
         {
             foreach (DockItem item in DockFrame.GetItems())
             {
                if (item.Content is Component)
                {
                   var component = item.Content as Component;
                   var allComponents = CollectAllComponentsOfType<Component>(component);
                   foreach (var c in allComponents)
                      c.Loaded();
                }
             }
         }

         total.Stop();

         #if DEBUG
         if(total.ElapsedMilliseconds>1500)
            MessageWriteLine("ComponentsLoaded() total time = {0}s", total.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture));
         #endif
      }

      private void ComponentsSave()
      {
         if(DockFrame!=null)
         {
             foreach(DockItem item in DockFrame.GetItems())
             {
                var allIPersistable = CollectAllComponentsOfType<IPersistable>(item.Content);
                foreach (var p in allIPersistable)
                {
                   try
                   {
                      p.SaveTo(Settings1);
                   }
                   catch (Exception e)
                   {
                      MessageWriteLine("{0}.SaveTo() Exception:{1}", p.GetType(), e);
                   }
                }
             }

             Settings1.SaveLayout(DockFrame);
         }
      }

      private void ComponentsRemove()
      {
         if(DockFrame!=null)
            foreach(DockItem item in DockFrame.GetItems())
               if(item.Content is Component)
                  foreach(DockItem other in DockFrame.GetItems())
                     if(other != item)
                        (item.Content as Component).ComponentRemoved(other);
      }

      private void SaveConfigurationFile()
      {
         ComponentsSave();
         Localization.WriteChangedResourceFiles();

         Settings1.SaveConfigurationFile();
      }


      protected virtual void LoadPersistency(bool installLayoutMenu) // TODO abolish, replace by implementing IPersistable
      {
         string instance = "MainWindow";

         //IsReadonly = persistency.LoadSetting("", "readonly", false); // exceptionally read earlier in LoadConfigurationFile(), not here

         int    windowstate = Settings1.LoadSetting(instance, "windowstate", 0);
         int    x           = Settings1.LoadSetting(instance, "x",           -9999999);
         int    y           = Settings1.LoadSetting(instance, "y",           -9999999);
         int    w           = Settings1.LoadSetting(instance, "w",           -9999999);
         int    h           = Settings1.LoadSetting(instance, "h",           -9999999);
         string layout      = Settings1.LoadSetting(instance, "layout",      "");

         if(x!=-9999999 && y!=-9999999 && w!=-9999999 && h!=-9999999)
         {
            this.Resize(w, h);
            this.Move(x, y);
         }
         if((windowstate & (int)Gdk.WindowState.Maximized)!=0)
            this.Maximize();

         AddLayout(TGSettings.DEFAULT_LAYOUT_NAME, false);
         if(DockFrame!=null)
            DockFrame.CurrentLayout = !String.IsNullOrEmpty(layout) ? layout : TGSettings.DEFAULT_LAYOUT_NAME;

         if (installLayoutMenu)
            InstallLayoutMenu(layout);

         string dir = Settings1.LoadSetting(instance, "FileChooserDialogLocalized.InitialFolderToShow", "");

         if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            Docking.Widgets.FileChooserDialogLocalized.InitialFolderToShow = dir;

         MaxRecentFiles = Settings1.LoadSetting(instance, "MaxRecentFiles", 9);
         List<string> recentfiles = Settings1.LoadSetting(instance, "RecentFiles", new List<string>());
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
         string instance = "MainWindow";

         Settings1.SaveSetting("", "ConfigSavedByVersion", AssemblyInfoExt.Version.ToString());
         Settings1.SaveSetting("", "readonly", Settings1.IsReadonly);

         int x, y, w, h;
         GetPosition(out x, out y);
         GetSize(out w, out h);

         Settings1.SaveSetting(instance, "windowstate", (int)WindowState);
         Settings1.SaveSetting(instance, "x",           x);
         Settings1.SaveSetting(instance, "y",           y);
         Settings1.SaveSetting(instance, "w",           w);
         Settings1.SaveSetting(instance, "h",           h);

         if(DockFrame!=null)
         {
            Settings1.SaveSetting(instance, "layout", DockFrame.CurrentLayout);
            Settings1.SaveSetting(instance, "TabAlgorithm", (int)DockFrame.TabType);
         }

         List<string> recentfiles = new List<string>();
         if(mRecentLogFiles!=null)
            foreach (TaggedImageMenuItem item in mRecentLogFiles)
               recentfiles.Add((string)item.Tag);
         if(mRecentDbFiles!=null)
            foreach (TaggedImageMenuItem item in mRecentDbFiles)
               recentfiles.Add((string)item.Tag);
         if(mRecentMiscFiles!=null)
            foreach (TaggedImageMenuItem item in mRecentMiscFiles)
               recentfiles.Add((string)item.Tag);

         Settings1.SaveSetting(instance, "FileChooserDialogLocalized.InitialFolderToShow", Docking.Widgets.FileChooserDialogLocalized.InitialFolderToShow);

         Settings1.SaveSetting(instance, "MaxRecentFiles", MaxRecentFiles);
         Settings1.SaveSetting(instance, "RecentFiles", recentfiles);
      }

      public void Quit(bool save_persistency, System.Action beforeShutdownAction = null)
      {
         GtkDispatcher.Instance.InitiateShutdown(save_persistency, beforeShutdownAction);
      }

      private bool QuitInternal(bool save_persistency, System.Action thingsToDoBeforeShutdown = null)
      {
         if(DockFrame!=null)
            foreach(DockItem item in DockFrame.GetItems())
               if((item.Content!=null) && (item.Content is Component))
                  if(!(item.Content as Component).IsCloseOK())
                     return false; // close has been canceled, for example by a dialog prompt which asks for saving an edited document

         // from here on, shutdown activity goes on, so returning false must not happen from here on!

         if(save_persistency)
         {
            SavePersistency();
            if(Settings1.IsReadonly)
               MessageWriteLine("skipping configuration saving because it is readonly");
            else
               SaveConfigurationFile();
            ComponentsRemove();
         }

         if(DockFrame!=null)
            foreach(DockItem item in DockFrame.GetItems())
               if((item.Content!=null) && (item.Content is Component))
                  (item.Content as Component).Closed();

         GtkDispatcher.Instance.IsShutdown = true;

         if(thingsToDoBeforeShutdown!=null)
            thingsToDoBeforeShutdown();

         Gtk.Application.Quit();
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
      //[Obsolete("Method is deprecated and will be removed soon")]
      public object LoadObject(String elementName, Type t, DockItem item)
      {
         return Settings1.LoadObject(elementName, t, item);
      }

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

      public bool CurrentLicenseCoversTheCreationOfComponent(string typename)
      {
         ComponentFactoryInformation cfi = ComponentFinder.FindComponent(typename);
         return cfi!=null && LicenseGroup.IsEnabled(cfi.LicenseGroup);
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
               di.Present(true);
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
               var allComponents = CollectAllComponentsOfType<Component>(component);
               var allIPersistable = CollectAllComponentsOfType<IPersistable>(component);

               foreach (var c in allComponents)
                  c.DockItem = item;
               foreach (var c in allIPersistable)
               {
                  try
                  {
                     c.LoadFrom(Settings1);
                  }
                  catch (Exception e)
                  {
                     MessageWriteLine("{0}.LoadFrom() Exception:{1}", c.GetType(), e);
                  }
               }

               AddComponent(component);

               foreach (var c in allComponents)
               {
                  try
                  {
                     c.Loaded();
                  }
                  catch (Exception e)
                  {
                     MessageWriteLine("{0}.Loaded() Exception:{1}", c.GetType(), e);
                  }
               }
            }

            if (item.Content is IPropertyViewer)
               mPropertyInterfaces.Add(item.Content as IPropertyViewer);

            if (item.Content is IScript)
               mScriptInterfaces.Add(item.Content as IScript);
         }
         return item;
      }

      IEnumerable<T> CollectAllComponentsOfType<T>(Widget widget)
      {
         var list = new List<T>();
         CollectAllComponentsOfType<T>(widget, list);
         return list;
      }

      void CollectAllComponentsOfType<T>(object widget, List<T> list)
      {
         if (widget is T)
            list.Add((T)widget);
         if (widget is Container)
         {
            foreach (var c in (widget as Container).AllChildren)
               if (c is Container)
                  CollectAllComponentsOfType<T>(c as Container, list);
         }
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

         // item.Widget.Destroy();
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
      private DockItem CreateItem(ComponentFactoryInformation cfi, String id)
      {
         Component component = cfi.CreateInstance(this);
         if(component==null)
            return null;

         // keep component factory info of creation
         component.ComponentInfo = cfi;

         DockItem item = new DockItem(DockFrame, id, component);
         DockFrame.AddItem(item);

         AddSelectNotifier(item, item.Content);
         AddSelectNotifier(item, item.TitleTab);

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
         if(filename!=null && filename.Length > 0)
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

         string s;
         try
         {
            if (args.Count() == 0)
               s = format;
            else
               s = String.Format(format, args);
         }
         catch (System.FormatException)
         {
            s = "(invalid format string)";
         }

         if (!Visible)
            Console.WriteLine(s);

         if (LogFile != null)
         {
            LogFile.WriteLine(s);
            LogFile.Flush();
         }

         if (PowerDown)
            return;

         if (Visible)
         {
            GtkDispatcher.Instance.Invoke(() => MessageWriteLineWithoutInvoke(s));
         }
      }

      protected void MessageWriteLineWithoutInvoke(String str)
      {
         foreach(KeyValuePair<string, IMessage> kvp in mMessage)
            kvp.Value.WriteLine(str);

         // queue all messages for new not yet existing receiver
         if (mMessage.Count() == 0)
            mMessageQueue.Add(str);
      }

      List<String> mMessageQueue = new List<string>();
      Dictionary<string, IMessage> mMessage = new Dictionary<string, IMessage>();

      #endregion

      public string ReadResource(String id)
      {
         Assembly asm = Assembly.GetCallingAssembly();
         Stream s = asm.GetManifestResourceStream(id);
         if (s == null)
         {
            return null;
         }

         StreamReader reader = new StreamReader(s);
         if (reader == null)
         {
            return null;
         }

         return reader.ReadToEnd();
      }

      public Gdk.Pixbuf Screenshot()
      {
         return Gdk.Pixbuf.FromDrawable(GdkWindow, GdkWindow.Colormap, 0, 0, 0, 0, (this as Gtk.Widget).Allocation.Width, (this as Gtk.Widget).Allocation.Height);
      }
   }
}

