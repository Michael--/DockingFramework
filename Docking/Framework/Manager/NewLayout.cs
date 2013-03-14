using System;
using Gtk;

namespace Docking.Components
{
    public partial class NewLayout : Gtk.Dialog
    {
        public NewLayout (Window parent) : base("New layout", parent,
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

		protected void SetOKButtonEnabling()
		{
			buttonOk.Sensitive = entryLayoutName.Text.Length>0;
		}

		protected void OnEntryLayoutNameChanged (object sender, EventArgs e)
		{
			SetOKButtonEnabling();
		}

		protected void OnRadiobuttonEmptyToggled (object sender, EventArgs e)
		{
			SetOKButtonEnabling();
		}
    }
}

