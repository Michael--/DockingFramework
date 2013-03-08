using System;
using System.Diagnostics;
using Gtk;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Collections.Generic;
using Docking.Helper;

namespace Docking.Components
{
    public class ComponentManager: Gtk.Window
    {
        #region Initialization
        public ComponentManager (WindowType wt) : base(wt)
        {
            AccelGroup = new AccelGroup();
            AddAccelGroup(AccelGroup);
            ComponentFinder = new Docking.Components.ComponentFinder();
            XmlDocument = new XmlDocument();
            PowerDown = false;
        }

        public void SetDockFrame(DockFrame df)
        {
            DockFrame = df;
            DockFrame.DockItemRemoved += HandleDockItemRemoved;
            DockFrame.CreateItem = this.CreateItem;

            DockVisualStyle style = new DockVisualStyle ();
            style.PadTitleLabelColor = Styles.PadLabelColor;
            style.PadBackgroundColor = Styles.PadBackground;
            style.InactivePadBackgroundColor = Styles.InactivePadBackground;
            // style.PadTitleHeight = barHeight;
            DockFrame.DefaultVisualStyle = style;

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
			String [] m = path.Split(new char[] {'\\'}, StringSplitOptions.RemoveEmptyEntries);

			// as a minimum 1 submenu name must exist where to append the new entry
			Debug.Assert(m.Length >= 1);

			MenuShell menuShell = MenuBar;
			Menu foundmenu = null;
			System.Collections.IEnumerable children = MenuBar.Children;
			for (int i = 0; i < m.Length; i++)
			{
				foundmenu = SearchOrCreateMenu(m[i], menuShell, children);
				children = foundmenu.AllChildren;
				menuShell = foundmenu;
			}

            return foundmenu;
		}

        protected void InsertMenu(String path, MenuItem item)
        {
            Menu foundmenu = FindMenu(path);
			if(foundmenu!=null)
               foundmenu.Insert(item, 0);
        }

        protected void SetMenuBar(MenuBar menuBar)
        {
            MenuBar = menuBar;
        }

        public AccelGroup AccelGroup  { get; private set; }

        /// <summary>
        /// Installs the layout menu, show all existing layouts
        /// and a possibility to add and remove layouts
        /// The main layout is not removeable.
        /// If the main layout name is empty or null "Default" will be used as name.
        /// </summary>
        public void SetDefaultLayout(String defaultLayout)
        {
            //AddLayout("Test2", false);
            //AddLayout("Test1", false);

            if (defaultLayout == null || defaultLayout.Length == 0)
                defaultLayout = "Default";
            AddLayout(defaultLayout, false);
            DockFrame.CurrentLayout = defaultLayout;

            ImageMenuItem deleteLayout = new ImageMenuItem ("Delete Current Layout");
            deleteLayout.Activated += (object sender, EventArgs e) => 
            {
            };

            ImageMenuItem newLayout = new ImageMenuItem ("New Layout...");
            newLayout.Activated += (object sender, EventArgs e) => 
            {
                String test = "Test1";
                if (!DockFrame.HasLayout (test))
                {
                    DockFrame.CreateLayout (test, true);
                    InsertLayoutMenu(test);
                    DockFrame.CurrentLayout = test;
                }
            };

            InsertMenu (@"View\Layout", deleteLayout);
            InsertMenu (@"View\Layout", newLayout);
            InsertMenu (@"View\Layout", new SeparatorMenuItem ());

            foreach (String s in DockFrame.Layouts)
                InsertLayoutMenu (s);

            MenuBar.ShowAll();
        }

        private void InsertLayoutMenu(String name)
        {
            CheckMenuItem item = new CheckMenuItem(name);
            item.Active = (name == DockFrame.CurrentLayout);
            
            item.Activated += (object sender, EventArgs e) => 
            {
                CheckMenuItem nitem = sender as CheckMenuItem;
                String label = (nitem.Child as Label).Text;
                
                // double check 
                if (DockFrame.HasLayout(label) && DockFrame.CurrentLayout != label)
                {
                    // uncheck all other
                    if (nitem.Parent is Menu)
                    {
                        foreach(object obj in ((Menu)nitem.Parent).AllChildren)
                        {
                            if (obj is CheckMenuItem)
                            {
                                CheckMenuItem other = obj as CheckMenuItem;
                                if (other != nitem)
                                    other.Active = false;
                            }
                        }
                    }

                    // before check selected layout
                    DockFrame.CurrentLayout = label;
                    nitem.Active = true;
                    Console.WriteLine(String.Format("CurrentLayout={0}", label));
                }
            };
            InsertMenu (@"View\Layout", item);
            item.ShowAll ();

        }

