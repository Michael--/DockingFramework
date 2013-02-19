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

            virtuallistview1.GetContentDelegate = GetContent;
            virtuallistview1.AddColumn("Column1", 150, true);
            virtuallistview1.AddColumn("Column2", 150, true);
            virtuallistview1.UpdateColumns();

            virtuallistview1.RowCount = 42000;
        }

        private String GetContent(int row, int column)
        {
            return String.Format("Row:{0} Column:{1}", row + 1, column + 1);
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
