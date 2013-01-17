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
        /// Another component has been added to the framework
        /// Time to inspect its interfaces
        /// </summary>
        void Added(object item);
        
        /// <summary>
        /// Another component or own component has been removed from the framework
        /// Time to release connected interfaces
        /// </summary>
        void Removed(object item);
        
        /// <summary>
        /// Component status changed from visible to hidden or vice versa
        /// </summary>
        void Visible(object item, bool visible);
    }
}

