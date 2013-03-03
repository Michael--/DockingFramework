using System;
using Docking.Components;
using Gtk;
using System.Collections.Generic;
using Docking;
using System.Xml.Serialization;

namespace Examples.VirtualList
{
    [System.ComponentModel.ToolboxItem(false)]
    public partial class VirtualListTest : Gtk.Bin, IComponent
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

            // set content size
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


        #region implement IComponent
        public ComponentManager ComponentManager { get; set; }

        void IComponent.Loaded(DockItem item)
        {
            item.Icon = Gdk.Pixbuf.LoadFromResource ("Examples.VirtualListTest-16.png");
            virtuallistview1.ComponentManager = this.ComponentManager;
            Persistence p = (Persistence)ComponentManager.LoadObject("VirtualListTest", typeof(Persistence));
            if (p != null)
                p.Load(virtuallistview1);

            // show changes
            virtuallistview1.UpdateColumns();
        }

        void IComponent.Save()
        {
            Persistence p = Persistence.Save(virtuallistview1);
            ComponentManager.SaveObject("VirtualListTest", p);
        }

        #endregion
    }

    [Serializable()]
    public class Persistence
    {
        static public Persistence Save(VirtualListView v)
        {
            Persistence p = new Persistence();
            p.m_Data = v.GetPersistence();
            return p;
        }

        public void Load(VirtualListView v)
        {
            if (m_Data != null)
                v.SetPersistence(m_Data);
        }

        // to have a simple persistence make the member public
        // otherwise you have to implement IXmlSerializable
        public int[] m_Data;
    }


#region Starter / Entry Point

public class Factory : ComponentFactory
{
    public override Type TypeOfInstance { get { return typeof(VirtualListTest); } }
    public override String MenuPath { get { return @"View\Examples\Virtual List Test"; } }
    public override String Comment { get { return "Test widget for testing virtual list view"; } }
    public override Mode Options { get { return Mode.MultipleInstance; } }
}

#endregion

}
