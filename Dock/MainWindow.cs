using System;
using Gtk;
using Docking;
using System.IO;
using DockingTest;
using Docking.Components;

public partial class MainWindow: Gtk.Window, IMainWindow
{	
    // DockFrame mDockFrame;
	ComponentFinder mFinder;
    String mConfig = "TestHow2Dock-config.layout.xml";

    public MainWindow (): base (Gtk.WindowType.Toplevel)
	{
		mFinder = new ComponentFinder ();
		mFinder.SearchForComponents (new string[] { @"./*.exe", @"./*.dll" } );

		// todo: should re-load from persistence
		SetSizeRequest (800, 600);

		// Create designer elements
		Build ();

		theDockFrame.DefaultItemHeight = 100;
		theDockFrame.DefaultItemWidth = 100;
		theDockFrame.Homogeneous = false;

#if false
		// add elements programmatically

		Gtk.Notebook nb = new Notebook ();
		DockItem doc_item = AddSimpleDockItem ("Document", nb, null);
		doc_item.Expand = true;
		nb.AppendPage (new Label ("Other page"), new Label ("The label"));
		nb.AppendPage (new TextView (), new Image ("gtk-new", IconSize.Menu));
		nb.ShowAll ();
        
		// See enum DockPosition
		AddSimpleDockItem ("Test1", new Label ("This test"), "Document/Left");
		AddSimpleDockItem ("Test2", new Label ("This is a test"), "Document/Right");
		AddSimpleDockItem ("Test3", new Label ("This is a test"), "right/Bottom");

#endif

#if true
		// Add widget created with designer
		foreach (ComponentFinder.ComponentFactoryInformation cfi in mFinder.ComponentInfos)
		{
			Widget w = cfi.CreateInstance(this);
			if (w != null)
			{
				DockItem item = theDockFrame.AddItem(w.ToString());
				item.Content = w;
				item.Behavior = DockItemBehavior.Normal;
				// item.DefaultLocation = "Document";
				item.DefaultVisible = true;
				item.DrawFrame = true;
				item.Label = w.ToString();
			}
		}
#endif

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
    
	DockItem AddSimpleDockItem (String label, Widget content, String location)
    {
        DockItem item = theDockFrame.AddItem (label);
        item.Behavior = DockItemBehavior.Normal;
		if (location != null)
        	item.DefaultLocation = location;
        item.DefaultVisible = true;
        item.Visible = true;
        item.Label = label;
        item.DrawFrame = true;
        item.Content = content;
		return item;
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
