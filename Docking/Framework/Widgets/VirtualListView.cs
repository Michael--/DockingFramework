using System;
using System.Collections.Generic;
using Gtk;
using GLib;
using Docking.Components;
using Docking.Helper;

namespace Docking.Widgets
{
   [System.ComponentModel.ToolboxItem(true)]
   public partial class VirtualListView : Component, ILocalizableWidget
   {
      public VirtualListView()
      {
         this.Build();
         LineLayout = GetLayout();
         int width, height;
         LineLayout.SetMarkup("XYZ");
         LineLayout.GetPixelSize(out width, out height);
         ConstantHeight = height;
         CurrentRow = 0;
         SelectedRow = 0;
         SelectionMode = false;
         DocumentEnd = true;

         mColumnControl = new ColumnControl();
         vbox1.Add(mColumnControl);

         Gtk.Box.BoxChild boxChild = ((Gtk.Box.BoxChild)(this.vbox1[mColumnControl]));
         boxChild.Position = 0;
         boxChild.Expand = false;
         boxChild.Fill = false;
         mColumnControl.Show();

         mColumnControl.ColumnChanged += (o, e) =>
         {
            drawingarea.QueueDraw();
         };
      }

      ColumnControl mColumnControl;
      VirtualListPersistenceList mPersistence;

      /// <summary>
      /// Sets the get content delegate. Will be called for any content request.
      /// </summary>
      public ContentDelegate GetContentDelegate { private get; set; }
      public delegate String ContentDelegate(int row, int column);

      public ColorDelegate GetColorDelegate { private get; set; }
      public delegate void ColorDelegate(int row, ref System.Drawing.Color background, ref System.Drawing.Color foreground);

      /// <summary>
      /// Gets the current row index
      /// </summary>
      public int CurrentRow { get; private set; }
      public event VirtualListViewEventHandler CurrentRowChanged;

      /// <summary>
      /// Return true if the current row is at document end
      /// Similar to CurrentRow == RowCount-1, but free of side effects if the document size grows
      /// </summary>
      public bool DocumentEnd { get; private set; }

      /// <summary>
      /// Gets or sets the row count, the possible size of the list
      /// </summary>
      public int RowCount
      {
         get
         {
            return mRow;
         }
         set
         {
            mRow = value;
            CurrentRow = Math.Min(CurrentRow, mRow - 1);
            SelectedRow = Math.Min(CurrentRow, mRow - 1);
            vscrollbar1.SetRange(0, Math.Max(1, mRow - 1));
         }
      }
      private int mRow;

      public void TriggerRepaint() { drawingarea.QueueDraw(); }

      /// <summary>
      /// Get selection. Return the range of selected lines.
      /// At least the current line is always selected
      /// </summary>
      public void GetSelection(out int bottom, out int top)
      {
         bottom = Math.Min(CurrentRow, SelectedRow);
         top = Math.Max(CurrentRow, SelectedRow);
      }

      private Pango.Layout LineLayout { get; set; }
      private int ConstantHeight { get; set; }
      private int SelectedRow { get; set; }
      private bool SelectionMode { get; set; }
      private int TopVisibleRow { get; set; }
      private int BottomVisibleRow { get; set; }

      private bool isRowSelected(int row)
      {
         int bottom = Math.Min(CurrentRow, SelectedRow);
         int top = Math.Max(CurrentRow, SelectedRow);
         return row >= bottom && row <= top;
      }

      /// <summary>
      /// Adds a new column, whereby the column is represented as a GTK label  
      /// After the last column has been added, you must confirm
      /// with UpdateColumns()
      /// </summary>
      /// <param name="name">Name.</param>
      /// <param name="width">Width.</param>
      /// <param name="visible">If set to <c>true</c> visible.</param>
      public void AddColumn(int tag, string name, int width, bool visible)
      {
         Label label = new Label(name);
         label.SetPadding(2, 2);
         label.Visible = visible;
         AddColumn(name, label, tag, width);
      }

