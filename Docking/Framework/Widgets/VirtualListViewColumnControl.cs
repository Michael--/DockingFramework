
using System;
using System.Collections.Generic;
using System.Linq;
using Docking.Components;
using Docking.Tools;
using Gtk;

namespace Docking.Widgets
{
   /// <summary>
   /// The ColumnControl widget for <seealso cref="VirtualListView"/>.
   /// </summary>
   public class VirtualListViewColumnControl : Fixed, ILocalizableWidget
   {
      /// <summary>
      /// </summary>
      public delegate void ColumnChangedEventHandler(object obj, EventArgs e);

      private int DragGripper = -1;
      private int LastDragX = 0;
      private int mCurrentScollOffset = 0;
      private int TotalHeight = 0;
      private int TotalWidth = 0;
      private readonly int TopOffset = 2;

      private readonly EventBox EventBox;
      private readonly Dictionary<Widget, Column> mColumns = new Dictionary<Widget, Column>();

      private readonly VirtualListView mView;
      public static Gdk.Cursor CursorSizing = new Gdk.Cursor(Gdk.CursorType.SbHDoubleArrow);

      public ColumnChangedEventHandler ColumnChanged;

      /// <summary>
      /// Initializes a new instance
      /// </summary>
      /// <param name="view">The parent widget</param>
      public VirtualListViewColumnControl(VirtualListView view)
      {
         mView = view;
         GripperWidth = 8;

         HasWindow = false;
         ExposeEvent += new ExposeEventHandler(TheExposeEvent);

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
         Add(EventBox);
         EventBox.ShowAll();
      }

      public int GripperWidth { get; private set; }

      public int MinViewWidth
      {
         get { return TotalWidth; }
      }

      public IEnumerable<KeyValuePair<Widget, Column>> Columns
      {
         get { return mColumns; }
      }

      void ILocalizableWidget.Localize(string namespc)
      {
         foreach (Column c in mColumns.Values)
         {
            if (c.Widget == null)
            {
               continue;
            }

            if (c.Widget is Container)
            {
               Localization.LocalizeControls(namespc, c.Widget as Container);
            }

            if (c.Widget is ILocalizableWidget)
            {
               (c.Widget as ILocalizableWidget).Localize(namespc);
            }
         }
      }

      public void ArangeColumns()
      {
         int dx = 0;
         foreach (var kvp in mColumns)
         {
            var w = kvp.Key;
            var c = kvp.Value;
            if (c.Visible)
            {
               c.X = dx;
               Move(w, dx - mCurrentScollOffset, TopOffset);
               dx += c.Width + GripperWidth;
            }
         }
      }

      public void AddColumn(string name, Widget widget, int tag, Pango.Layout layout, int width = 50, int min_width = 20)
      {
         int offset = 0;
         foreach (KeyValuePair<Widget, Column> kvp in mColumns)
         {
            if (kvp.Key.Visible)
            {
               offset += kvp.Value.Width + GripperWidth;
            }
         }

         Column column = new Column(name, widget, tag, width, min_width, layout) { SortOrder = mColumns.Count, X = offset };
         mColumns.Add(widget, column);
         Put(widget, offset, TopOffset);
         widget.SizeAllocated += (o, args) =>
         {
            Widget w = (Widget)o;
            Column co = mColumns[w];
            if (co.Initialized)
            {
               return;
            }

            co.Initialized = true;
            w.SetSizeRequest(co.Width, args.Allocation.Height);

            int ewidth, eheight;
            EventBox.GetSizeRequest(out ewidth, out eheight);
            if (args.Allocation.Height > eheight)
            {
               TotalHeight = args.Allocation.Height;
               EventBox.SetSizeRequest(TotalWidth, TotalHeight);
            }
         };
      }

      public Column GetColumn(int tag)
      {
         foreach (var c in mColumns.Values)
         {
            if (c.Tag == tag)
            {
               return c;
            }
         }

         return null;
      }

      public void Update(int tag, int width, bool visible)
      {
         for (int i = 0; i < mColumns.Count; i++)
         {
            var kvp = mColumns.ElementAt(i);
            if (kvp.Value.Tag == tag)
            {
               SetColumnWidth(i, kvp.Key, width, visible);
               break;
            }
         }
      }

      public IEnumerable<Column> GetColumns()
      {
         return mColumns.Values;
      }

      public void SetScrollOffset(int offset)
      {
         if (mCurrentScollOffset == offset)
         {
            return;
         }

         mCurrentScollOffset = offset;
         int dx = -offset;
         foreach (Column c in mColumns.Values)
         {
            if (c.Widget != null && c.Visible)
            {
               Widget w = c.Widget;
               Move(w, dx, TopOffset); // move to same position, helper to redraw parent
               dx += w.Allocation.Width + GripperWidth;
            }
         }
      }

      public Column[] GetVisibleColumnsInDrawOrder()
      {
         // TODO: could be initialzed on any change only and not an any method call
         List<Column> c = new List<Column>();
         foreach (KeyValuePair<Widget, Column> kvp in mColumns)
         {
            if (kvp.Key.Visible)
            {
               c.Add(kvp.Value);
            }
         }

         c.Sort(CompareSortOrder);
         return c.ToArray();
      }

