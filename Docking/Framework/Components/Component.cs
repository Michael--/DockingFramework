using System;
using Gtk;

namespace Docking.Components
{
	public class Component : Gtk.Bin
	{
		public Component(IntPtr raw) : base(raw) // http://jira.nts.neusoft.local/browse/NENA-790
		{}
        
        public Component() : base()
		{}
	}
}