      /// <summary>
      /// Adds a new column with an explicit given widget.
      /// Make visible with UpdateColumns() at least.
      /// </summary>
      /// <param name="name">Name.</param>
      /// <param name="widget">Widget.</param>
      /// <param name="width">Width.</param>
      /// <param name="visible">If set to <c>true</c> visible.</param>
      public void AddColumn(int tag, string name, Widget widget, int width, bool visible)
      {
         if (visible)
            widget.ShowAll();
         AddColumn(name, widget, tag, width);
      }

      void AddColumn(string name, Widget widget, int tag, int width)
      {
         if (mPersistence != null)
         {
            foreach (VirtualListPersistence p in mPersistence.Persistence)
            {
               if (p.Tag == tag)
               {
                  width = p.Width;
                  break;
               }
            }
         }
         mColumnControl.AddColumn(name, widget, tag, width);
      }

      /// <summary>
      /// Removes a column. Make visible with UpdateColumns().
      /// </summary>
      /// <param name="name">Name.</param>
      public void RemoveColumn(String name)
      {
         // todo
      }

      /// <summary>
      /// Clears the column definitions. Make visible with UpdateColumns(). 
      /// </summary>
      public void ClearColumns()
      {
         // todo
      }

      /// <summary>
      /// Sets the column visibility. 
      /// Make visible with UpdateColumns(). 
      /// </summary>
      /// <param name="name">Name.</param>
      /// <param name="visible">If set to <c>true</c> visible.</param>
      public void SetColumnVisibility(String name, bool visible)
      {
         // todo
      }

      /// <summary>
      /// Sets the width of the column.
      /// Make visible with UpdateColumns(). 
      /// </summary>
      /// <param name="name">Name.</param>
      /// <param name="width">Width.</param>
      public void SetColumnWidth(String name, int width)
      {
         // todo
      }

      /// <summary>
      /// Gets the width of the column.
      /// Make visible with UpdateColumns(). 
      /// </summary>
      /// <returns>The column width.</returns>
      /// <param name="name">Name.</param>
      public int GetColumnWidth(String name)
      {
         // todo
         return 0;
      }

      /// <summary>
      /// Gets the persistence data as int array
      /// </summary>
      /// <returns>The persistence.</returns>
      public void SavePersistence()
      {
         mPersistence = new VirtualListPersistenceList();
         ColumnControl.Column[] columns = mColumnControl.GetColumns();
         foreach(ColumnControl.Column c in columns)
         {
            VirtualListPersistence p = new VirtualListPersistence();
            p.Visible = c.Visible;
            p.Width = c.Width;
            p.Tag = c.Tag;
            mPersistence.Persistence.Add(p);
         }
         ComponentManager.SaveObject("VirtualListView", mPersistence);
      }

      /// <summary>
      /// Sets the persistence previously got with GetPersistence
      /// </summary>
      /// <param name="data">Data.</param>
      public void LoadPersistence()
      {
         mPersistence = (VirtualListPersistenceList)ComponentManager.LoadObject("VirtualListView", typeof(VirtualListPersistenceList));
      }

      private Pango.Layout GetLayout()
      {
         Pango.Layout layout = new Pango.Layout(this.PangoContext);
         layout.FontDescription = Pango.FontDescription.FromString("Tahoma 10");
         layout.Wrap = Pango.WrapMode.WordChar;
         return layout;
      }

