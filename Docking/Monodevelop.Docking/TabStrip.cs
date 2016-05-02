//
// TabStrip.cs
//
// Author:
//   Lluis Sanchez Gual
//

//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using Gtk; 

using System;
using System.Linq;
using Docking.Helper;

namespace Docking
{
   class TabStrip : Gtk.EventBox
   {
      int currentTab = -1;
      Box box, vBox, hBox, xbox;
      Label bottomFiller = new Label();
      DockVisualStyle visualStyle;

      public TabStrip(DockFrame frame)
      {
         xbox = new HBox();
         Add(xbox);

         vBox = new TabStripVBox() { TabStrip = this };
         hBox = new TabStripHBox() { TabStrip = this };

         box = hBox;


         xbox.PackStart(hBox, false, false, 0);
         xbox.PackStart(vBox, false, false, 0);

         ShowAll();
         vBox.Hide();
         bottomFiller.Hide();
         BottomPadding = 3;
         WidthRequest = 0;

         box.Removed += HandleRemoved;
      }


      public bool isVertical  { get { return box == vBox; } }

      public void Flip()
      {
         Box a = isVertical ? vBox : hBox;
         Box b = isVertical ? hBox : vBox;

         a.Removed -= HandleRemoved;

         while (a.Children.Count() > 0)
         {
            var child = a.Children[0];
            a.Remove(child);
            b.Add(child);
            if (child is DockItemTitleTab)
               (child as DockItemTitleTab).UpdateBehavior();
         }

         box.Hide();
         box = b;
         box.Removed += HandleRemoved;
         box.Show();
         box.QueueDraw();
         QueueResize();

         var dc = Parent as DockContainer;
         if (dc != null)
            dc.RecalcLayout();
      }

      public int BottomPadding
      {
         get { return bottomFiller.HeightRequest; }
         set
         {
            bottomFiller.HeightRequest = value;
            bottomFiller.Visible = value > 0;
         }
      }

      public DockVisualStyle VisualStyle
      {
         get { return visualStyle; }
         set
         {
            visualStyle = value;
            box.QueueDraw();
         }
      }

      public void AddTab(DockItemTitleTab tab)
      {
         if (tab.Parent != null)
            ((Gtk.Container)tab.Parent).Remove(tab);

         box.PackStart(tab, false, false, 0);
         tab.WidthRequest = tab.LabelWidth;
         if (currentTab == -1)
            CurrentTab = box.Children.Length - 1;
         else
         {
            tab.Active = false;
            tab.Page.Hide();
         }

         tab.ButtonPressEvent += OnTabPress;
      }

      void HandleRemoved(object o, RemovedArgs args)
      {
         Gtk.Widget w = args.Widget;
         w.ButtonPressEvent -= OnTabPress;
         if (currentTab >= box.Children.Length)
            currentTab = box.Children.Length - 1;
      }

      public void SetTabLabel(Gtk.Widget page, Gdk.Pixbuf icon, string label)
      {
         foreach (DockItemTitleTab tab in box.Children)
         {
            if (tab.Page == page)
            {
               tab.SetTitle(page, icon, label);
               UpdateEllipsize(Allocation);
               break;
            }
         }
      }

      public void UpdateStyle(DockItem item)
      {
         QueueResize();
      }

      public int TabCount
      {
         get { return box.Children.Length; }
      }

      public int CurrentTab
      {
         get { return currentTab; }
         set
         {
            if (currentTab == value)
               return;
            if (currentTab != -1)
            {
               DockItemTitleTab t = (DockItemTitleTab)box.Children[currentTab];
               t.Page.Hide();
               t.Active = false;
            }
            currentTab = value;
            if (currentTab != -1)
            {
               DockItemTitleTab t = (DockItemTitleTab)box.Children[currentTab];
               t.Active = true;
               t.Page.Show();
            }
         }
      }

      public Gtk.Widget CurrentPage
      {
         get
         {
            if (currentTab != -1)
            {
               DockItemTitleTab t = (DockItemTitleTab)box.Children[currentTab];
               return t.Page;
            }
            else
               return null;
         }
         set
         {
            if (value != null)
            {
               Gtk.Widget[] tabs = box.Children;
               for (int n = 0; n < tabs.Length; n++)
               {
                  DockItemTitleTab tab = (DockItemTitleTab)tabs[n];
                  if (tab.Page == value)
                  {
                     CurrentTab = n;
                     return;
                  }
               }
            }
            CurrentTab = -1;
         }
      }

      public void Clear()
      {
         currentTab = -1;
         foreach (DockItemTitleTab w in box.Children)
            box.Remove(w);
      }

      void OnTabPress(object s, Gtk.ButtonPressEventArgs args)
      {
         CurrentTab = Array.IndexOf(box.Children, s);
         DockItemTitleTab t = (DockItemTitleTab)s;
         DockItem.SetFocus(t.Page);
         QueueDraw();
         args.RetVal = true;
      }

