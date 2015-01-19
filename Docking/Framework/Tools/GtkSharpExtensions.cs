using System;
using System.Collections.Generic;
using System.Text;
using Docking.Components;
using Docking.Widgets;
using Gtk;
using Gdk;
using System.Diagnostics;


namespace Docking.Tools
{
    public enum TreeViewEventType
    {
       Added,         // new treeview entry has been added
       Removed,       // an existing entry has been removed/deleted
       Changed,       // content has been changed
       Selected,      // treeview entry has been selected
       UserDefined1,  // user defined, meaning depends on entry type
    }

    #region IEnumerable<TreeIter>

    public class TreeIterEnumerator : IEnumerator<TreeIter>
    {
       protected TreeModel mModel;
       protected TreeIter mIter;

       public TreeIterEnumerator(TreeModel model)
       {
          mModel = model;
          mIter = TreeIter.Zero;
       }

       #region IEnumerator

       void IDisposable.Dispose()
       {
          mModel = null;
          mIter = TreeIter.Zero;
       }

       TreeIter IEnumerator<TreeIter>.Current          { get { return mIter; } }
       object   System.Collections.IEnumerator.Current { get { return mIter; } }

       void System.Collections.IEnumerator.Reset()
       {
          mIter = TreeIter.Zero;
       }      

       bool System.Collections.IEnumerator.MoveNext()
       {
          if(mModel==null)
             return false;
          return mIter.Equals(TreeIter.Zero)
               ? mModel.GetIterFirst(out mIter)
               : mModel.IterNext(ref mIter);
       }

       #endregion
    }