      protected void OnDrawingareaExposeEvent(object o, Gtk.ExposeEventArgs args)
      {
         Gdk.EventExpose expose = args.Args[0] as Gdk.EventExpose;
         Gdk.Window win = expose.Window;
         int width, height;
         win.GetSize(out width, out height);
         Gdk.Rectangle exposeRect = expose.Area;
         bool fulldraw = width == exposeRect.Width && height == exposeRect.Height;

         win.DrawRectangle(Style.LightGC(StateType.Normal), true, exposeRect);
         if (GetContentDelegate == null)
            return; // todo: an error message could be displayed

         int offset = (int)vscrollbar1.Value;
         if (fulldraw)
         {
            TopVisibleRow = offset;
            BottomVisibleRow = offset;
         }
         int hscrollRange = 0;
         int dy = exposeRect.Top;
         offset += dy / ConstantHeight;
         dy -= dy % ConstantHeight;

         Gdk.GC backgound = new Gdk.GC((Gdk.Drawable)base.GdkWindow);
         Gdk.GC text = new Gdk.GC((Gdk.Drawable)base.GdkWindow);

         ColumnControl.Column[] columns = mColumnControl.GetVisibleColumnsInDrawOrder();

         for (int row = offset; row < RowCount; row++)
         {
            int dx = -(int)hscrollbar1.Value;
            Gdk.Rectangle rect = new Gdk.Rectangle(dx, dy, 0, ConstantHeight);

            System.Drawing.Color backColor = System.Drawing.Color.WhiteSmoke;
            System.Drawing.Color textColor = System.Drawing.Color.Black;

            if (isRowSelected(row))
            {
               if (HasFocus)
                  backColor = System.Drawing.Color.DarkGray;
               else
                  backColor = System.Drawing.Color.LightGray;
            }
            else
            {
               if (GetColorDelegate != null)
                  GetColorDelegate(row, ref backColor, ref textColor);
            }

            backgound.RgbFgColor = new Gdk.Color(backColor.R, backColor.G, backColor.B);
            text.RgbFgColor = new Gdk.Color(textColor.R, textColor.G, textColor.B);

            for (int c = 0; c < columns.Length; c++)
            {
               ColumnControl.Column column = columns[c];
               int columnIndex = column.SortOrder;
               int xwidth = column.Width;
               if (dx > exposeRect.Right)
                  break;
               rect = new Gdk.Rectangle(rect.Left, rect.Top, xwidth + mColumnControl.GripperWidth, ConstantHeight);
               if (c == columns.Length - 1)
                  rect.Width = Math.Max(rect.Width, exposeRect.Right - rect.Left + 1);
               String content = GetContentDelegate(row, columnIndex);
               LineLayout.SetText(content);
               win.DrawRectangle(backgound, true, rect);
               dx += 2;
               win.DrawLayout(text, dx, dy, LineLayout);
               dx += xwidth + mColumnControl.GripperWidth - 2;
               rect.Offset(xwidth + mColumnControl.GripperWidth, 0);
            }
            hscrollRange = Math.Max(hscrollRange, dx + rect.Width);
            dy += ConstantHeight;
            if (dy > exposeRect.Bottom)
               break;
            if (fulldraw && exposeRect.Height - dy >= ConstantHeight)
               BottomVisibleRow++;
         }

         if (fulldraw)
         {
            int pageSize = BottomVisibleRow - TopVisibleRow;
            if (vscrollbar1.Adjustment.PageSize != pageSize)
            {
               vscrollbar1.Adjustment.PageSize = pageSize;
               vscrollbar1.Adjustment.PageIncrement = pageSize;
            }
            hscrollRange += (int)hscrollbar1.Value;
            if (hscrollRange > 0)
               hscrollbar1.SetRange(0, hscrollRange);

            // position current row inside visible area
            // TODO: please think about, because of double redraw a more sophisticated solution could be possible
            if (CurrentRow >= 0 && CurrentRow < RowCount)
            {
               if (CurrentRow < TopVisibleRow)
                  OffsetCursor(TopVisibleRow - CurrentRow);
               else if (CurrentRow > BottomVisibleRow)
                  OffsetCursor(BottomVisibleRow - CurrentRow);
            }
         }

#if DEBUG2
            if (ComponentManager != null)
            {
                String t1 = String.Format("Expose.Area={0}, size={1}.{2}",
                                          expose.Area.ToString(), width, height);
                String t2 = String.Format("{0} T={1} B={2}", fulldraw ? "FULL" : "PART", TopVisibleRow, BottomVisibleRow);
                ComponentManager.MessageWriteLineInvoke(String.Format("{0} {1}", t1, t2));
            }
#endif
      }

