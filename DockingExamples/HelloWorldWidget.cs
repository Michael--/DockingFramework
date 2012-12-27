using System;
using Docking;
using Docking.Components;

namespace Examples.HelloWorld
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class HelloWorldWidget : Gtk.Bin
	{
        public HelloWorldWidget ()
		{
			this.Build ();
		}
	}


#region Starter / Entry Point
	
	public class HelloWorldWidgetFactory : ComponentFactory
	{
		public override Type TypeOfInstance { get { return typeof(HelloWorldWidget); } }
        public override String MenuPath { get { return @"Components\New\Examples\HelloWorldWidget"; } }
		public override String Comment { get { return "Example minimal dockable view like 'Hello World'"; } }
        public override Mode Options { get { return Mode.MultipleInstance; } }
    }
	
#endregion
	

}

