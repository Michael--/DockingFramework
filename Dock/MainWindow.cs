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
        // this.DeleteEvent += new DeleteEventHandler(this.OnDeleteEvent);
        
        SetSizeRequest (800, 600);
        
        df = new DockFrame ();
        df.DefaultItemHeight = 100;
        df.DefaultItemWidth = 100;
        df.Homogeneous = false;
        
        Add (df);
        
        DockItem doc_item = df.AddItem ("Document");
        doc_item.Behavior = DockItemBehavior.Normal;
        doc_item.Expand = true;
        doc_item.DrawFrame = false;
        doc_item.Label = "Documentos";
        Gtk.Notebook nb = new Notebook ();
        nb.AppendPage (new Label ("Other page"), new Label ("The label"));
        nb.AppendPage (new TextView (), new Image ("gtk-new", IconSize.Menu));
        //      nb.AppendPage( new TextEditor(), new Label( "Editor" ) );
        
        nb.ShowAll ();
        doc_item.Content = nb;
        doc_item.DefaultVisible = true;
        doc_item.Visible = true;
        
        // See enum DockPosition
        AddSimpleDockItem("left", "This is a test", "Document/Left");
        AddSimpleDockItem("right", "Content", "Document/Right");
        AddSimpleDockItem("right_bottom", "Hello", "right/Bottom");
        // AddSimpleDockItem("qwe", "qwe", "right/Bottom");


#if true
        DockItem testWidget = df.AddItem("testWidget");
        testWidget.Behavior = DockItemBehavior.CantClose;
        testWidget.DefaultLocation = "right/Bottom";
        testWidget.DefaultVisible = true;
        testWidget.DrawFrame = true;
        testWidget.Label = "TestWidget";
        testWidget.Content = new TestWidget(df);
#endif
        
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
        
        
        Build ();
        // ShowAll();
        
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
