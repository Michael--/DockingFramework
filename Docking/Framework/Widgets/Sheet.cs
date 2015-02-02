// This file implements a custom Gtk widget "Sheet", derived from TreeView,
// which in future shall behave like a tabular cells grid (visually similar to e.g. Excel).
// I.e., individual cells can be selected, as well as full columns or rows.


using System;
using System.Collections.Generic;
using Docking.Tools;
using Gtk;
using Gdk;


namespace Docking.Widgets
{
   // Helper class: a 2-dimensional location inside the sheet.
   public class CellLocation : Tuple<TreeIter, TreeViewColumn>
   {
      public TreeIter       Row { get { return Item1; } }
      public TreeViewColumn Col { get { return Item2; } }
      public CellLocation(TreeIter row, TreeViewColumn col) : base(row, col)  {}
   }

   // Helper class: provides information about which cells are currently selected.
   // You can select either some columns, or some rows, or a set of individual cells.
   // These 3 selection mechanisms work exclusively, you can only do one of them at a time.
   public class SheetSelection
   {
      public HashSet<TreeIter>       SelectedRows    = new HashSet<TreeIter>();
      public HashSet<TreeViewColumn> SelectedColumns = new HashSet<TreeViewColumn>();
      public HashSet<CellLocation>   SelectedCells   = new HashSet<CellLocation>();

      Sheet mSheet;
      public SheetSelection(Sheet sheet)
      {
         mSheet = sheet;
      }

      public void Notify()
      {
         mSheet.OnSheetSelectionChanged(this);
      }

      #region Clear

      public bool ClearSelectedRows(bool notify = true)    
      {
         bool dirty = SelectedRows.Count>0;
         if(dirty)
         {
            SelectedRows.Clear();
            if(notify)
               Notify();
         }
         return dirty;
      }

      public bool ClearSelectedColumns(bool notify = true)
      { 
         bool dirty = SelectedColumns.Count>0;
         if(dirty)
         {
            SelectedColumns.Clear();
            if(notify)
               Notify();
         }
         return dirty;
      }

      public bool ClearSelectedCells(bool notify = true)
      { 
         bool dirty = SelectedCells.Count>0;
         if(dirty)
         {
            SelectedCells.Clear();
            if(notify)
               Notify();
         }
         return dirty;
      }
     
      public bool Clear(bool notify = true)
      {
         bool nonempty1 = ClearSelectedRows(false);
         bool nonempty2 = ClearSelectedColumns(false);
         bool nonempty3 = ClearSelectedCells(false);
         if(nonempty1 || nonempty2 || nonempty3)        
         {
            if(notify)
               Notify();
            return true;
         }
         return false;
      }

      #endregion

      #region IsSelected

      public bool IsSelected(TreeIter       row )              { return SelectedRows.Contains(row);                }
      public bool IsSelected(TreeViewColumn col )              { return SelectedColumns.Contains(col);             }
      public bool IsSelected(CellLocation   cell)              { return SelectedCells.Contains(cell) 
                                                                     || SelectedColumns.Contains(cell.Col)
                                                                     || SelectedRows.Contains(cell.Row);           }
      public bool IsSelected(TreeIter row, TreeViewColumn col) { return IsSelected(new CellLocation(row, col));    }

      #endregion

      #region Unselect

      public void Unselect(TreeIter       row              , bool notify = true) { if(SelectedRows.Remove(row)                         && notify) Notify(); }
      public void Unselect(TreeViewColumn col              , bool notify = true) { if(SelectedColumns.Remove(col)                      && notify) Notify(); }
      public void Unselect(CellLocation   cell             , bool notify = true) { if(SelectedCells.Remove(cell)                       && notify) Notify(); }
      public void Unselect(TreeIter row, TreeViewColumn col, bool notify = true) { if(SelectedCells.Remove(new CellLocation(row, col)) && notify) Notify(); }

      #endregion

      #region Select

