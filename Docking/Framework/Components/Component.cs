using System;
using Gtk;

namespace Docking.Components
{
	public class Component : Gtk.Bin
	{
      public virtual void ComponentAdded(object item) {}
      public virtual void ComponentRemoved(object item) {}
      public virtual void VisibilityChanged(object component, bool visible) {}
      public virtual void FocusChanged(object item) {}
	}
}

