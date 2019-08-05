using System;
using System.Collections;
using System.Linq;
using Gdk;
using Gtk;
using Docking.Helper;
using System.Diagnostics;
using System.Text;
using Docking.Tools;
using System.Collections.Generic;

namespace Docking
{
   [System.ComponentModel.ToolboxItem(false)]
   public class TabSelector : Gtk.DrawingArea, ITabStrip
   {
      public TabSelector(bool active)
      {
         Active = active;
         ActiveIndex = -1;
         mTracker = new MouseTracker(this)
         {
            MotionEvents = true,
            EnterLeaveEvents = true,
            ButtonPressedEvents = true
         };

         mTracker.EnterNotify += (sender, e) =>
         {
            QueueDraw();
         };

         mTracker.LeaveNotify += (sender, e) =>
         {
            QueueDraw();
         };

         mTracker.MouseMoved += (sender, e) =>
         {
            QueueDraw();
         };

         mTracker.ButtonPressed += (sender, e) =>
         {
            if (e.Event.TriggersContextMenu())
            {
               int index;
               if (CalculateIndexAtPosition(mTracker.MousePosition.X, out index))
               {
                  var t = mTabs.ElementAt(index).DockItemTitleTab;
                  if (t != null)
                     t.item.ShowDockPopupMenu(e.Event.Time, this);
               }
            }
            else if (e.Event.Button == 1 && e.Event.Type == Gdk.EventType.ButtonPress)
            {
               int index;
               if (CalculateIndexAtPosition(mTracker.MousePosition.X, out index))
               {
                  if (ActiveIndex != index)
                  {
                     ActiveIndex = index;
                     CurrentTab = ActiveIndex;
                     if (SelectTab != null)
                        SelectTab(this, new SelectTabEventArgs(ActiveIndex, true));
                     QueueDraw();
                  }
                  // else
                  {
                     tabPressed = true;
                     pressX = e.Event.X;
                     pressY = e.Event.Y;
                  }
               }
            }
         };

         mTracker.ButtonReleased += (sender, e) =>
         {
            const int LEFT_MOUSE_BUTTON = 1;

            var t = mTabs.ElementAt(m_CurrentTab).DockItemTitleTab;
            if (tabActivated)
            {
               tabActivated = false;
               if (t.item.Status == DockItemStatus.AutoHide)
                  t.item.Status = DockItemStatus.Dockable;
               else
                  t.item.Status = DockItemStatus.AutoHide;
            }
            else if (!e.Event.TriggersContextMenu() && e.Event.Button == LEFT_MOUSE_BUTTON)
            {
               t.frame.DockInPlaceholder(t.item);
               t.frame.HidePlaceholder();
               if (GdkWindow != null)
                  GdkWindow.Cursor = null;
               t.frame.Toplevel.KeyPressEvent -= HeaderKeyPress;
               t.frame.Toplevel.KeyReleaseEvent -= HeaderKeyRelease;
               DockItem.SetFocus(t.Page);
            }
            tabPressed = false;
         };

         mTracker.MouseMoved += (sender, e) =>
         {
            if (m_CurrentTab != -1)
            {
               var t = mTabs.ElementAt(m_CurrentTab).DockItemTitleTab;

               if (tabPressed && !t.item.Behavior.HasFlag(DockItemBehavior.NoGrip) && Math.Abs(e.Event.X - pressX) > 3 && Math.Abs(e.Event.Y - pressY) > 3)
               {
                  t.frame.ShowPlaceholder(t.item);
                  GdkWindow.Cursor = fleurCursor;
                  t.frame.Toplevel.KeyPressEvent += HeaderKeyPress;
                  t.frame.Toplevel.KeyReleaseEvent += HeaderKeyRelease;
                  allowPlaceholderDocking = true;
                  tabPressed = false;
               }
               t.frame.UpdatePlaceholder(t.item, Allocation.Size, allowPlaceholderDocking);
            }
         };
      }


