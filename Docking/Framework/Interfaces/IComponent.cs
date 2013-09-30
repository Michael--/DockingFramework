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

        // Will get called immediately before this component will be destroyed.
        // One thing you should do here for example is to un-register from any events you are listening to,
        // otherwise, this object instance will be be garbage-collected.
        // Return true if your object is fine with being closed.
        // Return false if you want to prevent the closing from happening.
        // This for example can happen if your component is an editor, and it knows that the currently edited document is unsaved yet,
        // and it asks the user with a MessageBox "do you want to save document XYZ?", offering a "Cancel" button,
        // and the user presses "Cancel" then to continue working.
        bool Closed();

        /// <summary>
        /// Will get called to save the persistency (if any).
        /// Main usecase is immediately before application shutdown.
        /// </summary>
        void Save();
    }
}

