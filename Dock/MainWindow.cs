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

        // Add widget created with designer
        foreach (ComponentFactoryInformation cfi in mFinder.ComponentInfos)
        {
            Widget w = cfi.CreateInstance (this);
            if (w != null)
            {
                DockItem item = theDockFrame.AddItem (w.ToString ());
                item.Content = w;
                item.Behavior = DockItemBehavior.Normal;
                // item.DefaultLocation = "Document";
                item.DefaultVisible = true;
                item.DrawFrame = true;
                item.Label = w.ToString ();
            }
        }

        CreateComponentMenue();

        foreach (DockItem item  in theDockFrame.GetItems())
        {
            if (item.Content is IComponent)
                (item.Content as IComponent).ComponentsRegistered();
        }

        // layout from file or new
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
            String [] m = cfi.MenuPath.Split(new char[] {'\\'}, StringSplitOptions.RemoveEmptyEntries);
            Debug.Assert(m.Length >= 2);

            System.Object baseMenu = menubar3;
            Menu componentMenu = null;
            System.Collections.IEnumerable children = menubar3.Children;
            for (int i = 0; i < m.Length - 1;i++)
            {
                componentMenu = SearchOrCreateMenu(m[i], baseMenu, children);
                children = componentMenu.AllChildren;
                baseMenu = componentMenu;
            }
            ImageMenuItem item = new ImageMenuItem(m[m.Length - 1]);
            componentMenu.Add(item);
        }
        menubar3.ShowAll();
    }

    Menu SearchOrCreateMenu(String name, System.Object baseMenu, System.Collections.IEnumerable children)
    {
        Menu menu = null;
        
        // search menue to insert new element
        foreach(MenuItem mi in children)
        {
            Label label = (Label)mi.Child;
            if (label.Text == name)
            {
                menu = mi.Submenu as Menu;
                break;
            }
        }
        
        if (menu == null)
        {
            menu = new Menu ( );
            MenuItem menuItem = new MenuItem(name);
            menuItem.Child = new Label(name);
            menuItem.Submenu = menu;

            if (baseMenu is MenuBar)
                (baseMenu as MenuBar).Append (menuItem);
            else 
                (baseMenu as Menu).Append(menuItem);
        }

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
}
