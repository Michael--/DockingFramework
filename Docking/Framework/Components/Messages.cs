using System;

namespace Docking.Components
{
    [System.ComponentModel.ToolboxItem(false)]
    public partial class Messages : Gtk.Bin, IMessage
    {
        #region Implement IMessage
        // FIXME this function is NOT THREADSAFE at the moment!
        // It will crash if this function gets invoked from somewhere else
        // while this control is currently resizing itself OR painting itself OR .......
		// see http://delog.wordpress.com/2011/01/12/on-gtk-and-invokerequired/
        void IMessage.WriteLine(String str)
		{
			Gtk.TextIter iter = textview1.Buffer.EndIter;
			textview1.Buffer.Insert(ref iter, str + "\r\n");

			// TODO: scroll to end only if current cursor is at end
			//       should not scroll if user is anywhere in the message text
			//       scroll again when user set cursor at end of text

			//if (cursorAtEnd)
			{
				textview1.Buffer.MoveMark(m_Scroll2EndMark, textview1.Buffer.EndIter);
				textview1.ScrollToMark(m_Scroll2EndMark, 0.0, false, 0, 0);
			}

			// TODO: ScrollToIter doesn't work properly due to delayed line size calculation
			// textview1.ScrollToIter(textview1.Buffer.EndIter, 0.0, false, 0, 0);
        }

        Gtk.TextMark m_Scroll2EndMark;

        #endregion

        public Messages()
        {
            this.Build ();
            this.Name = "Messages";
            m_Scroll2EndMark = textview1.Buffer.CreateMark("Scroll2End", textview1.Buffer.EndIter, true);
			(this as IMessage).WriteLine("WARNING! THIS WINDOW IS NOT THREADSAFE YET! It will crash the application if a line gets appended to it while it is repainting or resizing!");
        }
    }

    #region Starter / Entry Point

    public class MessagesFactory : ComponentFactory
    {
        public override Type TypeOfInstance { get { return typeof(Messages); } }
        public override String MenuPath { get { return @"View\Infrastructure\Messages"; } }
        public override String Comment { get { return "shows runtime messages of the application"; } }
    }

    #endregion

}

