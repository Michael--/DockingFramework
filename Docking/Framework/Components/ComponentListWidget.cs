using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Docking.Components
{
    // todo: currently only available components are simply displayed
    //       - Display more details of each ComponentFactory
    //       - Display also information about existing instances
    //       - Add actions, like create/hide/show/erase
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

            Persistence p = (Persistence)ComponentManager.LoadObject("ComponentListWidget", typeof(Persistence));
            if (p != null)
                p.LoadColumnWidth(treeview1.Columns);
        }

        void IComponent.Save()
        {
            Persistence p = new Persistence();
            p.SaveColumnWidth(treeview1.Columns);
            ComponentManager.SaveObject("ComponentListWidget", p);
        }

        public class Persistence 
        {
            public void SaveColumnWidth(Gtk.TreeViewColumn []columns)
            {
                foreach (Gtk.TreeViewColumn c in columns)
                    m_Width.Add(c.Width);
            }

            public void LoadColumnWidth(Gtk.TreeViewColumn []columns)
            {
                if (columns.Length == m_Width.Count)
                {
                    for (int i = 0; i < columns.Length; i++)
                        columns[i].FixedWidth = m_Width[i];
                }
            }
        
            // to have a simple persistence make the member public
            // otherwise you have to implement IXmlSerializable
            public List<int> m_Width = new List<int>(); 
        }

        #endregion

        public ComponentListWidget ()
		{
			this.Build ();
            this.Name = "Component List";

			Gtk.TreeViewColumn componentColumn = new Gtk.TreeViewColumn ();
			componentColumn.Title = "Component";
            componentColumn.Resizable = true;
            componentColumn.Sizing = Gtk.TreeViewColumnSizing.Fixed;
			
			Gtk.TreeViewColumn descriptionColumn = new Gtk.TreeViewColumn ();
			descriptionColumn.Title = "Description";
            descriptionColumn.Resizable = true;
            descriptionColumn.Sizing = Gtk.TreeViewColumnSizing.Fixed;
			
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

