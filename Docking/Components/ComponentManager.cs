using System;
using System.Diagnostics;
using Gtk;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Collections.Generic;

namespace Docking.Components
{
    public class ComponentManager: Gtk.Window
    {
        #region Initialization
        public ComponentManager (WindowType wt) : base(wt)
        {
            ComponentFinder = new Docking.Components.ComponentFinder();
            XmlDocument = new XmlDocument();
        }

        public void SetDockFrame(DockFrame df)
        {
            DockFrame = df;
            DockFrame.DockItemRemoved += HandleDockItemRemoved;
            DockFrame.CreateItem = this.CreateItem;
        }

        public void SetStatusBar(Statusbar sb)
        {
            StatusBar = sb;
        }

        public void SetToolBar(Toolbar tb)
        {
            ToolBar = tb;
        }

        protected void SetMenuBar(MenuBar menuBar)
        {
            foreach (ComponentFactoryInformation cfi in ComponentFinder.ComponentInfos)
            {
                // the last name is the menu name, all other are menu/sub-menue names
                String [] m = cfi.MenuPath.Split(new char[] {'\\'}, StringSplitOptions.RemoveEmptyEntries);
                
                // as a minimum submenu-name & menu-name must exist
                Debug.Assert(m.Length >= 2);
                
                MenuShell menuShell = menuBar;
                Menu componentMenu = null;
                System.Collections.IEnumerable children = menuBar.Children;
                for (int i = 0; i < m.Length-1; i++)
                {
                    componentMenu = SearchOrCreateMenu(m[i], menuShell, children);
                    children = componentMenu.AllChildren;
                    menuShell = componentMenu;
                }
                TaggedImageMenuItem item = new TaggedImageMenuItem(m[m.Length - 1]);
                item.Tag = cfi;
                item.Activated += ComponentHandleActivated;
                componentMenu.Add(item);
            }
            menuBar.ShowAll();
        }

        private Menu SearchOrCreateMenu(String name, MenuShell menuShell, System.Collections.IEnumerable children)
        {
            // 1st search menue & return if existing
            foreach(MenuItem mi in children)
            {
                Label label = (Label)mi.Child;
                if (label.Text == name)
                    return mi.Submenu as Menu;
            }
            
            // 2nd append new menu 
            // todo: currently append at the end, may a dedicated position desired
            Menu menu = new Menu ( );
            MenuItem menuItem = new MenuItem(name);
            menuItem.Child = new Label(name);
            menuItem.Submenu = menu;
            menuShell.Append(menuItem);
            
            return menu;
        }

        #endregion

        #region Private properties
        private Statusbar StatusBar { get;  set; }
        private Toolbar ToolBar { get;  set; }
        private XmlDocument XmlDocument { get;  set; }
        private XmlNode XmlConfiguration { get;  set; }
        #endregion

        #region Public properties
        public DockFrame DockFrame { get; private set; }
        public ComponentFinder ComponentFinder { get; private set; }
        #endregion

        #region Configuration

        protected void LoadConfigurationFile(String filename)
        {
            // layout from file or new
            if (File.Exists (filename))
            {
                // the manager hold the persistence in memory all the time
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

        protected void ComponentsLoaded()
        {
            // tell all components about load state
            // time for late initialization and/or load persistence
            foreach (DockItem item in DockFrame.GetItems())
            {
                if (item.Content is IComponent)
                    (item.Content as IComponent).Loaded (item);

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
                    (item.Content as IComponent).Save();
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


        #endregion

        #region Persistence

        public object LoadObject(String elementName, Type t)
        {
            if(XmlConfiguration == null || elementName == null)
                return null;
            XmlNode element = XmlConfiguration.SelectSingleNode(elementName);
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
            XmlNode newNode = XmlDocument.CreateElement (elementName);
            newNode.AppendChild (importNode);
            // need new base node if started without old config
            if (XmlConfiguration == null) 
            {
                XmlConfiguration = XmlDocument.CreateElement ("DockingConfiguration");
                XmlDocument.AppendChild(XmlConfiguration);
            }
            XmlNode oldNode = XmlConfiguration.SelectSingleNode(elementName);
            if (oldNode != null)
                XmlConfiguration.ReplaceChild(newNode, oldNode);
            else
                XmlConfiguration.AppendChild(newNode);
        }
        #endregion

        #region Docking 
        private void ComponentHandleActivated(object sender, EventArgs e)
        {
            TaggedImageMenuItem menueitem = sender as TaggedImageMenuItem;
            ComponentFactoryInformation cfi = menueitem.Tag as ComponentFactoryInformation;
            
            String name;
            
            if (cfi.IsSingleInstance)
            {
                // show/hide existing components instance
                name = cfi.ComponentType.ToString();
                DockItem di = DockFrame.GetItem (name);
                if (di != null)
                {
                    di.Visible = !di.Visible;
                    return;
                }
            }
            else
            {
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
            // item.DefaultLocation = "Document";
            item.DefaultVisible = true;
            item.DrawFrame = true;
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
        }

        void HandleVisibleChanged (object sender, EventArgs e)
        {   
            DockItem item = sender as DockItem;
            if (item.Content is IComponentInteract)
                (item.Content as IComponentInteract).Visible(item, item.Visible);
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
            item.Label = name;
            item.VisibleChanged += HandleVisibleChanged;

            if (!cfi.IsSingleInstance)
                item.Behavior |= DockItemBehavior.CloseOnHide;

            if (item.Content is IMessage)
                mMessage.Add(item.Id, item.Content as IMessage);

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
            foreach(KeyValuePair<string, IMessage> kvp in mMessage)
                kvp.Value.WriteLine(message);
        }
        Dictionary<string, IMessage> mMessage = new Dictionary<string, IMessage>(); 
        #endregion
    }

    class TaggedImageMenuItem : ImageMenuItem
    {
        public TaggedImageMenuItem(String name) : base(name) {}
        public System.Object Tag { get; set; }
    }
}

