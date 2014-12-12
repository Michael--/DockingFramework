using System;
using System.Diagnostics;

namespace Docking.Widgets
{
   public class FindChangedEventArgs : EventArgs
   {
      public FindChangedEventArgs(string value)
      {
         Text = value;
      }
      public string Text { get; private set; }
   }


	[System.ComponentModel.ToolboxItem (true)]
	public partial class Find : Gtk.Bin
	{
		public Find ()
		{
			this.Build ();

         entryFind.Changed += (s, e) =>
         {
            if (Changed != null)
               Changed(this, new FindChangedEventArgs(entryFind.Text));
            Debug.WriteLine(entryFind.Text);
         };
		}

      public event EventHandler<FindChangedEventArgs> Changed;
      public event EventHandler<EventArgs> Escaped;

      protected override void OnFocusGrabbed()
      {
         base.OnFocusGrabbed();
         if (!entryFind.HasFocus)
            entryFind.GrabFocus();
      }

      protected override bool OnKeyPressEvent(Gdk.EventKey evnt)
      {
         switch (evnt.Key)
         {
            case Gdk.Key.Escape:
               if (Escaped != null)
                  Escaped(this, EventArgs.Empty);
               return true;
         }

         return base.OnKeyPressEvent(evnt);
      }
	}

}

 