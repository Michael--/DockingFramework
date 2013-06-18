using System;
using Gtk;
using Docking.Components;
using System.Collections.Generic;

namespace Docking.Components
{
   [System.ComponentModel.ToolboxItem(true)]
   public partial class LocalizationEditor : Gtk.Bin, ILocalizable, IComponent
   {
      public LocalizationEditor()
      {
         this.Build();

         // TODO: fix sizing could be better because of soome very long values

         TreeViewColumn keyColumn = new TreeViewColumn()          { Title = "Key", Resizable = true, Sizing = TreeViewColumnSizing.Autosize };
         TreeViewColumn usValueColumn = new TreeViewColumn()      { Title = "US name", Resizable = true, Sizing = TreeViewColumnSizing.Autosize };
         TreeViewColumn localValueColumn = new TreeViewColumn()   { Title = "Current name", Resizable = true, Sizing = TreeViewColumnSizing.Autosize };

         treeview1.AppendColumn(keyColumn);
         treeview1.AppendColumn(usValueColumn);
         treeview1.AppendColumn(localValueColumn);

         Gtk.CellRendererText keyCell = new Gtk.CellRendererText();
         Gtk.CellRendererText usValueCell = new Gtk.CellRendererText();
         Gtk.CellRendererText localValueCell = new Gtk.CellRendererText();

         localValueCell.Editable = true;
         localValueCell.Edited += new EditedHandler(localValueCell_Edited);

         keyColumn.PackStart(keyCell, true);
         usValueColumn.PackStart(usValueCell, true);
         localValueColumn.PackStart(localValueCell, true);

         keyColumn.AddAttribute(keyCell, "text", keyIndex);
         usValueColumn.AddAttribute(usValueCell, "text", usValueIndex);
         localValueColumn.AddAttribute(localValueCell, "text", localValueIndex);

         // Create a model that will hold the content, assign the model to the TreeView
         listStore = new Gtk.ListStore(typeof(Localization.Node), typeof(string), typeof(string), typeof(string)); 
         treeview1.Model = listStore;

         button1.Clicked += (sender, e) =>
         {
            ComponentManager.Localization.Write();
         };
      }

      void localValueCell_Edited(object o, EditedArgs args)
      {
         TreeIter iter;
         if (listStore.GetIter(out iter, new TreePath(args.Path)))
         {
            listStore.SetValue(iter, localValueIndex, args.NewText);

            Localization.Node node = listStore.GetValue(iter, nodeIndex) as Localization.Node;
            Localization.Node ln = ComponentManager.Localization.FindCurrentNode(node.Key);
            if (ln != null)
            {
               ln.Value = args.NewText;
            }
            else
            {
               Localization.Node newNode = new Localization.Node(node.Key, args.NewText, "", node.Base);
               ComponentManager.Localization.AddNewCurrentNode(newNode);
            }
            ComponentManager.UpdateLanguage();
         }
      }


      int displayedHashCode = 0;
      Gtk.ListStore listStore;
      const int nodeIndex = 0;
      const int keyIndex = 1;
      const int usValueIndex = 2;
      const int localValueIndex = 3;

      #region implement  ILocalizable

      // set the displayed name of the widget
      string ILocalizable.Name { get { return "Localization Editor"; } }

      void ILocalizable.LocalizationChanged(Docking.DockItem item)
      {
         UpdateList();
      }
      #endregion

      #region implement IComponent
      public ComponentManager ComponentManager { get; set; }

      void IComponent.Loaded(DockItem item)
      {
         UpdateList();
      }

      void IComponent.Save()
      {
      }
      #endregion

      void UpdateList()
      {
         if (displayedHashCode == ComponentManager.Localization.GetCurrentHashcode())
            return;
         displayedHashCode = ComponentManager.Localization.GetCurrentHashcode();
         listStore.Clear();

         Dictionary<string, Localization.Node> dn = ComponentManager.Localization.GetDefaultNodes();
         foreach (Localization.Node node in dn.Values)
         {
            Gtk.TreeIter iter = listStore.Append();
            listStore.SetValue(iter, nodeIndex, node);
            listStore.SetValue(iter, keyIndex, node.Key);
            listStore.SetValue(iter, usValueIndex, node.Value);

            Localization.Node ln = ComponentManager.Localization.FindCurrentNode(node.Key);
            if (ln != null)
               listStore.SetValue(iter, localValueIndex, ln.Value);
         }
      }
   }

   #region Starter / Entry Point

   public class LocalizationEditorFactory : ComponentFactory
   {
      public override Type TypeOfInstance { get { return typeof(LocalizationEditor); } }
      public override String MenuPath { get { return @"View\Infrastructure\Localization Editor"; } }
      public override String Comment { get { return "Edit possibility for all localization strings"; } }
      public override Mode Options { get { return Mode.CloseOnHide; } }
      // public override Gdk.Pixbuf Icon { get { return Gdk.Pixbuf.LoadFromResource("Docking.Framework.Components.PropertyViewer-16.png"); } }
   }
   #endregion

}

