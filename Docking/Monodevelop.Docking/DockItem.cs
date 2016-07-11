//
// DockItem.cs
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
using System.Xml;
using Gtk;
using Mono.Unix;
using Docking.Tools;
using Docking.Components;

namespace Docking
{
   public class DockItem
   {
      Widget content;
      DockItemContainer widget;
      string defaultLocation;
      bool defaultVisible = true;
      DockItemStatus defaultStatus = DockItemStatus.Dockable;
      string id;
      DockFrame frame;
      int defaultWidth = -1;
      int defaultHeight = -1;
      string title;
      Gdk.Pixbuf icon;
      bool expand;
      DockItemBehavior behavior;
      Gtk.Window floatingWindow;
      DockBarItem dockBarItem;
      bool lastVisibleStatus;
      bool lastContentVisibleStatus;
      bool gettingContent;
      bool isPositionMarker;
      bool stickyVisible;
      IDockItemLabelProvider dockLabelProvider;
      DockItemToolbar toolbarTop;
      DockItemToolbar toolbarBottom;
      DockItemToolbar toolbarLeft;
      DockItemToolbar toolbarRight;

      DockItemTitleTab titleTab;

      DockVisualStyle regionStyle;
      DockVisualStyle itemStyle;
      DockVisualStyle currentVisualStyle;

      public event EventHandler VisibleChanged;
      public event EventHandler ContentVisibleChanged;
      public event EventHandler ContentRequired;

      public DockFrame DockFrame { get { return frame; } }

      internal DockItem(DockFrame frame, string id)
      {
         this.frame = frame;
         this.id = id;
         currentVisualStyle = regionStyle = frame.GetRegionStyleForItem(this);
      }

      internal DockItem(DockFrame frame, Widget w, string id) : this(frame, id)
      {
         content = w;
      }

      public string Id
      {
         get { return id; }
      }

      public int InstanceIndex
      {
         get
         {
            int i = Id.LastIndexOf('-');
            if (i < 0)
               return 0;
            if (i + 1 >= Id.Length)
               return 0;
            string s = Id.Substring(i + 1);
            int index = Convert.ToInt32(s);
            return index;
         }
      }

      internal bool StickyVisible
      {
         get { return stickyVisible; }
         set { stickyVisible = value; }
      }

      public string Title
      {
         get { return title ?? string.Empty; }
         set
         {
            title = value;
            if (titleTab != null)
               titleTab.SetTitle(widget, icon, title);
            frame.UpdateTitle(this);
            if (floatingWindow != null)
               floatingWindow.Title = GetTitle();
         }
      }

      public void UpdateTitle()
      {
         if (this.Content == null)
            return;
         string newtitle = (this.Content is ILocalizableComponent)
                         ? (this.Content as ILocalizableComponent).Name.Localized(this.Content)
                         : this.Content.Name;
         if (InstanceIndex >= 2)
            newtitle += " " + InstanceIndex;
         this.Title = newtitle;
      }

      public bool Visible
      {
         get
         {
            return frame.GetVisible(this);
         }
         set
         {
            stickyVisible = value;
            frame.SetVisible(this, value);
            UpdateVisibleStatus();
         }
      }

      public bool VisibleInLayout(string layout)
      {
         return frame.GetVisible(this, layout);
      }

      internal DockItemTitleTab TitleTab
      {
         get
         {
            if (titleTab == null)
            {
               titleTab = new DockItemTitleTab(this, frame);
               titleTab.VisualStyle = currentVisualStyle;
               titleTab.SetTitle(Widget, icon, title);
               titleTab.ShowAll();
            }
            return titleTab;
         }
      }

      public DockItemStatus Status
      {
         get
         {
            return frame.GetStatus(this);
         }
         set
         {
            frame.SetStatus(this, value);
         }
      }

      public IDockItemLabelProvider DockLabelProvider
      {
         get { return this.dockLabelProvider; }
         set { this.dockLabelProvider = value; }
      }

      internal DockItemContainer Widget
      {
         get
         {
            if (widget == null)
            {
               widget = new DockItemContainer(frame, this);
               widget.VisualStyle = currentVisualStyle;
               widget.Visible = false; // Required to ensure that the Shown event is fired
               widget.Shown += SetupContent;
            }
            return widget;
         }
      }

      public DockVisualStyle VisualStyle
      {
         get { return itemStyle; }
         set { itemStyle = value; UpdateStyle(); }
      }