        private void AddLayout(string name, bool copyCurrent)
        {
            if (!DockFrame.HasLayout (name))
                DockFrame.CreateLayout (name, copyCurrent);
        }

        private void DeleteLayout(string name)
        {
            if (name != DockFrame.CurrentLayout)
            {
                DockFrame.DeleteLayout (name);
            }
        }


        private void InstallQuitMenu()
        {
            ImageMenuItem item = new ImageMenuItem("Quit");
			item.Image = new Image(Gdk.Pixbuf.LoadFromResource ("Docking.Framework.Manager.Quit-16.png"));
            item.AddAccelerator("activate", AccelGroup, new AccelKey(Gdk.Key.Q, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
            item.Activated +=  OnQuitActionActivated;
            InsertMenu("File", item);
        }

        protected void OnQuitActionActivated(object sender, EventArgs e)
        {
            PrepareExit();
        }

        /// <summary>
        /// Add all component start/create menu entries
        /// </summary>
        protected void AddComponentMenus()
        {
            InstallQuitMenu();

            foreach (ComponentFactoryInformation cfi in ComponentFinder.ComponentInfos)
            {
                // the last name is the menu name, all others are menu/sub-menu names
                String [] m = cfi.MenuPath.Split(new char[] {'\\'}, StringSplitOptions.RemoveEmptyEntries);

                // as a minimum submenu-name & menu-name must exist
                Debug.Assert(m.Length >= 2);

                // build path again
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < m.Length-1; i++)
                {
                    if (i > 0)
                        builder.Append("\\");
                    builder.Append(m[i]);
                }

                // use last entry as menu name and create
                TaggedImageMenuItem item = new TaggedImageMenuItem(m[m.Length - 1]);
                item.Tag = cfi;
                // TODO: make the menu image visible if you know how
                Gdk.Pixbuf pb = cfi.Icon;
                if (pb != null)
                    item.Image = new Image(pb);
                item.Activated += ComponentHandleActivated;
                InsertMenu(builder.ToString(), item);
            }
            MenuBar.ShowAll();
        }

        private Menu SearchOrCreateMenu(String name, MenuShell menuShell, System.Collections.IEnumerable children)
        {
            // 1st search menu & return if existing
            foreach(MenuItem mi in children)
            {
                Label label = (Label)mi.Child;
                if (label != null && label.Text == name)
                    return mi.Submenu as Menu;
            }

            // 2nd append new menu
            // todo: currently append at the end, may a dedicated position desired
            Menu menu = new Menu ( );
            MenuItem menuItem = new MenuItem(name);
            menuItem.Submenu = menu;

            // todo: menu insert position should be overworked
            //       position is dependent of content

            menuShell.Add(menuItem);

            return menu;
        }

        #endregion

        #region Private properties
        private Statusbar StatusBar { get;  set; }
        private Toolbar ToolBar { get;  set; }
        private MenuBar MenuBar { get; set; }
        private XmlDocument XmlDocument { get;  set; }
        private XmlNode XmlConfiguration { get;  set; }
        #endregion

        #region Public properties
        public DockFrame DockFrame { get; private set; }
        public ComponentFinder ComponentFinder { get; private set; }
        public bool PowerDown { get; set; }
        public String ConfigurationFile  { get; set; }
        #endregion

        #region Configuration

        protected void LoadConfigurationFile(String filename)
        {
            ConfigurationFile = filename;

            // layout from file or new
            if (File.Exists (filename))
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

                    DockFrame.LoadLayouts (xmlReader);
                }
            }
            else
            {
                DockFrame.CreateLayout ("Default", true);
            }
        }

