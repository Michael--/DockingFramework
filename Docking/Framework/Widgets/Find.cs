using System;
using System.Diagnostics;

namespace Docking.Widgets
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class Find : Gtk.Bin
	{
      /// <summary>
      /// Called on any change in find text box, Matches should be updated by receiver of this event
      /// </summary>
      public event EventHandler<FindChangedEventArgs> Changed;

      /// <summary>
      /// Called if current position hgas been changed, ask property Current for position
      /// </summary>
      public event EventHandler<EventArgs> CurrentChanged;

      /// <summary>
      /// Called on escaped key or button escaped, find closed
      /// </summary>
      public event EventHandler<EventArgs> Escaped;

		public Find ()
		{
			this.Build ();
         UpdateStatus();

         buttonLast.Visible = false; // TODO: not yet supported

         entryFind.Changed += (s, e) =>
         {
            if (Changed != null)
               Changed(this, new FindChangedEventArgs(entryFind.Text));
            Debug.WriteLine(entryFind.Text);
         };

         buttonNext.Clicked += (s, e) =>
         {
            MovePosition(1);
         };

         buttonPrev.Clicked += (s, e) =>
         {
            MovePosition(-1);
         };

         buttonClear.Clicked += (s, e) =>
         {
            if (Escaped != null)
               Escaped(this, EventArgs.Empty);
         };
      }

      /// <summary>
      /// Set the current match count
      /// if set Current will be reset to 0
      /// </summary>
      /// <param name="count"></param>
      public void SetMatches(int count)
      {
         Matches = count;
         Current = 0;
         UpdateStatus();
      }

      /// <summary>
      /// get/set total matches of last search result
      /// </summary>
      public int Matches { get; private set; }

      /// <summary>
      /// Current position between 0..Matches
      /// </summary>
      public int Current { get; private set; }

      private void MovePosition(int offset)
      {
         int newOffset = Math.Max(Math.Min(Current + offset, Matches - 1), 0);
         if (newOffset != Current && newOffset >= 0 && newOffset < Matches)
         {
            Current = newOffset;
            UpdateStatus();
            if (CurrentChanged != null)
               CurrentChanged(this, EventArgs.Empty);
         }
      }

      private void UpdateStatus()
      {
         if (Matches == 0)
         {
            if (entryFind.Text.Trim().Length == 0)
               labelStatus.Text = "";
            else
               labelStatus.Text = "no match";
         }
         else 
         {
            labelStatus.Text = string.Format("{0}/{1}", Current + 1, Matches);
         }
      }

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

            case Gdk.Key.F3:
               if ((evnt.State & Gdk.ModifierType.ShiftMask) != 0)
                  MovePosition(-1);
               else
                  MovePosition(1);
               return true;
         }

         return base.OnKeyPressEvent(evnt);
      }
	}

   public class FindChangedEventArgs : EventArgs
   {
      public FindChangedEventArgs(string value)
      {
         Text = value;
      }
      public string Text { get; private set; }
   }

}

 