      public void Select(TreeIter row, bool addToCurrentRowSelection, bool notify = true)
      {
         ClearSelectedColumns(false);
         ClearSelectedCells(false);
         if(!addToCurrentRowSelection)
            ClearSelectedRows(false);
         SelectedRows.Add(row);
         if(notify)
            Notify();
      }      

      public void Select(TreeViewColumn col, bool addToCurrentColumnSelection, bool notify = true)
      {
         ClearSelectedRows(false);
         ClearSelectedCells(false);
         if(!addToCurrentColumnSelection)
            ClearSelectedColumns(false);
         SelectedColumns.Add(col);
         if(notify)
            Notify();
      }

      public void Select(CellLocation cell, bool addToCurrentCellSelection, bool notify = true)
      {
         ClearSelectedRows(false);
         ClearSelectedColumns(false);
         if(!addToCurrentCellSelection)
            ClearSelectedCells(false);
         SelectedCells.Add(cell);
         if(notify)
            Notify();
      }

      public void Select(TreeIter row, TreeViewColumn col, bool addToCurrentCellSelection, bool notify = true)
      {
         Select(new CellLocation(row, col), addToCurrentCellSelection, notify);
      }

      // Selects a rectangular range inside the rectangle (row1,col1) - (row2,col2).
      // It is not necessary that row1<=row2: row2<=row1 will work as well.
      // The same goes for col1, col2:        col2<=col1 will work as well.  
      // col1 and col2 are indices inside the mSheet.Columns[] array (of which SOME may be currently .Visible). 
      // This function will only affect the .Visible columns of that array.
      public void SelectRange(TreeIter row1, int col1, TreeIter row2, int col2, bool notify = true)
      {
         bool dirty = Clear(false);
         if(col1<0 || col1>=mSheet.Columns.Length ||
            col2<0 || col2>=mSheet.Columns.Length)
         {
            if(dirty && notify)
               Notify();
            return;
         }

         // check if row1 is a predecessor of row2
         bool foundit = false;
         TreeIter row = row1;
         while(!row.Equals(TreeIter.Zero))
         {
            if(row.Equals(row2))
               foundit = true;
            if(row.Equals(row2) || !mSheet.Model.IterNext(ref row))
               row = TreeIter.Zero;
         }

         // check if row2 is a predecessor of row1
         if(!foundit)
         {
            row = row2;
            while(!row.Equals(TreeIter.Zero))
            {
               if(row.Equals(row1))
                  foundit = true;
               if(row.Equals(row1) || !mSheet.Model.IterNext(ref row))
                  row = TreeIter.Zero;
            }
            if(!foundit)
            {
               if(dirty && notify)
                  Notify();
               return; // ERROR: neither is row1 a predecessor of row2, nor is row2 a predecessor of row1. Bailout.
            }
            SelectRange(row2, col2, row1, col1, notify); // swap the order
            return;
         }

         // now do the selection
         row = row1;
         while(!row.Equals(TreeIter.Zero))
         {
            if(col2>=col1)
            {
               for(int col = col1; col<=col2; col++) if(mSheet.Columns[col].Visible)
               {
                  Select(row, mSheet.Columns[col], true, false);
                  dirty = true;
               }
            }
            else
            {
               for(int col = col2; col<=col1; col++) if(mSheet.Columns[col].Visible)
               {
                  Select(row, mSheet.Columns[col], true, false);
                  dirty = true;
               }
            }
            if(row.Equals(row2) || !mSheet.Model.IterNext(ref row))
               row = TreeIter.Zero;
         }
         if(dirty && notify)
            Notify();
      }

      #endregion
   }

   // Helper class: custom renderer for cell contents inside the sheet.
   // TODO our SheetRenderer should be able to deal with ANY other renderer, not just with CellRendererPixbuf.
   // However, currently problematic is that we need to OVERRIDE its Render() function to be able to tweak and then
   // invoke the parent's render function.
   public class SheetRenderer : CellRendererPixbuf 
   {
      public SheetRenderer()           : base()    {}
      public SheetRenderer(IntPtr raw) : base(raw) {}