      protected override bool OnScrollEvent(Gdk.EventScroll evnt)
      {
         if (evnt.Direction == Gdk.ScrollDirection.Down)
            vscrollbar1.Value = vscrollbar1.Value + 1;
         else if (evnt.Direction == Gdk.ScrollDirection.Up)
            vscrollbar1.Value = vscrollbar1.Value - 1;
         return base.OnScrollEvent(evnt);
      }

      // TODO throw away this function, use ShowContextMenu() from class TreeViewExtensions
      internal void ShowDockPopupMenu (uint time)
      {
         Menu menu = new Menu ();

         ColumnControl.Column[] columns = mColumnControl.GetColumns();
         foreach(ColumnControl.Column c in columns)
         {
            TaggedLocalizedCheckedMenuItem item = new TaggedLocalizedCheckedMenuItem(c.Name);
            item.Active = c.Visible;
            item.Tag = c;
            item.Activated += (object sender, EventArgs e) => 
            {
               TaggedLocalizedCheckedMenuItem it = sender as TaggedLocalizedCheckedMenuItem;
               ColumnControl.Column ct = it.Tag as ColumnControl.Column;
               // TODO: change column visibility, recalculate column control and redraw all
               //ct.Visible = !ct.Visible;
            };
            menu.Add(item);
         }
         
         menu.ShowAll ();
         menu.Popup (null, null, null, 3, time);
      }


      protected override bool OnButtonPressEvent(Gdk.EventButton evnt)
      {
         if(evnt.TriggersContextMenu())
         {
            ShowDockPopupMenu(evnt.Time);
         }
         else if (evnt.Button == 1 && evnt.Type == Gdk.EventType.ButtonPress)
         {
            int row = (int)evnt.Y / ConstantHeight + (int)vscrollbar1.Value;
            OffsetCursor(row - CurrentRow);
            if (!HasFocus)
               GrabFocus();
         }
         return base.OnButtonPressEvent(evnt);
      }

      protected override bool OnFocusInEvent(Gdk.EventFocus evnt)
      {
         drawingarea.QueueDraw();
         return base.OnFocusInEvent(evnt);
      }

      protected override bool OnFocusOutEvent(Gdk.EventFocus evnt)
      {
         SelectionMode = false;
         drawingarea.QueueDraw();
         return base.OnFocusOutEvent(evnt);
      }

      /// <summary>
      /// Move Cursor to line
      /// </summary>
      /// <param name="index"></param>
      public void MoveCursorToIndex(int index)
      {
         OffsetCursor(index - CurrentRow);
      }

      private void OffsetCursor(int offset)
      {
         if (offset == 0)
            return;

         int oldRow = CurrentRow;
         CurrentRow += offset;
         if (CurrentRow < 0)
            CurrentRow = 0;
         else if (CurrentRow >= RowCount)
            CurrentRow = RowCount - 1;

         bool redraw = oldRow != CurrentRow;
         DocumentEnd = CurrentRow == RowCount - 1;

         if (CurrentRow < TopVisibleRow)
         {
            redraw = true;
            vscrollbar1.Value = CurrentRow;
         }
         else if (CurrentRow > BottomVisibleRow)
         {
            redraw = true;
            vscrollbar1.Value = CurrentRow - (BottomVisibleRow - TopVisibleRow);
         }

         if (redraw)
            drawingarea.QueueDraw();

         if (!SelectionMode)
            SelectedRow = CurrentRow;

         // provide current row now with an event
         if (CurrentRow != oldRow && CurrentRow < RowCount && CurrentRowChanged != null)
            CurrentRowChanged(this, new VirtualListViewEventArgs(oldRow, CurrentRow));
      }

      protected override bool OnKeyReleaseEvent(Gdk.EventKey evnt)
      {
         switch (evnt.Key)
         {
            case Gdk.Key.Shift_L:
               SelectionMode = false;
               return true;

            case Gdk.Key.Shift_R:
               SelectionMode = false;
               return true;
         }
         return base.OnKeyReleaseEvent(evnt);
      }