      protected override bool OnMotionNotifyEvent(Gdk.EventMotion evnt)
      {
         if (DragGripper < 0)
         {
            int gripper = HitGripper((int)evnt.X);
            if (gripper >= 0 && evnt.Y >= 0 && evnt.Y < TotalHeight)
            {
               EventBox.GdkWindow.Cursor = CursorSizing;
            }
            else
            {
               EventBox.GdkWindow.Cursor = null;
            }
         }
         else
         {
            int dx = (int)(evnt.X) - LastDragX;
            if (dx != 0)
            {
               var kvp = mColumns.ElementAt(DragGripper);
               Widget widget = kvp.Key;
               Column c = kvp.Value;
               if (c.Width + dx >= c.MinWidth)
               {
                  LastDragX = (int)(evnt.X);
                  SetColumnWidth(DragGripper, widget, c.Width + dx);
                  if (ColumnChanged != null)
                  {
                     ColumnChanged(this, null);
                  }
               }
            }
         }

         return true;
      }

      private int HitGripper(int x)
      {
         var gripper = GripperPositions();
         foreach (var g in gripper)
         {
            int xv = g.Value;
            Gdk.Rectangle rect = new Gdk.Rectangle(xv - GripperWidth, 0, GripperWidth, TotalHeight);
            if (rect.Contains(x, 0))
            {
               return g.Key;
            }
         }

         return -1;
      }

      private void TheButtonPressEvent(object o, ButtonPressEventArgs args)
      {
         for (int i = 0; i < mColumns.Count; i++)
         {
            var kvp = mColumns.ElementAt(i);
            Widget w = kvp.Key;
            if (w.Visible && w.Allocation.Contains(new Gdk.Point((int)args.Event.X, w.Allocation.Top)))
            {
               mView.CallHeaderClickedEvent(args, i);
               break;
            }
         }

         if (args.Event.Button == Mouse.LEFT_MOUSE_BUTTON && DragGripper < 0)
         {
            DragGripper = HitGripper((int)args.Event.X);
            LastDragX = (int)args.Event.X;
         }
      }

      private void TheButtonReleaseEvent(object o, ButtonReleaseEventArgs args)
      {
         if (args.Event.Button == Mouse.LEFT_MOUSE_BUTTON && DragGripper >= 0)
         {
            DragGripper = -1;
         }
      }

      private void TheLeaveNotifyEvent(object o, LeaveNotifyEventArgs args)
      {
         EventBox.GdkWindow.Cursor = null;
      }

      private void SetColumnWidth(int index, Widget widget, int width, bool? visible = null)
      {
         Column c = mColumns[widget];
         if (c != null)
         {
            if (visible != null)
            {
               c.Visible = visible.Value;
            }

            int dx = width - c.Width;
            c.Width += dx;
            Requisition r = widget.SizeRequest();
            widget.SetSizeRequest(c.Width, r.Height);
            Move(widget, c.X - mCurrentScollOffset, TopOffset); // move to same position, helper to redraw parent

            // move all following
            for (int i = index + 1; i < mColumns.Count; i++)
            {
               var kvp = mColumns.ElementAt(i);
               c = kvp.Value;
               if (c.Visible)
               {
                  widget = kvp.Key;
                  c.X += dx;
                  Move(widget, c.X - mCurrentScollOffset, TopOffset);
               }
            }
         }
      }

      private IEnumerable<KeyValuePair<int, int>> GripperPositions()
      {
         List<KeyValuePair<int, int>> gripper = new List<KeyValuePair<int, int>>();

         for (int i = 0; i < mColumns.Count; i++)
         {
            var kvp = mColumns.ElementAt(i);
            Widget w = kvp.Key;
            if (w.Visible)
            {
               gripper.Add(new KeyValuePair<int, int>(i, w.Allocation.Right + 3));
            }
         }

         return gripper;
      }

      private void TheExposeEvent(object o, ExposeEventArgs args)
      {
         Gdk.EventExpose expose = args.Args[0] as Gdk.EventExpose;
         Gdk.Window win = expose.Window;

         Gdk.GC gc = new Gdk.GC(GdkWindow);
         gc.RgbFgColor = new Gdk.Color(150, 150, 150);

         var gripper = GripperPositions();
         foreach (var g in gripper)
         {
            int x = g.Value;
            win.DrawLine(gc, x + 2, Allocation.Top, x + 2, Allocation.Bottom);
         }

         TotalWidth = gripper.LastOrDefault().Value + 10;
         EventBox.SetSizeRequest(TotalWidth, TotalHeight);
      }

      private static int CompareSortOrder(Column c1, Column c2)
      {
         if (c1.SortOrder < c2.SortOrder)
         {
            return -1;
         }
         else if (c1.SortOrder > c2.SortOrder)
         {
            return 1;
         }

         return 0;
      }

      public class Column
      {
         public Column(string name, Widget w, int tag, int width, int minWidth, Pango.Layout layout)
         {
            Initialized = false;
            Name = name;
            Widget = w;
            Tag = tag;
            Width = width;
            MinWidth = minWidth;
            LineLayout = layout;
         }

         public bool Initialized { get; set; }
         public string Name { get; set; }
         public Widget Widget { get; set; }
         public int Tag { get; set; }
         public int SortOrder { get; set; }
         public int X { get; set; }
         public int Width { get; set; }
         public int MinWidth { get; set; }

         public bool Visible
         {
            get { return Widget.Visible; }
            set { Widget.Visible = value; }
         }

         public Pango.Layout LineLayout { get; set; }
      }
   }
}
