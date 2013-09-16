using Docking.Components;

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
   }
}
