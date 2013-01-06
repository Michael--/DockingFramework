using System;
using Gtk;
using Docking;
using System.IO;
using DockingTest;
using Docking.Components;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;

public partial class MainWindow : ComponentManager
{	
    String mConfig = "config.xml";

    public MainWindow (): base (Gtk.WindowType.Toplevel)
    {
        // Create designer elements
        Build ();

        // tell the component manager about all widgets to manage 
        SetDockFrame(theDockFrame);
        SetStatusBar(theStatusBar);
        SetToolBar(theToolBar);

        // search for all interrested components
        ComponentFinder.SearchForComponents (new string[] { @"./*.exe", @"./*.dll" });

        // add all default menu for any component
        SetMenuBar(menubar3);

        // load old configuration or init new one if not existing
        LoadConfigurationFile(mConfig);

        // update with own persistence
        LoadPersistence();

        // select current layout, multiple layout are allowed
        theDockFrame.CurrentLayout = "Default";

        // after layout has been set, call component initialization
        // any component could load its persistence data now
        ComponentsLoaded();
    }

    private void LoadPersistence()
    {
        MainWindowPersistence p = (MainWindowPersistence)LoadObject ("MainWindow", typeof(MainWindowPersistence));
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

        SaveObject("MainWindow", p);
    }

    private void PrepareExit()
    {
        // update own persistence before save configuration
        SavePersistence();
        SaveConfigurationFile(mConfig);
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

}

public class MainWindowPersistence
{
    public int WindowX { get; set; }
    public int WindowY { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