    public class RowEnumerator : IEnumerable<TreeIter>
    {
       TreeModel mModel;
       public RowEnumerator(TreeModel model) { mModel = model; }
       IEnumerator<TreeIter> IEnumerable<TreeIter>.GetEnumerator() { return new TreeIterEnumerator(mModel); }
       System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return new TreeIterEnumerator(mModel); }
    }

    #endregion

    public static class GtkSharpExtensions
    {
       // allows you to write:
       //   foreach(TreeIter iter in model.Rows())
       //   {
       //      ...
       //   }
       // Sadly, we cannot write
       //    public static RowEnumerator Rows { get { return new RowEnumerator(model); } }
       // , because currently only extension FUNCTIONS are syntactically possible, not extension PROPERTIES.
       public static RowEnumerator Rows(this TreeModel model) { return new RowEnumerator(model); }

       // Analogous function to NColumns by GTK. It is strange that this function is missing there.
       // Sadly, we cannot write
       //    public static int NRows { get { return ... } }
       // , because currently only extension FUNCTIONS are syntactically possible, not extension PROPERTIES.
       public static int NRows(this TreeModel model)
       {
          int result = 0;
          foreach (TreeIter iter in model.Rows())
             result++;
          return result;
       }

       // returns true if the model contains no rows
       public static bool IsEmpty(this TreeModel model)
       {
          TreeIter iter;
          return !model.GetIterFirst(out iter) || iter.Equals(TreeIter.Zero);
       }

       // opposite of .IterNext() - expensive linear search :(
       public static bool IterPrev(this TreeModel model, ref TreeIter iter)
       {
          TreeIter prev = TreeIter.Zero;
          foreach (TreeIter i in model.Rows())
          {
             if (i.Equals(iter))
             {
                iter = prev;
                return !iter.Equals(TreeIter.Zero);
             }
             prev = i;
          }
          iter = TreeIter.Zero;
          return false;
       }

       public static bool GetIterLast(this TreeModel model, out TreeIter iter)
       {
          iter = TreeIter.Zero;
          TreeIter result;
          if (!model.GetIterFirst(out result))
             return false;
          iter = result;
          while (model.IterNext(ref result))
             iter = result;
          return true;
       }

       public static IEnumerable<TreeIter> Find(this TreeModel model, int column, object searchfor)
       {
          List<TreeIter> result = new List<TreeIter>();
          foreach (TreeIter iter in model.Rows())
             if (model.GetValue(iter, column).Equals(searchfor))
                result.Add(iter);
          return result;
       }

       public static string RowToString(this TreeModel model, Gtk.TreeIter iter, List<int> columnids)
       {
          StringBuilder result = new StringBuilder();
          foreach (int i in columnids)
          {
             if (result.Length > 0)
                result.Append("\t");
             object o = model.GetValue(iter, i);
             if (o != null)
                result.Append(o.ToString());
          }
          return result.ToString();
       }

       public static IEnumerable<TreeIter> Childs(this TreeModel model, TreeIter parent)
       {
          List<TreeIter> childs = new List<TreeIter>();

          if (parent.Equals(TreeIter.Zero))
          {
             for (int i = 0; i < model.IterNChildren(); i++)
             {
                TreeIter iter;
                if (model.IterNthChild(out iter, i))
                {
                   childs.Add(iter);
                   childs.AddRange(model.Childs(iter));
                }
             }
          }
          else
          {
             for (int i = 0; i < model.IterNChildren(parent); i++)
             {
                TreeIter iter;
                if (model.IterNthChild(out iter, parent, i))
                {
                   childs.Add(iter);
                   childs.AddRange(model.Childs(iter));
                }
             }
          }
          return childs;
       }

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

      public static List<TreeViewColumn> VisibleColumns(this TreeView treeview)
       {
          List<TreeViewColumn> result = new List<TreeViewColumn>();
          foreach(TreeViewColumn col in treeview.Columns)
             if(col.Visible)
                result.Add(col);
          return result;
       }

       public static void RemoveAllColumns(this TreeView treeview)
       {
          List<TreeViewColumn> columns = new List<TreeViewColumn>();
          foreach(TreeViewColumn col in treeview.Columns)
             columns.Add(col);
          foreach(TreeViewColumn col in columns)
             treeview.RemoveColumn(col);
       }

       // TODO remove this function again. It is redundant to the existing ExpandToPath() function which does exactly the same, see http://wrapl.sourceforge.net/doc/Gtk/Gtk/TreeView.html
       public static void ExpandRowWithParents(this TreeView treeView, TreeIter iter)
       {
          Stack<TreePath> stack = new Stack<TreePath>();
          TreeStore treeStore = treeView.Model as TreeStore;
          TreePath tp = treeStore.GetPath(iter);
          do stack.Push(new TreePath(tp.ToString())); // clone
          while (tp.Up());
          while (stack.Count > 0)
             treeView.ExpandRow(stack.Pop(), false);
       }

      public static void CopySelectedRowsToClipboard(this TreeView treeView, List<int> columnids)
      {
         Gtk.TreePath[] rows = treeView.Selection.GetSelectedRows();
         StringBuilder result = new StringBuilder();
         foreach(Gtk.TreeIter iter in treeView.Selection.GetSelectedRows_TreeIter())
         {
            if(result.Length>0)
               result.AppendLine("");
            result.Append(treeView.Model.RowToString(iter, columnids));
         }
         if(result.Length>0)
            treeView.GetClipboard(Gdk.Selection.Clipboard).Text = result.ToString();
      }

       // class TreeSelection has a method ".SelectAll()" - we need the opposite, ".SelectNone()"
       public static void SelectNone(this TreeSelection selection)
       {
          // this does not work:
          //    selection.SelectIter(TreeIter.Zero);
          // workaround:
          TreePath[] selected = selection.GetSelectedRows();
          foreach(TreePath p in selected)
             selection.UnselectPath(p);          
       }

       // this function is the same as GetSelectedRows(), but returns List<TreeIter> instead of List<TreePath>
       // or
       // this function is the same as GetSelected(), but does not return just 1 TreeItem, but a list of them
       public static List<TreeIter> GetSelectedRows_TreeIter(this TreeSelection selection)
       {
          List<TreeIter> result = new List<TreeIter>();
          foreach(TreePath path in selection.GetSelectedRows())
          {
             TreeIter iter;
             if(selection.TreeView.Model.GetIter(out iter, path))
                result.Add(iter);
          }
          return result;
       }

      public static void DumpWidgetsHierarchy(this Gtk.Widget w, string prefix = "")
      {
         if (prefix == "")
               prefix = "this";
         string s = prefix + " = " + w.Name;
         if(w is Gtk.Label)
               s += "(\""+(w as Gtk.Label).LabelProp+"\")";
         else if(w is Gtk.Button)
               s += "(\"" + (w as Gtk.Button).Label + "\")";
         Debug.Print(s);
         Gtk.Container c = (w as Gtk.Container);
         if (c != null)
         {
               int i = 0;
               foreach (Gtk.Widget w2 in c)
               {
                  w2.DumpWidgetsHierarchy(prefix+"["+i+"]");
                  i++;
               }
         }
      }

      // comfort function to easily access a specific child widget in a nested widget children tree
      public static Gtk.Widget GetChild(this Gtk.Widget w, int i1)
      {
         Gtk.Container c = w as Gtk.Container;
         return (c==null || i1>=c.Children.Length) ? null : c.Children[i1];
      }

      // comfort function to easily access a specific child widget in a nested widget children tree
      public static Gtk.Widget GetChild(this Gtk.Widget w, int i1, int i2)
      {
         Gtk.Container c = w.GetChild(i1) as Gtk.Container;
         return c==null ? null : c.GetChild(i2);
      }

      // comfort function to easily access a specific c widget in a nested widget children tree
      public static Gtk.Widget GetChild(this Gtk.Widget w, int i1, int i2, int i3)
      {
         Gtk.Container c = w.GetChild(i1, i2) as Gtk.Container;
         return c == null ? null : c.GetChild(i3);
      }

      // comfort function to easily access a specific c widget in a nested widget children tree
      public static Gtk.Widget GetChild(this Gtk.Widget w, int i1, int i2, int i3, int i4)
      {
         Gtk.Container c = w.GetChild(i1, i2, i3) as Gtk.Container;
         return c == null ? null : c.GetChild(i4);
      }

      // comfort function to easily access a specific c widget in a nested widget children tree
      public static Gtk.Widget GetChild(this Gtk.Widget w, int i1, int i2, int i3, int i4, int i5)
      {
         Gtk.Container c = w.GetChild(i1, i2, i3, i4) as Gtk.Container;
         return c == null ? null : c.GetChild(i5);
      }

      // comfort function to easily access a specific c widget in a nested widget children tree
      public static Gtk.Widget GetChild(this Gtk.Widget w, int i1, int i2, int i3, int i4, int i5, int i6)
      {
         Gtk.Container c = w.GetChild(i1, i2, i3, i4, i5) as Gtk.Container;
         return c == null ? null : c.GetChild(i6);
      }

      // comfort function to easily access a specific c widget in a nested widget children tree
      public static Gtk.Widget GetChild(this Gtk.Widget w, int i1, int i2, int i3, int i4, int i5, int i6, int i7)
      {
         Gtk.Container c = w.GetChild(i1, i2, i3, i4, i5, i6) as Gtk.Container;
         return c == null ? null : c.GetChild(i7);
      }

      // comfort function to easily access a specific c widget in a nested widget children tree
      public static Gtk.Widget GetChild(this Gtk.Widget w, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8)
      {
         Gtk.Container c = w.GetChild(i1, i2, i3, i4, i5, i6, i7) as Gtk.Container;
         return c == null ? null : c.GetChild(i8);
      }

      // comfort function to easily access a specific c widget in a nested widget children tree
      public static Gtk.Widget GetChild(this Gtk.Widget w, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9)
      {
         Gtk.Container c = w.GetChild(i1, i2, i3, i4, i5, i6, i7, i8) as Gtk.Container;
         return c == null ? null : c.GetChild(i9);
      }

      // comfort function to easily access a specific c widget in a nested widget children tree
      public static Gtk.Widget GetChild(this Gtk.Widget w, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10)
      {
         Gtk.Container c = w.GetChild(i1, i2, i3, i4, i5, i6, i7, i8, i9) as Gtk.Container;
         return c == null ? null : c.GetChild(i10);
      }

      // comfort function to easily access a specific c widget in a nested widget children tree
      public static Gtk.Widget GetChild(this Gtk.Widget w, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11)
      {
         Gtk.Container c = w.GetChild(i1, i2, i3, i4, i5, i6, i7, i8, i9, i10) as Gtk.Container;
         return c == null ? null : c.GetChild(i11);
      }                 

      public static void AddTo(Gtk.Container container, Gtk.Widget w, bool fill = true, uint padding = 0)
      {
         container.Add(w);

         Gtk.Box.BoxChild bc = container[w] as Gtk.Box.BoxChild;
         if(bc!=null)
         {
            bc.Expand = fill;
            bc.Fill = fill;
            bc.Padding = padding;
         }
      }

      /**
      * Extends Gtk.Button in order to be able to retrieve/modify its icon.
      * This is needed, because the generated Gtk.Button instances do NOT support 
      * the "Button.Image property, but rather have a Gtk.Alignment child, which 
      * contains a Gtk.HBox child which happens to contain the displayed icon.
      */
      public static Gtk.Image GetImage(this Gtk.Button button)
      {
         Gtk.Widget c = button;
         while (c is Gtk.Container && (c as Gtk.Container).Children.Length > 0)
         {
            c = (c as Gtk.Container).Children[0];
         }
         return (c is Gtk.Image) ? (Gtk.Image)c : null;
      }
   }
}
