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
   public partial class ComponentSelectorDialog : Gtk.Dialog, ILocalizableWidget
   {
      private class UIComponentDescriptor 
      {
         /// <summary>
         /// name if item to show in tree view
         /// </summary>
         private string m_Text;

         /// <summary>
         /// image to show in tree view
         /// </summary>
         private Gdk.Pixbuf m_Icon;

         /// <summary>
         /// 
         /// </summary>
         private ComponentFactoryInformation m_ComponentFactoryInformation;

         /// <summary>
         /// Name of component to show in list view
         /// </summary>
         virtual public string Text { get { return m_Text; } }

         /// <summary>
         /// small Icon to show in list view
         /// </summary>
         public Pixbuf Icon { get { return m_Icon; } }

         /// <summary>
         /// the orignal constructor given factory info
         /// </summary>
         public ComponentFactoryInformation ComponentFactoryInformation { get { return m_ComponentFactoryInformation; } }

         /// <summary>
         /// 
         /// </summary>
         public Component Instance { get; private set; }

         /// <summary>
         /// checkbox is in selected state or not
         /// </summary>
         public bool Selected { get; set; }

         public UIComponentDescriptor(string text)
         {
            m_Text = text;
            m_Icon = Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.Component-16.png");
         }

         public UIComponentDescriptor(ComponentFactoryInformation cfi, Component component = null)
         {
            m_ComponentFactoryInformation = cfi;
            m_Icon = cfi.Icon;
            Instance = component;

            if(!String.IsNullOrEmpty(cfi.MenuPath))
            {
               // TODO we in future want to display the localized component name here.
               // Currently that is impossible because the getter for that name currently does not sit in the factory, but in the instance.
               // As a workaround, we take the menu item string (which usually matches the window title).
               string[] portions = cfi.MenuPath.Split('\\');
               m_Text = portions[portions.Length-1]; 
            }
            else
            {
               m_Text = "(unnamed component)";
            }
         }

      };

      /// <summary>
      /// the model to propagate all icon containers to to tree view
      /// </summary>
      protected TreeStore m_TreeStore = new TreeStore(typeof(UIComponentDescriptor));

      /// <summary>
      /// Constructor create an instance using a list of available already created components and all other kown components
      /// </summary>
      /// <param name="_components"></param>
      /// <param name="_instances"></param>
      public ComponentSelectorDialog(Gtk.Window parent, List<ComponentFactoryInformation> _components, List<Component> _instances)
      {
         this.TransientFor = parent; // make the window appear centered on top of its parent http://stackoverflow.com/questions/31781134/position-gtk-dialog-in-the-center-of-a-gtk-window
         this.Build();          
         this.Icon = Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.File-16.png");
         (this as ILocalizableWidget).Localize(this.GetType().ToString());         
         BuildTree();
         updateModel(_components, _instances);
      }

      #region ILocalizableWidget
      void ILocalizableWidget.Localize(string namespc)
      {
         Localization.LocalizeControls(namespc, this as Gtk.Container);

         // TODO implement this correctly, this is just a quick+dirty non-localizing implementation
         this.Title = "Choose Component";
      }
      #endregion

      public void updateModel(List<ComponentFactoryInformation> cfis, List<Component> instances)
      {
         // clear to avoid useless updates
         m_View.Model = null;
         m_TreeStore.Clear();

         TreeIter it1 = TreeIter.Zero;
         TreeIter it2 = TreeIter.Zero;
           
         if(instances.Count>0)
         {
            it1 = m_TreeStore.AppendValues(new UIComponentDescriptor("Currently Opened Components"));
            foreach(Component component in instances)
            {
               m_TreeStore.AppendValues(it1, new UIComponentDescriptor(component.ComponentInfo, component ));
            }
         }

         if(cfis.Count>0)
         {
            it2 = m_TreeStore.AppendValues(new UIComponentDescriptor("Available Components"));
            foreach(ComponentFactoryInformation cfi in cfis)
            {
               m_TreeStore.AppendValues(it2, new UIComponentDescriptor(cfi) );
            }            
         }

         m_View.Model = m_TreeStore;

         if(!it1.Equals(TreeIter.Zero))
            m_View.ExpandRowWithParents(it1);
         if(!it2.Equals(TreeIter.Zero))
            m_View.ExpandRowWithParents(it2);
      }

      public void GetSelectedComponents(ref List<ComponentFactoryInformation> components, ref List<Component> instances )
      {
         components.Clear();
         instances.Clear();

         // cannot operate on "instances" or "components" directly due to the lambda expression below (won't compile)
         List<ComponentFactoryInformation> components_ = new List<ComponentFactoryInformation>();
         List<Component>                   instances_  = new List<Component>();

         m_TreeStore.Foreach( (TreeModel model, TreePath path, TreeIter iter) => 
         { 
            UIComponentDescriptor item = model.GetValue( iter, 0 ) as UIComponentDescriptor;
            if(item!=null)
            {
               if(item.Selected)
               {
                  if(item.Instance==null)
                  {
                     components_.Add(item.ComponentFactoryInformation);
                  }
                  else
                  {
                     instances_.Add(item.Instance);
                  }
               }
            }
            return false; 
         } );

         // cannot operate on "instances" or "components" directly due to the lambda expression below (won't compile)
         components = components_;
         instances = instances_;
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
               cell.Text = model_item.Text;
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
               if ( null != model_item.ComponentFactoryInformation )
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
         m_TreeStore.GetIter(out it, new TreePath(_args.Path));
         (m_TreeStore.GetValue(it, 0) as UIComponentDescriptor).Selected = !(_inst as CellRendererToggle).Active;
      }
   }
}

