using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Docking.Components
{
    // todo: currently only available components are simply displayed
    //       - Display more details of each ComponentFactory
    //       - Display also information about existing instances
    //       - Add actions, like create/hide/show/erase
	[System.ComponentModel.ToolboxItem(false)]
	public partial class ComponentList : Gtk.Bin, IComponent, IComponentInteract
	{
        #region implement IComponent
        public ComponentManager ComponentManager { get; set; }

        void IComponent.Loaded(DockItem item)
        {
            item.Label = "Component List";

            foreach (ComponentFactoryInformation cfi in ComponentManager.ComponentFinder.ComponentInfos)
            {
                List<object> row = new List<object>();
                row.Add(cfi);
                row.Add(0);
                row.Add(cfi.ComponentType.ToString());
                row.Add(cfi.Comment);
                listStore.AppendValues(row.ToArray());
            }

            Persistence p = (Persistence)ComponentManager.LoadObject("ComponentList", typeof(Persistence));
            if (p != null)
                p.LoadColumnWidth(treeview1.Columns);
        }

        void IComponent.Save()
        {
            Persistence p = new Persistence();
            p.SaveColumnWidth(treeview1.Columns);
            ComponentManager.SaveObject("ComponentList", p);
        }
        #endregion

        #region implement IComponentInteract
        void IComponentInteract.Added(object item)
        {
            if (item is IProperty)
                mPropertyInterfaces.Add(item as IProperty);

            ChangeInstanceCount(item, 1);
        }

        void IComponentInteract.Removed(object item)
        {
            if (item is IProperty)
                mPropertyInterfaces.Remove(item as IProperty);

            ChangeInstanceCount(item, -1);
        }

        void IComponentInteract.Visible(object item, bool visible)
        {
        }

        void IComponentInteract.Current(object item)
        {
            if (this == item)
            {
                //foreach(IProperty it in mPropertyInterfaces)
                //    it.SetObject(the property object);
            }
        }

        #endregion

        public ComponentList ()
		{
			this.Build ();
            this.Name = "Component List";

            Gtk.TreeViewColumn componentColumn = new Gtk.TreeViewColumn ();
            componentColumn.Title = "Component";
            componentColumn.Resizable = true;
            componentColumn.Sizing = Gtk.TreeViewColumnSizing.Fixed;
            componentColumn.FixedWidth = 200;

            Gtk.TreeViewColumn instanceCountColumn = new Gtk.TreeViewColumn ();
            instanceCountColumn.Title = "Instances";
            instanceCountColumn.Resizable = true;
            instanceCountColumn.Sizing = Gtk.TreeViewColumnSizing.Fixed;
            instanceCountColumn.FixedWidth = 50;

			Gtk.TreeViewColumn descriptionColumn = new Gtk.TreeViewColumn ();
			descriptionColumn.Title = "Description";
            descriptionColumn.Resizable = true;
            descriptionColumn.Sizing = Gtk.TreeViewColumnSizing.Fixed;
            descriptionColumn.FixedWidth =  300;


			// Add the columns to the TreeView
            treeview1.AppendColumn (instanceCountColumn);
            treeview1.AppendColumn (componentColumn);
            treeview1.AppendColumn (descriptionColumn);

            // Create the text cells that will display the content
            Gtk.CellRendererText componentsCell = new Gtk.CellRendererText ();
            componentColumn.PackStart (componentsCell, true);

            Gtk.CellRendererText instanceCountCell = new Gtk.CellRendererText ();
            instanceCountColumn.PackStart (instanceCountCell, true);

            Gtk.CellRendererText descriptionCell = new Gtk.CellRendererText ();
            descriptionColumn.PackStart (descriptionCell, true);

            componentColumn.AddAttribute (componentsCell, "text", TypenameIndex);
            instanceCountColumn.AddAttribute (instanceCountCell, "text", InstanceCountIndex);
            descriptionColumn.AddAttribute (descriptionCell, "text", DescriptionIndex);

			// Create a model that will hold some value, assign the model to the TreeView
            listStore = new Gtk.ListStore (typeof(ComponentFactoryInformation), typeof(int), typeof (string), typeof (string));
            treeview1.Model = listStore;

            treeview1.CursorChanged += HandleCursorChanged;
		}

        void ChangeInstanceCount(object item, int dcount)
        {
            Gtk.TreeIter iter;
            if(item==null || !treeview1.Model.GetIterFirst (out iter))
                return;
            do
            {
                ComponentFactoryInformation cfi = treeview1.Model.GetValue(iter, CFIIndex) as ComponentFactoryInformation;
                if (cfi.ComponentType == item.GetType())
                {
                    object str = treeview1.Model.GetValue(iter, InstanceCountIndex);// as string;
                    int count = Convert.ToInt32(str);
                    count += dcount;
                    treeview1.Model.SetValue (iter, InstanceCountIndex, count);
                }
            }
            while (treeview1.Model.IterNext(ref iter));
        }

        void HandleCursorChanged (object sender, EventArgs e)
        {
            Gtk.TreeSelection selection = (sender as Gtk.TreeView).Selection;

            Gtk.TreeModel model;
            Gtk.TreeIter iter;

            // THE ITER WILL POINT TO THE SELECTED ROW
            if(selection.GetSelected(out model, out iter))
            {
			/*
                String msg = String.Format ("Selected Value:[{0}] {1} {2}",
                    model.GetValue(iter, InstanceCountIndex),
                    model.GetValue(iter, TypenameIndex).ToString(),
                    model.GetValue(iter, DescriptionIndex).ToString());
                Console.WriteLine(msg);
                ComponentManager.MessageWriteLine(msg);
            */
            }
        }

        Gtk.ListStore listStore;
        const int CFIIndex = 0;
        const int InstanceCountIndex = 1;
        const int TypenameIndex = 2;
        const int DescriptionIndex = 3;


        List<IProperty> mPropertyInterfaces = new List<IProperty>();
	}


    [Serializable()]
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
                    columns[i].FixedWidth = Math.Max(20, m_Width[i]);
            }
        }

        // to have a simple persistence make the member public
        // otherwise you have to implement IXmlSerializable
        public List<int> m_Width = new List<int>();
    }


    #region Starter / Entry Point

	public class ComponentListFactory : ComponentFactory
	{
		public override Type TypeOfInstance { get { return typeof(ComponentList); } }
        public override String MenuPath { get { return @"View\Infrastructure\Component List"; } }
		public override String Comment { get { return "displays a list of all components"; } }
        public override Gdk.Pixbuf Icon { get { return Gdk.Pixbuf.LoadFromResource ("Docking.Framework.Components.ComponentList-16.png"); } }
    }

    #endregion
}

