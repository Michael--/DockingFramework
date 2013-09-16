using Docking.Components;
using System;

namespace Docking.Tools
{
   public static class TreeViewExtensions
   {
      static public TreeViewColumnLocalized AppendColumn(this Gtk.TreeView treeview, TreeViewColumnLocalized column, Gtk.CellRenderer renderer, string attr, int modelcolumn)
      {
         treeview.AppendColumn(column);
         column.PackStart(renderer, true);
         column.AddAttribute(renderer, attr, modelcolumn);
         return column;
      }
      
      // This function is a workaround for the problem that you cannot write
      //    column.Width = width;
      // because .Width (strangely) is a READONLY property.
      static public void SetWidth(this Gtk.TreeViewColumn column, int width)
      {
         if(width<0)
            width = 0;
         
         // saving the old sizing mode and restoring it strangely does not work:
         // Gtk.TreeViewColumnSizing sizing = column.Sizing;
         // instead, we at least properly restore the .Resizable property...:
         bool resizable = column.Resizable;
         column.Sizing = Gtk.TreeViewColumnSizing.Fixed;

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
   }
}
