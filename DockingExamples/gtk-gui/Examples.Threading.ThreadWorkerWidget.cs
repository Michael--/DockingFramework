
// This file has been generated by the GUI designer. Do not modify.
namespace Examples.Threading
{
	public partial class ThreadWorkerWidget
	{
		private global::Gtk.VBox vbox1;
		private global::Gtk.Label label1;
		private global::Gtk.ProgressBar progressbar1;
		private global::Gtk.Button button1;
		
		protected virtual void Build ()
		{
			global::Stetic.Gui.Initialize (this);
			// Widget Examples.Threading.ThreadWorkerWidget
			global::Stetic.BinContainer.Attach (this);
			this.Name = "Examples.Threading.ThreadWorkerWidget";
			// Container child Examples.Threading.ThreadWorkerWidget.Gtk.Container+ContainerChild
			this.vbox1 = new global::Gtk.VBox ();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label ();
			this.label1.Name = "label1";
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString ("ThreadWorker");
			this.vbox1.Add (this.label1);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox1 [this.label1]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.progressbar1 = new global::Gtk.ProgressBar ();
			this.progressbar1.Name = "progressbar1";
			this.progressbar1.Ellipsize = ((global::Pango.EllipsizeMode)(3));
			this.vbox1.Add (this.progressbar1);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox1 [this.progressbar1]));
			w2.Position = 1;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.button1 = new global::Gtk.Button ();
			this.button1.CanFocus = true;
			this.button1.Name = "button1";
			this.button1.UseUnderline = true;
			this.button1.Label = global::Mono.Unix.Catalog.GetString ("Start a Task");
			this.vbox1.Add (this.button1);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox1 [this.button1]));
			w3.Position = 2;
			w3.Expand = false;
			w3.Fill = false;
			this.Add (this.vbox1);
			if ((this.Child != null)) {
				this.Child.ShowAll ();
			}
			this.Hide ();
			this.button1.Clicked += new global::System.EventHandler (this.OnButton1Clicked);
		}
	}
}