      protected override bool OnKeyPressEvent(Gdk.EventKey evnt)
      {
         switch (evnt.Key)
         {
            case Gdk.Key.Shift_L:
               SelectionMode = true;
               return true;

            case Gdk.Key.Shift_R:
               SelectionMode = true;
               return true;

            case Gdk.Key.Up:
            case Gdk.Key.KP_Up:
               OffsetCursor(-1);
               return true;

            case Gdk.Key.Page_Up:
            case Gdk.Key.KP_Page_Up:
               OffsetCursor(-(BottomVisibleRow - TopVisibleRow));
               return true;

            case Gdk.Key.Down:
            case Gdk.Key.KP_Down:
               OffsetCursor(+1);
               return true;

            case Gdk.Key.Page_Down:
            case Gdk.Key.KP_Page_Down:
               OffsetCursor(BottomVisibleRow - TopVisibleRow);
               return true;

            case Gdk.Key.Home:
            case Gdk.Key.KP_Home:
               OffsetCursor(int.MinValue / 2);
               return true;

            case Gdk.Key.End:
            case Gdk.Key.KP_End:
               OffsetCursor(int.MaxValue / 2);
               return true;

            case Gdk.Key.Left:
            case Gdk.Key.KP_Left:
               return true;

            case Gdk.Key.Right:
            case Gdk.Key.KP_Right:
               return true;
         }

         return base.OnKeyPressEvent(evnt);
      }

      protected void OnVscrollbar1ValueChanged(object sender, EventArgs e)
      {
         drawingarea.QueueDraw();
      }

      protected void OnHscrollbar1ValueChanged(object sender, EventArgs e)
      {
         mColumnControl.SetScrollOffset((int)hscrollbar1.Value);
         drawingarea.QueueDraw();
      }

      void ILocalizableWidget.Localize(string namespc)
      {
         ((ILocalizableWidget)mColumnControl).Localize(namespc);
      }
   }

   public class VirtualListViewEventArgs : EventArgs
   {
      public VirtualListViewEventArgs(int old, int _new)
      {
         OldRow = old;
         CurrentRow = _new;
      }
      public int OldRow { get; private set; }
      public int CurrentRow { get; private set; }
   }

   public delegate void VirtualListViewEventHandler(object sender, VirtualListViewEventArgs e);


   public class ColumnControl : Gtk.Fixed, ILocalizableWidget
   {
      public ColumnControl()
      {
         GripperWidth = 8;

         this.HasWindow = false;
         this.ExposeEvent += new ExposeEventHandler(TheExposeEvent);

         // need an additional EventBox, because the underlaying Gtk.Fixed widget don't receive and handle some possible events
         // this is one more suspicious behaviour of GTK
         // as you can see, the mouse buttons will be received by the EventBox widget,
         // but the mouse motion received by Fixed widget enabled by the EventBox ...
         EventBox = new EventBox();
         EventBox.Events |= Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.PointerMotionMask | Gdk.EventMask.LeaveNotifyMask;
         EventBox.ButtonPressEvent += new ButtonPressEventHandler(TheButtonPressEvent);
         EventBox.ButtonReleaseEvent += new ButtonReleaseEventHandler(TheButtonReleaseEvent);
         EventBox.LeaveNotifyEvent += new LeaveNotifyEventHandler(TheLeaveNotifyEvent);
         EventBox.VisibleWindow = false; // must not drawn itself
         this.Add(EventBox);
         EventBox.ShowAll();
      }

      int HitGripper(int x)
      {
         int[] gripper = GripperPositions();
         for (int i = 0; i < gripper.Length; i++)
         {
            Gdk.Rectangle rect = new Gdk.Rectangle(gripper[i], 0, 5, TotalHeight);
            if (rect.Contains(x, 0))
               return i;
         }
         return -1;
      }

