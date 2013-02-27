using System;

namespace Docking.Components
{
    [System.ComponentModel.ToolboxItem(false)]
    public partial class Messages : Gtk.Bin, IMessage
    {
        #region Implement IMessage
        // FIXME SLohse: This function currently may ONLY be called from the main GUI thread!
		// If you're calling it from a different thread context, it will crash the application
		// when it for example is currently resizing itself or painting.
		// For this reason, class ComponentManager contains an "Invoke" delegation in function "MessageWriteLine".
		// It should be considered to move that delegation to here.
		// I feel it misplaced in that class.
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

