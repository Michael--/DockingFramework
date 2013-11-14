using System;
using Gtk;

namespace Docking.Components
{
	public class Component : Gtk.Bin
	{
      /// <summary>
      /// Another component has been added to the framework -
      /// time to inspect its interfaces and establish communication with it if desired,
      /// usually by adding recpients to its events.
      /// </summary>
      public virtual void ComponentAdded(object component) { }

      /// <summary>
      /// Another component or own component has been removed from the framework:
      /// time to release connected interfaces.
      /// </summary>
      public virtual void ComponentRemoved(object component) { }

      /// <summary>
      /// Component status changed from visible to hidden or vice versa.
      /// </summary>
      public virtual void VisibilityChanged(object component, bool visible) { }

      /// <summary>
      /// Current dock item has been changed.
      /// If item == this, your component is now the current on having the focus.
      /// For example this means it's time to update the properties list component.
      /// </summary>
      public virtual void FocusChanged(object component) { }
	}
}

