using System;
using Docking;
using Docking.Components;

namespace Examples.HelloWorld
{
	[System.ComponentModel.ToolboxItem(false)]
	public partial class HelloWorldWidget : Gtk.Bin
	{
        public HelloWorldWidget ()
		{
			this.Build ();
            this.Name = "Hello World";
		}
	}


#region Starter / Entry Point

	public class HelloWorldWidgetFactory : ComponentFactory
	{
		public override Type TypeOfInstance { get { return typeof(HelloWorldWidget); } }
        public override String MenuPath { get { return @"View\Examples\Hello World"; } }
		public override String Comment { get { return "Example minimal dockable view like 'Hello World'"; } }
        public override Mode Options { get { return Mode.MultipleInstance; } }
    }

#endregion


}