      protected override void OnSizeAllocated(Gdk.Rectangle allocation)
      {
         UpdateEllipsize(allocation);
         base.OnSizeAllocated(allocation);
      }

      void UpdateEllipsize(Gdk.Rectangle allocation)
      {
         int tabsSize = 0;
         var children = box.Children;

         foreach (DockItemTitleTab tab in children)
            tabsSize += tab.LabelWidth;

         var totalWidth = allocation.Width;

         int[] sizes = new int[children.Length];
         double ratio = (double)allocation.Width / (double)tabsSize;

         if (ratio > 1 && visualStyle.ExpandedTabs.Value)
         {
            // The tabs have to fill all the available space. To get started, assume that all tabs with have the same size 
            var tsize = totalWidth / children.Length;
            // Maybe the assigned size is too small for some tabs. If it happens the extra space it requires has to be taken
            // from tabs which have surplus of space. To calculate it, first get the difference beteen the assigned space
            // and the required space.
            for (int n = 0; n < children.Length; n++)
               sizes[n] = tsize - ((DockItemTitleTab)children[n]).LabelWidth;

            // If all is positive, nothing is left to do (all tabs have enough space). If there is any negative, it means
            // that space has to be reassigned. The negative space has to be turned into positive by reducing space from other tabs
            for (int n = 0; n < sizes.Length; n++)
            {
               if (sizes[n] < 0)
               {
                  ReduceSizes(sizes, -sizes[n]);
                  sizes[n] = 0;
               }
            }
            // Now calculate the final space assignment of each tab
            for (int n = 0; n < children.Length; n++)
            {
               sizes[n] += ((DockItemTitleTab)children[n]).LabelWidth;
               totalWidth -= sizes[n];
            }
         }
         else
         {
            if (ratio > 1)
               ratio = 1;
            for (int n = 0; n < children.Length; n++)
            {
               var s = (int)((double)((DockItemTitleTab)children[n]).LabelWidth * ratio);
               sizes[n] = s;
               totalWidth -= s;
            }
         }

         // There may be some remaining space due to rounding. Spread it
         for (int n = 0; n < children.Length && totalWidth > 0; n++)
         {
            sizes[n]++;
            totalWidth--;
         }
         // Assign the sizes
         for (int n = 0; n < children.Length; n++)
            children[n].WidthRequest = sizes[n];
      }

      void ReduceSizes(int[] sizes, int amout)
      {
         // Homogeneously removes 'amount' pixels from the array of sizes, making sure
         // no size goes below 0.
         while (amout > 0)
         {
            int part;
            int candidates = sizes.Count(s => s > 0);
            if (candidates == 0)
               return;
            part = Math.Max(amout / candidates, 1);

            for (int n = 0; n < sizes.Length && amout > 0; n++)
            {
               var s = sizes[n];
               if (s <= 0)
                  continue;
               if (s > part)
               {
                  s -= part;
                  amout -= part;
               }
               else
               {
                  amout -= s;
                  s = 0;
               }
               sizes[n] = s;
            }
         }
      }

      void drawHVBox(Gdk.EventExpose evnt, Gdk.Window GdkWindow, Gdk.Rectangle alloc)
      {
         if (VisualStyle.TabStyle == DockTabStyle.Normal)
         {
            Gdk.GC gc = new Gdk.GC(GdkWindow);
            gc.RgbFgColor = VisualStyle.InactivePadBackgroundColor.Value;

            evnt.Window.DrawRectangle(gc, true, alloc);
            gc.Dispose();

            Gdk.GC bgc = new Gdk.GC(GdkWindow);
            var c = new HslColor(VisualStyle.PadBackgroundColor.Value);
            c.L *= 0.7;
            bgc.RgbFgColor = c;
            evnt.Window.DrawLine(bgc, alloc.X, alloc.Y + alloc.Height - 1, alloc.X + alloc.Width - 1, alloc.Y + alloc.Height - 1);
            bgc.Dispose();
         }
      }

      internal interface ITabStripBox
      {
         TabStrip TabStrip { get; }
         Widget[] Children { get; }
      }

      class TabStripVBox : VBox, ITabStripBox
      {
         public TabStrip TabStrip { get; set; }

         protected override bool OnExposeEvent(Gdk.EventExpose evnt)
         {
            TabStrip.drawHVBox(evnt, GdkWindow, Allocation);
            return base.OnExposeEvent(evnt);
         }
      }
      class TabStripHBox : HBox, ITabStripBox
      {
         public TabStrip TabStrip { get; set; }

         protected override bool OnExposeEvent(Gdk.EventExpose evnt)
         {
            TabStrip.drawHVBox(evnt, GdkWindow, Allocation);
            return base.OnExposeEvent(evnt);
         }
      }
   }
}
                    
            
