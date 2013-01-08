using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Docking.Components
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ComponentListWidget : Gtk.Bin, IComponent
	{
        #region implement IComponent
        public ComponentManager ComponentManager { get; set; }

        void IComponent.Loaded(DockItem item)
        {
            item.Label = "Component List";
            foreach (ComponentFactoryInformation cfi in ComponentManager.ComponentFinder.ComponentInfos)
            {
                List<String> row = new List<string>();
                row.Add(cfi.ComponentType.ToString());
                row.Add(cfi.Comment);
                listStore.AppendValues(row.ToArray());
            }

            TestPersistence p = (TestPersistence)ComponentManager.LoadObject("ComponentListWidget", typeof(TestPersistence));
            if (p != null)
                ComponentManager.MessageWriteLine(String.Format("Test Persistence loaded: {0}", p.test));
        }

        void IComponent.Save()
        {
            TestPersistence p =  new TestPersistence() { test = "TestTestTest" };
            ComponentManager.SaveObject("ComponentListWidget", p);
        }

        public class TestPersistence 
        {
            public String test { get; set; }
        }

        #endregion

        public ComponentListWidget ()
		{
			this.Build ();

			Gtk.TreeViewColumn componentColumn = new Gtk.TreeViewColumn ();
			componentColumn.Title = "Component";
            componentColumn.Resizable = true;
			
			// Create a column for the song title
			Gtk.TreeViewColumn descriptionColumn = new Gtk.TreeViewColumn ();
			descriptionColumn.Title = "Description";
            descriptionColumn.Resizable = true;
			
			// Add the columns to the TreeView
			treeview1.AppendColumn (componentColumn);
			treeview1.AppendColumn (descriptionColumn);
         

            // Create the text cells that will display the content
            Gtk.CellRendererText componentsCell = new Gtk.CellRendererText ();
            componentColumn.PackStart (componentsCell, true);
            Gtk.CellRendererText descriptionCell = new Gtk.CellRendererText ();
            descriptionColumn.PackStart (descriptionCell, true);
            componentColumn.AddAttribute (componentsCell, "text", 0);
            descriptionColumn.AddAttribute (descriptionCell, "text", 1);
			
			// Create a model that will hold two strings, Assign the model to the TreeView
            listStore = new Gtk.ListStore (typeof (string), typeof (string));
            treeview1.Model = listStore;

            treeview1.CursorChanged += HandleCursorChanged;
		}

        void HandleCursorChanged (object sender, EventArgs e)
        {
            Gtk.TreeSelection selection = (sender as Gtk.TreeView).Selection;
           
            Gtk.TreeModel model;
            Gtk.TreeIter iter;

            // THE ITER WILL POINT TO THE SELECTED ROW
            if(selection.GetSelected(out model, out iter))
            {
                String msg = String.Format ("Selected Value: {0} {1}", model.GetValue (iter, 0).ToString(), model.GetValue (iter, 1).ToString());
                Console.WriteLine(msg);
                ComponentManager.MessageWriteLine(msg);
            }
        }

        Gtk.ListStore listStore;
	}
	
    #region Starter / Entry Point

	public class ComponentListWidgetFactory : ComponentFactory
	{
		public override Type TypeOfInstance { get { return typeof(ComponentListWidget); } }
        public override String MenuPath { get { return @"Components\ComponentListWidget"; } }
		public override String Comment { get { return "Display a list of all found widgets"; } }
	}
	
    #endregion
}

