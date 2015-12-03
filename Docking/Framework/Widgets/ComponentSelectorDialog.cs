using System;
using System.Collections.Generic;
using Docking.Components;
using Docking;
using Docking.Tools;
using Gtk;
using Gdk;

namespace Docking.Widgets
{
   [System.ComponentModel.ToolboxItem(false)]
   public partial class ComponentSelectorDialog : Gtk.Dialog
   {
      private class UIComponentDescriptor 
      {
         /// <summary>
         /// name if item to show in tree view
         /// </summary>
         private string m_Name;

         /// <summary>
         /// image to show in tree view
         /// </summary>
         private Gdk.Pixbuf m_Icon;

         /// <summary>
         /// 
         /// </summary>
         private ComponentFactoryInformation m_Component;

         /// <summary>
         /// Name of component to show in list view
         /// </summary>
         virtual public string Name { get { return m_Name; } }

         /// <summary>
         /// small Icon to show in list view
         /// </summary>
         public Pixbuf Icon { get { return m_Icon; } }
         /// <summary>
         /// the orignal constructor given factory info
         /// </summary>
         public ComponentFactoryInformation Descriptor { get { return m_Component; } }

         /// <summary>
         /// 
         /// </summary>
         public Component Instance { get; private set; }

         /// <summary>
         /// checkbox is in sleeced state or not
         /// </summary>
         public bool Selected { get; set; }

         public UIComponentDescriptor(string _name)
         {
            m_Component = null;
            m_Name = _name;
            m_Icon = Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.ComponentList-16.png");
         }


         public UIComponentDescriptor(ComponentFactoryInformation _component)
         {
            m_Component = _component;
            m_Icon = _component.Icon;
            if ( null != _component.Name && _component.Name != string.Empty )
            {
               m_Name = _component.Name;
            }
            else 
            {
               m_Name = _component.Comment;
            }
         }

         public UIComponentDescriptor(ComponentFactoryInformation _component, Component _inst)
         {
            m_Component = _component;
            Instance = _inst;
            m_Icon = _component.Icon;
            if (null != _component.Name && _component.Name != string.Empty)
            {
               m_Name = _component.Name;
            }
            else
            {
               m_Name = _component.Comment;
            }
         }

      };

      /// <summary>
      /// the model to propagate all icon containers to to tree view
      /// </summary>
      protected TreeStore m_Components = new TreeStore(typeof(UIComponentDescriptor));

      /// <summary>
      /// Constructor create an instance using a list of available already created components and all other kown components
      /// </summary>
      /// <param name="_components"></param>
      /// <param name="_instances"></param>
      public ComponentSelectorDialog(List<ComponentFactoryInformation> _components, List<Component> _instances)
      {
         this.Build();

         BuildTree();

         // update model
         updateModel(_components, _instances);
      }

      public void updateModel(List<ComponentFactoryInformation> _components, List<Component> _instances)
      {
         // clear to avoid useless updates
         m_View.Model = null;
         m_Components.Clear();

         if ( 0 < _components.Count )
         {
            var it = m_Components.AppendValues(new UIComponentDescriptor("Available Commponent Types"));
            foreach( ComponentFactoryInformation cfi in _components )
            {
               m_Components.AppendValues( it, new UIComponentDescriptor(cfi) );
            }
         }

         if ( 0 < _instances.Count )
         {
            var it = m_Components.AppendValues(new UIComponentDescriptor("Available Commponent Instances"));
            foreach ( Component cfi in _instances)
            {
               m_Components.AppendValues( it, new UIComponentDescriptor(cfi.ComponentInfo, cfi ));
            }
         }

         // pass data to view
         m_View.Model = m_Components;
      }

      public bool selectedComponents( ref List<ComponentFactoryInformation> _components, ref List<Component> _instances )
      {
         _components.Clear();
         _instances.Clear();

         List<ComponentFactoryInformation> components = new List<ComponentFactoryInformation>();
         List<Component> instances = new List<Component>();

         m_Components.Foreach( (TreeModel model, TreePath path, TreeIter iter) => 
         { 
            UIComponentDescriptor item = model.GetValue( iter, 0 ) as UIComponentDescriptor;
            if( null != item )
            {
               if ( item.Selected)
               {
                  if( null == item.Instance )
                  {
                     components.Add(item.Descriptor);
                  }
                  else
                  {
                     instances.Add( item.Instance );
                  }
               }
            }
            return false; 
         } );

         _components = components;
         _instances = instances;

         return (0 < _components.Count) || (0 < _instances.Count);
      }

      private void BuildTree()
      {
         // Setup thumbnail column
         using (CellRendererPixbuf renderer = new CellRendererPixbuf())
         {
            TreeViewColumn column = new TreeViewColumn("", renderer);
            column.SetCellDataFunc(renderer, (TreeViewColumn _column, CellRenderer _cell, TreeModel _model, TreeIter _iter) =>
            {
               var model_item = _model.GetValue(_iter, 0) as UIComponentDescriptor;
               (_cell as Gtk.CellRendererPixbuf).Pixbuf = model_item.Icon;

            });
            m_View.AppendColumn(column);
         }

         // Setup name column
         using (CellRendererText renderer = new CellRendererText())
         {
            TreeViewColumn column = new TreeViewColumn("Name", renderer);

            // set data in cell
            column.SetCellDataFunc(renderer, (TreeViewColumn _column, CellRenderer _cell, TreeModel _model, TreeIter _iter) =>
            {
               Gtk.CellRendererText cell = _cell as Gtk.CellRendererText;
               var model_item = _model.GetValue(_iter, 0) as UIComponentDescriptor;
               cell.Text = model_item.Name;
            });
            m_View.AppendColumn(column);
         }

         // setup checkbox column
         {
            var renderer = new CellRendererToggle();
            renderer.Toggled += new ToggledHandler( OnComponentSelected );

            TreeViewColumn column = new TreeViewColumn("", renderer);
            column.SetCellDataFunc(renderer, (TreeViewColumn _column, CellRenderer _cell, TreeModel _model, TreeIter _iter) =>
            {
               var model_item = _model.GetValue(_iter, 0) as UIComponentDescriptor;
               if ( null != model_item.Descriptor )
               {
                  (_cell as CellRendererToggle).Active = (model_item as UIComponentDescriptor).Selected;
                  (_cell as CellRendererToggle).Visible = true;
               }
               else
               {
                  (_cell as CellRendererToggle).Visible = false;
               }
            });
            m_View.AppendColumn(column);
         }
      }

		protected void OnComponentSelected( object _inst, ToggledArgs _args )
      {
         TreeIter it;
         m_Components.GetIter(out it, new TreePath(_args.Path));
         (m_Components.GetValue(it, 0) as UIComponentDescriptor).Selected = !(_inst as CellRendererToggle).Active;
      }
   }
}

