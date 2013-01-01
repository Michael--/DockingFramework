using System;

namespace Docking.Components
{
    public interface IComponent
    {
        ComponentManager ComponentManager { get; set; }

        /// <summary>
        /// After component created and registered at least each
        /// component could load its persistence data
        /// </summary>
        void ComponentLoaded(DockItem item);

        /// <summary>
        /// Called to prepare persistence, mainly before quit application
        /// </summary>
        void ComponentSave();
    }
}

