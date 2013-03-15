using System;
using Gtk;

namespace Docking.Components
{
    public class MessageBox
    {            
		public static ResponseType Show(Window parent, MessageType mt, ButtonsType bt, string format, params object[] args)
		{
			MessageDialog md = new MessageDialog (parent, 
			                                      DialogFlags.Modal,
			                                      mt, 
			                                      bt,
			                                      format, args);
			md.SetPosition(WindowPosition.CenterOnParent);    
			ResponseType result = (ResponseType)md.Run ();
			md.Destroy();
			return result;
		}

		public static ResponseType Show(string format, params object[] args)
		{
			return Show(null, MessageType.Other, ButtonsType.Ok, format, args);
		}

		public static ResponseType Show(string s)
		{
			return Show(null, MessageType.Other, ButtonsType.Ok, s);
		}
	}
}

