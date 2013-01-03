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
        void Added(DockItem item);
        
        /// <summary>
        /// Another component or own component has been removed from the framework
        /// Time to release connected interfaces
        /// </summary>
        void Removed(DockItem item);
        
        /// <summary>
        /// A component got the focus, will be called also for our own
        /// </summary>
        void Activated(DockItem item);
    }
}