      // TODO instead of hardcoding colors here, use the standard system colors
      public Gdk.Color ColorCellBgNormal               = System.Drawing.Color.FromArgb(128, 255, 255, 255).ToGdk();
      public Gdk.Color ColorCellBgSelectedWithFocus    = System.Drawing.Color.FromArgb(255,  51, 153, 255).ToGdk();
      public Gdk.Color ColorCellBgSelectedWithoutFocus = System.Drawing.Color.FromArgb(255, 240, 240, 240).ToGdk();      
      //TreeView.OddRowColor                           = System.Drawing.Color.FromArgb(255, 255,   0,   0).ToGdk();
      //TreeView.EvenRowColor                          = System.Drawing.Color.FromArgb(255,   0, 255,   0).ToGdk();

      // This is the central function determining how we render cells.
      // Whatever renderer class it gets as input, it customizes that renderer's settings
      // to toggle the visual appearance of cells between "unselected" and "selected".
      // Note that the "selected" state is visualized differently (following the other Gtk widget's appearance logic)
      // whether the widget containing it has the focus or not.
      public void DataFunc(TreeViewColumn col, CellRenderer renderer, TreeModel model, TreeIter row)
      {         
         Sheet sheet = (col.TreeView as Sheet);
         renderer.CellBackgroundGdk = sheet.SheetSelection.IsSelected(row, col)
                                    ? (sheet.HasFocus ? ColorCellBgSelectedWithFocus : ColorCellBgSelectedWithoutFocus)
                                    : ColorCellBgNormal;
      }

      protected override void Render(Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
      {
         // Note that this renderer always passes "0" as last parameter.
         // This way, no further rendering logic is applied. We want to turn off/inhibit all the default TreeView rendering logic
         // and instead configure this renderer per cell individually by our DataFunc().
         base.Render(window, widget, background_area, cell_area, expose_area, /*flags*/0); // TODO check if we really need to do this trick
      }
   }

   public delegate void SheetSelectionChangedEventHandler(SheetSelection sheetselection);

   // Sheet widget. A 2-dimensional set of cells.
   // You can select some cells individually (or a cell range) and will get notified outside by event SheetSelectionChanged.
   // Current implementation is derived from TreeView.
   [System.ComponentModel.ToolboxItem(true)]
   public class Sheet : TreeView
   {
      public SheetSelection SheetSelection;      
      public event SheetSelectionChangedEventHandler SheetSelectionChanged;
      public void OnSheetSelectionChanged(SheetSelection sheetselection) { if(SheetSelectionChanged!=null) SheetSelectionChanged(sheetselection); }
      public ModifierType CurrentKeyboardModifier { get; protected set; }
      public uint CurrentButton { get; protected set; }
      protected CellLocation mSelectionHook = null;

      public Sheet()                : base()      { Constructor(); }
      public Sheet(IntPtr raw)      : base(raw)   { Constructor(); }
      public Sheet(TreeModel model) : base(model) { Constructor(); }
      private void Constructor()
      {
         CursorChanged += HandleParentCursorChanged;

         // Disable parent selection model
         Selection.Mode = SelectionMode.None;

         // Implementation of own selection model
         SheetSelection = new SheetSelection(this);
         HeadersClickable = true;
         //FocusLineWidth = 0; // TODO does not work. find some other way to hide the dotted "focused cells" line
      }

      public List<CellLocation> Find(int column, object searchfor)
      {
         List<CellLocation> result = new List<CellLocation>();
         foreach(TreeIter row in Model.Rows())
            if(Model.GetValue(row, column).Equals(searchfor))
               result.Add(new CellLocation(row, Columns[column]));
         return result;
      }

