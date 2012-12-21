using System;
using Gtk;
using MonoDevelop.Components.Docking;
using System.IO;
using Dock;
using Docking;

public partial class MainWindow: Gtk.Window
{	
    DockFrame mDockFrame;
	ComponentFinder mFinder;
    String mConfig = "TestHow2Dock-config.layout.xml";

    public MainWindow (): base (Gtk.WindowType.Toplevel)
	{
		mFinder = new ComponentFinder ();
		mFinder.SearchForComponents (@".");

		// todo: should re-load from persistence
		SetSizeRequest (800, 600);

		// Create designer elements
		Build ();

		// add elements programmatically
		mDockFrame = this.theDockFrame;
		mDockFrame.DefaultItemHeight = 100;
		mDockFrame.DefaultItemWidth = 100;
		mDockFrame.Homogeneous = false;

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

		// Add widget created with designer
		foreach (ComponentFinder.ComponentFactoryInformation cfi in mFinder.ComponentInfos)
		{
			Widget w = cfi.CreateInstance(mDockFrame);
			if (w != null)
			{
				DockItem testWidget = mDockFrame.AddItem("testWidget");
				testWidget.Behavior = DockItemBehavior.Normal;
				testWidget.DefaultLocation = "right/Bottom";
				testWidget.DefaultVisible = true;
				testWidget.DrawFrame = true;
				testWidget.Label = "TestWidget";
				// testWidget.Content = new TestWidget(mDockFrame);
				testWidget.Content = w;
			}
		}


        // layout from file or new
        if (File.Exists (mConfig))
        {
            mDockFrame.LoadLayouts (mConfig);
        } 
        else
        {
            mDockFrame.CreateLayout ("test", true);
        }
        mDockFrame.CurrentLayout = "test";
    }
    
	DockItem AddSimpleDockItem (String label, Widget content, String location)
    {
        DockItem item = mDockFrame.AddItem (label);
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
        mDockFrame.SaveLayouts(mConfig);
        Application.Quit();
        a.RetVal = true;
    }

    protected void OnQuitActionActivated(object sender, EventArgs e)
    {
        // todo: close window which will call OnDeleteEvent() above. Don't know how to do at the moement 
        mDockFrame.SaveLayouts(mConfig);
        Application.Quit();
    }

    protected void OnUndoActionActivated(object sender, EventArgs e)
    {
        foreach (DockItem item in mDockFrame.GetItems()) 
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