      internal void SetRegionStyle(DockVisualStyle style)
      {
         regionStyle = style;
         UpdateStyle();
      }

      void UpdateStyle()
      {
         var s = itemStyle != null ? itemStyle : regionStyle;
         if (s != currentVisualStyle)
         {
            currentVisualStyle = s;
            if (titleTab != null)
               titleTab.VisualStyle = s;
            if (widget != null)
               widget.VisualStyle = s;
            frame.UpdateStyle(this);
         }
      }

      void SetupContent(object ob, EventArgs args)
      {
         widget.Shown -= SetupContent;

         if (ContentRequired != null)
         {
            gettingContent = true;
            try
            {
               ContentRequired(this, EventArgs.Empty);
            }
            finally
            {
               gettingContent = false;
            }
         }

         widget.UpdateContent();
         widget.Shown += delegate
         {
            UpdateContentVisibleStatus();
         };
         widget.Hidden += delegate
         {
            UpdateContentVisibleStatus();
         };
         widget.ParentSet += delegate
         {
            UpdateContentVisibleStatus();
         };
         UpdateContentVisibleStatus();
      }

      public Widget Content
      {
         get
         {
            return content;
         }
         set
         {
            content = value;
            if (!gettingContent && widget != null)
               widget.UpdateContent();
         }
      }

      public DockItemToolbar GetToolbar(PositionType position)
      {
         switch (position)
         {
            case PositionType.Top:
               if (toolbarTop == null)
                  toolbarTop = new DockItemToolbar(this, PositionType.Top);
               return toolbarTop;
            case PositionType.Bottom:
               if (toolbarBottom == null)
                  toolbarBottom = new DockItemToolbar(this, PositionType.Bottom);
               return toolbarBottom;
            case PositionType.Left:
               if (toolbarLeft == null)
                  toolbarLeft = new DockItemToolbar(this, PositionType.Left);
               return toolbarLeft;
            case PositionType.Right:
               if (toolbarRight == null)
                  toolbarRight = new DockItemToolbar(this, PositionType.Right);
               return toolbarRight;
            default: throw new ArgumentException();
         }
      }

      internal bool HasWidget
      {
         get { return widget != null; }
      }

      public string DefaultLocation
      {
         get { return defaultLocation; }
         set { defaultLocation = value; }
      }

      public bool DefaultVisible
      {
         get { return defaultVisible; }
         set { defaultVisible = value; }
      }

      public DockItemStatus DefaultStatus
      {
         get { return defaultStatus; }
         set { defaultStatus = value; }
      }

      public int DefaultWidth
      {
         get
         {
            return defaultWidth;
         }
         set
         {
            defaultWidth = value;
         }
      }

      public int DefaultHeight
      {
         get
         {
            return defaultHeight;
         }
         set
         {
            defaultHeight = value;
         }
      }

      public Gdk.Pixbuf Icon
      {
         get
         {
            return icon;
         }
         set
         {
            icon = value;
            if (titleTab != null)
               titleTab.SetTitle(widget, icon, title);
         }
      }

      public DockItemBehavior Behavior
      {
         get
         {
            return behavior;
         }
         set
         {
            behavior = value;
            if (titleTab != null)
               titleTab.UpdateBehavior();
         }
      }

      public bool Expand
      {
         get
         {
            return expand;
         }
         set
         {
            expand = value;
         }
      }

      public bool DrawFrame
      {
         get
         {
            return false;
            //				return drawFrame;
         }
         set
         {
         }
      }

      public void Present(bool giveFocus)
      {
         if (dockBarItem != null)
            dockBarItem.Present(Status == DockItemStatus.AutoHide || giveFocus);
         else
            frame.Present(this, Status == DockItemStatus.AutoHide || giveFocus);
      }

      public bool ContentVisible
      {
         get
         {
            if (widget == null)
               return false;
            return widget.Parent != null && widget.Visible;
         }
      }

      public void SetDockLocation(string location)
      {
         frame.SetDockLocation(this, location);
      }

      internal void SetFocus()
      {
         SetFocus(Content);
      }

      internal static void SetFocus(Widget w)
      {
         w.ChildFocus(DirectionType.Down);

         Window win = w.Toplevel as Gtk.Window;
         if (win == null)
            return;

         // Make sure focus is not given to internal children
         if (win.Focus != null)
         {
            Container c = win.Focus.Parent as Container;
            if (c.Children.Length == 0)
               win.Focus = c;
         }
      }

