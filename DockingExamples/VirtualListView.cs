using System;
using Gtk;
using GLib;
using System.Collections.Generic;

namespace Examples.VirtualList
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class VirtualListView : Gtk.Bin
    {
        private class Column
        {
            public Column(String name, int width, bool visible)
            {
                Name = name;
                Width = width;
                Visible = visible;
            }

            public String Name { get; private set; }
            public int Width { get; set; }
            public bool Visible { get; set; }
        }

        public VirtualListView ()
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
            vscrollbar1.SetRange(0, RowCount);

            AddColumn("Column1", 50, true);
            AddColumn("Column2", 150, true);
            AddColumn("Column3", 50, true);
            AddColumn("Column4", 50, true);

            UpdateColumns();
           
            moveHandleTimer = new System.Timers.Timer();
            moveHandleTimer.Elapsed += HandleElapsed;
            moveHandleTimer.Interval = 50;
            moveHandleTimer.Enabled = true;
        }

        void HandleElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (hpanedPosition)
            {
                if (!createdWorkaround)
                    return;
                // due to can't find out why HPaned.MoveHandle don't fire
                // its events, use as a workaround a timer polling 
                for (int i = 0; i < hpaned.Count; i++)
                {
                    if (hpaned[i].Position != hpanedPosition[i])
                    {
                        hpanedPosition[i] = hpaned[i].Position;
                        Gtk.Application.Invoke(delegate
                        {
                            drawingarea.QueueDraw();
                        });
                        return;
                    }
                }
            }
        }

        // workaround for HandleMoveHandle
        System.Timers.Timer moveHandleTimer;

        // ubfortunately this event will not called
        void HandleMoveHandle (object o, MoveHandleArgs args)
        {
            Console.WriteLine("HandleMoveHandle args={0}", args.ToString());
            drawingarea.QueueDraw();
        }

        private Pango.Layout LineLayout { get; set; }
        private int ConstantHeight { get; set; }
        private int CurrentRow { get; set; }
        private int SelectedRow { get; set; }
        private bool SelectionMode { get; set; }
        private int TopVisibleRow { get; set; }
        private int BottomVisibleRow { get; set; }
        private int RowCount { get { return 42000; } }
        private int ColumnCount { get { return hpaned.Count + 1; } }
        private int GetColumWidth(int column)
        {
            if (column < hpaned.Count)
            {
                int result = hpaned[column].Position;  

                // consider gripper size of previous columns
                // todo: gripper size may not be constant, replace value 5 soon
                while(--column >= 0)
                    return result += 5;
                return result;
            }
            return 9999;
        }

        private String GetContent(int row, int column)
        {
            return String.Format("Row:{0} Column:{1}", row, column);
        }

        private bool isRowSelected(int row)
        {
            int bottom = Math.Min(CurrentRow, SelectedRow);
            int top = Math.Max(CurrentRow, SelectedRow);
            return row >= bottom && row <= top;
        }

        public void AddColumn(String name, int width, bool visible)
        {
            columns.Add(new Column(name, width, visible));
        }

        private void SetColumnVisibility(String name, bool visible)
        {
        }

        public void UpdateColumns()
        {
            lock (hpanedPosition)
            {
                hpaned.Clear();
                hpanedPosition.Clear();
                int countVisible = 0;
                foreach (Column c in columns)
                {
                    if (c.Visible)
                        countVisible++;
                }

                if (countVisible == 0)
                    return;

                HPaned hp = new HPaned();
                vbox1.Remove(hbox1);
                vbox1.Remove(hscrollbar1);
                vbox1.PackStart(hp, false, true, 0);
                vbox1.PackStart(hbox1, true, true, 0);
                vbox1.PackStart(hscrollbar1, false, true, 0);
                bool add = true;
                foreach (Column c in columns)
                {
                    if (!c.Visible)
                        continue;

                    if (add)
                        AddNewHPaned(hp, c.Width);
                    add = false;

                    HBox hbox = new HBox();
                    Label label = new Label(c.Name);
                    hbox.PackStart(label, false, true, 0);

                    if (hp.Child1 == null)
                    {
                        hp.Add1(hbox);
                    }
                    else if (countVisible == 1)
                    {
                        hp.Add2(hbox);
                    }
                    else
                    {
                        HPaned hp2 = new HPaned();
                        AddNewHPaned(hp2, c.Width);
                        hp.Add2(hp2);
                        hp = hp2;
                        hp.Add1(hbox);
                    }
                    countVisible--;
                }
                createdWorkaround = false;
            }
        }

        private void AddNewHPaned(HPaned hp, int width)
        {
            hp.MoveHandle += HandleMoveHandle;
            hp.Position = width;
            hpaned.Add(hp);
            hpanedPosition.Add(width);
        }

        List<Column> columns = new List<Column>();
        List<HPaned> hpaned = new List<HPaned>();
        List<int> hpanedPosition = new List<int>();

        private Pango.Layout GetLayout()
        {
            Pango.Layout layout = new Pango.Layout(this.PangoContext);
            layout.FontDescription = Pango.FontDescription.FromString("Tahoma 12");
            layout.Wrap = Pango.WrapMode.WordChar;
            return layout;
        }

        private bool createdWorkaround = false;

        protected void OnDrawingareaExposeEvent(object o, Gtk.ExposeEventArgs args)
        {
            /// can't set paned.Position in contructor
            /// all earlier changes useless
            /// until found a better solution, use this workaround 
            /// to set desired position here
            if (!createdWorkaround)
            {
                createdWorkaround = true;
                for (int i = 0; i < hpaned.Count; i++)
                {
                    hpaned[i].Position = hpanedPosition[i];
                }
            }
            Gdk.EventExpose expose = args.Args[0] as Gdk.EventExpose;
            Gdk.Window win = expose.Window;
            Gdk.Rectangle clientRect = expose.Area;

            win.DrawRectangle(Style.LightGC(StateType.Normal), true, clientRect);
          
            int offset = (int)vscrollbar1.Value;
            TopVisibleRow = offset;
            BottomVisibleRow = offset;
            int dy = 0;
            for (int row = offset; row < RowCount; row++)
            {
                int dx = -(int)hscrollbar1.Value;
                Gdk.Rectangle rect = new Gdk.Rectangle(dx, dy, 0, ConstantHeight);
                StateType st;
                if (isRowSelected(row))
                    st = StateType.Selected;
                else if (HasFocus)
                    st = StateType.Prelight;
                else 
                    st = StateType.Insensitive;
                Gdk.GC gc = Style.BackgroundGC(st);

                for (int column = 0; column < ColumnCount; column++)
                {
                    int xwidth = GetColumWidth(column);
                    rect = new Gdk.Rectangle(rect.Left, rect.Top, xwidth, ConstantHeight);
                    if (dx > clientRect.Right)
                        break;
                    String content = GetContent(row, column);
                    LineLayout.SetMarkup(content);
                    win.DrawRectangle(gc, true, rect);
                    dx += 2;
                    win.DrawLayout(Style.BlackGC, dx, dy, LineLayout);
                    dx += xwidth;
                    rect.Offset(xwidth, 0);
                }
                rect.Width = clientRect.Right - rect.Left + 1;
                win.DrawRectangle(Style.BackgroundGC(StateType.Normal), true, rect);

                dy += ConstantHeight;
                if (dy > clientRect.Height)
                    break;
                if (clientRect.Height - dy >= ConstantHeight)
                    BottomVisibleRow++;
            }

            int pageSize = BottomVisibleRow - TopVisibleRow;
            if (vscrollbar1.Adjustment.PageSize != pageSize)
            {
                vscrollbar1.Adjustment.PageSize = pageSize;
                vscrollbar1.Adjustment.PageIncrement = pageSize;
            }
        }

        protected override bool OnScrollEvent(Gdk.EventScroll evnt)
        {
            if (evnt.Direction == Gdk.ScrollDirection.Down)
                vscrollbar1.Value = vscrollbar1.Value + 1;
            else if (evnt.Direction == Gdk.ScrollDirection.Up)
                vscrollbar1.Value = vscrollbar1.Value - 1;
            return base.OnScrollEvent(evnt);
        }

        protected override bool OnButtonPressEvent(Gdk.EventButton evnt)
        {
            Console.WriteLine("OnButtonPressEvent Button={0} Type={1}",
                evnt.Button.ToString(), evnt.Type.ToString());
            if (evnt.Button == 1 && evnt.Type == Gdk.EventType.ButtonPress)
            {
                int row = (int)evnt.Y / ConstantHeight + (int)vscrollbar1.Value;
                MoveCursor(row - CurrentRow);
                if (!HasFocus)
                    GrabFocus();
            }
            return base.OnButtonPressEvent(evnt);
        }

        protected override bool OnFocusInEvent(Gdk.EventFocus evnt)
        {
            Console.WriteLine("OnFocusInEvent IN={0}", evnt.In);
            drawingarea.QueueDraw();
            return base.OnFocusInEvent(evnt);
        }

        protected override bool OnFocusOutEvent(Gdk.EventFocus evnt)
        {
            Console.WriteLine("OnFocusOutEvent IN={0}", evnt.In);
            SelectionMode = false;
            drawingarea.QueueDraw();
            return base.OnFocusOutEvent(evnt);
        }

        private void MoveCursor(int offset)
        {
            int oldRow = CurrentRow;
            CurrentRow += offset;
            if (CurrentRow < 0)
                CurrentRow = 0;
            else if (CurrentRow >= RowCount)
                CurrentRow = RowCount - 1;

            bool redraw = oldRow != CurrentRow;

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
                    MoveCursor(-1);
                    return true;

                case Gdk.Key.Page_Up:
                    MoveCursor(-(BottomVisibleRow - TopVisibleRow));
                    return true;

                case Gdk.Key.Down:
                    MoveCursor(+1);
                    return true;

                case Gdk.Key.Page_Down:
                    MoveCursor(BottomVisibleRow - TopVisibleRow);
                    return true;

                case Gdk.Key.Home:
                    MoveCursor(int.MinValue / 2);
                    return true;
                    
                case Gdk.Key.End:
                    MoveCursor(int.MaxValue / 2);
                    return true;
                    
                case Gdk.Key.Left:
                    return true;
                    
                case Gdk.Key.Right:
                    return true;
            }

            Console.WriteLine("OnKeyPressEvent key={0}, {1}", evnt.Key, evnt.KeyValue);
            return base.OnKeyPressEvent(evnt);
        }

        protected void OnVscrollbar1ValueChanged(object sender, EventArgs e)
        {
            if (!HasFocus)
                GrabFocus();
            drawingarea.QueueDraw();
        }

        protected void OnHscrollbar1ValueChanged(object sender, EventArgs e)
        {
            if (!HasFocus)
                GrabFocus();
            drawingarea.QueueDraw();
        }
    }
}
