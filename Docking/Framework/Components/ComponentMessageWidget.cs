using System;

namespace Docking.Components
{
    [System.ComponentModel.ToolboxItem(false)]
    public partial class ComponentMessageWidget : Gtk.Bin, IMessage
    {
        #region Implement IMessage
        void IMessage.WriteLine(String str)
        {
            Gtk.TextIter iter = textview1.Buffer.EndIter;
            textview1.Buffer.Insert(ref iter, str + "\r\n");

            // todo: scroll to end only if current cursor is at end
            //       should not scroll if user is anywhere in the message text
            //       scroll again when user set cursor at end of text

            //if (cursorAtEnd)
            {
                textview1.Buffer.MoveMark(m_Scroll2EndMark, textview1.Buffer.EndIter);
                textview1.ScrollToMark(m_Scroll2EndMark, 0.0, false, 0, 0);
            }

            // note: ScrollToIter don't work properly due to delayed line size calculation 
            // textview1.ScrollToIter(textview1.Buffer.EndIter, 0.0, false, 0, 0);
        }

        Gtk.TextMark m_Scroll2EndMark;

        #endregion

        public ComponentMessageWidget ()
        {
            this.Build ();
            this.Name = "Messages";
            m_Scroll2EndMark = textview1.Buffer.CreateMark("Scroll2End", textview1.Buffer.EndIter, true);
        }
    }

    #region Starter / Entry Point
    
    public class ComponentMessageWidgetFactory : ComponentFactory
    {
        public override Type TypeOfInstance { get { return typeof(ComponentMessageWidget); } }
        public override String MenuPath { get { return @"Components\ComponentMessageWidget"; } }
        public override String Comment { get { return "Message and logging widget"; } }
    }
    
    #endregion

}

