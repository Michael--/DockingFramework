using System;
using Docking.Components;

namespace Examples.VirtualList
{
    [System.ComponentModel.ToolboxItem(false)]
    public partial class VirtualListTest : Gtk.Bin
    {
        public VirtualListTest ()
        {
            this.Build();
            this.Name = "Virtual List Test";
        }
       
    }

#region Starter / Entry Point

public class Factory : ComponentFactory
{
    public override Type TypeOfInstance { get { return typeof(VirtualListTest); } }
    public override String MenuPath { get { return @"File\New\Examples\VirtualListTest"; } }
    public override String Comment { get { return "Test widget to testing virtual list view"; } }
    public override Mode Options { get { return Mode.MultipleInstance; } }
}

#endregion

}
