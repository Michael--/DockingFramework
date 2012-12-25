using System;
using Gtk;
using Docking;
using System.IO;
using DockingTest;
using Docking.Components;
using System.Diagnostics;

public partial class MainWindow: Gtk.Window, IMainWindow
{	
    #region implement IMainWindow
    DockFrame IMainWindow.DockFrame { get { return theDockFrame; } }
    ComponentFactoryInformation[] IMainWindow.ComponentInfos { get { return mFinder.ComponentInfos; } }
    #endregion

	ComponentFinder mFinder;
    String mConfig = "TestHow2Dock-config.layout.xml";

    public MainWindow (): base (Gtk.WindowType.Toplevel)
    {
        mFinder = new ComponentFinder ();
        mFinder.SearchForComponents (new string[] { @"./*.exe", @"./*.dll" });

        // todo: should re-load from persistence
        SetSizeRequest (800, 600);

        // Create designer elements
        Build ();

        theDockFrame.DefaultItemHeight = 100;
        theDockFrame.DefaultItemWidth = 100;
        theDockFrame.Homogeneous = false;

        // add all default menu for any component
        CreateComponentMenue();

        // tell all components about end of component registering
        foreach (DockItem item  in theDockFrame.GetItems())
        {
            if (item.Content is IComponent)
                (item.Content as IComponent).ComponentsRegistered(item);
        }

        // layout from file or new
        // todo: init instances from config
        if (File.Exists (mConfig))
        {
            theDockFrame.LoadLayouts (mConfig);
        } 
        else
        {
            theDockFrame.CreateLayout ("test", true);
		}
        theDockFrame.CurrentLayout = "test";
	}

    void CreateComponentMenue()
    {
        foreach (ComponentFactoryInformation cfi in mFinder.ComponentInfos)
        {
            // the last name is the menu name, all other are menu/sub-menue names
            String [] m = cfi.MenuPath.Split(new char[] {'\\'}, StringSplitOptions.RemoveEmptyEntries);

            // as a minimum submenu-name & menu-name must exist
            Debug.Assert(m.Length >= 2);

            MenuShell menuShell = menubar3;
            Menu componentMenu;
            System.Collections.IEnumerable children = menubar3.Children;
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
        menubar3.ShowAll();
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
            DockItem item = theDockFrame.GetItem (name);
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
            while(theDockFrame.GetItem(name) != null);
        }

        // add new instance of desired component
        Widget w = cfi.CreateInstance (this);
        if (w != null)
        {
            DockItem item = theDockFrame.AddItem (name);
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

    protected void OnDeleteEvent (object sender, DeleteEventArgs a)
    {
        theDockFrame.SaveLayouts(mConfig);
        Application.Quit();
        a.RetVal = true;
    }

    protected void OnQuitActionActivated(object sender, EventArgs e)
    {
        // todo: close window which will call OnDeleteEvent() above. Don't know how to do at the moement 
        theDockFrame.SaveLayouts(mConfig);
        Application.Quit();
    }

    protected void OnUndoActionActivated(object sender, EventArgs e)
    {
        foreach (DockItem item in theDockFrame.GetItems()) 
        {
            if (!item.Visible && item.Label.Length > 0)
                item.Visible = true;
        }
    }

	int mTextCounter = 0;
	uint mUniqueId = 0;
	protected void OnAddActionActivated (object sender, EventArgs e)
	{
		// push simple message with its 'unique' context id
		String text = String.Format("Hello {0} at {1}", ++mTextCounter, DateTime.Now.ToLongTimeString());
		this.statusbar1.Push(++mUniqueId, text);
	}

	protected void OnRemoveActionActivated (object sender, EventArgs e)
	{
		if (mUniqueId > 0)
			this.statusbar1.Pop(mUniqueId--);
	}


    class TaggedImageMenuItem :ImageMenuItem
    {
        public TaggedImageMenuItem(String name) : base(name) {}
        public System.Object Tag { get; set; }
    }
}
