﻿
// This file has been generated by the GUI designer. Do not modify.
namespace Docking.Widgets
{
	public partial class VirtualListView
	{
		private global::Gtk.VBox vbox1;
		private global::Gtk.HBox hbox1;
		private global::Gtk.DrawingArea drawingarea;
		private global::Gtk.VScrollbar vscrollbar1;
		private global::Gtk.HScrollbar hscrollbar1;

		protected virtual void Build ()
		{
			global::Stetic.Gui.Initialize (this);
			// Widget Docking.Components.VirtualListView
			global::Stetic.BinContainer.Attach (this);
			this.Name = "Docking.Components.VirtualListView";
			// Container child Docking.Components.VirtualListView.Gtk.Container+ContainerChild
			this.vbox1 = new global::Gtk.VBox ();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 1;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox ();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 1;
			// Container child hbox1.Gtk.Box+BoxChild
			this.drawingarea = new global::Gtk.DrawingArea ();
			this.drawingarea.CanFocus = true;
			this.drawingarea.Events = ((global::Gdk.EventMask)(2113796));
			this.drawingarea.Name = "drawingarea";
			this.hbox1.Add (this.drawingarea);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.hbox1 [this.drawingarea]));
			w1.Position = 0;
			// Container child hbox1.Gtk.Box+BoxChild
			this.vscrollbar1 = new global::Gtk.VScrollbar (null);
			this.vscrollbar1.Name = "vscrollbar1";
			this.vscrollbar1.Adjustment.Upper = 100;
			this.vscrollbar1.Adjustment.PageIncrement = 10;
			this.vscrollbar1.Adjustment.PageSize = 10;
			this.vscrollbar1.Adjustment.StepIncrement = 1;
			this.hbox1.Add (this.vscrollbar1);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox1 [this.vscrollbar1]));
			w2.Position = 1;
			w2.Expand = false;
			w2.Fill = false;
			this.vbox1.Add (this.hbox1);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox1 [this.hbox1]));
			w3.Position = 1;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hscrollbar1 = new global::Gtk.HScrollbar (null);
			this.hscrollbar1.Name = "hscrollbar1";
			this.hscrollbar1.Adjustment.Upper = 100;
			this.hscrollbar1.Adjustment.PageIncrement = 10;
			this.hscrollbar1.Adjustment.PageSize = 10;
			this.hscrollbar1.Adjustment.StepIncrement = 1;
			this.vbox1.Add (this.hscrollbar1);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox1 [this.hscrollbar1]));
			w4.Position = 2;
			w4.Expand = false;
			w4.Fill = false;
			this.Add (this.vbox1);
			if ((this.Child != null)) {
				this.Child.ShowAll ();
			}
			this.Hide ();
			this.drawingarea.ExposeEvent += new global::Gtk.ExposeEventHandler (this.OnDrawingareaExposeEvent);
			this.vscrollbar1.ValueChanged += new global::System.EventHandler (this.OnVscrollbar1ValueChanged);
			this.hscrollbar1.ValueChanged += new global::System.EventHandler (this.OnHscrollbar1ValueChanged);
		}
	}
}
