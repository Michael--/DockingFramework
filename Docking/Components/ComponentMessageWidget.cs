using System;

namespace Docking.Components
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class ComponentMessageWidget : Gtk.Bin, IMessage
    {
        #region Implement IMessage
        void IMessage.WriteLine(String str)
        {
            textview1.Buffer.InsertAtCursor(str + "\r\n");
            // todo: scroll to insertion
        }
        #endregion

        public ComponentMessageWidget ()
        {
            this.Build ();
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