      void TheButtonPressEvent(object o, ButtonPressEventArgs args)
      {
         if (args.Event.Button == 1 && DragGripper < 0)
         {
            DragGripper = HitGripper((int)args.Event.X);
            LastDragX = (int)args.Event.X;
         }
      }

      void TheButtonReleaseEvent(object o, ButtonReleaseEventArgs args)
      {
         if (args.Event.Button == 1 && DragGripper >= 0)
            DragGripper = -1;
      }

      public static Gdk.Cursor CursorSizing = new Gdk.Cursor(Gdk.CursorType.SbHDoubleArrow);

      void TheLeaveNotifyEvent(object o, LeaveNotifyEventArgs args)
      {
         EventBox.GdkWindow.Cursor = null;
      }

      protected override bool OnMotionNotifyEvent(Gdk.EventMotion evnt)
      {
         if (DragGripper < 0)
         {
            int gripper = HitGripper((int)evnt.X);
            if (gripper >= 0 && evnt.Y >= 0 && evnt.Y < TotalHeight)
               EventBox.GdkWindow.Cursor = CursorSizing;
            else
               EventBox.GdkWindow.Cursor = null;
         }
         else
         {
            int dx = (int)(evnt.X) - LastDragX;

            Widget widget = GetWidgetFromGripperIndex(DragGripper);
            if (widget != null)
            {
               Column c = mColumns[widget];
               if (c != null && c.Width + dx >= c.MinWidth)
               {
                  LastDragX = (int)(evnt.X);
                  SetColumnWidth(widget, c.Width + dx);
                  if (ColumnChanged != null)
                     ColumnChanged(this, null);
               }
            }
         }
         return true;
      }

      void SetColumnWidth(Widget widget, int width)
      {
         Column c = mColumns[widget];
         if (c != null)
         {
            int dx = width - c.Width;
            c.Width += dx;
            Requisition r = widget.SizeRequest();
            widget.SetSizeRequest(c.Width, r.Height);
            this.Move(widget, c.X - mCurrentScollOffset, TopOffset); // move to same position, helper to redraw parent

            for (int i = DragGripper + 1; i < mColumns.Count; i++)
            {
               widget = GetWidgetFromGripperIndex(i);
               if (widget == null)
                  break;
               c = mColumns[widget];
               c.X += dx;
               this.Move(widget, c.X - mCurrentScollOffset, TopOffset);
            }
         }
      }

      public Gtk.EventBox EventBox;

      Dictionary<Widget, Column> mColumns = new Dictionary<Widget, Column>();
      public ColumnChangedEventHandler ColumnChanged;

      /// <summary>
      /// </summary>
      public delegate void ColumnChangedEventHandler(object obj, EventArgs e);

         public int GripperWidth { get; private set; }

         int TopOffset = 2;
         int TotalHeight = 0;
         int DragGripper = -1;
         int LastDragX = 0;

         public void AddColumn(string name, Widget widget, int tag, int width = 50, int min_width = 20)
         {
            int offset = 0;
            foreach (KeyValuePair<Widget, Column> kvp in mColumns)
               if (kvp.Key.Visible)
                  offset += kvp.Value.Width + GripperWidth;

            Column column = new Column(name, widget, tag, width, min_width) { SortOrder = mColumns.Count, X = offset };
            mColumns.Add(widget, column);
            base.Put(widget, offset, TopOffset);
            widget.SizeAllocated += (o, args) =>
            {
               Widget w = (Widget)o;
               Column co = mColumns[w];
               if (co.Initialized)
                  return;
               co.Initialized = true;
               w.SetSizeRequest(co.Width, args.Allocation.Height);

               int ewidth, eheight;
               this.EventBox.GetSizeRequest(out ewidth, out eheight);
               if (args.Allocation.Height > eheight)
               {
                  this.EventBox.SetSizeRequest(5000, args.Allocation.Height);
                  TotalHeight = args.Allocation.Height;
               }
            };
         }