      internal void UpdateVisibleStatus()
      {
         bool vis = frame.GetVisible(this);
         if (vis != lastVisibleStatus)
         {
            lastVisibleStatus = vis;
            if (VisibleChanged != null)
               VisibleChanged(this, EventArgs.Empty);
         }
         UpdateContentVisibleStatus();
      }

      internal void UpdateContentVisibleStatus()
      {
         bool vis = ContentVisible;
         if (vis != lastContentVisibleStatus)
         {
            lastContentVisibleStatus = vis;
            if (ContentVisibleChanged != null)
               ContentVisibleChanged(this, EventArgs.Empty);
         }
      }

      internal void ShowWidget()
      {
         if (floatingWindow != null)
            floatingWindow.Show();
         if (dockBarItem != null)
            dockBarItem.Show();
         Widget.Show();
      }

      internal void HideWidget()
      {
         if (floatingWindow != null)
            floatingWindow.Hide();
         else if (dockBarItem != null)
            dockBarItem.Hide();
         else if (widget != null)
            widget.Hide();
      }

      public bool Close()
      {
         Component component = this.Content as Component;
         if (component != null)
         {
            if (!component.IsCloseOK())
               return false; // closing has been canceled!
            component.Closed();
         }

         Visible = false;

         if ((Behavior & DockItemBehavior.CloseOnHide) != 0)
            frame.RemoveItemIfInvisibleInAllLayouts(this);

         return true;
      }

      internal void SetFloatMode(Gdk.Rectangle rect)
      {
         if (floatingWindow == null)
         {
            ResetMode();
            SetRegionStyle(frame.GetRegionStyleForItem(this));

            floatingWindow = new DockFloatingWindow((Window)frame.Toplevel, GetTitle());
            floatingWindow.DefaultWidth = 640;
            floatingWindow.DefaultHeight = 480;

            VBox box = new VBox();
            box.Show();
            box.PackStart(TitleTab, false, false, 0);
            box.PackStart(Widget, true, true, 0);
            floatingWindow.Add(box);
            floatingWindow.DeleteEvent += delegate (object o, DeleteEventArgs a)
            {
               if (behavior == DockItemBehavior.CantClose)
                  Status = DockItemStatus.Dockable;
               else
                  Visible = false;
               a.RetVal = true;
            };
         }
         floatingWindow.Move(rect.X, rect.Y);
         floatingWindow.Resize(rect.Width, rect.Height);
         floatingWindow.Show();
         if (titleTab != null)
            titleTab.UpdateBehavior();
         Widget.Show();
      }

      void ResetFloatMode()
      {
         if (floatingWindow != null)
         {
            // The widgets have already been removed from the window in ResetMode
            floatingWindow.Destroy();
            floatingWindow = null;
            if (titleTab != null)
               titleTab.UpdateBehavior();
         }
      }

      internal Gdk.Rectangle FloatingPosition
      {
         get
         {
            if (floatingWindow != null)
            {
               int x, y, w, h;
               floatingWindow.GetPosition(out x, out y);
               floatingWindow.GetSize(out w, out h);
               return new Gdk.Rectangle(x, y, w, h);
            }
            else
               return Gdk.Rectangle.Zero;
         }
      }

      internal void ResetMode()
      {
         if (Widget.Parent != null)
            ((Gtk.Container)Widget.Parent).Remove(Widget);
         if (TitleTab.Parent != null)
            ((Gtk.Container)TitleTab.Parent).Remove(TitleTab);

         ResetFloatMode();
         ResetBarUndockMode();
      }

      internal void SetAutoHideMode(Gtk.PositionType pos, int size)
      {
         ResetMode();
         if (widget != null)
         {
            widget.Hide(); // Avoids size allocation warning
            if (widget.Parent != null)
            {
               ((Gtk.Container)widget.Parent).Remove(widget);
            }
         }
         dockBarItem = frame.BarDock(pos, this, size);
         if (titleTab != null)
            titleTab.UpdateBehavior();

         SetRegionStyle(frame.GetRegionStyleForItem(this));
      }

      void ResetBarUndockMode()
      {
         if (dockBarItem != null)
         {
            dockBarItem.Close();
            dockBarItem = null;
            if (titleTab != null)
               titleTab.UpdateBehavior();
         }
      }

      internal int AutoHideSize
      {
         get
         {
            if (dockBarItem != null)
               return dockBarItem.Size;
            else
               return -1;
         }
      }

