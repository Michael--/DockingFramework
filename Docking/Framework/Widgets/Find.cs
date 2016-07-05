using Gtk;
using System;
using System.Linq;
using System.Diagnostics;
using Gdk;
using System.Collections.Generic;
using Docking.Components;

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


		public Find ()
		{
			this.Build ();
         UpdateStatus();

         comboFind.Changed += (s, e) =>
         {
            if (Changed != null)
               Changed(this, new FindChangedEventArgs(comboFind.ActiveText));
         };

         comboFind.EditingDone += (s, e) =>
         {
            UpdateComboEntries(comboFind.ActiveText);
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
            ((Entry)comboFind.Child).Text = "";
         };
      }

      /// <summary>
      /// Get all combo box entries, including active text as 1st list element
      /// </summary>
      IEnumerable<String> GetComboListEntries()
      {
         var result = new List<String>();
         foreach (object[] row in (ListStore)comboFind.Model)
            result.Add(row[0].ToString());
         return result;
      }

      #region IPersistable

      public void SaveTo(IPersistency persistency, string instance)
      {
         persistency.SaveSetting(instance, "ActiveText", comboFind.ActiveText);
         persistency.SaveSetting(instance, "List", GetComboListEntries().ToList());
      }

      public void LoadFrom(IPersistency persistency, string instance)
      {
         var list = persistency.LoadSetting(instance, "List", new List<string>());
         foreach (var s in list)
            comboFind.AppendText(s);
         ((Entry)comboFind.Child).Text = persistency.LoadSetting(instance, "ActiveText", "");
      }

      #endregion

      void UpdateComboEntries(String value)
      {
         if (value.Length == 0)
            return;

         // care LRU list

         // remove new value from list when already exist
         {
            var entries = GetComboListEntries();
            for (var i = 0; i < entries.Count(); i++)
            {
               // Check for match
               if (value == entries.ElementAt(i))
               {
                  // entry already at first position, nothing to do
                  if (i == 0)
                     return;

                  comboFind.RemoveText(i);
                  break;
               }
            }
         }

         // insert new value at first position
         comboFind.InsertText(0, value);
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
         // good time to update combo entry list
         UpdateComboEntries(comboFind.ActiveText);

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
            if (comboFind.ActiveText.Trim().Length == 0)
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
         if (!comboFind.HasFocus)
            comboFind.GrabFocus();
      }

      protected override bool OnFocusOutEvent(EventFocus evnt)
      {
         UpdateComboEntries(comboFind.ActiveText);
         return base.OnFocusOutEvent(evnt);
      }

      protected override bool OnKeyPressEvent(Gdk.EventKey evnt)
      {
         switch (evnt.Key)
         {
            case Gdk.Key.Escape:
               ((Entry)comboFind.Child).Text = "";
               return true;

            case Gdk.Key.F3:
               if ((evnt.State & Gdk.ModifierType.ShiftMask) != 0)
                  MovePosition(-1);
               else
                  MovePosition(1);
               return true;

            case Gdk.Key.Return:
            case Gdk.Key.KP_Enter:
               UpdateComboEntries(comboFind.ActiveText);
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

 