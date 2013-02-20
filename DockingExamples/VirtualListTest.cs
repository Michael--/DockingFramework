using System;
using Docking.Components;
using Gtk;

namespace Examples.VirtualList
{
    [System.ComponentModel.ToolboxItem(false)]
    public partial class VirtualListTest : Gtk.Bin
    {
        public VirtualListTest ()
        {
            this.Build();
            this.Name = "Virtual List Test";

            // callback requesting data to display
            virtuallistview1.GetContentDelegate = GetContent;

            // add simple label columns
            virtuallistview1.AddColumn("Index", 75, true);
            virtuallistview1.AddColumn("Context", 75, true);

            // add a more complex custom made Column
            VBox box = new VBox();
            box.PackStart(new Label("Message"), false, false, 0);
            box.PackStart(new Entry(""), false, false, 0);
            virtuallistview1.AddColumn("Message", box, 150, true);

            // show changes
            virtuallistview1.UpdateColumns();
            virtuallistview1.RowCount = 42000;
        }

        private String GetContent(int row, int column)
        {
            switch (column)
            {
                case 0:
                    return String.Format("{0}", row + 1);
                case 1:
                    return String.Format("{0}:{1}", row + 1, column + 1);
                case 2:
                    return String.Format("Content of row {0}", row + 1);
            }
            return "?";
        }
    }

#region Starter / Entry Point

public class Factory : ComponentFactory
{
    public override Type TypeOfInstance { get { return typeof(VirtualListTest); } }
    public override String MenuPath { get { return @"File\New\Examples\VirtualListTest"; } }
    public override String Comment { get { return "Test widget for testing virtual list view"; } }
    public override Mode Options { get { return Mode.MultipleInstance; } }
}

#endregion

}
