using System;

namespace Docking.Components
{
    /// <summary>
    /// Basic communication with the component manager.
    /// </summary>
    public interface IComponent
    {
        /// <summary>
        /// Get access to the ComponentManager for interaction.
        /// </summary>
        ComponentManager ComponentManager { get; set; }

        /// <summary>
        /// Will get called after component construction and addition of it into the ComponentManager's
        /// internal data structures.
        /// Put any post-construction initialization here, i.e., avoid doing much work in the constructor.
        /// Normally, you'll load your component's persistency inside this implementation.
        /// </summary>
        void Loaded(DockItem item);

        /// <summary>
        /// Will get called to save the persistency (if any).
        /// Main usecase is immediately before application shutdown.
        /// </summary>
        void Save();
    }
}

