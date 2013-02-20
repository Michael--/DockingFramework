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
            public Column(String name, Widget widget, int width, bool visible)
            {
                Name = name;
                Widget = widget;
                Width = width;
                Visible = visible;
            }

            public String Name { get; private set; }
            public Widget Widget { get; set; }
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
        private void HandleMoveHandle (object o, MoveHandleArgs args)
        {
            Console.WriteLine("HandleMoveHandle args={0}", args.ToString());
            drawingarea.QueueDraw();
        }

        /// <summary>
        /// Sets the get content delegate. Will be called for any content request.
        /// </summary>
        public ContentDelegate GetContentDelegate { private get; set; } 
        public delegate String ContentDelegate(int row, int column);

        /// <summary>
        /// Gets the current row index
        /// </summary>
        public int CurrentRow { get; private set; }

        /// <summary>
        /// Gets or sets the row count, the possible size of the list
        /// </summary>
        public  int RowCount
        {
            get
            {
                return mRow;
            }
            set
            {
                mRow = value;
                CurrentRow = Math.Max(CurrentRow, mRow - 1);
                SelectedRow = Math.Max(CurrentRow, mRow - 1);
                vscrollbar1.SetRange(0, Math.Max(0, mRow - 1));
                drawingarea.QueueDraw();
            }
        }
        private int mRow;


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
        public void AddColumn(String name, int width, bool visible)
        {
            Label label = new Label(name);
            label.SetPadding(2, 2);
            columns.Add(name, new Column(name, label, width, visible));
        }

        /// <summary>
        /// Adds a new column with an explicit given widget.
        /// Make visible with UpdateColumns() at least.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="widget">Widget.</param>
        /// <param name="width">Width.</param>
        /// <param name="visible">If set to <c>true</c> visible.</param>
        public void AddColumn(String name, Widget widget, int width, bool visible)
        {
            columns.Add(name, new Column(name, widget, width, visible));
        }

        /// <summary>
        /// Removes a column. Make visible with UpdateColumns().
        /// </summary>
        /// <param name="name">Name.</param>
        public void RemoveColumn(String name)
        {
            columns.Remove(name);
        }

        /// <summary>
        /// Clears the column definitions. Make visible with UpdateColumns(). 
        /// </summary>
        public void ClearColumns()
        {
            columns.Clear();
        }

        /// <summary>
        /// Sets the column visibility. 
        /// Make visible with UpdateColumns(). 
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="visible">If set to <c>true</c> visible.</param>
        public void SetColumnVisibility(String name, bool visible)
        {
            Column c;
            if (columns.TryGetValue(name, out c))
                c.Visible = visible;
        }

        /// <summary>
        /// Sets the width of the column.
        /// Make visible with UpdateColumns(). 
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="width">Width.</param>
        public void SetColumnWidth(String name, int width)
        {
            Column c;
            if (columns.TryGetValue(name, out c))
                c.Width = width;
        }

        /// <summary>
        /// Gets the width of the column.
        /// Make visible with UpdateColumns(). 
        /// </summary>
        /// <returns>The column width.</returns>
        /// <param name="name">Name.</param>
        public int GetColumnWidth(String name)
        {
            Column c;
            if (columns.TryGetValue(name, out c))
                return c.Width;
            return 0;
        }

        /// <summary>
        /// Updates the columns view and make all changes visible
        /// </summary>
        public void UpdateColumns()
        {
            lock (hpanedPosition)
            {
                hpaned.Clear();
                hpanedPosition.Clear();
                int countVisible = 0;
                foreach (KeyValuePair<String, Column> kvp in columns)
                {
                    if (kvp.Value.Visible)
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
                foreach (KeyValuePair<String, Column> kvp  in columns)
                {
                    if (!kvp.Value.Visible)
                        continue;

                    if (add)
                        AddNewHPaned(hp, kvp.Value.Width);
                    add = false;

                    Widget widget = kvp.Value.Widget;

                    if (hp.Child1 == null)
                    {
                        hp.Add1(widget);
                    }
                    else if (countVisible == 1)
                    {
                        hp.Add2(widget);
                    }
                    else
                    {
                        HPaned hp2 = new HPaned();
                        AddNewHPaned(hp2, kvp.Value.Width);
                        hp.Add2(hp2);
                        hp = hp2;
                        hp.Add1(widget);
                    }
                    countVisible--;
                }
                vbox1.ShowAll();
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

        Dictionary<String, Column> columns = new Dictionary<string, Column>();
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
            if (GetContentDelegate == null)
                return; // todo: an error message could be displayed
          
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
                    String content = GetContentDelegate(row, column);
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