         Widget GetWidgetFromGripperIndex(int i)
         {
            foreach (KeyValuePair<Widget, Column> kvp in mColumns)
            {
               Widget w = kvp.Key;
               Column c = kvp.Value;
               if (c.GripperIndex == i)
                  return w;
            }
            return null;
         }

         int[] GripperPositions()
         {
            List<int> gripper = new List<int>();
            foreach (KeyValuePair<Widget, Column> kvp in mColumns)
            {
               Widget w = kvp.Key;
               Column c = kvp.Value;
               if (w.Visible)
               {
                  c.GripperIndex = gripper.Count;
                  gripper.Add(w.Allocation.Right + 3);
               }
            }
            return gripper.ToArray();
         }

         void TheExposeEvent(object o, ExposeEventArgs args)
         {
            Gdk.EventExpose expose = args.Args[0] as Gdk.EventExpose;
            Gdk.Window win = expose.Window;

            Gdk.GC gc = new Gdk.GC(this.GdkWindow);
            gc.RgbFgColor = new Gdk.Color(150, 150, 150);

            int[] gripper = GripperPositions();
            int dy = 8;
            foreach (int x in gripper)
            {
               win.DrawLine(gc, x + 2, dy, x + 2, TotalHeight - dy + 1);
            }
         }

         public Column[] GetColumns()
         {
            List<Column> c = new List<Column>();
            foreach (KeyValuePair<Widget, Column> kvp in mColumns)
               c.Add(kvp.Value);
            return c.ToArray();
         }

         int mCurrentScollOffset = 0;

         public void SetScrollOffset(int offset)
         {
            if (mCurrentScollOffset == offset)
               return;
            mCurrentScollOffset = offset;
            int dx = -offset;
            foreach (Column c in mColumns.Values)
            {
               if (c.Widget == null)
                  continue;
               Widget w = c.Widget;
               this.Move(w, dx, TopOffset); // move to same position, helper to redraw parent
               dx += w.Allocation.Width + GripperWidth;
            }
         }

         void ILocalizableWidget.Localize(string namespc)
         {
            foreach (Column c in mColumns.Values)
            {
               if (c.Widget == null)
                  continue;
               if (c.Widget is Gtk.Container)
                  Localization.LocalizeControls(namespc, c.Widget as Gtk.Container);
               if (c.Widget is ILocalizableWidget)
                  (c.Widget as ILocalizableWidget).Localize(namespc);
            }
         }

         public Column[] GetVisibleColumnsInDrawOrder()
         {
            // TODO: could be initialzed on any change only and not an any method call
            List<Column> c = new List<Column>();
            foreach (KeyValuePair<Widget, Column> kvp in mColumns)
            {
               if (kvp.Key.Visible)
                  c.Add(kvp.Value);
            }
            c.Sort(CompareSortOrder);
            return c.ToArray();
         }

         private static int CompareSortOrder(Column c1, Column c2)
         {
            if (c1.SortOrder < c2.SortOrder)
               return -1;
            else
            if (c1.SortOrder > c2.SortOrder)
               return 1;
            return 0;
         }

         public class Column
         {
            public Column(string name, Widget w, int tag, int width, int minWidth)
            {
               Initialized = false;
               Name = name;
               Widget = w;
               Tag = tag;
               Width = width;
               MinWidth = minWidth;
            }

            public bool Initialized { get; set; }

            public string Name { get; set; }

            public Widget Widget { get; set; }

            public int Tag { get; set; }

            public int SortOrder { get; set; }

            public int X { get; set; }

            public int Width { get; set; }

            public int MinWidth { get; set; }

            public int GripperIndex { get; set; }

            public bool Visible { get { return Widget.Visible; } }
         }
      }

      [Serializable()]
      public class VirtualListPersistence
      {
         public int Tag { get; set; }

         public bool Visible { get; set; }

         public int Width { get; set; }
      }

      [Serializable]
      public class VirtualListPersistenceList
      {
         public List<VirtualListPersistence> Persistence = new List<VirtualListPersistence>();
      }
   }
