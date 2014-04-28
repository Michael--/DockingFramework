using System;
using Gtk;

namespace Docking.Components
{
    public class MessageBox
    {            
      public static ComponentManager ComponentManager { get; set; } // gets assigned from inside the constructor MainWindow::MainWindow()

		public static ResponseType Show(Window parent, MessageType msgtype, ButtonsType buttontype, string format, params object[] args)
		{
         if(parent==null && ComponentManager!=null)
            parent = ComponentManager;
			MessageDialog md = new MessageDialog(parent, DialogFlags.Modal, msgtype, buttontype, format, args);
         md.SetPosition(parent==null ? WindowPosition.Center : WindowPosition.CenterOnParent);
         if(ComponentManager!=null)
         {
            md.Title = ComponentManager.ApplicationName;
            md.Icon  = ComponentManager.Icon;
         }
         else
         {
            md.Title = "";
            md.Icon  = null;
         }
			ResponseType result = (ResponseType)md.Run();
			md.Destroy();
			return result;
		}

		public static ResponseType Show(MessageType msgtype, ButtonsType buttontype, string format, params object[] args)
		{
			return Show(null, msgtype, buttontype, format, args);
		}

		public static ResponseType Show(MessageType msgtype, ButtonsType buttontype, string s)
		{
			return Show(null, msgtype, buttontype, "{0}", s);
		}

		public static ResponseType Show(string format, params object[] args)
		{
			return Show(null, MessageType.Warning, ButtonsType.Ok, format, args);
		}

		public static ResponseType Show(string s)
		{
			return Show(null, MessageType.Warning, ButtonsType.Ok, "{0}", s);
		}
	}
}