      void HeaderKeyPress(object ob, Gtk.KeyPressEventArgs a)
      {
         var t = mTabs.ElementAt(m_CurrentTab).DockItemTitleTab;
         if (a.Event.Key == Gdk.Key.Control_L || a.Event.Key == Gdk.Key.Control_R)
         {
            allowPlaceholderDocking = false;
            t.frame.UpdatePlaceholder(t.item, Allocation.Size, false);
         }
         if (a.Event.Key == Gdk.Key.Escape)
         {
            t.frame.HidePlaceholder();
            t.frame.Toplevel.KeyPressEvent -= HeaderKeyPress;
            t.frame.Toplevel.KeyReleaseEvent -= HeaderKeyRelease;
            Gdk.Pointer.Ungrab(0);
         }
      }

      void HeaderKeyRelease(object ob, Gtk.KeyReleaseEventArgs a)
      {
         var t = mTabs.ElementAt(m_CurrentTab).DockItemTitleTab;
         if (a.Event.Key == Gdk.Key.Control_L || a.Event.Key == Gdk.Key.Control_R)
         {
            allowPlaceholderDocking = true;
            t.frame.UpdatePlaceholder(t.item, Allocation.Size, true);
         }
      }

      public string LabelProp { get; set; }

      [GLib.Property("xpad")]
      public int Xpad { get; set; }

      [GLib.Property("ypad")]
      public int Ypad { get; set; }

      MouseTracker mTracker;
      bool allowPlaceholderDocking;
      bool tabPressed, tabActivated;
      double pressX, pressY;
      static Gdk.Cursor fleurCursor = new Gdk.Cursor(Gdk.CursorType.Fleur);


      class Tab
      {
         public Tab(string label, Pixbuf image)
         {
            DockItemTitleTab = null;
            Label = label;
            Image = image;
         }

         public Tab(DockItemTitleTab value)
         {
            DockItemTitleTab = value;
            Label = DockItemTitleTab.label;
            if (DockItemTitleTab.tabIcon != null)
               Image = DockItemTitleTab.tabIcon.Pixbuf;
         }

         public DockItemTitleTab DockItemTitleTab { get; private set; }
         public string Label { get; set; }
         public Pixbuf Image { get; set; }
         internal int IdealWidth { get; set; }
         internal float CurrentWidth { get; set; }
      }

      List<Tab> mTabs = new List<Tab>();
      public int ActiveIndex { get; set; }
      public int HoveredIndex { get; set; }
      bool Active { get; set; }
      private bool UseActiveIndex
      {
         get
         {
            int index;
            return (!mTracker.Hovered || !CalculateIndexAtPosition(mTracker.MousePosition.X, out index));
         }
      }

      const int space = 5;

      public class SelectTabEventArgs : EventArgs
      {
         public SelectTabEventArgs(int index, bool selected)
         {
            Index = index;
            Selected = selected;
            Hovered = !Selected;
         }

         public int Index { get; private set; }
         public bool Selected { get; private set; }
         public bool Hovered { get; private set; }
      }

      public delegate void SelectTabEventHandler(object o, SelectTabEventArgs args);

      public event SelectTabEventHandler SelectTab;

      int ALeft = 2;
      int ATop = 2;

      int DrawWidth { get { return Allocation.Width - ALeft - Xpad - Xpad; } }

      protected override void OnSizeRequested(ref Requisition requisition)
      {
         // base.OnSizeRequested(ref requisition);
         requisition.Height = 25;
         requisition.Width = 100;
      }

