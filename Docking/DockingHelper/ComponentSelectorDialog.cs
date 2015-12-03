using System;
using Docking.Components;
using Docking;
using Docking.Tools;
using Gtk;

namespace Docking.Widgets
{
   public partial class ComponentSelectorDialog : Gtk.Dialog
   {
      private class UIComponentDescriptor
      {
         private ComponentFactory m_ComponentDescriptor;

         public string Name { get { return m_ComponentDescriptor.Name; } }

         public UIComponentDescriptor(ComponentFactory _component)
         {
            m_ComponentDescriptor = _component;
         }
      };

      /// <summary>
      /// the model to propagate all icon containers to to tree view
      /// </summary>
      protected ListStore m_Components = new ListStore(typeof(UIComponentDescriptor));
      
      public ComponentSelectorDialog()
      {
         this.Build();

         BuildTree();
      }

      private void BuildTree()
      {
         // Setup thumbnail column
         {
            CellRendererPixbuf renderer = new CellRendererPixbuf();
            TreeViewColumn column = new TreeViewColumn("", renderer);
            column.SetCellDataFunc(renderer, (TreeViewColumn _column, CellRenderer _cell, TreeModel _model, TreeIter _iter) =>
            {
               UIComponentDescriptor c = _model.GetValue(_iter, 0) as UIComponentDescriptor;
               //(_cell as Gtk.CellRendererPixbuf).Pixbuf = _model.GetValue( _iter, 1) as Gdk.Pixbuf;
            });
            m_View.AppendColumn(column);
         }

         // Setup name column
         {
            CellRendererText renderer = new CellRendererText();
            TreeViewColumn column = new TreeViewColumn("Name", renderer);

            // set data in cell
            column.SetCellDataFunc(renderer, (TreeViewColumn _column, CellRenderer _cell, TreeModel _model, TreeIter _iter) =>
            {
               UIComponentDescriptor c = _model.GetValue(_iter, 0) as UIComponentDescriptor;
               //(_cell as Gtk.CellRendererText).Text = c.Component.
            });
            m_View.AppendColumn(column);
         }

         // setup checkbox column
         {
            CellRendererToggle renderer = new CellRendererToggle();
            TreeViewColumn column = new TreeViewColumn("", renderer);
            column.SetCellDataFunc(renderer, (TreeViewColumn _column, CellRenderer _cell, TreeModel _model, TreeIter _iter) =>
            {
               
            });
            m_View.AppendColumn(column);

         }
      }

      protected void OnMButtonApplyClicked (object sender, EventArgs e)
      {
         throw new NotImplementedException ();
      }

   }
}

