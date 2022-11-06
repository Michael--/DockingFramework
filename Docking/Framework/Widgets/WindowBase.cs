
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Docking.Components;
using Gtk;
using Docking.Tools;
using System.Diagnostics;
using System.IO;
using Docking.Framework.Interfaces;

namespace Docking.Widgets
{
   public class MainWindowBase : Gtk.Window, IMenuService
   {
      private enum FileType { DLT, NDS, MISC }

      private bool mRecursionFlag = false; //workaround

      private Statusbar     mStatusBar;
      private Toolbar       mToolBar;
      private MenuBar       mMenuBar;
      private Menu          mLanguageBaseMenu = null;
      private ImageMenuItem m_DeleteLayout;
      private Menu          mExportSubmenu = new Menu();

      private TaggedLocalizedCheckedMenuItem mTA_Proven;
      private TaggedLocalizedCheckedMenuItem mTA_Smooth;
      private TaggedLocalizedCheckedMenuItem mTA_Active;

      private          SeparatorMenuItem         mRecentFilesBegin = null;
      private readonly List<TaggedImageMenuItem> mRecentLogFiles   = new List<TaggedImageMenuItem>();
      private readonly List<TaggedImageMenuItem> mRecentDbFiles    = new List<TaggedImageMenuItem>();
      private readonly List<TaggedImageMenuItem> mRecentMiscFiles  = new List<TaggedImageMenuItem>();

      private string[] mCmdLineArgs;

      protected MainWindowBase()
         : this(new List<string>().ToArray())
      { }

      protected MainWindowBase(string[] args)
         : this(args, "en-US", Assembly.GetEntryAssembly().GetName().Name, null)
      { }