      /// <summary>
      /// All tabs have to share the header width.
      /// The current selected tab will get the most size, but not more as 2/3 as available.
      /// All other tabs share the rest of the size in equal parts.
      /// </summary>
      void RecalcTabSize(Pango.Layout la, int currentTab)
      {
         // calc best size for all tabs
         foreach (var t in mTabs)
         {
            la.SetMarkup(t.Label);
            int width, height;
            la.GetPixelSize(out width, out height);
            if (t.Image != null)
            {
               width += t.Image.Width;
               width += space;
            }
            width += space;
            t.IdealWidth = width;
            t.CurrentWidth = Math.Min(width, DrawWidth);
         }

         var totalWidth = mTabs.Sum(x => x.CurrentWidth);


         // if sum less than draw limits, grow the last one to the limits
         if (totalWidth < DrawWidth && mTabs.Count() > 0)
         {
            mTabs.Last().CurrentWidth += DrawWidth - totalWidth;
         }

         // if sum of all tabs exceeds draw limits, shrink some tabs
         else if (totalWidth > DrawWidth)
         {
            float otherWidth = totalWidth;
            if (currentTab >= 0 && currentTab < mTabs.Count())
            {
               var t = mTabs.ElementAt(currentTab);
               t.CurrentWidth = Math.Min(t.CurrentWidth, DrawWidth * 2 / 3);
               otherWidth -= t.CurrentWidth;
            }
            var p = (totalWidth - DrawWidth) / otherWidth;
            for (int i = 0; i < mTabs.Count; i++)
            {
               if (i != currentTab)
               {
                  var t = mTabs.ElementAt(i);
                  t.CurrentWidth -= t.CurrentWidth * p;
               }
            }
         }
      }

      void DrawTabs(Pango.Layout la, Gdk.Window win, Gdk.GC gc, Cairo.Context cairo, int currentTab)
      {
         int tx = ALeft + Xpad;
         int ty = ATop + Ypad;



         float left = 0;
         for (int i = 0; i < mTabs.Count(); i++)
         {
            var t = mTabs.ElementAt(i);
            int x1 = (int)(tx + left + 0.5f);
            var cliprect = new Gdk.Rectangle(x1, 0, (int)(t.CurrentWidth + 0.5f), Allocation.Height);
            gc.ClipRectangle = cliprect;
            cairo.Rectangle(cliprect.ToCairoRect());
            cairo.Clip();

            Color colorBG;
            Color colorFG;
            if (i == ActiveIndex && t.DockItemTitleTab.Active)
            {
               colorBG = Style.Background(StateType.Selected);
               colorFG = Style.Foreground(StateType.Selected);
            }
            else if (i == currentTab)
            {
               colorBG = Style.Background(StateType.Prelight);
               colorFG = Style.Foreground(StateType.Prelight);
            }
            else
            {
               colorBG = Style.Background(StateType.Insensitive);
               colorFG = Style.Foreground(StateType.Insensitive);
            }

            DrawRectangle(cairo, cliprect.X, cliprect.Y, cliprect.Width, cliprect.Height, colorBG.ToCairo(), true);

            x1 += space;
            if (t.Image != null)
            {
               win.DrawPixbuf(gc, t.Image, 0, 0, x1, (Allocation.Height - t.Image.Height + Ypad) / 2, -1, -1, Gdk.RgbDither.None, 0, 0);
               x1 += t.Image.Width;
               x1 += space;
            }

            la.SetMarkup(t.Label);
            DrawText(win, gc, x1, ty, colorFG, la);
            left += t.CurrentWidth;

            gc.ClipRectangle = new Gdk.Rectangle(0, 0, Allocation.Width, Allocation.Height);
            cairo.ResetClip();
         }
      }

      bool CalculateIndexAtPosition(int X, out int i)
      {
         i = -1;
         if (mTabs.Count() == 0)
            return false;

         int tx = ALeft + Xpad;
         float left = 0;
         for (i = 0; i < mTabs.Count(); i++)
         {
            var t = mTabs.ElementAt(i);
            int x1 = (int)(tx + left + 0.5f);
            if (X >= x1 && X < x1 + t.CurrentWidth)
               return true;
            left += t.CurrentWidth;
         }
         i = -1;
         return false;
      }

