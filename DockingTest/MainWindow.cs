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
    ComponentManager IMainWindow.ComponentManager { get { return mManager; } }
    #endregion

    ComponentManager mManager;
    String mConfig = "TestHow2Dock-config.layout.xml";

    public MainWindow (): base (Gtk.WindowType.Toplevel)
    {

        // Create designer elements
        Build ();
        theDockFrame.DefaultItemHeight = 100;
        theDockFrame.DefaultItemWidth = 100;
        theDockFrame.Homogeneous = false;

        mManager = new ComponentManager(this, theDockFrame);

        // search for all interrested components
        mManager.ComponentFinder.SearchForComponents (new string[] { @"./*.exe", @"./*.dll" });

        // add all default menu for any component
        mManager.CreateComponentMenue(menubar3);

        // tell all components about end of component registering
        mManager.ComponentsRegeistered();

        // layout from file or new
        // todo: init instances from config, update onoff concept soon
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
