
// This file has been generated by the GUI designer. Do not modify.
namespace Docking.Components
{
	public partial class LocalizationEditor
	{
		private global::Gtk.VBox vbox2;
		private global::Gtk.ScrolledWindow GtkScrolledWindow;
		private global::Gtk.TreeView treeview1;
		private global::Gtk.HBox hbox1;
		private global::Docking.Widgets.ButtonLocalized button1;
		private global::Docking.Widgets.LabelLocalized labelChanges;
		private global::Docking.Widgets.ButtonLocalized buttonTranslate;
		private global::Docking.Widgets.ButtonLocalized buttonTranslateAll;

		protected virtual void Build ()
		{
			global::Stetic.Gui.Initialize (this);
			// Widget Docking.Components.LocalizationEditor
			global::Stetic.BinContainer.Attach (this);
			this.Name = "Docking.Components.LocalizationEditor";
			// Container child Docking.Components.LocalizationEditor.Gtk.Container+ContainerChild
			this.vbox2 = new global::Gtk.VBox ();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow ();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.treeview1 = new global::Gtk.TreeView ();
			this.treeview1.CanFocus = true;
			this.treeview1.Name = "treeview1";
			this.GtkScrolledWindow.Add (this.treeview1);
			this.vbox2.Add (this.GtkScrolledWindow);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox2 [this.GtkScrolledWindow]));
			w2.Position = 1;
			// Container child vbox2.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox ();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.button1 = new global::Docking.Widgets.ButtonLocalized ();
			this.button1.CanFocus = true;
			this.button1.Name = "button1";
			this.button1.UseUnderline = true;
			this.button1.Label = "Save";
			this.hbox1.Add (this.button1);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox1 [this.button1]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.labelChanges = new global::Docking.Widgets.LabelLocalized ();
			this.labelChanges.Name = "labelChanges";
			this.labelChanges.LabelProp = "Changes: 0";
			this.hbox1.Add (this.labelChanges);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox1 [this.labelChanges]));
			w4.Position = 1;
			w4.Expand = false;
			w4.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.buttonTranslate = new global::Docking.Widgets.ButtonLocalized ();
			this.buttonTranslate.CanFocus = true;
			this.buttonTranslate.Name = "buttonTranslate";
			this.buttonTranslate.UseUnderline = true;
			this.buttonTranslate.Label = "Translate";
			this.hbox1.Add (this.buttonTranslate);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox1 [this.buttonTranslate]));
			w5.Position = 2;
			w5.Expand = false;
			w5.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.buttonTranslateAll = new global::Docking.Widgets.ButtonLocalized ();
			this.buttonTranslateAll.CanFocus = true;
			this.buttonTranslateAll.Name = "buttonTranslateAll";
			this.buttonTranslateAll.UseUnderline = true;
			this.buttonTranslateAll.Label = "Translate All";
			this.hbox1.Add (this.buttonTranslateAll);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.hbox1 [this.buttonTranslateAll]));
			w6.PackType = ((global::Gtk.PackType)(1));
			w6.Position = 3;
			w6.Expand = false;
			w6.Fill = false;
			this.vbox2.Add (this.hbox1);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox2 [this.hbox1]));
			w7.Position = 2;
			w7.Expand = false;
			w7.Fill = false;
			this.Add (this.vbox2);
			if ((this.Child != null)) {
				this.Child.ShowAll ();
			}
			this.Hide ();
		}
	}
}
