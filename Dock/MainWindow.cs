using System;
using Gtk;
using MonoDevelop.Components.Docking;
using System.IO;
using Dock;

public partial class MainWindow: Gtk.Window
{	
    DockFrame df;
    String config = "TestHow2Dock-config.layout.xml";

    public MainWindow(): base (Gtk.WindowType.Toplevel)
    {
        // todo: should re-load from persistence
        SetSizeRequest (800, 600);

        // Create designer elements
        Build ();

        // add elements programmatically
        df = this.theDockFrame;
        df.DefaultItemHeight = 100;
        df.DefaultItemWidth = 100;
        df.Homogeneous = false;

        DockItem doc_item = df.AddItem ("Document");
        doc_item.Behavior = DockItemBehavior.Normal;
        doc_item.Expand = true;
        doc_item.DrawFrame = false;
        doc_item.Label = "Documents";
        Gtk.Notebook nb = new Notebook ();
        nb.AppendPage (new Label ("Other page"), new Label ("The label"));
        nb.AppendPage (new TextView (), new Image ("gtk-new", IconSize.Menu));

        nb.ShowAll ();
        doc_item.Content = nb;
        doc_item.DefaultVisible = true;
        doc_item.Visible = true;
        
        // See enum DockPosition
        AddSimpleDockItem("Test1", "This is a test", "Document/Left");
		AddSimpleDockItem("Test2", "This is a test", "Document/Right");
		AddSimpleDockItem("Test3", "This is a test", "right/Bottom");

        // Add widget created with designer
        DockItem testWidget = df.AddItem("testWidget");
        testWidget.Behavior = DockItemBehavior.Normal;
        testWidget.DefaultLocation = "right/Bottom";
        testWidget.DefaultVisible = true;
        testWidget.DrawFrame = true;
        testWidget.Label = "TestWidget";
        testWidget.Content = new TestWidget(df);
		     

        // layout from file or new
        if (File.Exists (config))
        {
            df.LoadLayouts (config);
        } 
        else
        {
            df.CreateLayout ("test", true);
        }
        df.CurrentLayout = "test";
    }
    
    void AddSimpleDockItem (String label, String content, String location)
    {
        DockItem item = df.AddItem (label);
        item.Behavior = DockItemBehavior.Normal;
        item.DefaultLocation = location;
        item.DefaultVisible = true;
        item.Visible = true;
        item.Label = label;
        item.DrawFrame = true;
        item.Content = new Label (content);
    }
    
    protected void OnDeleteEvent (object sender, DeleteEventArgs a)
    {
        df.SaveLayouts(config);
        Application.Quit();
        a.RetVal = true;
    }

    protected void OnQuitActionActivated(object sender, EventArgs e)
    {
        // todo: close window which will call OnDeleteEvent() above. Don't know how to do at the moement 
        df.SaveLayouts(config);
        Application.Quit();
    }

    protected void OnUndoActionActivated(object sender, EventArgs e)
    {
        foreach (DockItem item in df.GetItems()) 
        {
            if (!item.Visible && item.Label.Length > 0)
                item.Visible = true;
        }
    }

	int counter = 0;
	uint id = 0;
	protected void OnAddActionActivated (object sender, EventArgs e)
	{
		// push simple message with its 'unique' context id
		String text = String.Format("Hello {0} at {1}", ++counter, DateTime.Now.ToLongTimeString());
		this.statusbar1.Push(++id, text);
	}

	protected void OnRemoveActionActivated (object sender, EventArgs e)
	{
		if (id > 0)
			this.statusbar1.Pop(id--);
	}

}