      protected override bool OnExposeEvent(EventExpose evnt)
      {
         var win = evnt.Window;
         int width, height;
         win.GetSize(out width, out height);

         using (var cairo = Gdk.CairoHelper.Create(win))
         using (var gc = new Gdk.GC(win))
         using (var la = new Pango.Layout(PangoContext))
         {
            int index;
            if (!mTracker.Hovered || !CalculateIndexAtPosition(mTracker.MousePosition.X, out index))
            {
               index = ActiveIndex;
            }
            // else
            {
               if (HoveredIndex != index)
               {
                  HoveredIndex = index;
                  if (Active)
                  {
                     CurrentTab = HoveredIndex;
                     if (SelectTab != null)
                        SelectTab(this, new SelectTabEventArgs(HoveredIndex, false));
                  }
               }
            }

            DrawRectangle(cairo, 0, 0, width, height, Style.Mid(StateType.Normal).ToCairo(), true);
            RecalcTabSize(la, index);
            DrawTabs(la, win, gc, cairo, index);

            TooltipText = string.Empty;
            if (index >= 0 && index < mTabs.Count())
            {
               var t = mTabs.ElementAt(index);
               if (t.CurrentWidth != t.IdealWidth)
                  TooltipText = mTabs.ElementAt(index).Label;
            }
         }
         return true;
      }

      void DrawText(Gdk.Window win, Gdk.GC gc, int x, int y, Gdk.Color foregound, Pango.Layout layout)
      {
         gc.RgbFgColor = foregound;
         win.DrawLayout(gc, x, y, layout);
      }

      void DrawRectangle(Cairo.Context cairo, double x, double y, int width, int height, Cairo.Color color, bool filled = false, double lineWidth = 1.0)
      {
         if (!filled)
         {
            cairo.LineWidth = lineWidth;
            cairo.LineCap = Cairo.LineCap.Butt;
            cairo.LineJoin = Cairo.LineJoin.Bevel;
         }

         cairo.SetSourceColor(color);
         cairo.Rectangle(x, y, width, height);

         if (filled)
            cairo.Fill();
         else
            cairo.Stroke();

      }


      #region ITabStrip
      public DockVisualStyle VisualStyle { get; set; }

      public void Clear()
      {
         mTabs.Clear();
         m_CurrentTab = -1;
         ActiveIndex = -1;
      }

      int m_CurrentTab = -1;
      public int CurrentTab
      {
         get
         {
            return m_CurrentTab;
         }
         set
         {
            if (m_CurrentTab == value)
               return;
            if (m_CurrentTab >= 0 && m_CurrentTab < mTabs.Count())
            {
               var t = mTabs.ElementAt(m_CurrentTab).DockItemTitleTab;
               t.Page.Hide();
               t.Active = false;
            }

            m_CurrentTab = value;
            if (m_CurrentTab >= 0 && m_CurrentTab < mTabs.Count())
            {
               var t = mTabs.ElementAt(m_CurrentTab).DockItemTitleTab;
               t.Active = true;
               t.Page.Show();
               if (UseActiveIndex && ActiveIndex != value)
                  ActiveIndex = value;
               else if (ActiveIndex == -1)
                  ActiveIndex = value;
               DockItem.SetFocus(t.Page);
            }
            QueueDraw();
         }
      }

      public bool isVertical { get { return false; } }

      public void Flip()
      {
      }

      public void AddTab(DockItemTitleTab tab)
      {
         mTabs.Add(new Tab(tab));

         tab.Active = false;
         tab.Page.Hide();
      }

      public int TabCount { get { return mTabs.Count(); } }

      public int BottomPadding { get; set; }

      public void SetTabLabel(Gtk.Widget page, Gdk.Pixbuf icon, string label)
      {
         foreach (var t in mTabs)
         {
            if (t.DockItemTitleTab.Page == page)
            {
               t.DockItemTitleTab.SetTitle(page, icon, label);
               t.Label = label;
               t.Image = icon;
               QueueDraw();
               break;
            }
         }
      }

      public void UpdateStyle(DockItem item)
      {
         QueueResize();
      }

      #endregion
   }
}

