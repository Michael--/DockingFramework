using System;

namespace Docking.Components
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class PropertyViewer : Gtk.Bin
    {
        public PropertyViewer ()
        {
            this.Build();
            this.Name = "Properties";

            this.propertygrid1.CurrentObject = this.propertygrid1;
        }
    }

    #region Starter / Entry Point
    
    public class PropertyViewerFactory : ComponentFactory
    {
        public override Type TypeOfInstance { get { return typeof(PropertyViewer); } }
        public override String MenuPath { get { return @"Components\Properties"; } }
        public override String Comment { get { return "Show selected Properties"; } }
        public override Mode Options { get { return Mode.CloseOnHide; } }
    }
    #endregion
}

