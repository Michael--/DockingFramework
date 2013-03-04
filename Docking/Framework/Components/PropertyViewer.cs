using System;

namespace Docking.Components
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class PropertyViewer : Gtk.Bin, IProperty
    {
        public PropertyViewer ()
        {
            this.Build();
            this.Name = "Properties";

            // this.propertygrid1.CurrentObject = this.propertygrid1;
        }

        #region implement IProperty

        void IProperty.SetObject(Object obj)
        {
            this.propertygrid1.CurrentObject = obj;
        }

        /// <summary>
        /// Get an event on any property changes
        /// </summary>
        PropertyChangedEventHandler IProperty.PropertyChanged
        {
            get { return PropertyChangedHandler; }
            set { PropertyChangedHandler = value; }
        }

        private PropertyChangedEventHandler PropertyChangedHandler;

        #endregion
    }

    #region IProperty

    public class PropertyChangedEventArgs : EventArgs
    {
        public PropertyChangedEventArgs(Object obj)
        {
            Object = obj;
        }
        public Object Object { get; private set; }
    }


    public delegate void PropertyChangedEventHandler(PropertyChangedEventArgs e);

    public interface IProperty
    {
        /// <summary>
        /// Sets the current object to display its properties
        /// </summary>
        void SetObject(Object obj);

        /// <summary>
        /// Get an event on any property changes
        /// </summary>
        PropertyChangedEventHandler PropertyChanged { get; set; }
    }

    #endregion

    #region Starter / Entry Point

    public class PropertyViewerFactory : ComponentFactory
    {
        public override Type TypeOfInstance { get { return typeof(PropertyViewer); } }
        public override String MenuPath { get { return @"View\Infrastructure\Properties"; } }
        public override String Comment { get { return "Show selected Properties"; } }
        public override Mode Options { get { return Mode.CloseOnHide; } }
		public override Gdk.Pixbuf Icon { get { return Gdk.Pixbuf.LoadFromResource ("Docking.Framework.Components.PropertyViewer-16.png"); } }
    }
    #endregion
}

