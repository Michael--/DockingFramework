using System;
using Gtk;
using Docking;
using System.IO;
using DockingTest;
using Docking.Components;
using System.Diagnostics;
using System.Xml;

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
        LoadConfiguration();

        // select current layout, multiple layout are allowed
        theDockFrame.CurrentLayout = "Default";

        // after layout has been set, call component initialization
        // any component could load its persistence data now
        mManager.ComponentLoaded();
    }

    private void LoadConfiguration()
    {
        // layout from file or new
        if (File.Exists (mConfig))
        {
            // the manager hold the persistence in memory all the time
            mManager.XmlDocument.Load(mConfig);

            // load XML node "layouts" in a memory file
            // we should let the implementation of the Mono Develop Docking as it is
            // to make it easier to update with newest version

            XmlNode layouts = mManager.XmlDocument.SelectSingleNode("layouts");
            
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xmlWriter = new XmlTextWriter(ms, System.Text.Encoding.UTF8);
            
            layouts.WriteTo(xmlWriter);
            xmlWriter.Flush();
            XmlReader xmlReader = new XmlTextReader(new MemoryStream(ms.ToArray()));
            
            theDockFrame.LoadLayouts (xmlReader);
        } 
        else
        {
            theDockFrame.CreateLayout ("Default", true);
        }
    }

    private void SaveConfiguration()
    {
        // save first DockFrame persistence in own (memory) file
        MemoryStream ms = new MemoryStream();
        XmlTextWriter xmlWriter = new XmlTextWriter(ms, System.Text.Encoding.UTF8);
        theDockFrame.SaveLayouts (xmlWriter);
        xmlWriter.Flush();
        XmlReader xmlReader = new XmlTextReader(new MemoryStream(ms.ToArray()));

        // re-load as XmlDocument
        XmlDocument doc = new XmlDocument();
        doc.Load(xmlReader);

        // select layouts and replace in managed persistence
        // note that a node from other document must imported before use for add/replace
        XmlNode layouts = doc.SelectSingleNode("layouts");
        XmlNode newLayouts = mManager.XmlDocument.ImportNode(layouts, true);
        XmlNode oldLayouts = mManager.XmlDocument.SelectSingleNode("layouts");
        mManager.XmlDocument.ReplaceChild(newLayouts, oldLayouts);

        // at least save complete persistence to file
        mManager.XmlDocument.Save(mConfig);
    }

    protected void OnDeleteEvent (object sender, DeleteEventArgs a)
    {
        SaveConfiguration();
        Application.Quit();
        a.RetVal = true;
    }

    protected void OnQuitActionActivated(object sender, EventArgs e)
    {
        // todo: close window which will call OnDeleteEvent() above. Don't know how to do at the moement 
        SaveConfiguration();
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
