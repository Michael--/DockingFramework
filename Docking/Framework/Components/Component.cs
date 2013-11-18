using System;
using Gtk;

namespace Docking.Components
{
	public class Component : Gtk.Bin
	{
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
	}
}

