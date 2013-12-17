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
using System.Runtime.Serialization.Formatters.Binary;
using System.Globalization;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using IronPython.Hosting;
using IronPython.Runtime;
using Docking.Helper;
using Docking.Tools;
using Docking.Framework;
using Gtk;

namespace Docking.Components
{
    public class ComponentManager : Gtk.Window, IPersistency, IMessageWriteLine, ICut, ICopy, IPaste
    {

        #region Initialization

        private static int mMainThreadID;

        public bool IsMainThread
        {
            get { return Thread.CurrentThread.ManagedThreadId == mMainThreadID; }
        }
        // make sure that you construct this class from the main thread!
        public ComponentManager(WindowType wt, string pythonBaseVariableName = "cm")
         : base(wt)
        {
            mMainThreadID = Thread.CurrentThread.ManagedThreadId; // make sure that you construct this class from the main thread!

            Localization = new Components.Localization(this);
            Localization.SearchForResources(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Languages", "*.resx"));
            AccelGroup = new AccelGroup();
            AddAccelGroup(AccelGroup);
            ComponentFinder = new Docking.Components.ComponentFinder();
            XmlDocument = new XmlDocument();
            PowerDown = false;
            InitPythonEngine(pythonBaseVariableName);

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
            mSelectedStyle.PadBackgroundColor = new Gdk.Color(255, 0, 0);
        }

        public void SetStatusBar(Statusbar sb)
        {
            StatusBar = sb;
        }

        public void SetToolBar(Toolbar tb)
        {
            ToolBar = tb;
        }

        public Menu FindMenu(String path)
        {
            // the last name is the menu name, all others are menu/sub-menu names
            String[] m = path.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

            // as a minimum 1 submenu name must exist where to append the new entry
            Debug.Assert(m.Length >= 1);

            MenuShell menuShell = MenuBar;
            Menu foundmenu = null;
            System.Collections.IEnumerable children = MenuBar.Children;
            for (int i = 0; i < m.Length; i++)
            {
                foundmenu = SearchOrCreateMenu(m[i], menuShell, children);
                children = foundmenu.Children;
                menuShell = foundmenu;
            }

            return foundmenu;
        }

        protected void AppendMenu(String path, MenuItem item)
        {
            Menu foundmenu = FindMenu(path);
            if (foundmenu != null)
                foundmenu.Append(item);
        }

        protected void SetMenuBar(MenuBar menuBar)
        {
            MenuBar = menuBar;
        }

        public AccelGroup AccelGroup { get; private set; }

        String m_DefaultLayoutName;
        ImageMenuItem m_DeleteLayout;

        /// <summary>
        /// Installs the layout menu, show all existing layouts
        /// and a possibility to add and remove layouts
        /// The main layout is not removeable.
        /// If the main layout name is empty or null "Default" will be used as name.
        /// </summary>
        public void InstallLayoutMenu(String defaultLayoutName)
        {
            if (defaultLayoutName == null || defaultLayoutName.Length == 0)
                defaultLayoutName = "Default"; // TODO can we localize this string? Careful, the name is persisted...
            AddLayout(defaultLayoutName, false);
            if (m_LoadedPersistence != null && m_LoadedPersistence.Layout != null)
                DockFrame.CurrentLayout = m_LoadedPersistence.Layout;
            else
                DockFrame.CurrentLayout = defaultLayoutName;
            m_DefaultLayoutName = defaultLayoutName;

            m_DeleteLayout = new TaggedLocalizedImageMenuItem("Delete Current Layout");
            m_DeleteLayout.Activated += (object sender, EventArgs e) =>
            {
                if (DockFrame.CurrentLayout != m_DefaultLayoutName)
                {
                    ResponseType result = MessageBox.Show(this, MessageType.Question,
                                              ButtonsType.YesNo,
                                              "Are you sure to remove the current layout?");

                    if (result == ResponseType.Yes)
                    {
                        MenuItem nitem = sender as MenuItem;
                        DockFrame.DeleteLayout(DockFrame.CurrentLayout);
                        RemoveMenuItem(nitem.Parent, DockFrame.CurrentLayout);
                        DockFrame.CurrentLayout = m_DefaultLayoutName;
                        CheckMenuItem(nitem.Parent, DockFrame.CurrentLayout);
                        m_DeleteLayout.Sensitive = (DockFrame.CurrentLayout != m_DefaultLayoutName);
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
                    m_DeleteLayout.Sensitive = (DockFrame.CurrentLayout != m_DefaultLayoutName);
                }
            };

            AppendMenu(@"Options\Layout", newLayout);
            AppendMenu(@"Options\Layout", m_DeleteLayout);
            AppendMenu(@"Options\Layout", new SeparatorMenuItem());

            foreach (String s in DockFrame.Layouts)
                AppendLayoutMenu(s, true);

            m_DeleteLayout.Sensitive = (DockFrame.CurrentLayout != m_DefaultLayoutName);
            MenuBar.ShowAll();
        }

        private void CheckMenuItem(object baseMenu, string name)
        {
            recursionWorkaround = true;
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

        private void UncheckMenuChildren(object baseMenu, object except)
        {
            recursionWorkaround = true;
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
            recursionWorkaround = false;
        }

        bool recursionWorkaround = false;

        private void AppendLayoutMenu(String name, bool init)
        {
            CheckMenuItem item = new CheckMenuItem(name);

            item.Activated += (object sender, EventArgs e) =>
            {
                if (recursionWorkaround)
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
                        Console.WriteLine(String.Format("CurrentLayout={0}", label));
                        m_DeleteLayout.Sensitive = (DockFrame.CurrentLayout != m_DefaultLayoutName);
                    }
                    else
                    if (!nitem.Active)
                    {
                        nitem.Active = true;
                    }
                }
            };
            AppendMenu(@"Options\Layout", item);
            if (!init)
                UncheckMenuChildren(item.Parent, item);
            item.Active = (name == DockFrame.CurrentLayout);
            item.ShowAll();
        }

        private void AddLayout(string name, bool copyCurrent)
        {
            if (!DockFrame.HasLayout(name))
                DockFrame.CreateLayout(name, copyCurrent);
        }

        private void DeleteLayout(string name)
        {
            if (name != DockFrame.CurrentLayout)
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
            AppendMenu("File", item);
        }

        protected void OnQuitActionActivated(object sender, EventArgs e)
        {
            PrepareExit();
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
            AppendMenu("Edit", mMenuCut);

            mMenuCopy = new TaggedLocalizedImageMenuItem("Copy");
            mMenuCopy.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.Copy-16.png"));
            mMenuCopy.Activated += OnCopyActivated;
            mMenuCopy.Sensitive = false;
            mMenuCopy.AddAccelerator("activate", AccelGroup, new AccelKey(Gdk.Key.c, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
            mMenuCopy.AddAccelerator("activate", AccelGroup, new AccelKey(Gdk.Key.Insert, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
            AppendMenu("Edit", mMenuCopy);

            mMenuPaste = new TaggedLocalizedImageMenuItem("Paste");
            mMenuPaste.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.Paste-16.png"));
            mMenuPaste.Activated += OnPasteActivated;
            mMenuPaste.Sensitive = false;
            mMenuPaste.AddAccelerator("activate", AccelGroup, new AccelKey(Gdk.Key.v, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
            mMenuPaste.AddAccelerator("activate", AccelGroup, new AccelKey(Gdk.Key.Insert, Gdk.ModifierType.ShiftMask, AccelFlags.Visible));
            AppendMenu("Edit", mMenuPaste);
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
            if (CurrentDockItem != null && CurrentDockItem.Content != null)
            {
                if (CurrentDockItem.Content is ICut)
                    (CurrentDockItem.Content as ICut).Cut();
                else
                    MessageWriteLine("current component does not implement interface ICut");
            }
        }

        void ICopy.Copy()
        {
            if (CurrentDockItem != null && CurrentDockItem.Content != null)
            {
                if (CurrentDockItem.Content is ICopy)
                    (CurrentDockItem.Content as ICopy).Copy();
                else
                    MessageWriteLine("current component does not implement interface ICopy");
            }
        }

        void IPaste.Paste()
        {
            if (CurrentDockItem != null && CurrentDockItem.Content != null)
            {
                if (CurrentDockItem.Content is IPaste)
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
            InstallQuitMenu();
            InstallEditMenu();

            foreach (ComponentFactoryInformation cfi in ComponentFinder.ComponentInfos)
            {
                // the last name is the menu name, all others are menu/sub-menu names
                String[] m = cfi.MenuPath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

                // as a minimum submenu-name & menu-name must exist
                Debug.Assert(m.Length >= 2);

                // build path again
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < m.Length - 1; i++)
                {
                    if (i > 0)
                        builder.Append("\\");
                    builder.Append(m[i]);
                }

                // use last entry as menu name and create
                TaggedLocalizedImageMenuItem item = new TaggedLocalizedImageMenuItem(m[m.Length - 1]);
                item.Tag = cfi;
                // TODO: make the menu image visible if you know how
                Gdk.Pixbuf pb = cfi.Icon;
                if (pb != null)
                    item.Image = new Image(pb);
                item.Activated += ComponentHandleActivated;
                AppendMenu(builder.ToString(), item);
            }
            MenuBar.ShowAll();
        }

        private Menu SearchOrCreateMenu(String name, MenuShell menuShell, System.Collections.IEnumerable children)
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

            // 2nd append new menu
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
            if (languages == null || languages.Length == 0)
                return;

            foreach (string s in languages)
            {
                string[] split = s.Split(new Char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length < 2)
                    continue;

                string code = split[0];
                string name = split[1];

                TaggedLocalizedCheckedMenuItem item = new TaggedLocalizedCheckedMenuItem(name) { IgnoreLocalization = true };
                item.Activated += OnLanguageActivated;
                item.Tag = code;
                AppendMenu(string.Format("{0}\\Language", baseMenu, name), item);

                if (mLanguageBaseMenu == null)
                    mLanguageBaseMenu = item.Parent as Menu;

                item.Active = Localization.CurrentLanguageCode == code;
            }
        }

        protected void OnLanguageActivated(object sender, EventArgs e)
        {
            if (recursionWorkaround)
                return;

            TaggedLocalizedCheckedMenuItem nitem = sender as TaggedLocalizedCheckedMenuItem;
            string code = nitem.Tag as string;
            SetLanguage(code, false, true);
        }

        protected void SetLanguage(string code, bool enforceLanguageChangedNotification, bool triggerRedraw)
        {
            if (recursionWorkaround)
                return;

            bool result = Localization.SetLanguage(code);
            UncheckMenuChildren(mLanguageBaseMenu, null);
            CheckMenuItem(mLanguageBaseMenu, Localization.CurrentLanguageName);

            if (result || enforceLanguageChangedNotification)
                UpdateLanguage(triggerRedraw);
        }

        public void UpdateLanguage(bool triggerRedraw)
        {
            // tell all components about changed language
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

            // todo: change menue and further language depending stuff
            Localization.LocalizeMenu(MenuBar);

            if (triggerRedraw)
            {
                // trigger redraw - this is a brute-force workaround, we found no other way yet to properly trigger a full-redraw of everything         
                this.Hide();
                this.Show();
            }
        }

        #endregion

        #region private properties

        private Statusbar StatusBar { get; set; }

        private Toolbar ToolBar { get; set; }

        private MenuBar MenuBar { get; set; }

        private XmlDocument XmlDocument { get; set; }

        private XmlNode XmlConfiguration { get; set; }

        #endregion

        #region public properties

        public DockFrame DockFrame { get; private set; }

        public ComponentFinder ComponentFinder { get; private set; }

        public bool PowerDown { get; set; }

        public String ConfigurationFile { get; set; }

        public Localization Localization { get; private set; }

        #endregion

        #region OpenFile

        void InstallFileOpenMenu()
        {
            ImageMenuItem item = new TaggedLocalizedImageMenuItem("Open...");
            item.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.File-16.png"));
            item.AddAccelerator("activate", AccelGroup, new AccelKey(Gdk.Key.O, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
            item.Activated += (sender, e) =>
            {
                List<FileFilter> filefilters = new List<FileFilter>();
                foreach (DockItem d in DockFrame.GetItems())
                    if (d.Content is IFileOpen)
                        filefilters.AddRange((d.Content as IFileOpen).SupportedFileTypes());

#if false // as long as we have no mechanism (see below) which offers the user to instantiate new components which can handle new file types, we should not offer him this *.* option
                FileFilter any = new FileFilter();
                any.AddPattern("*.*");
                any.Name = "*.* - Any File";
                filefilters.Add(any);
#endif

                String filename = OpenFileDialog("Choose a file to open...".Localized("Docking.Components"), filefilters);
                if (filename != null)
                    OpenFile(filename);
            };
            AppendMenu("File", item);
        }

        public bool OpenFile(string filename)
        {
            if (Directory.Exists(filename))
            {
                MessageWriteLine("Opening whole directories like {0} currently isn't implemented".FormatLocalizedWithPrefix("Docking.Components", filename));
                return false;
            }

            if (!File.Exists(filename))
            {
                MessageWriteLine("File {0} does not exist".FormatLocalizedWithPrefix("Docking.Components", filename));
                return false;
            }

            List<KeyValuePair<IFileOpen, string>> openers = new List<KeyValuePair<IFileOpen, string>>();
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

            // TODO: consider all and let the user pick which one
            MessageWriteLine(Localization.Format("Docking.Components.Opening file {0} as {1}"), filename, openers[0].Value);
            return openers[0].Key.OpenFile(filename);
        }

        public String OpenFileDialog(string prompt)
        {
            return OpenFileDialog(prompt, new List<FileFilter>());
        }

        public String OpenFileDialog(string prompt, FileFilter filefilter)
        {
            List<FileFilter> L = null;
            if (filefilter != null)
            {
                L = new List<FileFilter>();
                L.Add(filefilter);
            }
            return OpenFileDialog(prompt, L);
        }

        public String OpenFileDialog(string prompt, List<FileFilter> filefilters)
        {
            String result = null;
            FileChooserDialogLocalized dlg = new FileChooserDialogLocalized(prompt, this, FileChooserAction.Open,
                                                 "Cancel".Localized("Docking.Components"), ResponseType.Cancel,
                                                 "Open".Localized("Docking.Components"), ResponseType.Accept);

            if (filefilters != null)
                foreach (FileFilter filter in filefilters)
                    dlg.AddFilter(filter);

            if (dlg.Run() == (int)ResponseType.Accept)
            {
                result = dlg.Filename;
            }

            dlg.Destroy();
            return result;
        }

        public String SaveFileDialog(string prompt, FileFilterExt[] filefilters = null)
        {
            String result = null;
            FileChooserDialogLocalized dlg = new FileChooserDialogLocalized(prompt, this, FileChooserAction.Save,
                                                 "Cancel".Localized("Docking.Components"), ResponseType.Cancel,
                                                 "Save".Localized("Docking.Components"), ResponseType.Accept);

            if (filefilters != null)
                foreach (FileFilterExt filter in filefilters)
                    dlg.AddFilter(filter.Filter);

            if (dlg.Run() == (int)ResponseType.Accept)
            {
                result = dlg.Filename;

                FileFilter selectedFilter = dlg.Filter;
                if (selectedFilter != null)
                {
                    foreach (FileFilterExt f in filefilters)
                    {
                        if (f.Filter == selectedFilter)
                        {
                            string expectedExtension = f.Pattern.TrimStart(new char[] { '*' });

                            if (!result.EndsWith(expectedExtension, true, null))
                            {
                                result = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(result),
                                    System.IO.Path.GetFileName(result) + expectedExtension);
                            }
                            break;
                        }
                    }
                }
            }

            dlg.Destroy();
            return result;
        }

        static bool PlatformIsWin32ish
        {
            get
            {
                switch(Environment.OSVersion.Platform)
                {
                    case PlatformID.Win32S:
                        return true;
                    case PlatformID.Win32Windows:
                        return true;
                    case PlatformID.Win32NT:
                        return true;
                    case PlatformID.WinCE:
                        return false; // note this. WinCE is very different than Win32
                    case PlatformID.Unix:
                        return false;
                    case PlatformID.Xbox:
                        return false; // or should we return true here better???
                    case PlatformID.MacOSX:
                        return false;
                    default:
                        return false;
                }
            }
        }

        const string URL_PREFIX_FILE = "file://";
        const string URL_PREFIX_HTTP = "http://";
        const string URL_PREFIX_HTTPS = "https://";

        public bool OpenURL(string url_)
        {
            string url = System.Uri.UnescapeDataString(url_);
            if (url.StartsWith(URL_PREFIX_FILE))
            {
                string filename = url.Substring(URL_PREFIX_FILE.Length);
                if (PlatformIsWin32ish)
                {
                    // treat how local filenames are encoded on Windows. Example: file:///D:/some/folder/myfile.txt
                    if (filename.Length >= 3 &&
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
            if (url.StartsWith(URL_PREFIX_HTTP) || url.StartsWith(URL_PREFIX_HTTPS))
            {
                string filename;
                if (url.StartsWith(URL_PREFIX_HTTP))
                    filename = url.Substring(URL_PREFIX_HTTP.Length);
                else
                if (url.StartsWith(URL_PREFIX_HTTPS))
                    filename = url.Substring(URL_PREFIX_HTTPS.Length);
                else
                    return false;
                string[] portions = filename.Split('/');
                if (portions.Length < 1)
                    return false;
                filename = portions[portions.Length - 1];
                if (!filename.Contains("."))
                    filename = System.IO.Path.GetFileNameWithoutExtension(System.AppDomain.CurrentDomain.FriendlyName) + " TempFile.tmp";
                filename = System.IO.Path.Combine(System.IO.Path.GetTempPath(), filename);
                if (File.Exists(filename))
                {
                    int i = 2;
                    string newfilename = filename;
                    while (File.Exists(newfilename))
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
                catch(Exception)
                {
                    file = null;
                }
                if (file != null)
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
            foreach (string line in lines)
            {
                if (line.StartsWith("#"))
                    continue;
                string s = line.Trim();
                if (s == "\0")
                    continue;
                result.Add(s);
            }
            return result;
        }

        void OnDragDataReceived(object sender, DragDataReceivedArgs args)
        {
            bool success = false;
            if (args != null && args.SelectionData != null)
            {
                switch((DragDropDataType)args.Info)
                {
                    case DragDropDataType.Text:
                  // we currently have no usecase for this
                        break;
                    case DragDropDataType.URL:
                        {
                            // untested:
                            // string url = Encoding.UTF8.GetString(args.SelectionData.Data).Trim(); 
                            // success = OpenURL(url);
                            success = false;
                            break;
                        }
                    case DragDropDataType.URLList:
                        {
                            List<string> uris = ParseURLListRFC2483(args.SelectionData.Data);
                            foreach (string uri in uris)
                                success |= OpenURL(uri);
                            break;
                        }
                }
            }
            if (success)
                Gtk.Drag.Finish(args.Context, success, false, args.Time);
        }

        #endregion

        #region Configuration

        protected void LoadConfigurationFile(String filename)
        {
            ConfigurationFile = filename;

            // layout from file or new
            if (File.Exists(filename))
            {
                // the manager holds the persistence in memory all the time
                LoadConfiguration(filename);

                // load XML node "layouts" in a memory file
                // we should let the implementation of the Mono Develop Docking as it is
                // to make it easier to update with newest version

                XmlNode layouts = XmlConfiguration.SelectSingleNode("layouts");
                if (layouts != null)
                {
                    MemoryStream ms = new MemoryStream();
                    XmlTextWriter xmlWriter = new XmlTextWriter(ms, System.Text.Encoding.UTF8);

                    layouts.WriteTo(xmlWriter);
                    xmlWriter.Flush();
                    XmlReader xmlReader = new XmlTextReader(new MemoryStream(ms.ToArray()));

                    DockFrame.LoadLayouts(xmlReader);
                }
            }
            else
            {
                DockFrame.CreateLayout("Default", true);
            }
        }

        protected void SaveConfigurationFile(String filename)
        {
            // save first DockFrame persistence in own (memory) file
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xmlWriter = new XmlTextWriter(ms, System.Text.Encoding.UTF8);
            DockFrame.SaveLayouts(xmlWriter);
            xmlWriter.Flush();
            XmlReader xmlReader = new XmlTextReader(new MemoryStream(ms.ToArray()));

            // re-load as XmlDocument
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlReader);

            // select layouts and replace in managed persistence
            // note that a node from other document must imported before use for add/replace
            XmlNode layouts = doc.SelectSingleNode("layouts");
            XmlNode newLayouts = XmlDocument.ImportNode(layouts, true);
            XmlNode oldLayouts = XmlConfiguration.SelectSingleNode("layouts");
            if (oldLayouts != null)
                XmlConfiguration.ReplaceChild(newLayouts, oldLayouts);
            else
                XmlConfiguration.AppendChild(newLayouts);

            ComponentsSave();

            Localization.WriteChangedResourceFiles();

            SaveConfiguration(filename);

            ComponentsRemove();
        }

        private void LoadConfiguration(String filename)
        {
            XmlDocument.Load(filename);
            XmlConfiguration = XmlDocument.SelectSingleNode("DockingConfiguration");
        }

        private void SaveConfiguration(String filename)
        {
            XmlDocument.Save(filename);
        }
        // contains the current component while load/save persistence
        // note: because of this load/save is not thread safe, load/save have to use threads carefully
        // TODO This is an ugly quickhack - get rid of this variable!
        private DockItem currentLoadSaveItem;
        private bool mInitialLoadOfComponentsCompleted = false;
        private List<object> mComponents = new List<object>();

        public void AddComponent(object o)
        {
            Debug.Assert(!mComponents.Contains(o));
            mComponents.Add(o);
            if (mInitialLoadOfComponentsCompleted)
            {
                foreach (object item in mComponents)
                {
                    if (item is Component)
                        (item as Component).ComponentAdded(o);
                    if (o is Component)
                        (o as Component).ComponentAdded(item);
                }
            }
        }

        public void RemoveComponent(object o)
        {
            Debug.Assert(mComponents.Contains(o));
            mComponents.Remove(o);
            Debug.Assert(!mComponents.Contains(o));
            if (mInitialLoadOfComponentsCompleted)
            {
                foreach (object item in mComponents)
                    if (item is Component)
                        (item as Component).ComponentRemoved(o);
            }
        }

        protected void ComponentsLoaded()
        {
            m_LockHandleVisibleChanged = false; // unlock visible change notices

            // some startup time measurements
            System.Diagnostics.Stopwatch total = new System.Diagnostics.Stopwatch();
            total.Start();

            // tell all components about load state
            // time for late initialization and/or loading persistence
            foreach (DockItem item in DockFrame.GetItems())
            {
                if (item.Content is Component)
                {
                    System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
                    w.Start();
                    currentLoadSaveItem = item;
                    (item.Content as Component).Loaded(item);
                    currentLoadSaveItem = null;
                    w.Stop();
                    //if (w.ElapsedMilliseconds > 25)
                    if (w.ElapsedMilliseconds > 300) // raise the limit to get rid of annoying output we currently cannot change anyway
                  MessageWriteLine("Invoking IComponent.Loaded() for component {0} took {1:0.00}s", item.Id, w.Elapsed.TotalSeconds);
                }
                if (item.Content is Component)
                    (item.Content as Component).VisibilityChanged(item.Content, item.Visible);

                if (item.Content is IProperty)
                    mPropertyInterfaces.Add(item.Content as IProperty);

                if (item.Content is IScript)
                    mScriptInterfaces.Add(item.Content as IScript);

                if (item.Content is Component)
                    AddComponent(item.Content as Component);
            }

            mInitialLoadOfComponentsCompleted = true;
            List<object> components = mComponents;
            mComponents = new List<object>();
            foreach (object o in components)
                AddComponent(o);


            foreach (DockItem item in DockFrame.GetItems())
            {
                if (item.Content is Component)
                {
                    (item.Content as Component).InitComplete();
                }
            }

            total.Stop();
            if (total.ElapsedMilliseconds > 100)
                MessageWriteLine("ComponentsLoaded() total time = {0}s", total.Elapsed.TotalSeconds);
        }

        private void ComponentsSave()
        {
            foreach (DockItem item in DockFrame.GetItems())
            {
                if (item.Content is Component)
                {
                    currentLoadSaveItem = item;
                    (item.Content as Component).Save();
                    currentLoadSaveItem = null;
                }
            }
        }

        private void ComponentsRemove()
        {
            foreach (DockItem item in DockFrame.GetItems())
                if (item.Content is Component)
                    foreach (DockItem other in DockFrame.GetItems())
                        if (other != item)
                            (item.Content as Component).ComponentRemoved(other);
        }

        MainWindowPersistence m_LoadedPersistence = null;

        protected virtual void LoadPersistence()
        {
            currentLoadSaveItem = null;
            MainWindowPersistence p = (MainWindowPersistence)LoadObject("MainWindow", typeof(MainWindowPersistence));
            if (p != null)
            {
                this.Resize(p.Width, p.Height);
                this.Move(p.WindowX, p.WindowY);
                if ((p.WindowState & (int)Gdk.WindowState.Maximized) != 0)
                    this.Maximize();
            }
            m_LoadedPersistence = p;
        }

        protected virtual void SavePersistence()
        {
            int wx, wy, width, height;
            this.GetPosition(out wx, out wy);
            this.GetSize(out width, out height);

            MainWindowPersistence p = new MainWindowPersistence();
            p.WindowX = wx;
            p.WindowY = wy;
            p.Width = width;
            p.Height = height;
            p.Layout = DockFrame.CurrentLayout;
            p.WindowState = (int)WindowState;

            currentLoadSaveItem = null;
            SaveObject("MainWindow", p);
        }

        /// <summary>
        /// exit application
        /// </summary>
        public void quit()
        {
            PrepareExit();
        }

        protected void PrepareExit()
        {
            PowerDown = true;
            // update own persistence before save configuration
            SavePersistence();
            SaveConfigurationFile(ConfigurationFile);
            Application.Quit();
        }

        protected void OnDeleteEvent(object sender, DeleteEventArgs a)
        {
            PrepareExit();
            a.RetVal = true;
        }

        #endregion

        #region Binary Persistency

        // TODO It does not really make sense to but binary blobs into XML... this way the file is not really editable/parsable anymore. Suggestion: Prefer using IPersistency.
        /// <summary>
        /// Load an object from persistence.
        /// The optional parameter 'item' can be used to identify the proper DockItem instance.
        /// </summary>
        public object LoadObject(String elementName, Type t, DockItem item = null)
        {
            String pimpedElementName = elementName;
            if (item != null)
                pimpedElementName += "_" + item.Id.ToString();
            else
            if (currentLoadSaveItem != null)
                pimpedElementName += "_" + currentLoadSaveItem.Id.ToString();

            if (XmlConfiguration == null || pimpedElementName == null)
                return null;

            XmlNode element = XmlConfiguration.SelectSingleNode(pimpedElementName);
            if (element == null)
                return null;

            // deserialize new method
            XmlNode node = element.SelectSingleNode(t.Name + "_FMT");
            if (node != null)
            {
                MemoryStream formattedStream = new MemoryStream();
                byte[] data = FromHexString(node.InnerText);
                formattedStream.Write(data, 0, data.Length);
                formattedStream.Flush();
                formattedStream.Seek(0, SeekOrigin.Begin);

                try
                {
                    System.Runtime.Serialization.IFormatter formatter = new BinaryFormatter();
                    object result = (object)formatter.Deserialize(formattedStream);
                    return result;
                }
                catch(Exception)
                {
                    return null;
                }
            }

            // deserialize old method, only necessary to read old persistence, can be removed in some weeks
            node = element.SelectSingleNode(t.Name);
            if (node == null)
                return null;

            MemoryStream ms = new MemoryStream();
            XmlTextWriter xmlWriter = new XmlTextWriter(ms, System.Text.Encoding.UTF8);

            node.WriteTo(xmlWriter);
            xmlWriter.Flush();
            XmlReader xmlReader = new XmlTextReader(new MemoryStream(ms.ToArray()));

            try
            {
                // TODO: contruction of XmlSerializer(type) needs a lot of time totally unexpected, an optimization rergarding persistence could be necessary ...
                XmlSerializer serializer = new XmlSerializer(t);
                return serializer.Deserialize(xmlReader);
            }
            catch(Exception)
            {
                return null;
            }
        }
        // hexdump hex dump (copied from LittleHelper, need in also in other context)
        // TODO this code is misplaced in this class, move it to somewhere else, e.g. "Tools"
        public static String ToHexString(byte[] ar)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < ar.Length; i++)
                result.Append(BitConverter.ToString(ar, i, 1));
            return result.ToString();
        }
        // Byte array from hexdump string
        // TODO this code is misplaced in this class, move it to somewhere else, e.g. "Tools"
        public static Byte[] FromHexString(String s)
        {
            if (s == null || (s.Length % 2) != 0)
                return null;
            Byte[] bytes = new Byte[s.Length / 2];
            for (int i = 0; i < s.Length / 2; i++)
                bytes[i] = Convert.ToByte(s.Substring(i * 2, 2), 16);
            return bytes;
        }

        /// <summary>
        /// Save an object to persistence.
        /// The optional paranmeter should be used only loading from threads to identify correct DockItem
        /// </summary>
        public void SaveObject(String elementName, object obj, DockItem item = null)
        {
            String pimpedElementName = elementName;
            if (item != null)
                pimpedElementName += "_" + item.Id.ToString();
            else
            if (currentLoadSaveItem != null)
                pimpedElementName += "_" + currentLoadSaveItem.Id.ToString();

            // replace in managed persistence
            XmlNode newNode = XmlDocument.CreateElement(pimpedElementName);

            // add serialized data
            MemoryStream formattedStream = new MemoryStream();
            System.Runtime.Serialization.IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(formattedStream, obj);
            formattedStream.Flush();
            string serializedAsHex = ToHexString(formattedStream.GetBuffer());
            XmlNode importNode = XmlDocument.CreateElement(obj.GetType().Name + "_FMT");
            importNode.InnerText = serializedAsHex;
            newNode.AppendChild(importNode);

            // need new base node if started without old config
            if (XmlConfiguration == null)
            {
                XmlConfiguration = XmlDocument.CreateElement("DockingConfiguration");
                XmlDocument.AppendChild(XmlConfiguration);
            }
            XmlNode oldNode = XmlConfiguration.SelectSingleNode(pimpedElementName);
            if (oldNode != null)
                XmlConfiguration.ReplaceChild(newNode, oldNode);
            else
                XmlConfiguration.AppendChild(newNode);
        }

        #endregion

        #region IPersistency

        #region save

        public void SaveSetting(string instance, string key, string val)
        {
            if (XmlConfiguration == null)
                return;

            List<string> portions = new List<string>(instance.Split('/'));
            portions.Add(key);
            if (portions.Count <= 0)
                return;

            XmlNode N = null;
            XmlNode parent = XmlConfiguration;
            foreach (string p in portions)
            {
                try
                {
                    N = parent.SelectSingleNode(p);
                }
                catch
                {
                    N = null;
                }
                if (N == null)
                {
                    N = XmlDocument.CreateElement(p);
                    parent.AppendChild(N);
                }
                parent = N;
            }
            N.InnerText = val;
        }

        public void SaveSetting(string instance, string key, List<string> val)
        {
            int count = val == null ? 0 : val.Count;
            SaveSetting(instance, key + ".Count", count);
            for (int i = 0; i < count; i++)
            {
                SaveSetting(instance, key + "." + i, val[i]);
            }
        }

        public void SaveSetting(string instance, string key, List<bool> val)
        {
            int count = val == null ? 0 : val.Count;
            SaveSetting(instance, key + ".Count", count);
            for (int i = 0; i < count; i++)
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
            foreach (TreeViewColumn col in treeview.Columns)
            {

                int w = col.Width;
                if (widths.Length > 0)
                    widths.Append(";");
                string title = (col is TreeViewColumnLocalized) ? (col as TreeViewColumnLocalized).LocalizationKey : col.Title;
                title = Regex.Replace(title, "[=;]", "");
                widths.Append(title).Append("=").Append(col.Width);
            }
            SaveSetting(instance, treeview.Name + ".ColumnWidths", widths.ToString());
        }

        #endregion

        #region load

        public string LoadSetting(string instance, string key, string defaultval)
        {
            if (XmlConfiguration == null)
                return defaultval;

            List<string> portions = new List<string>(instance.Split('/'));
            portions.Add(key);
            if (portions.Count <= 0)
                return defaultval;

            try
            {
                XmlNode N = null;
                XmlNode parent = XmlConfiguration;
                foreach (string p in portions)
                {
                    N = parent.SelectSingleNode(p);
                    if (N == null)
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
            if (count < 0)
                return defaultval;
            List<string> result = new List<string>();
            for (int i = 0; i < count; i++)
                result.Add(LoadSetting(instance, key + "." + i, ""));
            return result;
        }

        public List<bool> LoadSetting(string instance, string key, List<bool> defaultval)
        {
            int count = LoadSetting(instance, key + ".Count", -1);
            if (count < 0)
                return defaultval;
            List<bool> result = new List<bool>();
            for (int i = 0; i < count; i++)
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
            if (s == "true")
                return true;
            else
            if (s == "false")
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
            foreach (string s in all)
            {
                string[] one = s.Split('=');
                if (one.Length == 2)
                {
                    one[0] = one[0].ToLowerInvariant();
                    int width;
                    if (Int32.TryParse(one[1], out width))
                    {
                        if (width < 5) // quickfix: make sure no columns become invisible
                     width = 5;
                        foreach (TreeViewColumn col in treeview.Columns)
                        {
                            string title = (col is TreeViewColumnLocalized) ? (col as TreeViewColumnLocalized).LocalizationKey : col.Title;
                            title = title.ToLowerInvariant();
                            if (title == one[0])
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
            TaggedLocalizedImageMenuItem menuitem = sender as TaggedLocalizedImageMenuItem;
            ComponentFactoryInformation cfi = menuitem.Tag as ComponentFactoryInformation;

            String name;

            if (cfi.IsSingleInstance)
            {
                // show existing components instance
                // do nothing if exist and already visible
                name = cfi.ComponentType.ToString();
                DockItem di = DockFrame.GetItem(name);
                if (di != null)
                {
                    // make object visible, leave already visible single instance object as it is
                    if (!di.Visible)
                        di.Visible = true;
                    return;
                }
                // no instance exist, need to create a new instance
            }
            else
            {
                // behaviour of multiple instance is different
                // because we could create multilple object, it is
                // necessary to create a new one despite another object exist
                // so far so good, but ...
                // also a multiple instance object could be hidden in current layout
                // and exist in another layout as a visible object
                // as a solution we search for hidden multiple objects of requested type
                // and show it in current layout
                // create only a new instance if only visible instances found

                name = cfi.ComponentType.ToString();
                DockItem[] some = DockFrame.GetItemsContainsId(name);
                foreach (DockItem it in some)
                {
                    // make object visible if possible
                    // ignore already visible items
                    if (!it.Visible)
                    {
                        it.Visible = true;
                        return;
                    }
                }

                // no item found which can be visible again
                // need to create a new instance with a unique name
                int instance = 1;
                do
                {
                    name = cfi.ComponentType.ToString() + "-" + instance.ToString();
                    instance++;
                }
                while (DockFrame.GetItem(name) != null);
            }

            // add new instance of desired component
            DockItem item = CreateItem(cfi, name);
            if (item == null)
            {
                MessageWriteLine("ERROR: cannot instantiate component " + name);
                return;
            }
            item.Behavior = DockItemBehavior.Normal;
            if (!cfi.IsSingleInstance)
                item.Behavior |= DockItemBehavior.CloseOnHide;

            Localization.LocalizeControls(item.Content.GetType().Namespace, item.Widget);

            item.DefaultVisible = false;
            //item.DrawFrame = true;
            item.Visible = true;

            // call initialization of new created component
            currentLoadSaveItem = item;
            if (item.Content is Component)
                (item.Content as Component).Loaded(item);
            currentLoadSaveItem = null;

            if (item.Content is Component)
                AddComponent(item.Content);

            if (item.Content is IProperty)
                mPropertyInterfaces.Add(item.Content as IProperty);

            if (item.Content is IScript)
                mScriptInterfaces.Add(item.Content as IScript);
        }

        private void HandleDockItemRemoved(DockItem item)
        {
            if (item.Content is IProperty)
                mPropertyInterfaces.Remove(item.Content as IProperty);

            // tell all other about current item changed if it the removed component
            if (CurrentDockItem == item)
            {
                // care all IProperty Widgets
                foreach (IProperty ip in mPropertyInterfaces)
                    ip.SetObject(null);

                // care all IScript Widgets
                foreach (IScript isc in mScriptInterfaces)
                    isc.SetScript(null, null);

                CurrentDockItem = null;
                foreach (DockItem other in DockFrame.GetItems())
                {
                    if (other != item && other.Content is Component)
                        (other.Content as Component).FocusChanged(null);
                }
            }

            // tell all other about removed component
            foreach (DockItem other in DockFrame.GetItems())
            {
                if (other != item && other.Content is Component)
                    (other.Content as Component).ComponentRemoved(item.Content);
            }

            // remove from message dictionary
            if (item.Content is IMessage)
                mMessage.Remove(item.Id);

            // tell component about it instance itself has been removed from dock container
            if (item.Content is Component)
                (item.Content as Component).ComponentRemoved(item.Content);

            item.Widget.Destroy();
        }

        bool m_LockHandleVisibleChanged = true;
        // startup lock
        void HandleVisibleChanged(object sender, EventArgs e)
        {
            if (!m_LockHandleVisibleChanged)
            {
                DockItem item = sender as DockItem;
                if (item.Content is Component)
                    (item.Content as Component).VisibilityChanged(item, item.Visible);
            }
        }

        /// <summary>
        /// Create new item, called from menu choice or persistence
        /// </summary>
        private DockItem CreateItem(ComponentFactoryInformation cfi, String name)
        {
            // add new instance of desired component
            Widget w = cfi.CreateInstance(this);
            if (w == null)
                return null;

            DockItem item = DockFrame.AddItem(name);
            AddSelectNotifier(item, w);
            AddSelectNotifier(item, item.TitleTab);
            item.Content = w;
            if (item.Content is ILocalizableComponent)
                item.Content.Name = (item.Content as ILocalizableComponent).Name.Localized(item.Content);
            item.UpdateTitle();
            item.Icon = cfi.Icon;
            item.DefaultVisible = false;
            item.VisibleChanged += HandleVisibleChanged;

            if (cfi.CloseOnHide)
                item.Behavior |= DockItemBehavior.CloseOnHide;

            if (item.Content is IMessage)
            {
                mMessage.Add(item.Id, item.Content as IMessage);

                // push all queued messages
                foreach (String m in mMessageQueue)
                    (item.Content as IMessage).WriteLine(m);
            }

            return item;
        }

        /// <summary>
        /// Create new item, called from persistence
        /// </summary>
        private DockItem CreateItem(string id)
        {
            String[] m = id.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (m.Length == 0)
                return null;
            String typename = m[0];

            ComponentFactoryInformation cfi = ComponentFinder.FindComponent(typename);
            if (cfi == null)
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
        List<IProperty> mPropertyInterfaces = new List<IProperty>();
        List<IScript> mScriptInterfaces = new List<IScript>();

        /// <summary>
        /// Adds events for any child widget to find out which
        /// DockItem is selected by the user.
        /// Care the current selected DockItem, color the title bar.
        /// </summary>
        private void AddSelectNotifier(DockItem item, Widget w)
        {
            if (mSelectRelation.ContainsKey(w))
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

            if (w is Gtk.Container)
            {
                foreach (Widget xw in ((Gtk.Container)w))
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
                        Console.WriteLine ("{0} Test TreeViewColumn.Clicked {1}", qwe++, sender);
                    };
                }
            }
#endif
        }

        void SelectCurrentEvent(object item)
        {
            DockItem select;
            if (mSelectRelation.TryGetValue(item, out select))
            {
                if (CurrentDockItem != select)
                {
                    if (CurrentDockItem != null)
                    {
                        CurrentDockItem.TitleTab.VisualStyle = mNormalStyle;
                    }
                    CurrentDockItem = select;
                    CurrentDockItem.TitleTab.VisualStyle = mSelectedStyle;

                    mMenuCut.Sensitive = CurrentDockItem.Content is ICut;
                    mMenuCopy.Sensitive = CurrentDockItem.Content is ICopy;
                    mMenuPaste.Sensitive = CurrentDockItem.Content is IPaste;

                    // notify all IProperty Widgets
                    if (!(CurrentDockItem.Content is IProperty))
                    {
                        foreach (IProperty ip in mPropertyInterfaces)
                            ip.SetObject(null);
                    }
                    // notify all IScript Widgets
                    if (!(CurrentDockItem.Content is IScript))
                    {
                        foreach (IScript isc in mScriptInterfaces)
                            isc.SetScript(null, null);
                    }

                    // notify all other components that the current item changed
                    foreach (DockItem other in DockFrame.GetItems())
                    {
                        if (other != item && other.Content is Component)
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
            Debug.Assert(StatusBar != null);
            uint id = mStatusBarUniqueId++;
            StatusBar.Push(id, txt);
            return id;
        }

        /// <summary>
        /// Pop a message from the statusbar.
        /// </summary>
        public void PopStatusbar(uint id)
        {
            StatusBar.Pop(id);
        }

        #endregion

        #region Toolbar

        public void AddToolItem(ToolItem item)
        {
            item.Show();
            ToolBar.Insert(item, -1);
        }

        public void RemoveToolItem(ToolItem item)
        {
            ToolBar.Remove(item);
        }

        #endregion

        #region IMessageWriteLine

        public void MessageWriteLine(String format, params object[] args)
        {
            if (format == null)
                return;

            if (PowerDown)
                return;

            Gtk.Application.Invoke(delegate
            {
                MessageWriteLineWithoutInvoke(format, args);
            });
        }

        protected void MessageWriteLineWithoutInvoke(String format, params object[] args)
        {
            if (format == null)
                return;

            if (PowerDown)
                return;

            foreach (KeyValuePair<string, IMessage> kvp in mMessage)
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
            ScriptScope scope = IronPython.Hosting.Python.GetBuiltinModule(ScriptEngine);
            scope.SetVariable("__import__", new ImportDelegate(DoPythonModuleImport));

            // access to this using "ComponentManager"
            m_ScriptingInstance = new ComponentManagerScripting(this);
            ScriptScope.SetVariable(pythonBaseVariableName, m_ScriptingInstance);

            // add Python commands like "message(...)" 
            Execute(ReadResource("cm.py").Replace("[INSTANCE]", pythonBaseVariableName));
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
            if (s == null)
                return null;
            System.IO.StreamReader reader = new System.IO.StreamReader(s);
            if (reader == null)
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
            return compiled.Execute(ScriptScope);
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
            public void quit()
            {
                ComponentManager.quit();
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
            /// Gets the component identifier based on the component name and id
            /// The identifier should be unique for all localizations and
            /// very simliar to the component title name (english title name)
            /// </summary>
            private string GetComponentIdentifier(DockItem item)
            {
                Component component = item.Content as Component;
                if (component == null)
                    return null;

                // localized items must use its unlocalized name for identification
                if (component is ILocalizableComponent)
                {
                    ILocalizableComponent lc = component as ILocalizableComponent;
                    // consider instance index in same way as used for title name calculation
                    if (item.InstanceIndex >= 2)
                        return lc.Name + " " + item.InstanceIndex;
                    return lc.Name;
                }

                // not localized items can use their name
                return item.Content.Name;
            }

            /// <summary>
            /// Get the python scripting instance of a special component
            /// </summary>
            public object GetInstance(string identifier)
            {
                foreach (DockItem item in ComponentManager.DockFrame.GetItems())
                {
                    if (item.Content == null)
                        continue;
                    Component component = item.Content as Component;
                    if (component.GetScriptingInstance() == null)
                        continue;

                    string cid = GetComponentIdentifier(item);
                    if (cid != null && cid == identifier)
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
                foreach (DockItem item in ComponentManager.DockFrame.GetItems())
                {
                    if (item.Content == null)
                        continue;
                    Component component = item.Content as Component;
                    if (component.GetScriptingInstance() == null)
                        continue;

                    string cid = GetComponentIdentifier(item);
                    if (cid != null)
                        result.Add(cid);
                }
                return result.ToArray();
            }
        }

        #endregion

    }

    public class TaggedLocalizedMenuItem : MenuItem, ILocalizableWidget
    {
        public TaggedLocalizedMenuItem(IntPtr raw) : base(raw)
        {
        }

        public TaggedLocalizedMenuItem(String name) : base(name)
        {
        }

        public System.Object Tag { get; set; }

        void ILocalizableWidget.Localize(string namespc)
        {
            Label l = this.Child as Label;
            if (LocalizationKey == null)
                LocalizationKey = l.Text;
            l.Text = LocalizationKey.Localized(namespc);
        }

        public string LocalizationKey { get; set; }
    }

    public class TaggedLocalizedImageMenuItem : ImageMenuItem, ILocalizableWidget
    {
        public TaggedLocalizedImageMenuItem(IntPtr raw) : base(raw)
        {
        }

        public TaggedLocalizedImageMenuItem(String name) : base(name)
        {
        }

        public System.Object Tag { get; set; }

        void ILocalizableWidget.Localize(string namespc)
        {
            Label l = this.Child as Label;
            if (LocalizationKey == null)
                LocalizationKey = l.Text;
            l.Text = LocalizationKey.Localized(namespc);
        }

        public string LocalizationKey { get; set; }
    }

    public class TaggedLocalizedCheckedMenuItem : CheckMenuItem, ILocalizableWidget
    {
        public TaggedLocalizedCheckedMenuItem(IntPtr raw) : base(raw)
        {
            IgnoreLocalization = false;
        }

        public TaggedLocalizedCheckedMenuItem(String name) : base(name)
        {
            IgnoreLocalization = false;
        }

        public System.Object Tag { get; set; }

        void ILocalizableWidget.Localize(string namespc)
        {
            if (!IgnoreLocalization)
            {
                Label l = this.Child as Label;
                if (LocalizationKey == null)
                    LocalizationKey = l.Text;
                l.Text = LocalizationKey.Localized(namespc);
            }
        }

        public string LocalizationKey { get; set; }

        public bool IgnoreLocalization { get; set; }
    }
    // used for SaveAs dialog, necessary to extent with correct file extension
    public class FileFilterExt
    {
        public FileFilterExt(string pattern, string name)
        {
            Pattern = pattern;
            Filter = new FileFilter();
            Filter.AddPattern(pattern);
            Filter.Name = name;
        }

        public FileFilter Filter { get; private set; }

        public string Pattern { get; set; }
    }

    [Serializable]
    public class MainWindowPersistence
    {
        public int WindowX { get; set; }

        public int WindowY { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public string Layout { get; set; }

        public int WindowState { get; set; }
    }
}

