using System;
using Gtk;
using GLib;
using System.Collections.Generic;

namespace Examples.VirtualList
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class VirtualListView : Gtk.Bin
    {
        public VirtualListView ()
        {
            this.Build();
            LineLayout = GetLayout();
            int width, height;
            LineLayout.SetMarkup("XYZ");
            LineLayout.GetPixelSize(out width, out height);
            ConstantHeight = height;
            CurrentRow = 0;
            vscrollbar1.SetRange(0, RowCount);
       
            hpaned.Add(hpaned1);
            hpaned.Add(hpaned2);
            hpaned.Add(hpaned3);
            hpanedPosition.Add(hpaned1.Position);
            hpanedPosition.Add(hpaned2.Position);
            hpanedPosition.Add(hpaned3.Position);

            // todo: MoveHandle event will not called, don't know why
            hpaned1.MoveHandle += HandleMoveHandle;
            hpaned2.MoveHandle += HandleMoveHandle;
            hpaned3.MoveHandle += HandleMoveHandle;

            moveHandleTimer = new System.Timers.Timer();
            moveHandleTimer.Elapsed += HandleElapsed;
            moveHandleTimer.Interval = 50;
            moveHandleTimer.Enabled = true;
        }

        void HandleElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
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

        private String GetContent(int line, int row)
        {
            return String.Format("Line:{0} Row:{1}", line, row);
        }

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
                hpaned1.Position = 50;
                hpaned2.Position = 100;
                hpaned3.Position = 200;
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
                if (row == CurrentRow)
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
        }

        protected override bool OnKeyPressEvent(Gdk.EventKey evnt)
        {
            switch (evnt.Key)
            {
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


#if false

// String b = @"<span foreground=#0000FF>Blue text</span> is <i>cool</i>!";
//String body = "Body <b>context</b> string <span foreground=\"blue\">with any</span> data encoded";
String body = "Body <b>context</b> string:" + refreshCount++.ToString() + " : " + offset.ToString();
Pango.Layout bodyLayout = GetLayout(body);

int w1, h1;
bodyLayout.GetPixelSize(out w1, out h1);

win.DrawRectangle(Style.DarkGC(StateType.Normal), true, rect);

//Gdk.Region region = Gdk.Region.Rectangle(new Gdk.Rectangle(10, 100, 30, 100));
//win.InputShapeCombineRegion(region, 0, 0);
//Gdk.Region clip = win.ClipRegion;
//clip.Intersect(region);
//win.InputShapeCombineRegion(clip, 0, 0);
//win.BeginPaintRegion(region);

int i = 0;
for (; i < 3; i++)
{
    win.DrawLayout(Style.BlackGC, 2, offset + 2 + h1 * i, bodyLayout);
}
for (; i < 6; i++)
{
    win.DrawRectangle(Style.LightGC(StateType.Active), true, 2, offset + 2 + h1 * i, w1, h1);
    win.DrawLayout(Style.BlackGC, 2, offset + 2 + h1 * i, bodyLayout);
    
}
for (; i < 9; i++)
{
    win.DrawLayoutWithColors(Style.BlackGC, 2, offset + 2 + h1 * i,
                             bodyLayout, new Gdk.Color(255, 0, 0), new Gdk.Color(100, 100, 0));
}
// win.MoveRegion(region, 10, 10);
//win.EndPaint();


var cr = Gdk.CairoHelper.Create(win);

// Set clipping area in order to avoid unnecessary drawing
cr.Rectangle(50, 50, 50, 50);
cr.Clip();

PointD p1,p2,p3,p4;
p1 = new PointD (60,60);
p2 = new PointD (200,60);
p3 = new PointD (200,200);
p4 = new PointD (60,200);

cr.MoveTo (p1);
cr.LineTo (p2);
cr.LineTo (p3);
cr.LineTo (p4);
cr.LineTo (p1);
cr.ClosePath ();

cr.Color = new Color (0,0,0);
cr.FillPreserve ();
cr.Color = new Color (1,0,0);
cr.Stroke ();

((IDisposable) cr.Target).Dispose ();                                      
((IDisposable) cr).Dispose ();
#endif

