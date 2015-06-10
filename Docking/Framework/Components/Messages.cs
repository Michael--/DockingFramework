using System;
using Docking.Widgets;
using Docking.Components;
using Docking.Tools;
using Gtk;

namespace Docking.Components
{
    [System.ComponentModel.ToolboxItem(false)]
   public partial class Messages : Component, IMessage, ILocalizableComponent, ICopy
    {
        #region IMessage
        // FIXME SLohse: This function currently may ONLY be called from the main GUI thread!
		// If you're calling it from a different thread context, it will crash the application
		// when it for example is currently resizing itself or painting.
		// For this reason, class ComponentManager contains an "Invoke" delegation in function "MessageWriteLine".
		// It should be considered to move that delegation to here.
		// I feel it misplaced in that class.
      void IMessage.WriteLine(String format, params object[] args)
		{
         if (format == null)
            return;
			TextIter iter = textview1.Buffer.EndIter;
         String str = String.Format(format, args);
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

        TextMark m_Scroll2EndMark;

        #endregion

        #region ILocalizableComponent

        string ILocalizableComponent.Name { get { return "Messages"; } }

        void ILocalizableComponent.LocalizationChanged(Docking.DockItem item)
        {}
        #endregion

        void Clear()
        {
           textview1.Buffer.Clear();
           m_Scroll2EndMark = textview1.Buffer.CreateMark("Scroll2End", textview1.Buffer.EndIter, true);
        }

        public Messages()
        {
            this.Build ();

            Clear();

            textview1.PopulatePopup += (object o, PopulatePopupArgs args) =>
            {
               TaggedLocalizedImageMenuItem newitem = new TaggedLocalizedImageMenuItem("Clear");
               newitem.Image = new Image(Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.Broom-16.png"));
               newitem.Activated += (object sender, EventArgs e) => Clear();               
               newitem.ShowAll();
               args.Menu.Append(newitem);

               Localization.LocalizeMenu(args.Menu);
            };
        }

        #region ICopy

        // TODO: extend the ICopy mechanism. Whenever a textentry or textview control has the focus which supports Cut/Copy/Paste, then connect it to the main Cut/Copy/Paste menu of the application.
        void ICopy.Copy()
        {
           textview1.Buffer.CopyClipboard(Clipboard.Get(Gdk.Selection.Clipboard));
        }

        #endregion
    }

    #region Starter / Entry Point

    public class MessagesFactory : ComponentFactory
    {
        public override Type TypeOfInstance { get { return typeof(Messages); } }
        public override String MenuPath { get { return @"View\Infrastructure\Messages"; } }
        public override String Comment { get { return "shows runtime messages of the application"; } }
        public override Gdk.Pixbuf Icon { get { return Gdk.Pixbuf.LoadFromResource ("Docking.Framework.Resources.Messages-16.png"); } }
        public override string LicenseGroup { get { return "default"; } } 
    }

    #endregion

}

