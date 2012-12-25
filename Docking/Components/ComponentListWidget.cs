using System;
using System.Collections.Generic;

namespace Docking.Components
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ComponentListWidget : Gtk.Bin, IComponent
	{
        #region implement IComponent
        public ComponentManager ComponentManager { get; set; }

        void IComponent.ComponentsRegistered(DockItem item)
        {
            item.Label = "Component List";
            foreach (ComponentFactoryInformation cfi in ComponentManager.ComponentFinder.ComponentInfos)
            {
                List<String> row = new List<string>();
                row.Add(cfi.ComponentType.ToString());
                row.Add(cfi.Comment);
                listStore.AppendValues(row.ToArray());
            }
        }

        #endregion

        public ComponentListWidget ()
		{
			this.Build ();

			Gtk.TreeViewColumn componentColumn = new Gtk.TreeViewColumn ();
			componentColumn.Title = "Component";
			
			// Create a column for the song title
			Gtk.TreeViewColumn descriptionColumn = new Gtk.TreeViewColumn ();
			descriptionColumn.Title = "Description";
			
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

