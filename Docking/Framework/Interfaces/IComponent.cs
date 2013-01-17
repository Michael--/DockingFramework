using System;

namespace Docking.Components
{
    /// <summary>
    /// Need for all basic component communication using the 
    /// component manager.
    /// Also persistence handling are included.
    /// </summary>
    public interface IComponent
    {
        /// <summary>
        /// Get access to the ComponentManager, e.g. need for persistence
        /// </summary>
        ComponentManager ComponentManager { get; set; }

        /// <summary>
        /// After component created and registered at least each
        /// component could load its persistence data
        /// </summary>
        void Loaded(DockItem item);

        /// <summary>
        /// Called to prepare persistence, mainly before quit application
        /// </summary>
        void Save();
    }
}

