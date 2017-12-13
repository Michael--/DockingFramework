//
// PlaceholderWindow.cs
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

using System;
using Gdk;
using Gtk;

namespace Docking
{
   internal class PlaceholderWindow : Gtk.Window
   {
      Gdk.GC redgc;
      int rx, ry, rw, rh;

      public bool AllowDocking { get; set; }

      public PlaceholderWindow(DockFrame frame) : base(Gtk.WindowType.Popup)
      {
         SkipTaskbarHint = true;
         Decorated = false;
         TransientFor = (Gtk.Window)frame.Toplevel;
         TypeHint = WindowTypeHint.Utility;

         Realize();
         redgc = new Gdk.GC(GdkWindow);
         redgc.RgbFgColor = frame.Style.Background(StateType.Selected);
      }

      protected override void OnRealized()
      {
         base.OnRealized();
         GdkWindow.Opacity = 0.5;
      }

      protected override void OnSizeAllocated(Rectangle allocation)
      {
         base.OnSizeAllocated(allocation);
      }

      protected override bool OnExposeEvent(Gdk.EventExpose args)
      {
         int w, h;
         GetSize(out w, out h);
         GdkWindow.DrawRectangle(redgc, true, 0, 0, w, h);

         return true;
      }

      public void Relocate(int x, int y, int w, int h)
      {
         Gdk.Rectangle geometry = Docking.Helper.GtkWorkarounds.GetUsableMonitorGeometry(Screen, Screen.GetMonitorAtPoint(x, y));
         if (x < geometry.X)
            x = geometry.X;
         if (x + w > geometry.Right)
            x = geometry.Right - w;
         if (y < geometry.Y)
            y = geometry.Y;
         if (y > geometry.Bottom - h)
            y = geometry.Bottom - h;

         if (x != rx || y != ry || w != rw || h != rh)
         {
            Resize(w, h);
            Move(x, y);

            rx = x; ry = y; rw = w; rh = h;
         }
      }

      public DockDelegate DockDelegate { get; private set; }
      public Gdk.Rectangle DockRect { get; private set; }

      public void SetDockInfo(DockDelegate dockDelegate, Gdk.Rectangle rect)
      {
         DockDelegate = dockDelegate;
         DockRect = rect;
      }
   }

   class PadTitleWindow : Gtk.Window
   {
      public PadTitleWindow(DockFrame frame, DockItem draggedItem) : base(Gtk.WindowType.Popup)
      {
         SkipTaskbarHint = true;
         Decorated = false;
         TransientFor = (Gtk.Window)frame.Toplevel;
         TypeHint = WindowTypeHint.Utility;

         VBox mainBox = new VBox();

         HBox box = new HBox(false, 3);
         if (draggedItem.Icon != null)
         {
            Gtk.Image img = new Gtk.Image(draggedItem.Icon);
            box.PackStart(img, false, false, 0);
         }
         Gtk.Label la = new Label();
         la.Markup = draggedItem.Title;
         box.PackStart(la, false, false, 0);

         mainBox.PackStart(box, false, false, 0);

         CustomFrame f = new CustomFrame();
         f.SetPadding(12, 12, 12, 12);
         f.SetMargins(2, 2, 2, 2);
         f.Add(mainBox);

         Add(f);
         ShowAll();
      }
   }
}
