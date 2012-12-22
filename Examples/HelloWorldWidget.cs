using System;
using Docking;
using Docking.Components;

namespace Examples.HelloWorld
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class HelloWorldWidget : Gtk.Bin
	{
		public HelloWorldWidget (DockFrame df)
		{
			this.Build ();
		}
	}


	#region Starter / Entry Point
	
	public class HelloWorldWidgetFactory : ComponentFactory
	{
		public override Type TypeOfInstance { get { return typeof(HelloWorldWidget); } }
		public override String MenuPath { get { return @"Examples\HelloWorldWidget"; } }
		public override String Comment { get { return "Example minimal dockable view like 'Hello World'"; } }
	}
	
#endregion
	

}

