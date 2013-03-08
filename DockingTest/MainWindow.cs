using System;
using Gtk;
using Docking;
using System.IO;
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
        SetMenuBar(menubar3);

        // search for all interrested components
        ComponentFinder.SearchForComponents (new string[] { @"./*.exe", @"./*.dll" });

        // install all known component menus
        AddComponentMenus();

        // load old configuration or init new one if not existing
        LoadConfigurationFile(mConfig);

        // update with own persistence
        LoadPersistence();

        // set default layout and add layout menu
        SetDefaultLayout("Default");

        // after layout has been set, call component initialization
        // any component could load its persistence data now
        ComponentsLoaded();
    }
}
