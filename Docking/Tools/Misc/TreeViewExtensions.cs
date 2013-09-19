using System;
using Docking.Components;
using Gtk;


namespace Docking.Tools
{
   public static class TreeViewExtensions
   {
      static public TreeViewColumnLocalized AppendColumn(this TreeView treeview, TreeViewColumnLocalized column, CellRenderer renderer, string attr, int modelcolumn)
      {
         treeview.AppendColumn(column);
         column.PackStart(renderer, true);
         column.AddAttribute(renderer, attr, modelcolumn);
         return column;
      }
      
      // This function is a workaround for the problem that you cannot write
      //    column.Width = width;
      // because .Width (strangely) is a READONLY property.
      static public void SetWidth(this TreeViewColumn column, int width)
      {
         if(width<0)
            width = 0;
         
         // saving the old sizing mode and restoring it strangely does not work:
         // TreeViewColumnSizing sizing = column.Sizing;
         // instead, we at least properly restore the .Resizable property...:
         bool resizable = column.Resizable;
         column.Sizing = TreeViewColumnSizing.Fixed;

         column.FixedWidth = width;
         if(column.MaxWidth>=0 && column.MaxWidth<width)
            column.MaxWidth = width;
         if(column.MinWidth<0)
            column.MinWidth = 0;
         if(column.MinWidth>width)
            column.MinWidth = width;

         //column.Sizing = sizing; // does not work, will throw away our width settings again >:(
         column.Resizable = resizable;
      }

      static public void ShowContextMenu(this TreeView treeview, Menu menu, bool add_menu_entries_for_column_visibility, uint time)
      {
         if(add_menu_entries_for_column_visibility)
         {
            foreach (TreeViewColumn c in treeview.Columns)
            {
               TaggedLocalizedCheckedMenuItem item = new TaggedLocalizedCheckedMenuItem(c.Title);
               item.Active = c.Visible;
               item.Tag = c;
               item.Activated += (object sender, EventArgs e) =>
               {
                  TaggedLocalizedCheckedMenuItem it = sender as TaggedLocalizedCheckedMenuItem;
                  TreeViewColumn ct = it.Tag as TreeViewColumn;
                  ct.Visible = !ct.Visible;
               };
               menu.Add(item);
            }
         }
         menu.ShowAll();
         menu.Popup(null, null, null, 3, time);
      }

   }
}