      protected void HandleParentCursorChanged(object sender, EventArgs e)
      {
         TreePath path;
         TreeIter row;
         TreeViewColumn col;
         GetCursor(out path, out col);
         if (Model.GetIter(out row, path) && col != null && CurrentButton != RIGHT_MOUSE_BUTTON)
         {
            bool shiftDown = (CurrentKeyboardModifier & ModifierType.ShiftMask) != ModifierType.None;
            bool ctrlDown = (CurrentKeyboardModifier & ModifierType.ControlMask) != ModifierType.None;

            mSelectionHook = (shiftDown) ? mSelectionHook : new CellLocation(row, col);
            List<CellLocation> selectedcells = new List<CellLocation>(SheetSelection.SelectedCells);
            int col1 = (selectedcells.Count > 0) ? Array.IndexOf(Columns, mSelectionHook.Col) : -1;
            if (col1 >= 0 && shiftDown)
            {
               SheetSelection.SelectRange(mSelectionHook.Row, col1, row, Array.IndexOf(Columns, col));
            }
            else
            {
               if (SheetSelection.IsSelected(row, col) && (ctrlDown || selectedcells.Count == 1))
               {
                  SheetSelection.Unselect(row, col);
               }
               else
               {
                  SheetSelection.Select(row, col, ctrlDown);
               }
            }
            QueueDraw();
         }
      }

      #region key press / release

      [GLib.ConnectBeforeAttribute]
      protected override bool OnKeyPressEvent(EventKey evnt)
      {
         CurrentKeyboardModifier = evnt.State;
         CurrentButton = 0U;

         // Workaround for gtk broken cursor left<->right movement when shift is pressed ~.~
         if ((CurrentKeyboardModifier & ModifierType.ShiftMask) != ModifierType.None)
         {
            TreePath path;
            TreeIter row;
            TreeViewColumn col;
            GetCursor(out path, out col);
            if (Model.GetIter(out row, path) && col != null)
            {
               List<TreeViewColumn> visiblecols = (this as TreeView).VisibleColumns();
               switch (evnt.Key)
               {
                  case Gdk.Key.Left:
                  case Gdk.Key.KP_Left:
                  {
                     int i = visiblecols.IndexOf(col);
                     if (i > 0)
                     {
                        // Workaround: Need double call because gtk cursor change event is posted BEFORE cursor property update ~.~
                        SetCursor(path, visiblecols[i - 1], false);
                        SetCursor(path, visiblecols[i - 1], false);
                     }
                     return true;
                  }
                  case Gdk.Key.Right:
                  case Gdk.Key.KP_Right:
                  {
                     int i = visiblecols.IndexOf(col);
                     if (i > -1 && i < visiblecols.Count - 1)
                     {
                        // Workaround: Need double call because gtk cursor change event is posted BEFORE cursor property update ~.~
                        SetCursor(path, visiblecols[i + 1], false);
                        SetCursor(path, visiblecols[i + 1], false);
                     }
                     return true;
                  }
                  default:
                     break;
               }
            }
         }

         return base.OnKeyPressEvent(evnt);
      }

      protected override bool OnKeyReleaseEvent(EventKey evnt)
      {
         CurrentKeyboardModifier = evnt.State;
         CurrentButton = 0U;
         return base.OnKeyReleaseEvent(evnt);
      }

      #endregion

      protected const int LEFT_MOUSE_BUTTON  = 1;
      protected const int RIGHT_MOUSE_BUTTON = 3;

      protected override bool OnButtonPressEvent(EventButton evnt)
      {
         CurrentKeyboardModifier = evnt.State;
         CurrentButton = evnt.Button;
         return base.OnButtonPressEvent(evnt);
      }

      protected int HeaderHeight = 0;

      protected override bool OnExposeEvent(EventExpose evnt)
      {
         if(HeaderHeight<=0)
         {
            int dummyx;
            ConvertTreeToWidgetCoords(0, 0, out dummyx, out HeaderHeight); // note the difference between "Widget Coordinates" and "Tree Coordinates"...: http://hackage.haskell.org/packages/archive/gtk/latest/doc/html/Graphics-UI-Gtk-ModelView-TreeView.html
         }
         return base.OnExposeEvent(evnt);
      }
   }
}

