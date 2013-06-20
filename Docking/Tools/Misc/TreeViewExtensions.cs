using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Docking.Components;

namespace Docking.Tools
{
   public static class TreeViewExtensions
   {
      static public TreeViewColumnLocalized AppendColumn(this Gtk.TreeView treeview, TreeViewColumnLocalized column, Gtk.CellRenderer cell, string attr, int col)
      {
         treeview.AppendColumn(column);
         column.PackStart(cell, true);
         column.AddAttribute(cell, attr, col);
         return column;
      }
   }
}
