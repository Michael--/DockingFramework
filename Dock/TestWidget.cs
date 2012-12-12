using System;
using MonoDevelop.Components.Docking;

namespace Dock
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class TestWidget : Gtk.Bin
    {
        DockFrame df;

        public TestWidget(DockFrame df)
        {
            this.df = df;
            this.Build();
        }

        protected void OnButtonPushMeActivated(object sender, EventArgs e)
        {
            foreach (DockItem item in df.GetItems()) 
            {
                if (!item.Visible && item.Label.Length > 0)
                    item.Visible = true;
            }
        }
    }
}

