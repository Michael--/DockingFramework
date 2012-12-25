using System;
using System.Diagnostics;
using Gtk;

namespace Docking.Components
{
    public class ComponentManager
    {
        public ComponentManager (DockFrame df)
        {
            DockFrame = df;
            ComponentFinder = new Docking.Components.ComponentFinder();
        }

        public DockFrame DockFrame { get; private set; }

        public ComponentFinder ComponentFinder { get; private set; }
 
        public void CreateComponentMenue(MenuBar menuBar)
        {
            foreach (ComponentFactoryInformation cfi in ComponentFinder.ComponentInfos)
            {
                // the last name is the menu name, all other are menu/sub-menue names
                String [] m = cfi.MenuPath.Split(new char[] {'\\'}, StringSplitOptions.RemoveEmptyEntries);
                
                // as a minimum submenu-name & menu-name must exist
                Debug.Assert(m.Length >= 2);
                
                MenuShell menuShell = menuBar;
                Menu componentMenu;
                System.Collections.IEnumerable children = menuBar.Children;
                for (int i = 0; i < m.Length - 1;i++)
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
        
        void ComponentHandleActivated(object sender, EventArgs e)
        {
            TaggedImageMenuItem menueitem = sender as TaggedImageMenuItem;
            ComponentFactoryInformation cfi = menueitem.Tag as ComponentFactoryInformation;
            
            String name;
            
            if (cfi.IsSingleInstance)
            {
                // show/hide existing components instance
                name = cfi.ComponentType.ToString();
                DockItem item = DockFrame.GetItem (name);
                if (item != null)
                {
                    item.Visible = !item.Visible;
                    return;
                }
            }
            else
            {
                int instance = 1;
                do
                {
                    name = cfi.ComponentType.ToString() + instance.ToString();
                    instance++;
                }
                while(DockFrame.GetItem(name) != null);
            }
            
            // add new instance of desired component
            Widget w = cfi.CreateInstance (this);
            if (w != null)
            {
                DockItem item = DockFrame.AddItem (name);
                item.Content = w;
                item.Behavior = DockItemBehavior.Normal;
                // item.DefaultLocation = "Document";
                item.DefaultVisible = true;
                item.DrawFrame = true;
                item.Label = name;
                item.Visible = true;
                
                // todo: think about on off
                if (item.Content is IComponent)
                    (item.Content as IComponent).ComponentsRegistered(item);
                
            }
        }
        
        Menu SearchOrCreateMenu(String name, MenuShell menuShell, System.Collections.IEnumerable children)
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


        // todo: should be a part of power up/down concept to be defined detailed
        public void ComponentsRegeistered()
        {
            // tell all components about end of component registering
            foreach (DockItem item in DockFrame.GetItems())
            {
                if (item.Content is IComponent)
                    (item.Content as IComponent).ComponentsRegistered (item);
            }
        }

    }

    class TaggedImageMenuItem :ImageMenuItem
    {
        public TaggedImageMenuItem(String name) : base(name) {}
        public System.Object Tag { get; set; }
    }
}

