using System;

namespace Docking.Components
{
    /// <summary>
    /// Need to interact between and with other components.
    /// To find out which other components are existing, use this interface
    /// and examine all other components interfaces.
    /// </summary>
    public interface IComponentInteract
    {
        /// <summary>
        /// Another component has been added to the framework -
        /// time to inspect its interfaces and establish communication with it if desired,
		  /// usually by adding recpients to its events.
        /// </summary>
        void Added(object component);
        
        /// <summary>
        /// Another component or own component has been removed from the framework:
        /// time to release connected interfaces.
        /// </summary>
        void Removed(object component);
        
        /// <summary>
        /// Component status changed from visible to hidden or vice versa.
        /// </summary>
        void Visible(object component, bool visible);

        /// <summary>
        /// Current dock item has been changed.
        /// If item == this, your component is now the current on having the focus.
        /// For example this means it's time to update the properties list component.
        /// </summary>
        void Current(object component);
    }
}

