using System;
using Gtk;

namespace Docking.Components
{
	public class Component : Gtk.Bin
	{
		public Component(IntPtr raw) : base(raw)
		{}
        
        public Component() : base()
		{}
	}
}

