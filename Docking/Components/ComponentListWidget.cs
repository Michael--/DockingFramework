using System;

namespace Docking.Components
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ComponentListWidget : Gtk.Bin
	{
        public ComponentListWidget (IMainWindow main)
		{
            mainWindow = main;
			this.Build ();

			Gtk.TreeViewColumn componentColumn = new Gtk.TreeViewColumn ();
			componentColumn.Title = "Component";
			
			// Create a column for the song title
			Gtk.TreeViewColumn descriptionColumn = new Gtk.TreeViewColumn ();
			descriptionColumn.Title = "Description";
			
			// Add the columns to the TreeView
			treeview1.AppendColumn (componentColumn);
			treeview1.AppendColumn (descriptionColumn);
			
			// Create a model that will hold two strings
			Gtk.ListStore listStore = new Gtk.ListStore (typeof (string), typeof (string));
			
			// Assign the model to the TreeView
			treeview1.Model = listStore;
		}

        IMainWindow mainWindow { get; set; }
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