      internal void Minimize()
      {
         if (dockBarItem != null)
            dockBarItem.Minimize();
      }

      internal bool IsPositionMarker
      {
         get
         {
            return isPositionMarker;
         }
         set
         {
            isPositionMarker = value;
         }
      }

      string GetTitle()
      {
         if (Title.IndexOf('<') == -1)
            return Title;
         try
         {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<a>" + Title + "</a>");
            return doc.InnerText;
         }
         catch
         {
            return title;
         }
      }

      internal bool ShowingContextMemu { get; set; }

      internal void ShowDockPopupMenu(uint time, TabStrip tabstrip = null)
      {
         Menu menu = new Menu();

         MenuItem citem;

         // Close menuitem
         if ((Behavior & DockItemBehavior.CantClose) == 0)
         {
            citem = new MenuItem(Catalog.GetString("Close"));
            citem.Activated += delegate { Close(); };
            menu.Append(citem);
         }

         {
            citem = new MenuItem(this.Status==DockItemStatus.AutoHide
                                 ? Catalog.GetString("Restore" )
                                 : Catalog.GetString("Minimize"));
            citem.Activated += (o, e) =>
            {
               TitleTab.OnClickDock(o, e);
               if(this.Status==DockItemStatus.AutoHide)
                  (o as MenuItem).Name = this.Status==DockItemStatus.AutoHide
                                       ? Catalog.GetString("Restore" )
                                       : Catalog.GetString("Minimize");
            };
            menu.Append(citem);
         }

#if false
         // Hide menuitem
         if ((Behavior & DockItemBehavior.CantClose) == 0
                && (Behavior & DockItemBehavior.CloseOnHide) == 0)
            {
            citem = new MenuItem (Catalog.GetString("Hide"));
            citem.Activated += delegate { Visible = false; };
            menu.Append (citem);
         }

         // Auto Hide menuitem
         if ((Behavior & DockItemBehavior.CantAutoHide) == 0 && Status != DockItemStatus.AutoHide) {
            citem = new MenuItem (Catalog.GetString("Minimize"));
            citem.Activated += delegate { Status = DockItemStatus.AutoHide; };
            menu.Append (citem);
         }
#endif

         if (Status != DockItemStatus.Dockable)
         {
            // Dockable menuitem
            citem = new MenuItem(Catalog.GetString("Dock"));
            citem.Activated += delegate { Status = DockItemStatus.Dockable; };
            menu.Append(citem);
         }

         // Floating menuitem
         if ((Behavior & DockItemBehavior.NeverFloating) == 0 && Status != DockItemStatus.Floating)
         {
            citem = new MenuItem(Catalog.GetString("Undock"));
            citem.Activated += delegate { Status = DockItemStatus.Floating; };
            menu.Append(citem);
         }

         // flip tab menuitem
         if (tabstrip != null && (Behavior & DockItemBehavior.NeverFloating) == 0 && Status != DockItemStatus.Floating
            && (Status == DockItemStatus.Dockable || Status == DockItemStatus.AutoHide))
         {
            citem = new MenuItem(Catalog.GetString("Toggle Horizontal/Vertical Tab Layout"));
            citem.Activated += (o, e) =>
            {
               tabstrip.Flip();
            };
            menu.Append(citem);
         }

         if (menu.Children.Length == 0)
         {
            menu.Destroy();
            return;
         }

         ShowingContextMemu = true;

         menu.ShowAll();
         menu.Hidden += (o, e) =>
         {
            ShowingContextMemu = false;
         };
         menu.Popup(null, null, null, 3, time);
      }
   }

   // NOTE: Apparently GTK+ utility windows are not supposed to be "transient" to a parent, and things
   // break pretty badly on MacOS, so for now we don't make it transient to the "parent". Maybe in future
   // we should re-evaluate whether they're even Utility windows.
   //
   // See BXC9883 - Xamarin Studio hides when there is a nonmodal floating window and it loses focus
   // https://bugzilla.xamarin.com/show_bug.cgi?id=9883
   //
   class DockFloatingWindow : Window
   {
      public DockFloatingWindow(Window dockParent, string title) : base(title)
      {
         TypeHint = Gdk.WindowTypeHint.Utility;
         this.DockParent = dockParent;
      }

      public Window DockParent { get; private set; }
   }

   public interface IDockItemLabelProvider
   {
      Gtk.Widget CreateLabel(Orientation orientation);
   }
}
