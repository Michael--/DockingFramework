using System;

namespace Docking.Components
{
    [System.ComponentModel.ToolboxItem(false)]
    public partial class PropertyViewer : Gtk.Bin, IProperty
    {
        public PropertyViewer ()
        {
            this.Build();
            this.Name = "Properties";
            this.propertygrid1.Changed += (sender, e) => 
			{
                if (PropertyChangedHandler != null)
                    PropertyChangedHandler(new PropertyChangedEventArgs(this.propertygrid1.CurrentObject));
			};
        }

        #region implement IProperty

        void IProperty.SetObject(Object obj)
        {
            if(obj==this.propertygrid1.CurrentObject)
                return;

            this.propertygrid1.CurrentObject = obj;
            this.propertygrid1.QueueDraw(); // TODO: work currently not as expected
        }

        void IProperty.SetObject(Object obj, Object[] providers)
        {
            if (obj == this.propertygrid1.CurrentObject)
                return;

            this.propertygrid1.SetCurrentObject(obj, providers);
            this.propertygrid1.QueueDraw(); // TODO: work currently not as expected
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
        /// Sets the current object and display the properties of given providers
        /// Show the properties of more than one instance
        /// The base object is the anchor, also used to send PropertyChanged event
        /// </summary>
        void SetObject(Object obj, Object[] providers);

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

