using System;
using System.Collections.Generic;
using System.Diagnostics;
using Docking.Widgets;

namespace Docking.Components
{
   // todo: currently only available components are simply displayed
   //       - Display more details of each ComponentFactory
   //       - Display also information about existing instances
   //       - Add actions, like create/hide/show/erase
   [System.ComponentModel.ToolboxItem(false)]
   public partial class ComponentList : Component, ILocalizableComponent
   {
      public override void Loaded(DockItem item)
      {
         base.Loaded(item);

         item.Title = "Component List";

         foreach (ComponentFactoryInformation cfi in ComponentManager.ComponentFinder.ComponentInfos)
         {
            List<object> row = new List<object>();
            row.Add(cfi);
            row.Add(0);
            row.Add(cfi.ComponentType.ToString());
            row.Add(cfi.Comment);
            listStore.AppendValues(row.ToArray());
         }

         Persistence p = (Persistence)ComponentManager.LoadObject("ComponentList", typeof(Persistence), item); // TODO early prototype - abolish, implement IPersistable instead!
         if (p != null)
            p.LoadColumnWidth(treeview1.Columns);
      }

      public override void Save() // TODO early prototype - abolish, implement IPersistable instead!
      {
         base.Save();

         Persistence p = new Persistence(); 
         p.SaveColumnWidth(treeview1.Columns);
         ComponentManager.SaveObject("ComponentList", p, DockItem);
      }

      #region Component - Interaction

      public override void ComponentAdded(object item)
      {
         base.ComponentAdded(item);
         ChangeInstanceCount(item, 1);
      }

      public override void ComponentRemoved(object item)
      {
         base.ComponentRemoved(item);
         ChangeInstanceCount(item, -1);
      }

      #endregion

      #region ILocalizableComponent

      string ILocalizableComponent.Name { get { return "Component List"; } }

      void ILocalizableComponent.LocalizationChanged(Docking.DockItem item)
      {}
      #endregion


      public ComponentList()
      {
         this.Build();
         this.Name = "Component List";

         Gtk.TreeViewColumn componentColumn     = new TreeViewColumnLocalized() { Title = "Component",   Sizing = Gtk.TreeViewColumnSizing.Fixed, FixedWidth = 200 };
         Gtk.TreeViewColumn instanceCountColumn = new TreeViewColumnLocalized() { Title = "Instances",   Sizing = Gtk.TreeViewColumnSizing.Fixed, FixedWidth =  50 };
         Gtk.TreeViewColumn descriptionColumn   = new TreeViewColumnLocalized() { Title = "Description", Sizing = Gtk.TreeViewColumnSizing.Fixed, FixedWidth = 300 };

         // Add the columns to the TreeView
         treeview1.AppendColumn(instanceCountColumn);
         treeview1.AppendColumn(componentColumn);
         treeview1.AppendColumn(descriptionColumn);

         // Create the text cells that will display the content
         Gtk.CellRendererText componentsCell = new Gtk.CellRendererText();
         componentColumn.PackStart(componentsCell, true);

         Gtk.CellRendererText instanceCountCell = new Gtk.CellRendererText();
         instanceCountColumn.PackStart(instanceCountCell, true);

         Gtk.CellRendererText descriptionCell = new Gtk.CellRendererText();
         descriptionColumn.PackStart(descriptionCell, true);

         componentColumn.AddAttribute(componentsCell, "text", TypenameIndex);
         instanceCountColumn.AddAttribute(instanceCountCell, "text", InstanceCountIndex);
         descriptionColumn.AddAttribute(descriptionCell, "text", DescriptionIndex);

         // Create a model that will hold some value, assign the model to the TreeView
         listStore = new Gtk.ListStore(typeof(ComponentFactoryInformation), typeof(int), typeof(string), typeof(string));
         treeview1.Model = listStore;

         treeview1.Selection.Changed += HandleCursorChanged; // do not use treeview1.CursorChanged - see http://ntswiki.nts.neusoft.local/wiki/Known_GTKSharp_Issues
      }

      void ChangeInstanceCount(object item, int dcount)
      {
         Gtk.TreeIter iter;

         if (item == null || treeview1==null || treeview1.Model==null || !treeview1.Model.GetIterFirst(out iter))
            return;

         do
         {
            ComponentFactoryInformation cfi = treeview1.Model.GetValue(iter, CFIIndex) as ComponentFactoryInformation;
            if (cfi.ComponentType == item.GetType())
            {
               object str = treeview1.Model.GetValue(iter, InstanceCountIndex);// as string;
               int count = Convert.ToInt32(str);
               count += dcount;
               treeview1.Model.SetValue(iter, InstanceCountIndex, count);
            }
         }
         while (treeview1.Model.IterNext(ref iter));
      }

      void HandleCursorChanged(object sender, EventArgs e)
      {
         if (sender is Gtk.TreeView)
         {
            Gtk.TreeSelection selection = (sender as Gtk.TreeView).Selection;

            Gtk.TreeModel model;
            Gtk.TreeIter iter;

            // THE ITER WILL POINT TO THE SELECTED ROW
            if (selection.GetSelected(out model, out iter))
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
      }

      Gtk.ListStore listStore;
      const int CFIIndex = 0;
      const int InstanceCountIndex = 1;
      const int TypenameIndex = 2;
      const int DescriptionIndex = 3;
   }


   [Serializable()]
   public class Persistence 
   {
      public void SaveColumnWidth(Gtk.TreeViewColumn[] columns)
      {
         m_Width.Clear();
         foreach (Gtk.TreeViewColumn c in columns)
            m_Width.Add(c.Width);
      }

      public void LoadColumnWidth(Gtk.TreeViewColumn[] columns)
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
      public override Gdk.Pixbuf Icon { get { return Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.ComponentList-16.png"); } }
   }

   #endregion
}

