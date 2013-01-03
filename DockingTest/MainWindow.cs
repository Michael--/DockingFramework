using System;
using Gtk;
using Docking;
using System.IO;
using DockingTest;
using Docking.Components;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

public partial class MainWindow: Gtk.Window
{	
    ComponentManager mManager;
    String mConfig = "config.xml";

    public MainWindow (): base (Gtk.WindowType.Toplevel)
    {
        // Create designer elements
        Build ();

        mManager = new ComponentManager(theDockFrame);

        // search for all interrested components
        mManager.ComponentFinder.SearchForComponents (new string[] { @"./*.exe", @"./*.dll" });

        // add all default menu for any component
        mManager.CreateComponentMenue(menubar3);

        // load old configuration or init new one if not existing
        mManager.LoadConfigurationFile(mConfig);

        // update with own persistence
        LoadPersistence();

        // select current layout, multiple layout are allowed
        theDockFrame.CurrentLayout = "Default";

        // after layout has been set, call component initialization
        // any component could load its persistence data now
        mManager.ComponentsLoaded();
    }

    private void LoadPersistence()
    {
        MainWindowPersistence p = (MainWindowPersistence)mManager.LoadObject ("MainWindow", typeof(MainWindowPersistence));
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

        mManager.SaveObject("MainWindow", p);
    }

    private void PrepareExit()
    {
        // update own persistence before save configuration
        SavePersistence();
        mManager.SaveConfigurationFile(mConfig);
        Application.Quit();
    }

 
    protected void OnDeleteEvent (object sender, DeleteEventArgs a)
    {
        PrepareExit();
        a.RetVal = true;
    }

    protected void OnQuitActionActivated(object sender, EventArgs e)
    {
        PrepareExit();
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

public class MainWindowPersistence
{
    public int WindowX { get; set; }
    public int WindowY { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

