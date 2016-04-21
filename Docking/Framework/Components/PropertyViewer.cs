using System;

namespace Docking.Components
{
    [System.ComponentModel.ToolboxItem(false)]
    public partial class PropertyViewer : Component, IPropertyViewer, ILocalizableComponent, IPersistable
    {
        public PropertyViewer ()
        {
            this.Build();
            this.propertygrid1.Changed += (sender, e) => 
         {
                if (PropertyChangedHandler != null)
                    PropertyChangedHandler(new PropertyChangedEventArgs(this.propertygrid1.CurrentObject));
         };
        }

        // set the displayed name of the widget
        string ILocalizableComponent.Name { get { return "Properties"; } }

        // force redraw with same data
        void ILocalizableComponent.LocalizationChanged(Docking.DockItem item)
        {
           Object save = this.propertygrid1.CurrentObject;
           this.propertygrid1.CurrentObject = null;
           this.propertygrid1.CurrentObject = save;
           this.propertygrid1.QueueDraw(); 
        }

        #region IPropertyViewer

        void IPropertyViewer.SetObject(Object obj)
        {
            if(obj==this.propertygrid1.CurrentObject)
                return;

            this.propertygrid1.CurrentObject = obj;
            this.propertygrid1.QueueDraw(); // TODO: work currently not as expected
        }

        void IPropertyViewer.SetObject(Object obj, Object[] providers)
        {
            if (obj == this.propertygrid1.CurrentObject)
                return;

            this.propertygrid1.SetCurrentObject(obj, providers);
            this.propertygrid1.QueueDraw(); // TODO: work currently not as expected
        }

        /// <summary>
        /// Get an event on any property changes
        /// </summary>
        PropertyChangedEventHandler IPropertyViewer.PropertyChanged
        {
            get { return PropertyChangedHandler; }
            set { PropertyChangedHandler = value; }
        }

        private PropertyChangedEventHandler PropertyChangedHandler;

        #endregion

        #region IPersistable

        void IPersistable.SaveTo(IPersistency persistency)
        {
           string instance = DockItem.Id.ToString();
           persistency.SaveSetting(instance, "ShowHelp", this.propertygrid1.ShowHelp);
           persistency.SaveSetting(instance, "Sort",     (int)this.propertygrid1.PropertySort);
        }

        void IPersistable.LoadFrom(IPersistency persistency)
        {
           string instance = DockItem.Id.ToString();

           bool b = persistency.LoadSetting(instance, "ShowHelp", this.propertygrid1.ShowHelp);
           this.propertygrid1.ShowHelp = !b; // workaround: have to toggle this to have an effect...
           this.propertygrid1.ShowHelp =  b; //

           int i = persistency.LoadSetting(instance, "Sort", (int)this.propertygrid1.PropertySort);
           if(i==(int)MonoDevelop.Components.PropertyGrid.PropertySort.Alphabetical ||
              i==(int)MonoDevelop.Components.PropertyGrid.PropertySort.Categorized)
              this.propertygrid1.PropertySort = (MonoDevelop.Components.PropertyGrid.PropertySort)i;
        }

        #endregion
    }

    #region IPropertyViewer

    public class PropertyChangedEventArgs : EventArgs
    {
        public PropertyChangedEventArgs(Object obj)
        {
            Object = obj;
        }
        public Object Object { get; private set; }
    }

    #endregion

    #region Starter / Entry Point

    public class PropertyViewerFactory : ComponentFactory
    {
        public override Type TypeOfInstance { get { return typeof(PropertyViewer); } }
        public override String Name { get { return "Properties"; } }
        public override String MenuPath { get { return @"View\Infrastructure\Properties"; } }
        public override String Comment { get { return "show and edit properties of the currently focused item"; } }
        public override Gdk.Pixbuf Icon { get { return Docking.Tools.ResourceLoader_Docking.LoadPixbuf("PropertyViewer-16.png"); } }
        public override string LicenseGroup { get { return "default"; } }
    }
    #endregion
}

