using System;
using Gtk;

namespace Docking.Components
{
    public class MessageBox
    {            
		public static ResponseType Show(Window parent, MessageType msgtype, ButtonsType buttontype, string format, params object[] args)
		{
			MessageDialog md = new MessageDialog (parent, DialogFlags.Modal, msgtype, buttontype, format, args);
			md.SetPosition(WindowPosition.CenterOnParent);    
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
			return Show(null, msgtype, buttontype, s);
		}

		public static ResponseType Show(MessageType msgtype, string format, params object[] args)
		{
			return Show(null, msgtype, ButtonsType.Ok, format, args);
		}

		public static ResponseType Show(MessageType msgtype, string s)
		{
			return Show(null, msgtype, ButtonsType.Ok, s);
		}

		public static ResponseType Show(string format, params object[] args)
		{
			return Show(null, MessageType.Warning, ButtonsType.Ok, format, args);
		}

		public static ResponseType Show(string s)
		{
			return Show(null, MessageType.Warning, ButtonsType.Ok, s);
		}
	}
}

