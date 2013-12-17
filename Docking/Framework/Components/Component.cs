using System;
using Gtk;

namespace Docking.Components
{
	public class Component : Gtk.Bin
	{
      // The component manager is the main host application.
      // By operating with this instance, you can get access to it.
      public virtual ComponentManager ComponentManager { get; set; } // TODO REMOVE THE VIRTUAL!
 
      // The parent DockItem inside the ComponentManager (if any) which hosts this component.
      // Gets set by the docking framework.
      // Your component derived from this class will only do read access, if any.
      public DockItem DockItem { get; set; }

      #region Python scripting
      /// <summary>
      /// Get an instance containing methods/getter/setter which will be available for python at runtime
      /// This can be the component itself, but to avoid overall and deep access normally a specilized object.
      /// All public access to this object can be used inside python script.
      /// </summary>
      public virtual object GetScriptingInstance() { return null; }
      #endregion

      #region Component Lifecycle

      /// <summary>
      /// Will get called after component construction and addition of it into the ComponentManager's
      /// internal data structures.
      /// Put any post-construction initialization here, i.e., avoid doing much work in the constructor.
      /// Normally, you'll load your component's persistency inside this implementation.
      /// </summary>
      public virtual void Loaded(DockItem item) { this.DockItem = item; }

      /// <summary>
      /// Will get called to save the persistency (if any).
      /// Main usecase is immediately before application shutdown.
      /// </summary>
      public virtual void Save() {}

      // Will get called immediately before this component will be destroyed.
      // One thing you should do here for example is to un-register from any events you are listening to,
      // otherwise, this object instance will be be garbage-collected.
      // Return true if your object is fine with being closed.
      // Return false if you want to prevent the closing from happening.
      // This for example can happen if your component is an editor, and it knows that the currently edited document is unsaved yet,
      // and it asks the user with a MessageBox "do you want to save document XYZ?", offering a "Cancel" button,
      // and the user presses "Cancel" then to continue working.
      public virtual bool Closed() { return true; }

      #endregion

      #region notifications from ComponentManager

      /// <summary>
      /// When this function gets called, your class gets informed about the addition of a new component in the system.
      /// This can be either
      /// - a new instance of a class "Component"
      /// - a new instance of a class "MapLayer"
      /// - a new instance of a class "Plugin"
      /// - in future: a new instance of a class "File"
      /// You can check if the object you get as a parameter is something you're interested in
      /// and, if yes, do something with it, for example, store a reference to it in an internal member variable.
      /// </summary>
      public virtual void ComponentAdded(object component) {}

      /// <summary>
      /// This function informs you about that a component has been removed from the framework.
      /// If your implementation has a reference in its implementation to that component,
      /// this is the time to assign null to that.
      /// </summary>
      public virtual void ComponentRemoved(object component) {}

      /// <summary>
      /// The current visibility state of some component has been changed.
      /// </summary>
      public virtual void VisibilityChanged(object component, bool visible) {}

      /// <summary>
      /// Current dock item has been changed.
      /// If item == this, your component is now the current one having the focus.
      /// For example this means it's time to update the properties list component.
      /// </summary>
      public virtual void FocusChanged(object component) {}


      /// <summary>
      /// Called after any call for any component ( Loaded() and ComponentAdded() )
      /// </summary>
      public virtual void InitComplete() { }

      #endregion
	}
}