      // make sure that you construct this class from the main thread!
      protected MainWindowBase(string[] args, string default_language, string application_name, string pythonApplicationObjectName = null)
         : base(WindowType.Toplevel)
      {
         Title                            = application_name;
         mCmdLineArgs                     = args;
         ComponentManager                 = new ComponentManager();
         ComponentManager.MainWindowBase  = this;
         ComponentManager.LogWriter.Title = Title;
         ComponentManager.DialogProvider  = new DialogProvider(this);
         ComponentManager.Localization    = new Localization(default_language, ComponentManager.LogWriter);
         ComponentManager.Localization.SearchForResources(
            System.IO.Path.Combine(
               System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Languages", "*.resx"));

         if (!string.IsNullOrEmpty(pythonApplicationObjectName))
         {
            ComponentManager.ScriptEngine.Initialize(pythonApplicationObjectName);
         }

         AddAccelGroup(ComponentManager.AccelGroup);

         WindowStateEvent += OnWindowStateChanged;

         MessageBox.Initialize(this, Title, ComponentManager.LogWriter);
      }

      protected DockFrame DockFrame { get; set; }

      public Gdk.WindowState WindowState { get; set; }

      public ImageMenuItem MenuCut { get; private set; }
      public ImageMenuItem MenuCopy { get; private set; }
      public ImageMenuItem MenuPaste { get; private set; }

      public ComponentManager ComponentManager { get; private set; }

      public bool IsBatchModeEnabled()
      {
         return ComponentManager.CurrentLicenseCoversTheCreationOfComponent("TempoGiusto.BatchMode");
      }

      protected void SetDockFrame(DockFrame frame)
      {
         DockFrame = frame;
      }

      protected void SetMenuBar(MenuBar menuBar)
      {
         mMenuBar = menuBar;
      }

      protected void SetStatusBar(Statusbar sb)
      {
         mStatusBar = sb;
      }

      protected void SetToolBar(Toolbar tb)
      {
         mToolBar = tb;
      }

      #region Status Bar

      uint mStatusBarUniqueId = 0;
      // helper to generate unique IDs
      /// <summary>
      /// Push message to the statusbar, return unique ID used to pop message
      /// </summary>
      public uint PushStatusbar(String txt)
      {
         uint id = mStatusBarUniqueId++;
         if (mStatusBar != null)
            mStatusBar.Push(id, txt);
         return id;
      }

      /// <summary>
      /// Pop a message from the statusbar.
      /// </summary>
      public void PopStatusbar(uint id)
      {
         if (mStatusBar != null)
            mStatusBar.Pop(id);
      }

      #endregion

      #region Toolbar

      public void AddToolItem(ToolItem item)
      {
         if (mToolBar == null)
            return;

         item.Show();
         mToolBar.Insert(item, -1);
      }

      public void RemoveToolItem(ToolItem item)
      {
         if (mToolBar == null)
            return;
         mToolBar.Remove(item);
      }

      #endregion

      // This function is an ugly workaround for GTK:
      // After modifying a menu, you need to call .ShowAll() on its root element to properly make all menus and submenus show up.
      // Without this call, some of them may not be visible.
      public void OnModifyingMenuFinished()
      {
         mMenuBar.ShowAll();
      }

      public void LoadLayout()
      {
         string instance = "MainWindow";

         DockFrame.TabType = (DockFrame.TabAlgorithm)
            ComponentManager.Settings1.LoadSetting(instance, "TabAlgorithm", (int)DockFrame.TabAlgorithm.Proven);

         UpdateTabAlgorithmMenu();

         ComponentManager.Settings1.LoadLayout(DockFrame);
      }


      public void InstallOptionsMenu(Image pic, EventHandler onOptionsActivated)
      {
         ImageMenuItem item = new TaggedLocalizedImageMenuItem("Options");
         item.Image = pic;
         item.AddAccelerator("activate", ComponentManager.AccelGroup, new AccelKey(Gdk.Key.o, Gdk.ModifierType.Mod1Mask, AccelFlags.Visible));
         item.Activated += onOptionsActivated;

         AppendMenuItem("Options", item);
      }

      public void InstallAboutMenu(Image pic, EventHandler onAboutActivated)
      {
         var item       = new TaggedLocalizedImageMenuItem("About");
         item.Image = pic;
         item.AddAccelerator("activate", ComponentManager.AccelGroup, new AccelKey(Gdk.Key.F1, Gdk.ModifierType.None, AccelFlags.Visible));
         item.Activated += onAboutActivated;
         AppendMenuItem("Help", item);
      }

      public void InstallLanguageMenu(string baseMenu)
      {
         string[] languages = ComponentManager.Localization.AvailableLanguages();
         if (languages == null || languages.Length == 0)
            return;

         foreach (string s in languages)
         {
            string[] split = s.Split(new Char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length < 2)
               continue;

            string code = split[0];
            string name = split[1];

            var item = new TaggedLocalizedCheckedMenuItem(name) { IgnoreLocalization = true };
            item.Activated += OnLanguageActivated;
            item.Tag = code;
            AppendMenuItem(string.Format("{0}\\Language", baseMenu, name), item);

            if (mLanguageBaseMenu == null)
               mLanguageBaseMenu = item.Parent as Menu;

            item.Active = ComponentManager.Localization.CurrentLanguageCode == code;
         }
      }

      public void Quit()
      {
         ComponentManager.Quit(false);
      }

      protected void OnLanguageActivated(object sender, EventArgs e)
      {
         if (mRecursionFlag)
            return;

         var nitem = sender as TaggedLocalizedCheckedMenuItem;
         string code = nitem.Tag as string;
         SetLanguage(code, false, true);
      }

      public void SetLanguage(string code, bool enforceLanguageChangedNotification, bool triggerRedraw)
      {
         if (mRecursionFlag)
            return;

         bool result = ComponentManager.Localization.SetLanguage(code);
         UncheckMenuChildren(mLanguageBaseMenu, null);
         CheckMenuItem(mLanguageBaseMenu, ComponentManager.Localization.CurrentLanguageName);

         if (result || enforceLanguageChangedNotification)
            UpdateLanguage(triggerRedraw);
      }

      public void UpdateLanguage(bool triggerRedraw)
      {
         bool isvis = this.Visible;

         if (isvis && triggerRedraw)
            Hide();

         try
         {
            foreach (DockItem item in DockFrame.GetItems())
            {
               if (item.Content != null)
               {
                  Localization.LocalizeControls(item.Content.GetType().Namespace, item.Widget);

                  if (item.Content is ILocalizableComponent)
                  {
                     ILocalizableComponent il = item.Content as ILocalizableComponent;
                     il.LocalizationChanged(item);
                     item.Content.Name = il.Name.Localized(item.Content);
                  }
               }
               item.UpdateTitle();
            }

            Localization.LocalizeMenu(mMenuBar);
         }
         catch (Exception e)
         {
            if (isvis && triggerRedraw)
               Show();

            throw e;
         }

         // after localization change, the child elements may need re-layouting
         //DockFrame.ResizeChildren(); // TODO this breaks VirtualListView layout, commented it out

         if (isvis && triggerRedraw)
            Show();
      }

      #region cut, copy, paste

      private void InstallEditMenu()
      {
         MenuCut = new TaggedLocalizedImageMenuItem("Cut");
         MenuCut.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.Cut-16.png"));
         MenuCut.Activated += OnCutActivated;
         MenuCut.Sensitive = false;
         MenuCut.AddAccelerator("activate", ComponentManager.AccelGroup, new AccelKey(Gdk.Key.x, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
         MenuCut.AddAccelerator("activate", ComponentManager.AccelGroup, new AccelKey(Gdk.Key.Delete, Gdk.ModifierType.ShiftMask, AccelFlags.Visible));
         AppendMenuItem("Edit", MenuCut);

         MenuCopy = new TaggedLocalizedImageMenuItem("Copy");
         MenuCopy.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.Copy-16.png"));
         MenuCopy.Activated += OnCopyActivated;
         MenuCopy.Sensitive = false;
         MenuCopy.AddAccelerator("activate", ComponentManager.AccelGroup, new AccelKey(Gdk.Key.c, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
         MenuCopy.AddAccelerator("activate", ComponentManager.AccelGroup, new AccelKey(Gdk.Key.Insert, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
         AppendMenuItem("Edit", MenuCopy);

         MenuPaste = new TaggedLocalizedImageMenuItem("Paste");
         MenuPaste.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.Paste-16.png"));
         MenuPaste.Activated += OnPasteActivated;
         MenuPaste.Sensitive = false;
         MenuPaste.AddAccelerator("activate", ComponentManager.AccelGroup, new AccelKey(Gdk.Key.v, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
         MenuPaste.AddAccelerator("activate", ComponentManager.AccelGroup, new AccelKey(Gdk.Key.Insert, Gdk.ModifierType.ShiftMask, AccelFlags.Visible));
         AppendMenuItem("Edit", MenuPaste);
      }

      protected void OnCutActivated(object sender, EventArgs e)
      {
         (ComponentManager as ICut).Cut();
      }

      protected void OnCopyActivated(object sender, EventArgs e)
      {
         (ComponentManager as ICopy).Copy();
      }

      protected void OnPasteActivated(object sender, EventArgs e)
      {
         (ComponentManager as IPaste).Paste();
      }

      #endregion

      /// <summary>
      /// Add all component start/create menu entries
      /// </summary>
      protected void AddComponentMenus(List<ComponentFactoryInformation> componentInfos, bool minimalistic = false)
      {
         InstallFileOpenMenu();

         if (!minimalistic)
         {
            InstallFileSaveConfigMenu();
            InstallExportMenu();
         }

         InstallQuitMenu(minimalistic);
         InstallEditMenu();

         // get all menu entries first
         var pathMenus = new List<KeyValuePair<string, TaggedLocalizedImageMenuItem>>();
         foreach (ComponentFactoryInformation cfi in componentInfos)
         {
            if (cfi.MenuPath == null)
               continue;

            if (!ComponentManager.LicenseGroup.IsEnabled(cfi.LicenseGroup))
               continue;

            // the last name is the menu name, all others are menu/sub-menu names
            String[] menuName = cfi.MenuPath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

            // as a minimum submenu-name & menu-name must exist
            Debug.Assert(menuName.Length >= 2);

            // build path again
            var path = new StringBuilder();
            for (int i = 0; i < menuName.Length - 1; i++)
            {
               if (i > 0)
                  path.Append("\\");
               path.Append(menuName[i]);
            }

            // use last entry as menu name and create
            var item = new TaggedLocalizedImageMenuItem(menuName[menuName.Length - 1]);
            item.Tag = cfi;
            // TODO: make the menu image visible if you know how
            Gdk.Pixbuf pb = cfi.Icon;
            if (pb != null)
               item.Image = new Image(pb);
            item.Activated += ComponentHandleActivated;

            pathMenus.Add(new KeyValuePair<string, TaggedLocalizedImageMenuItem>(path.ToString(), item));
         }

         // after collecting sort by path and name before add to menu
         pathMenus.Sort((p1, p2) => (p1.Key + p1.Value.LabelText).CompareTo(p2.Key + p2.Value.LabelText));
         foreach (var pathMenuPair in pathMenus)
         {
            AppendMenuItem(pathMenuPair.Key, pathMenuPair.Value);
         }

         OnModifyingMenuFinished();
      }

      private void ComponentHandleActivated(object sender, EventArgs e)
      {
         TaggedLocalizedImageMenuItem menuItem = sender as TaggedLocalizedImageMenuItem;
         ComponentFactoryInformation cfi = menuItem.Tag as ComponentFactoryInformation;
         ComponentManager.CreateComponent(cfi, true);
      }

      // TODO: quickhack to add "Export" menu entries - find a better way to let components add and remove menu items later
      private void AppendExportMenuQuickHack(MenuItem item)
      {
         mExportSubmenu.Append(item);
         OnModifyingMenuFinished();
      }

      public void RemoveExportMenuQuickHack(MenuItem item)
      {
         mExportSubmenu.Remove(item);
         OnModifyingMenuFinished();
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
            if (DockFrame.CurrentLayout != TGSettings.DEFAULT_LAYOUT_NAME)
            {
               ResponseType result = MessageBox.Show(this, MessageType.Question,
                                         ButtonsType.YesNo,
                                         "Are you sure to remove the current layout?".L());

               if (result == ResponseType.Yes)
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
            ResponseType response = (ResponseType)dialog.Run();
            if (response == ResponseType.Ok)
            {
               if (dialog.LayoutName.Length > 0)
                  newLayoutName = dialog.LayoutName;
               createEmptyLayout = dialog.EmptyLayout;
            }
            dialog.Destroy();

            if (newLayoutName == null)
               return;

            if (!DockFrame.HasLayout(newLayoutName))
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

         foreach (String s in DockFrame.Layouts)
            AppendLayoutMenu(s, true);

         m_DeleteLayout.Sensitive = (DockFrame.CurrentLayout != TGSettings.DEFAULT_LAYOUT_NAME);
         OnModifyingMenuFinished();
      }

      private void InstallFileOpenMenu()
      {
         var menuItem = new TaggedLocalizedImageMenuItem("Open...");
         menuItem.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.File-16.png"));
         menuItem.AddAccelerator("activate", ComponentManager.AccelGroup, new AccelKey(Gdk.Key.O, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
         menuItem.Activated += (sender, e) =>
         {
            List<FileFilterExt> filters = new List<FileFilterExt>();

            if (ComponentManager.Archive != null && ComponentManager.Archive is IFileOpen) // if an archive handler is installed that implements IFileOpen
            {
               List<FileFilterExt> morefilters = (ComponentManager.Archive as IFileOpen).SupportedFileTypes();
               filters.AddRange(morefilters);
            }

            foreach (DockItem d in DockFrame.GetItems())
            {
               if (d.Content is IFileOpen)
               {
                  List<FileFilterExt> morefilters = (d.Content as IFileOpen).SupportedFileTypes();
#if DEBUG
                  foreach (FileFilterExt f in morefilters)
                     ComponentManager.MessageWriteLine("{0} supports opening {1}", d.Content.Name, f.Name);
#endif
                  filters.AddRange(morefilters);
               }
            }

            String filename = ComponentManager.DialogProvider.OpenFileDialog("Open file...", filters);
            if (filename != null)
               ComponentManager.OpenFile(filename);
         };
         AppendMenuItem("File", menuItem);
      }

      private void InstallFileSaveConfigMenu()
      {
         var menuItem = new TaggedLocalizedImageMenuItem("Save Config As...");
         menuItem.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.Save-16.png"));
         menuItem.AddAccelerator("activate", ComponentManager.AccelGroup, new AccelKey(Gdk.Key.S, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
         menuItem.Activated += (sender, e) =>
         {
            string filename = ComponentManager.DialogProvider.SaveFileDialog("Save Config As...", new List<FileFilterExt>()
            {
               new FileFilterExt("*.xml", "Config File")
            }, ComponentManager.Settings1.NewFilename);

            if (filename != null)
            {
               ComponentManager.Settings1.NewFilename = filename;
               ComponentManager.SaveConfigurationFile();
            }
         };
         AppendMenuItem("File", menuItem);
      }

      private void InstallExportMenu()
      {
         if (ComponentManager.LicenseGroup.IsEnabled("neusoft"))
         {
            MenuItem menuItem = new TaggedLocalizedImageMenuItem("Export");
            menuItem.Submenu = mExportSubmenu;
            AppendMenuItem("File", menuItem);
         }
      }

      protected void InstallTabAlgorithmMenu(string baseMenu)
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

      private void InstallQuitMenu(bool minimalistic = false)
      {
         if (ComponentManager.Settings1.IsReadonly)
         {
            {
               ImageMenuItem item = new TaggedLocalizedImageMenuItem("Quit Without Saving Config (Config is Read-Only)");
               item.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.Quit-16.png"));
               item.AddAccelerator("activate", ComponentManager.AccelGroup, new AccelKey(Gdk.Key.F4, Gdk.ModifierType.Mod1Mask, AccelFlags.Visible));
               item.AddAccelerator("activate", ComponentManager.AccelGroup, new AccelKey(Gdk.Key.Q, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
               item.Activated += (sender, args) => ComponentManager.Quit(false);
               AppendMenuItem("File", item);
            }
         }
         else
         {
            if (!minimalistic)
            {
               ImageMenuItem item = new TaggedLocalizedImageMenuItem("Quit Without Saving Config");
               item.Image     =  new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.Quit-16.png"));
               item.Activated += (sender, args) => ComponentManager.Quit(false) ;
               AppendMenuItem("File", item);
            }

            {
               ImageMenuItem item = new TaggedLocalizedImageMenuItem("Quit");
               item.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.Quit-16.png"));
               item.AddAccelerator("activate", ComponentManager.AccelGroup, new AccelKey(Gdk.Key.F4, Gdk.ModifierType.Mod1Mask, AccelFlags.Visible));
               item.AddAccelerator("activate", ComponentManager.AccelGroup, new AccelKey(Gdk.Key.Q, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
               item.Activated += (sender, args) => ComponentManager.Quit(true);
               AppendMenuItem("File", item);
            }
         }
      }

      private void UpdateTabAlgorithmMenu()
      {
         if (mTA_Proven == null || mTA_Smooth == null || mTA_Active == null)
            return;

         mTA_Proven.Active = DockFrame.TabType == DockFrame.TabAlgorithm.Proven;
         mTA_Smooth.Active = DockFrame.TabType == DockFrame.TabAlgorithm.Smooth;
         mTA_Active.Active = DockFrame.TabType == DockFrame.TabAlgorithm.Active;
      }

      protected void AppendMenuItem(String path, MenuItem item)
      {
         Menu menu = FindMenu(path, true);
         if (menu != null)
            menu.Append(item);
      }

      private static Menu SearchMenu(String name, MenuShell menuShell, System.Collections.IEnumerable children)
      {
         // 1st search menu & return if existing
         foreach (MenuItem mi in children)
         {
            if (mi is TaggedLocalizedMenuItem)
            {
               if ((mi as TaggedLocalizedMenuItem).LocalizationKey != null && (mi as TaggedLocalizedMenuItem).LocalizationKey == name)
                  return mi.Submenu as Menu;
            }
            else
            if (mi is TaggedLocalizedImageMenuItem)
            {
               if ((mi as TaggedLocalizedImageMenuItem).LocalizationKey != null && (mi as TaggedLocalizedImageMenuItem).LocalizationKey == name)
                  return mi.Submenu as Menu;
            }
            else
            if (mi is TaggedLocalizedCheckedMenuItem)
            {
               if ((mi as TaggedLocalizedCheckedMenuItem).LocalizationKey != null && !(mi as TaggedLocalizedCheckedMenuItem).IgnoreLocalization && (mi as TaggedLocalizedCheckedMenuItem).LocalizationKey == name)
                  return mi.Submenu as Menu;
            }

            // When we get here, localization hasn't taken place yet (LocalizationKey==null)
            // OR
            // we are dealing with non-localized menu entries.
            // In both cases, the current label text of the menu is the one to look at.

            Label label = (Label)mi.Child;
            if (label != null && label.Text == name)
               return mi.Submenu as Menu;
         }

         return null;
      }

      public Menu FindMenu(String path, bool createIfNotExist)
      {
         // the last name is the menu name, all others are menu/sub-menu names
         String[] m = path.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

         if (m.Length <= 0)
            return null;

         MenuShell menuShell = mMenuBar;
         Menu foundmenu = null;
         System.Collections.IEnumerable children = mMenuBar.Children;

         for (int i = 0; i < m.Length; i++)
         {
            foundmenu = SearchMenu(m[i], menuShell, children);
            if (foundmenu == null && createIfNotExist)
            {
               foundmenu = CreateMenu(m[i], menuShell);
            }

            if (foundmenu != null)
            {
               children  = foundmenu.Children;
               menuShell = foundmenu;
            }
         }
         return foundmenu;
      }

      private static Menu CreateMenu(String name, MenuShell menuShell)
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

      private static void RemoveMenuItem(object baseMenu, string name)
      {
         if (baseMenu is Menu)
         {
            Menu bm = baseMenu as Menu;
            foreach (object obj in bm)
            {
               if (obj is CheckMenuItem)
               {
                  CheckMenuItem mi = obj as CheckMenuItem;
                  String label = (mi.Child as Label).Text;

                  if (label == name)
                  {
                     bm.Remove(mi);
                     bm.ShowAll();
                     return;
                  }
               }
            }
         }
      }

      private void CheckMenuItem(object baseMenu, string name)
      {
         mRecursionFlag = true;
         if (baseMenu is Menu)
         {
            Menu bm = baseMenu as Menu;
            foreach (object obj in bm)
            {
               if (obj is CheckMenuItem)
               {
                  CheckMenuItem mi = obj as CheckMenuItem;
                  String label = (mi.Child as Label).Text;

                  if (label == name)
                  {
                     if (!mi.Active)
                        mi.Active = true;
                     mRecursionFlag = false;
                     return;
                  }
               }
            }
         }
         mRecursionFlag = false;
      }

      private void UncheckMenuChildren(object baseMenu, object except)
      {
         mRecursionFlag = true;
         // uncheck all other
         if (baseMenu is Menu)
         {
            foreach (object obj in ((Menu)baseMenu))
            {
               if (obj is CheckMenuItem && obj != except)
               {
                  CheckMenuItem mi = obj as CheckMenuItem;
                  if (mi.Active)
                     mi.Active = false;
               }
            }
         }
         mRecursionFlag = false;
      }

      private void AppendLayoutMenu(String name, bool init)
      {
         CheckMenuItem item = new CheckMenuItem(name);

         item.Activated += (object sender, EventArgs e) =>
         {
            if (mRecursionFlag)
               return;

            CheckMenuItem nitem = sender as CheckMenuItem;
            String label = (nitem.Child as Label).Text;

            // double check
            if (DockFrame.HasLayout(label))
            {
               if (DockFrame.CurrentLayout != label)
               {
                  // uncheck all other
                  UncheckMenuChildren(nitem.Parent, nitem);

                  // before check selected layout
                  DockFrame.CurrentLayout = label;
                  if (!nitem.Active)
                     nitem.Active = true;
                  ComponentManager.MessageWriteLine(String.Format("CurrentLayout={0}", label));
                  m_DeleteLayout.Sensitive = (DockFrame.CurrentLayout != TGSettings.DEFAULT_LAYOUT_NAME);
               }
               else
                  if (!nitem.Active)
               {
                  nitem.Active = true;
               }
            }
         };

         AppendMenuItem(@"Options\Layout", item);
         if (!init)
         {
            UncheckMenuChildren(item.Parent, item);
         }
         item.Active = (name == DockFrame.CurrentLayout);
         item.ShowAll();
      }

      private static FileType GetFileType(String filename)
      {
         FileInfo fi = new FileInfo(filename);
         String ext = fi.Extension.ToLower();
         if (".dlt" == ext)
            return FileType.DLT;
         if (".nds" == ext)
            return FileType.NDS;
         return FileType.MISC;
      }

      public void AddRecentFile(string filename, bool do_update_menu = true)
      {
         if (string.IsNullOrEmpty(filename))
            return;

         string filename_normalized = Platform.AdjustDirectorySeparators(filename);
         FileType ftype = GetFileType(filename);

         RemoveRecentFile(filename_normalized, ftype, false);
         var filename_shortened = StringTools.ShrinkPath(filename_normalized, 80);

         TaggedImageMenuItem newitem = new TaggedImageMenuItem(filename_shortened);
         newitem.Tag = filename_normalized; // the FULL filename
         //newitem.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.File-16.png"));
         newitem.Activated += OnRecentFileActivated;
         (newitem.Child as Label).UseUnderline = false;

         List<TaggedImageMenuItem> refList = FindRecentList(ftype);

         refList.Insert(0, newitem);
         if (refList.Count > ComponentManager.MaxRecentFiles)
            refList.RemoveRange(ComponentManager.MaxRecentFiles, refList.Count - ComponentManager.MaxRecentFiles);

         if (do_update_menu)
            UpdateRecentFilesMenu();
      }

      public List<string> RecentFiles()
      {
         var recentfiles = new List<string>();

         if (mRecentLogFiles != null)
            foreach (TaggedImageMenuItem item in mRecentLogFiles)
               recentfiles.Add((string)item.Tag);

         if (mRecentDbFiles != null)
            foreach (TaggedImageMenuItem item in mRecentDbFiles)
               recentfiles.Add((string)item.Tag);

         if (mRecentMiscFiles != null)
            foreach (TaggedImageMenuItem item in mRecentMiscFiles)
               recentfiles.Add((string)item.Tag);

         return recentfiles;
      }

      private List<TaggedImageMenuItem> FindRecentList(FileType ftype)
      {
         List<TaggedImageMenuItem> refList = null;
         switch (ftype)
         {
            case FileType.DLT:
               refList = mRecentLogFiles;
               break;
            case FileType.NDS:
               refList = mRecentDbFiles;
               break;
            case FileType.MISC:
               refList = mRecentMiscFiles;
               break;
         }

         return refList;
      }


      private void RemoveRecentFile(string filename)
      {
         FileType ftype = GetFileType(filename);
         RemoveRecentFile(filename, ftype, true);
      }


      private void RemoveRecentFile(string filename, FileType ftype, bool do_update_menu = true)
      {
         List<TaggedImageMenuItem> founditems = new List<TaggedImageMenuItem>();
         List<TaggedImageMenuItem> refList = FindRecentList(ftype);

         StringComparison mode = Platform.IsWindows
                               ? StringComparison.InvariantCultureIgnoreCase
                               : StringComparison.InvariantCulture;
         foreach (TaggedImageMenuItem item in refList)
            if (((string)item.Tag).Equals(filename, mode))
               founditems.Add(item);

         foreach (TaggedImageMenuItem item in founditems)
            refList.Remove(item);

         if (do_update_menu)
            UpdateRecentFilesMenu();
      }


      public void UpdateRecentFilesMenu(List<string> recentfiles)
      {
         recentfiles.Reverse();
         if (recentfiles.Count > 0)
         {
            foreach (string filename in recentfiles)
               AddRecentFile(filename, false);
            UpdateRecentFilesMenu();
         }
      }


      public Gdk.Pixbuf Screenshot()
      {
         return Gdk.Pixbuf.FromDrawable(GdkWindow,
                                        GdkWindow.Colormap,
                                        0, 0, 0, 0,
                                        Allocation.Width,
                                        Allocation.Height);
      }

      private void UpdateRecentFilesMenu()
      {
         Menu filemenu = FindMenu("File", true);

         // step 1/2: completely remove all old recent file menu entries

         List<Widget> oldstuff = new List<Widget>();
         oldstuff.AddRange(filemenu.Children);

         if (mRecentFilesBegin != null)
         {
            bool deletionmode = false;
            foreach (Widget w in oldstuff)
            {
               if (w == mRecentFilesBegin)
                  deletionmode = true;
               if (deletionmode)
                  filemenu.Remove(w);
            }
         }
         mRecentFilesBegin = null;

         // step 2/2: reconstruct recent files menu

         if (mRecentLogFiles.Count > 0)
         {
            SeparatorMenuItem sep1 = new SeparatorMenuItem();
            filemenu.Append(sep1);
            sep1.ShowAll();
            if (mRecentFilesBegin == null)
               mRecentFilesBegin = sep1;

            foreach (TaggedImageMenuItem r in mRecentLogFiles)
            {
               filemenu.Append(r);
               r.ShowAll();
            }
         }

         if (mRecentDbFiles.Count > 0)
         {
            SeparatorMenuItem sep2 = new SeparatorMenuItem();
            filemenu.Append(sep2);
            sep2.ShowAll();
            if (mRecentFilesBegin == null)
               mRecentFilesBegin = sep2;

            foreach (TaggedImageMenuItem r in mRecentDbFiles)
            {
               filemenu.Append(r);
               r.ShowAll();
            }
         }

         if (mRecentMiscFiles.Count > 0)
         {
            SeparatorMenuItem sep3 = new SeparatorMenuItem();
            filemenu.Append(sep3);
            sep3.ShowAll();

            if (mRecentFilesBegin == null)
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
         if (item == null)
         {
            return;
         }

         string filename = (string)item.Tag;
         if (!File.Exists(filename))
         {
            if (MessageBox.Show(MessageType.Question, ButtonsType.YesNo,
                               "File '{0}' does not exist. Do you want to remove it from the recent files list?".L(), filename) == ResponseType.Yes)
            {
               RemoveRecentFile(filename);
            }
            return;
         }

         if (!ComponentManager.OpenFile(filename))
         {
            if (MessageBox.Show(MessageType.Question, ButtonsType.YesNo,
                               "Opening file '{0}' failed. Do you want to remove the file from the recent files list?".L(), filename) == ResponseType.Yes)
            {
               RemoveRecentFile(filename);
            }
            return;
         }

         // no call AddToRecentFiles() is necessary here, OpenFile() already takes care of that
      }

      private void OnWindowStateChanged(object sender, WindowStateEventArgs args)
      {
         WindowState = args.Event.NewWindowState;
      }
   }
}
