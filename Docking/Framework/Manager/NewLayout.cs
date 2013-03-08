using System;
using Gtk;

namespace Docking.Components
{
    public partial class NewLayout : Gtk.Dialog
    {
        public NewLayout () : base("New layout", null,
                                   DialogFlags.DestroyWithParent | DialogFlags.Modal,
                                   ResponseType.Ok)
        {
            this.Build ();
        }

        public String LayoutName
        {
            get 
            {
                return entryLayoutName.Text;
            }
        }

        public bool EmptyLayout
        {
            get
            {
                return radiobuttonEmpty.Active;
            }
        }
    }
}

