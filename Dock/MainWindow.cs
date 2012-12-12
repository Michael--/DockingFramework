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
        df = new DockFrame ();
        df.DefaultItemHeight = 100;
        df.DefaultItemWidth = 100;
        df.Homogeneous = false;

        DockItem doc_item = df.AddItem ("Document");
        doc_item.Behavior = DockItemBehavior.Normal;
        doc_item.Expand = true;
        doc_item.DrawFrame = false;
        doc_item.Label = "Documentos";
        Gtk.Notebook nb = new Notebook ();
        nb.AppendPage (new Label ("Other page"), new Label ("The label"));
        nb.AppendPage (new TextView (), new Image ("gtk-new", IconSize.Menu));

        nb.ShowAll ();
        doc_item.Content = nb;
        doc_item.DefaultVisible = true;
        doc_item.Visible = true;
        
        // See enum DockPosition
        AddSimpleDockItem("left", "This is a test", "Document/Left");
        AddSimpleDockItem("right", "Content", "Document/Right");
        AddSimpleDockItem("right_bottom", "Hello", "right/Bottom");

        // Add widget created with designer
        DockItem testWidget = df.AddItem("testWidget");
        testWidget.Behavior = DockItemBehavior.CantClose;
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
        df.HandlePadding = 0;
        df.HandleSize = 10;        

        // add to the vertical box in the lowest position and redraw
        vbox1.Add(df);
        // Box.BoxChild bc = (Box.BoxChild)this.vbox1[df];
        // bc.Position = vbox1.Children.Length - 1;

        if (this.Child != null) 
            this.Child.ShowAll ();
               
        // (test) workaround to make test widget visable in all cases
        if (!testWidget.Visible)
            testWidget.Visible = true;
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
}
