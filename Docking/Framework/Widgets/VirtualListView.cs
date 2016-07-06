using System;
using System.Collections.Generic;
using Gtk;
using GLib;
using Docking.Components;
using Docking.Helper;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace Docking.Widgets
{
   [System.ComponentModel.ToolboxItem(true)]
   public partial class VirtualListView : Component, ILocalizableWidget, ICopy, IPersistable
   {
      public VirtualListView()
      {
         this.Build();
         DefaultLayout = NewLayout(Pango.FontDescription.FromString("Tahoma 10")); // TODO use the same font as the other GUI elements, for example like GTK#'s list control
         CurrentRow = 0;
         CurrentSelectionMode = SelectionMode.None;
         DocumentEnd = true;

         mColumnControl = new ColumnControl(this);
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
         var bc = (Box.BoxChild)vbox1[findwidget];
         bc.Position = 0;

         Find.CurrentChanged += (o, e) =>
         {
            CurrentSelectionMode = SelectionMode.None; // as a workaround to avoid selection with CTRL+F3
         };
         FindPossibility = true;

         drawingarea.Events |= Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.ButtonMotionMask;

         drawingarea.ButtonPressEvent += drawingarea_ButtonPressEvent;
         drawingarea.ButtonReleaseEvent += drawingarea_ButtonReleaseEvent;
         drawingarea.MotionNotifyEvent += drawingarea_MotionNotifyEvent;
      }

      ColumnControl mColumnControl;
      Dictionary<int, ColumnPersistence> mColumnPersistence = new Dictionary<int, ColumnPersistence>();

      /// <summary>
      /// Sets the get content delegate. Will be called for any content request.
      /// </summary>
      public ContentDelegate GetContentDelegate { private get; set; }
      public delegate object ContentDelegate(int row, int column);

      public ColorDelegate GetColorDelegate { private get; set; }
      public delegate void ColorDelegate(int row, ref System.Drawing.Color background, ref System.Drawing.Color foreground);

      // click a element at row/column
      public delegate void ItemClickedEventHandler(ButtonPressEventArgs args, int row, int column);
      public event ItemClickedEventHandler ItemClickedEvent;

      // click a header at column
      public delegate void HeaderClickedEventHandler(ButtonPressEventArgs args, int column);
      public event HeaderClickedEventHandler HeaderClickedEvent;

      public void CallHeaderClickedEvent(ButtonPressEventArgs args, int column)
      {
         if (HeaderClickedEvent != null)
            HeaderClickedEvent(args, column);
      }

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
            vscrollbar1.SetRange(0, Math.Max(1, mRow - 1));
         }
      }
      private int mRow;

      public void TriggerRepaint() { drawingarea.QueueDraw(); }
      public Find Find { get { return findwidget; } }
      public bool FindPossibility { get; set; } // true (default) if find options is possible and displayed/toggling pressing CTRL+F

      /// <summary>
      /// Call if find box visibility has been changed
      /// </summary>
      public event EventHandler<EventArgs> FindBoxVisibility;

      /// <summary>
      /// Get selection. Return the selected lines indicies.
      /// </summary>
      public IEnumerable<int> GetSelection()
      {
         // TODO: as an alternative m_Selection could be a SortedSet, but with less performance overall
         return m_Selection.OrderBy(x => x);
      }

      public void ClearSelection()
      {
         m_Selection.Clear();
         TriggerRepaint();
      }

      Pango.Layout NewLayout(Pango.FontDescription fd)
      {
         return new Pango.Layout(this.PangoContext)
         {
            FontDescription = fd,
            Wrap = Pango.WrapMode.WordChar
         };
      }

      public Pango.Layout DefaultLayout { get; private set; }
      private int ConstantHeight { get; set; }
      private HashSet<int> m_Selection = new HashSet<int>();

      enum SelectionMode
      {
         None,    // no selection, only the current line is "selected" as default
         Block,   // all line between old current line and new current line will be added to the current selection (SHIFT-KEY)
         Single   // the new current line added to the current selection                                           (CTRL-KEY)
      }

      private SelectionMode CurrentSelectionMode { get; set; }
      private int TopVisibleRow { get; set; }
      private int BottomVisibleRow { get; set; }

      /// <summary>
      /// Set a new font for a column
      /// </summary>
      public void SetColumnFontDescription(int tag, Pango.FontDescription fd)
      {
         var c = mColumnControl.GetColumn(tag);
         Debug.Assert(c != null);
         c.LineLayout.FontDescription = fd;
         DetermineLayout();
      }

      /// <summary>
      /// Adds a new column, whereby the column is represented as a GTK label  
      /// After the last column has been added, you must confirm
      /// with UpdateColumns()
      /// </summary>
      /// <param name="name">Name.</param>
      /// <param name="width">Width.</param>
      /// <param name="visible">If set to <c>true</c> visible.</param>
      public void AddColumn(int tag, string name, int width, bool visible, Pango.FontDescription fd = null)
      {
         Label label = new Label(name)
         {
            UseMarkup = true,
            Visible = true
         };
         label.SetAlignment(0, 0.5f);
         label.SetPadding(2, 2);
         AddColumn(name, label, tag, width, fd != null ? NewLayout(fd) : DefaultLayout);
      }

      /// <summary>
      /// Adds a new column with an explicit given widget.
      /// Make visible with UpdateColumns() at least.
      /// </summary>
      /// <param name="name">Name.</param>
      /// <param name="widget">Widget.</param>
      /// <param name="width">Width.</param>
      /// <param name="visible">If set to <c>true</c> visible.</param>
      public void AddColumn(int tag, string name, Widget widget, int width, bool visible, Pango.FontDescription fd = null)
      {
         if (visible)
            widget.ShowAll();
         AddColumn(name, widget, tag, width, layout: fd != null ? NewLayout(fd) : DefaultLayout);
      }

      void AddColumn(string name, Widget widget, int tag, int width, Pango.Layout layout)
      {
         ColumnPersistence p;
         if (mColumnPersistence.TryGetValue(tag, out p))
         {
            width = p.Width;
            widget.Visible = p.Visible;
         }
         mColumnControl.AddColumn(name, widget, tag, layout, width);
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

      public IEnumerable<int> GetVisibleColumnIndex()
      {
         List<int> columnIndex = new List<int>();

         ColumnControl.Column[] columns = mColumnControl.GetVisibleColumnsInDrawOrder();
         foreach (var c in mColumnControl.GetVisibleColumnsInDrawOrder())
            columnIndex.Add(c.SortOrder);
         return columnIndex;
      }

      public IEnumerable<string> GetVisibleColumnNames()
      {
         List<string> names = new List<string>();

         ColumnControl.Column[] columns = mColumnControl.GetVisibleColumnsInDrawOrder();
         foreach (var c in mColumnControl.GetVisibleColumnsInDrawOrder())
            names.Add(c.Name);
         return names;
      }


      #region IPersistency

      void IPersistable.SaveTo(IPersistency persistency)
      {
         string instance = DockItem.Id.ToString();
         mColumnPersistence.Clear();
         var columns = mColumnControl.GetColumns();
         foreach (var c in columns)
            mColumnPersistence.Add(c.Tag, new ColumnPersistence(c.Visible, c.Width));

         StringBuilder b = new StringBuilder();
         foreach (var s in mColumnPersistence)
            b.AppendFormat("[{0} {1} {2}]", s.Key, s.Value.Visible ? 1 : 0, s.Value.Width);

         persistency.SaveSetting(instance, "Columns", b.ToString());
         persistency.SaveSetting(instance, "FindVisible", Find.Visible);
      }

      void IPersistable.LoadFrom(IPersistency persistency)
      {
         string instance = DockItem.Id.ToString();
         mColumnPersistence.Clear();
         var columns = persistency.LoadSetting(instance, "Columns", "");
         var sc = columns.Split(new char[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);

         foreach (var c in sc)
         {
            var v = c.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (v.Length >= 3)
            {
               int tag, visible, width;
               if (int.TryParse(v[0], out tag) && int.TryParse(v[1], out visible) && int.TryParse(v[2], out width))
                  mColumnPersistence.Add(tag, new ColumnPersistence(visible != 0, width));
            }
         }

         bool loadedFindVisibility = persistency.LoadSetting(instance, "FindVisible", false);
         ShowFindBox(loadedFindVisibility, true);
      }

      #endregion

      private void DetermineLayout()
      {
         ConstantHeight = 0;
         foreach (var c in mColumnControl.GetColumns())
         {
            int width, height;
            c.LineLayout.SetMarkup("XYZ");
            c.LineLayout.GetPixelSize(out width, out height);
            ConstantHeight = Math.Max(ConstantHeight, height);
         }
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

         if (ConstantHeight == 0)
         {
            DetermineLayout();
            if (ConstantHeight == 0)
               return; // should never happen
         }

         int maxHScrollRange = 0;
         int dy = exposeRect.Top;
         offset += dy / ConstantHeight;
         dy -= dy % ConstantHeight;

         Gdk.GC background = new Gdk.GC((Gdk.Drawable)base.GdkWindow);
         Gdk.GC text = new Gdk.GC((Gdk.Drawable)base.GdkWindow);

         ColumnControl.Column[] columns = mColumnControl.GetVisibleColumnsInDrawOrder();

         for (int row = offset; row < RowCount; row++)
         {
            int dx = -(int)hscrollbar1.Value;
            Gdk.Rectangle rect = new Gdk.Rectangle(dx, dy, 0, ConstantHeight);

            System.Drawing.Color backColor = System.Drawing.Color.WhiteSmoke;
            System.Drawing.Color textColor = System.Drawing.Color.Black;

            if (row == CurrentRow)
               backColor = System.Drawing.Color.DarkGray;

            else if (m_Selection.Contains(row))
               backColor = System.Drawing.Color.LightGray;

            else if (GetColorDelegate != null)
               GetColorDelegate(row, ref backColor, ref textColor);

            background.RgbFgColor = new Gdk.Color(backColor.R, backColor.G, backColor.B);
            text.RgbFgColor = new Gdk.Color(textColor.R, textColor.G, textColor.B);
            int totalwidth = 0;
            for (int c = 0; c < columns.Length; c++)
            {
               ColumnControl.Column column = columns[c];
               int columnIndex = column.SortOrder;
               int xwidth = column.Width;
               rect = new Gdk.Rectangle(rect.Left, rect.Top, xwidth + mColumnControl.GripperWidth, ConstantHeight);
               if (c == columns.Length - 1)
                  rect.Width = Math.Max(rect.Width, exposeRect.Right - rect.Left + 1);
               object content = GetContentDelegate(row, columnIndex);
               if (content is Gdk.Pixbuf)
               {
                  Gdk.Pixbuf image = (Gdk.Pixbuf)content;
                  win.DrawRectangle(background, true, rect);
                  dx += 2;
                  image.RenderToDrawable(win, text, 0, 0, dx, dy, image.Width, image.Height, Gdk.RgbDither.Normal, 0, 0);
                  dx += xwidth + mColumnControl.GripperWidth - 2;
                  rect.Offset(xwidth + mColumnControl.GripperWidth, 0);
                  totalwidth += 2 + rect.Width;
               }
               else
               {
                  column.LineLayout.SetText(content.ToString());
                  win.DrawRectangle(background, true, rect);
                  dx += 2;
                  win.DrawLayout(text, dx, dy, column.LineLayout);
                  dx += xwidth + mColumnControl.GripperWidth - 2;
                  rect.Offset(xwidth + mColumnControl.GripperWidth, 0);

                  int dwidth, dheight;
                  column.LineLayout.GetPixelSize(out dwidth, out dheight);
                  totalwidth += 2 + mColumnControl.GripperWidth + dwidth;
               }
            }
            maxHScrollRange = Math.Max(maxHScrollRange, totalwidth);
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
            if (maxHScrollRange > 0 && maxHScrollRange > hscrollbar1.Value)
               hscrollbar1.SetRange(0, maxHScrollRange);

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

      internal void ShowDockPopupMenu (uint time)
      {
         Menu menu = new Menu ();

         var columns = mColumnControl.mColumns;

         foreach(var column in columns)
         {
            var widget = column.Key;
            var c = column.Value;

            // remove markup language from label before display as menu name
            var name = System.Text.RegularExpressions.Regex.Replace(c.Name, "(<.*?>)", "");
            TaggedLocalizedCheckedMenuItem item = new TaggedLocalizedCheckedMenuItem(name);
            item.Active = c.Visible;
            item.Tag = c;
            item.Activated += (object sender, EventArgs e) => 
            {
               var itm = sender as TaggedLocalizedCheckedMenuItem;
               var ct = itm.Tag as ColumnControl.Column;
               ct.Visible = !ct.Visible;
               mColumnControl.ArangeColumns();
               drawingarea.QueueDraw();
            };
            menu.Add(item);
         }
         
         menu.ShowAll ();
         menu.Popup (null, null, null, 3, time);
      }

      void drawingarea_ButtonPressEvent(object o, ButtonPressEventArgs args)
      {
         if (args.Event.Button == LEFT_MOUSE_BUTTON)
         {
            int row = (int)args.Event.Y / ConstantHeight + (int)vscrollbar1.Value;
            OffsetCursor(row - CurrentRow, true);
            if (!HasFocus)
               GrabFocus();

            if (ItemClickedEvent != null)
            {
               // genereate event ItemClicked(row, column)
               ColumnControl.Column[] columns = mColumnControl.GetVisibleColumnsInDrawOrder();
               int dx = -(int)hscrollbar1.Value;
               for (int c = 0; c < columns.Length; c++)
               {
                  ColumnControl.Column column = columns[c];
                  int columnIndex = column.SortOrder;
                  int xwidth = column.Width + mColumnControl.GripperWidth;

                  if (args.Event.X >= dx && args.Event.X <= dx + xwidth)
                  {
                     ItemClickedEvent(args, CurrentRow, column.Tag);
                     break;
                  }
                  dx += xwidth;
               }
            }
         }
      }

      void drawingarea_MotionNotifyEvent(object o, MotionNotifyEventArgs args)
      {
      }

      void drawingarea_ButtonReleaseEvent(object o, ButtonReleaseEventArgs args)
      {
      }


      protected override bool OnButtonPressEvent(Gdk.EventButton evnt)
      {
         if (evnt.TriggersContextMenu())
         {
            ShowDockPopupMenu(evnt.Time);
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
         CurrentSelectionMode = SelectionMode.None;
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

      private void OffsetCursor(int offset, bool forceSelection = false)
      {
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

         // care selection
         {
            if (!forceSelection && m_Selection.Count <= 1)
               forceSelection = true;

            if (forceSelection)
            {
               var s = new HashSet<int>();
               if (CurrentSelectionMode == SelectionMode.Block)
               {
                  var bottom = Math.Min(CurrentRow, oldRow);
                  var top = Math.Max(CurrentRow, oldRow);
                  var lines = Enumerable.Range(bottom, top - bottom + 1).ToArray();
                  s.UnionWith(lines);
               }
               else
               {
                  s.Add(CurrentRow);
               }

               if (CurrentSelectionMode == SelectionMode.None)
               {
                  m_Selection.Clear();
                  m_Selection.UnionWith(s);
               }
               else if (CurrentSelectionMode == SelectionMode.Block)
               {
                  m_Selection.UnionWith(s);
               }
               else
               {
                  m_Selection.SymmetricExceptWith(s);
               }

               if (CurrentSelectionMode != SelectionMode.Single)
                  m_Selection.Add(CurrentRow);
            }

            if (redraw || forceSelection)
            {
               drawingarea.QueueDraw();

               // provide current row now with an event
               if (CurrentRowChanged != null)
                  CurrentRowChanged(this, new VirtualListViewEventArgs(oldRow, CurrentRow));
            }
         }
      }

      public void ShowFindBox(bool visible)
      {
         ShowFindBox(visible, false);
      }

      private void ShowFindBox(bool visible, bool force)
      {
         if (force || FindPossibility)
         {
            if ((force || Find.Visible) && !visible)
            {
               Find.Visible = false;
               if (!HasFocus)
                  GrabFocus();
            }
            else if ((force || !Find.Visible) && visible)
            {
               Find.Visible = true;
               if (!Find.HasFocus)
                  Find.GrabFocus();
            }
         }
         if (FindBoxVisibility != null)
            FindBoxVisibility(this, new EventArgs());
      }

      protected override bool OnKeyReleaseEvent(Gdk.EventKey evnt)
      {
         switch (evnt.Key)
         {
            case Gdk.Key.Escape:
               CurrentSelectionMode = SelectionMode.None;
               OffsetCursor(0, true);
               return true;
            case Gdk.Key.Control_L:
            case Gdk.Key.Control_R:
            case Gdk.Key.Shift_L:
            case Gdk.Key.Shift_R:
               CurrentSelectionMode = SelectionMode.None;
               return true;
         }
         return base.OnKeyReleaseEvent(evnt);
      }

      protected override bool OnKeyPressEvent(Gdk.EventKey evnt)
      {
         switch (evnt.Key)
         {
            case Gdk.Key.F:
            case Gdk.Key.f:
               if ((evnt.State & Gdk.ModifierType.ControlMask) != 0)
               {
                  ShowFindBox(!Find.Visible, false);
                  return true;
               }
               break;

            case Gdk.Key.Shift_L:
            case Gdk.Key.Shift_R:
               if (CurrentSelectionMode == SelectionMode.None)
                  CurrentSelectionMode = SelectionMode.Block;
               return true;

            case Gdk.Key.Control_L:
            case Gdk.Key.Control_R:
               if (CurrentSelectionMode == SelectionMode.None)
                  CurrentSelectionMode = SelectionMode.Single;
               return true;

            case Gdk.Key.Up:
            case Gdk.Key.KP_Up:
               OffsetCursor(-1, CurrentSelectionMode != SelectionMode.None);
               return true;

            case Gdk.Key.Page_Up:
            case Gdk.Key.KP_Page_Up:
               OffsetCursor(-(BottomVisibleRow - TopVisibleRow), CurrentSelectionMode != SelectionMode.None);
               return true;

            case Gdk.Key.Down:
            case Gdk.Key.KP_Down:
               OffsetCursor(+1, CurrentSelectionMode != SelectionMode.None);
               return true;

            case Gdk.Key.Page_Down:
            case Gdk.Key.KP_Page_Down:
               OffsetCursor(BottomVisibleRow - TopVisibleRow, CurrentSelectionMode != SelectionMode.None);
               return true;

            case Gdk.Key.Home:
            case Gdk.Key.KP_Home:
               OffsetCursor(int.MinValue / 2, CurrentSelectionMode != SelectionMode.None);
               return true;

            case Gdk.Key.End:
            case Gdk.Key.KP_End:
               OffsetCursor(int.MaxValue / 2, CurrentSelectionMode != SelectionMode.None);
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

      #region ICopy

      void ICopy.Copy()
      {
         var selection = GetSelection();
         int count = selection.Count();
         if (count < 1000 || MessageBox.Show(MessageType.Question, ButtonsType.YesNo, "You wan't to copy {0} lines to clipboard ?", count) == ResponseType.Yes)
         {
            StringBuilder result = new StringBuilder();
            foreach (var i in selection)
            {
               ColumnControl.Column[] columns = mColumnControl.GetVisibleColumnsInDrawOrder();
               for (int c = 0; c < columns.Length; c++)
               {
                  int columnIndex = columns[c].SortOrder;
                  object columnContent = GetContentDelegate(i, columnIndex);
                  if (c > 0)
                     result.Append(";");
                  if (columnContent is Gdk.Pixbuf)
                     result.Append("*");
                  else
                     result.Append(columnContent.ToString());
               }
               result.Append("\n");
            }
            if (result.Length > 0)
               this.GetClipboard(Gdk.Selection.Clipboard).Text = result.ToString();
         }
      }
      #endregion
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
      public ColumnControl(VirtualListView view)
      {
         mView = view;
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
         var gripper = GripperPositions();
         foreach (var g in gripper)
         {
            int xv = g.Value;
            Gdk.Rectangle rect = new Gdk.Rectangle(xv - GripperWidth, 0, GripperWidth, TotalHeight);
            if (rect.Contains(x, 0))
               return g.Key;
         }
         return -1;
      }

      protected const int LEFT_MOUSE_BUTTON  = 1;
      protected const int RIGHT_MOUSE_BUTTON = 3;

      void TheButtonPressEvent(object o, ButtonPressEventArgs args)
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

         if (args.Event.Button == LEFT_MOUSE_BUTTON && DragGripper < 0)
         {
            DragGripper = HitGripper((int)args.Event.X);
            LastDragX = (int)args.Event.X;
         }
      }

      void TheButtonReleaseEvent(object o, ButtonReleaseEventArgs args)
      {
         if (args.Event.Button == LEFT_MOUSE_BUTTON && DragGripper >= 0)
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
                     ColumnChanged(this, null);
               }
            }
         }
         return true;
      }

      private void SetColumnWidth(int index, Widget widget, int width)
      {
         Column c = mColumns[widget];
         if (c != null)
         {
            int dx = width - c.Width;
            c.Width += dx;
            Requisition r = widget.SizeRequest();
            widget.SetSizeRequest(c.Width, r.Height);
            this.Move(widget, c.X - mCurrentScollOffset, TopOffset); // move to same position, helper to redraw parent

            // move all following
            for (int i = index + 1; i < mColumns.Count; i++)
            {
               var kvp = mColumns.ElementAt(i);
               c = kvp.Value;
               if (c.Visible)
               {
                  widget = kvp.Key;
                  c.X += dx;
                  this.Move(widget, c.X - mCurrentScollOffset, TopOffset);
               }
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


      public Gtk.EventBox EventBox;

      public Dictionary<Widget, Column> mColumns = new Dictionary<Widget, Column>();
      public ColumnChangedEventHandler ColumnChanged;
      private VirtualListView mView;

      /// <summary>
      /// </summary>
      public delegate void ColumnChangedEventHandler(object obj, EventArgs e);

      public int GripperWidth { get; private set; }
      public int MinViewWidth { get { return TotalWidth; } }

      int TopOffset = 2;
      int TotalHeight = 0;
      int TotalWidth = 0;
      int DragGripper = -1;
      int LastDragX = 0;

      public void AddColumn(string name, Widget widget, int tag, Pango.Layout layout, int width = 50, int min_width = 20)
      {
         int offset = 0;
         foreach (KeyValuePair<Widget, Column> kvp in mColumns)
            if (kvp.Key.Visible)
               offset += kvp.Value.Width + GripperWidth;

         Column column = new Column(name, widget, tag, width, min_width, layout) { SortOrder = mColumns.Count, X = offset };
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
               TotalHeight = args.Allocation.Height;
               EventBox.SetSizeRequest(TotalWidth, TotalHeight);
            }
         };
      }

      IEnumerable<KeyValuePair<int, int>> GripperPositions()
      {
         List<KeyValuePair<int, int>> gripper = new List<KeyValuePair<int,int>>();

         for (int i = 0; i < mColumns.Count; i++)
         {
            var kvp = mColumns.ElementAt(i);
            Widget w = kvp.Key;
            if (w.Visible)
               gripper.Add(new KeyValuePair<int, int>(i, w.Allocation.Right + 3));
         }
         return gripper;
      }

      void TheExposeEvent(object o, ExposeEventArgs args)
      {
         Gdk.EventExpose expose = args.Args[0] as Gdk.EventExpose;
         Gdk.Window win = expose.Window;

         Gdk.GC gc = new Gdk.GC(this.GdkWindow);
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

      public Column GetColumn(int tag)
      {
         foreach(var c in mColumns.Values)
            if (c.Tag == tag)
               return c;
         return null;
      }

      public IEnumerable<Column> GetColumns()
      {
         return mColumns.Values;
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
            if (c.Widget != null && c.Visible)
            {
               Widget w = c.Widget;
               this.Move(w, dx, TopOffset); // move to same position, helper to redraw parent
               dx += w.Allocation.Width + GripperWidth;
            }
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
         public bool Visible { get { return Widget.Visible; } set { Widget.Visible = value; } }
         public Pango.Layout LineLayout { get; set; }
      }
   }

   public class ColumnPersistence
   {
      public ColumnPersistence(bool visible, int width)
      {
         Visible = visible;
         Width = width;
      }

      public bool Visible { get; private set; }
      public int Width { get; private set; }
   }
}