        protected void SaveConfigurationFile(String filename)
        {
            // save first DockFrame persistence in own (memory) file
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xmlWriter = new XmlTextWriter(ms, System.Text.Encoding.UTF8);
            DockFrame.SaveLayouts (xmlWriter);
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

            // save all components data
            ComponentsSave();

            // at least save complete persistence to file
            SaveConfiguration(filename);
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
        private DockItem currentLoadSaveItem;

        protected void ComponentsLoaded()
        {
            m_LockHandleVisibleChanged = false; // unlock visible change notices

            // tell all components about load state
            // time for late initialization and/or load persistence
            foreach (DockItem item in DockFrame.GetItems())
            {
                if (item.Content is IComponent)
                {
                    currentLoadSaveItem = item;
                    (item.Content as IComponent).Loaded (item);
                }
                if (item.Content is IComponentInteract)
                    (item.Content as IComponentInteract).Visible(item.Content, item.Visible);
            }

            // tell any component about all other component
            foreach (DockItem item in DockFrame.GetItems())
            {
                if (item.Content is IComponentInteract)
                {
                    foreach(DockItem other in DockFrame.GetItems())
                        if (item != other)
                            (item.Content as IComponentInteract).Added (other.Content);
                }
            }
        }

        private void ComponentsSave()
        {
            foreach (DockItem item in DockFrame.GetItems())
            {
                if (item.Content is IComponent)
                {
                    currentLoadSaveItem = item;
                    (item.Content as IComponent).Save();
                }
            }

            // tell any component about all other component
            foreach (DockItem item in DockFrame.GetItems())
            {
                if (item is IComponentInteract)
                {
                    foreach(DockItem other in DockFrame.GetItems())
                        if (item != other)
                            (item.Content as IComponentInteract).Removed (other);
                }
            }
        }


        protected void LoadPersistence()
        {
            currentLoadSaveItem = null;
            MainWindowPersistence p = (MainWindowPersistence)LoadObject ("MainWindow", typeof(MainWindowPersistence));
            if (p != null)
            {
                this.Resize(p.Width, p.Height);
                this.Move (p.WindowX, p.WindowY);
            }
        }

        private void SavePersistence()
        {
            int wx, wy, width, height;
            this.GetPosition(out wx, out wy);
            this.GetSize(out width, out height);

            MainWindowPersistence p = new MainWindowPersistence();
            p.WindowX = wx;
            p.WindowY = wy;
            p.Width = width;
            p.Height = height;

            currentLoadSaveItem = null;
            SaveObject("MainWindow", p);
        }

        protected void PrepareExit()
        {
            PowerDown = true;
            // update own persistence before save configuration
            SavePersistence();
            SaveConfigurationFile(ConfigurationFile);
            Application.Quit();
        }

        protected void OnDeleteEvent (object sender, DeleteEventArgs a)
        {
            PrepareExit();
            a.RetVal = true;
        }


        #endregion

        #region Persistence

        public object LoadObject(String elementName, Type t)
        {
            String pimpedElementName = elementName;
            if (currentLoadSaveItem != null)
                pimpedElementName += "_" + currentLoadSaveItem.Id.ToString ();

            if(XmlConfiguration == null || pimpedElementName == null)
                return null;
            XmlNode element = XmlConfiguration.SelectSingleNode(pimpedElementName);
            if (element == null)
                return null;
            XmlNode node = element.SelectSingleNode(t.Name);
            if (node == null)
                return null;

            MemoryStream ms = new MemoryStream();
            XmlTextWriter xmlWriter = new XmlTextWriter(ms, System.Text.Encoding.UTF8);

            node.WriteTo(xmlWriter);
            xmlWriter.Flush();
            XmlReader xmlReader = new XmlTextReader(new MemoryStream(ms.ToArray()));

            XmlSerializer serializer = new XmlSerializer(t);
            return serializer.Deserialize(xmlReader);
        }

        public void SaveObject(String elementName, object obj)
        {
            String pimpedElementName = elementName;
            if (currentLoadSaveItem != null)
                pimpedElementName += "_" + currentLoadSaveItem.Id.ToString ();

            MemoryStream ms = new MemoryStream ();
            XmlTextWriter xmlWriter = new XmlTextWriter (ms, System.Text.Encoding.UTF8);
            XmlSerializer serializer = new XmlSerializer (obj.GetType ());
            serializer.Serialize (xmlWriter, obj);
            xmlWriter.Flush ();

            XmlReader xmlReader = new XmlTextReader (new MemoryStream (ms.ToArray ()));

            // re-load as XmlDocument
            XmlDocument doc = new XmlDocument ();
            doc.Load (xmlReader);

            // replace in managed persistence
            XmlNode node = doc.SelectSingleNode (obj.GetType ().Name);
            XmlNode importNode = XmlDocument.ImportNode (node, true);
            XmlNode newNode = XmlDocument.CreateElement (pimpedElementName);
            newNode.AppendChild (importNode);
            // need new base node if started without old config
            if (XmlConfiguration == null)
            {
                XmlConfiguration = XmlDocument.CreateElement ("DockingConfiguration");
                XmlDocument.AppendChild(XmlConfiguration);
            }
            XmlNode oldNode = XmlConfiguration.SelectSingleNode(pimpedElementName);
            if (oldNode != null)
                XmlConfiguration.ReplaceChild(newNode, oldNode);
            else
                XmlConfiguration.AppendChild(newNode);
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
            TaggedImageMenuItem menuitem = sender as TaggedImageMenuItem;
            ComponentFactoryInformation cfi = menuitem.Tag as ComponentFactoryInformation;

            String name;

            if (cfi.IsSingleInstance)
            {
                // show existing components instance
                // do nothing if exist and already visible
                name = cfi.ComponentType.ToString();
                DockItem di = DockFrame.GetItem (name);
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
                foreach(DockItem it in some)
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
                while(DockFrame.GetItem(name) != null);
            }

            // add new instance of desired component
            DockItem item = CreateItem (cfi, name);
            item.Behavior = DockItemBehavior.Normal;
            if (!cfi.IsSingleInstance)
                item.Behavior |= DockItemBehavior.CloseOnHide;

            //item.DefaultVisible = true;
            //item.DrawFrame = true;
            item.Visible = true;

            // call initialization of new created component
            if (item.Content is IComponent)
                (item.Content as IComponent).Loaded(item);

            // tell all other about new component
            foreach(DockItem other in DockFrame.GetItems())
            {
                if (other != item && other.Content is IComponentInteract)
                    (other.Content as IComponentInteract).Added (item.Content);
            }

            // tell new component about all other components
            if (item.Content is IComponentInteract)
                foreach(DockItem other in DockFrame.GetItems())
                    (item.Content as IComponentInteract).Added (other.Content);
        }

        private void HandleDockItemRemoved(DockItem item)
        {
            // tell all other about removed component
            foreach (DockItem other in DockFrame.GetItems())
            {
                if (other != item && other.Content is IComponentInteract)
                    (other.Content as IComponentInteract).Removed (item.Content);
            }

            // remove from message dictionary
            if (item.Content is IMessage)
                mMessage.Remove(item.Id);

            // tell component about it instance itself has been removed from dock container
            if (item.Content is IComponentInteract)
                (item.Content as IComponentInteract).Removed (item.Content);

            item.Widget.Destroy();
        }

        bool m_LockHandleVisibleChanged = true; // startup lock
        void HandleVisibleChanged(object sender, EventArgs e)
        {
            if (!m_LockHandleVisibleChanged)
            {
                DockItem item = sender as DockItem;
                if (item.Content is IComponentInteract)
                    (item.Content as IComponentInteract).Visible (item, item.Visible);
            }
        }

        /// <summary>
        /// Create new item, called from menu choice or persistence
        /// </summary>
        private DockItem CreateItem(ComponentFactoryInformation cfi, String name)
        {
            // add new instance of desired component
            Widget w = cfi.CreateInstance (this);
            if (w == null)
                return null;
            DockItem item = DockFrame.AddItem (name);
            item.Content = w;
            item.Label = w.Name;
            item.Icon = cfi.Icon;
            item.VisibleChanged += HandleVisibleChanged;

            if (cfi.CloseOnHide)
                item.Behavior |= DockItemBehavior.CloseOnHide;

            if (item.Content is IMessage)
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
        /// </summary>
        private DockItem CreateItem(string id)
        {
            String []m = id.Split(new char[] {'-'}, StringSplitOptions.RemoveEmptyEntries);
            if (m.Length == 0)
                return null;
            String typename = m[0];

            ComponentFactoryInformation cfi = ComponentFinder.FindComponent(typename);
            if (cfi == null)
                return null;

            return CreateItem (cfi, id);
        }
        #endregion

        #region Status Bar

        uint mStatusBarUniqueId = 0; // helper to generate unique IDs

        /// <summary>
        /// Push message to the statusbar, return unique ID used to pop message
        /// </summary>
        public uint PushStatusbar(String txt)
        {
            Debug.Assert (StatusBar != null);
            uint id = mStatusBarUniqueId++;
            StatusBar.Push(id, txt);
            return id;
        }

        /// <summary>
        /// Pop a message from the statusbar.
        /// </summary>
        public void PopStatusbar(uint id)
        {
            StatusBar.Pop (id);
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

        #region Message

        public void MessageWriteLine(String message)
        {
            if (PowerDown)
                return;

            Gtk.Application.Invoke(delegate {
				MessageWriteLineWithoutInvoke(message);
            });
        }

        protected void MessageWriteLineWithoutInvoke(String message)
        {
            if (PowerDown)
                return;

            foreach (KeyValuePair<string, IMessage> kvp in mMessage)
                kvp.Value.WriteLine (message);

            // queue all messages for new not yet existing receiver
            // todo: may should store only some messages to avoid memory leak ?
            mMessageQueue.Add(message);
        }
        List<String> mMessageQueue = new List<string>();
        Dictionary<string, IMessage> mMessage = new Dictionary<string, IMessage>();
        #endregion
    }

    class TaggedImageMenuItem : ImageMenuItem
    {
        public TaggedImageMenuItem(String name) : base(name) {}
        public System.Object Tag { get; set; }
    }

    public class MainWindowPersistence
    {
        public int WindowX { get; set; }
        public int WindowY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

}

