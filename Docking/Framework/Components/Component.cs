namespace Docking.Components
{
	public class Component : Gtk.Bin
	{
      // The component manager is the main host application.
      // By operating with this instance, you can get access to it.
      public ComponentManager ComponentManager { get; set; }
 
      // The parent DockItem inside the ComponentManager (if any) which hosts this component.
      // Gets set by the docking framework.
      // Your component derived from this class will only do read access, if any.
      public DockItem DockItem { get; set; }

      #region Python scripting
      /// <summary>
      /// Get an instance containing methods/getter/setter which will be available for python at runtime
      /// This can be the component itself, but to avoid overall and deep access normally a specialized object.
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
      public virtual void Loaded(DockItem item)
      {
         this.DockItem = item;
         if(this is IPersistable)
            (this as IPersistable).LoadFrom(ComponentManager as IPersistency);
      }

      // Will get called when this component is about to be closed (for example by the user or on application shutdown).
      // The component shall return true when it agrees to the closure and false if not.
      // For example, if the component is an editor with unsaved changes,
      // this function is the one to prompt the user for saving with a message box YES/NO/CANCEL,
      // and on CANCEL return false here.
      // Note that the component should NOT YET shutdown itself. That must happen in the "Close" call
      // which will occur subsesquently.
      public virtual bool IsCloseOK() { return true; }

      /// <summary>
      /// Will get called to save the persistency (if any).
      /// Will be called when a component is closed by the user and on application shutdown.
      /// </summary>
      public virtual void Save()
      {
         if(this is IPersistable)
            (this as IPersistable).SaveTo(ComponentManager as IPersistency);
      }

      // Will get called immediately before this component will be destroyed.
      // Inside this function, please cleanup everything you want to do before your destructor runs,
      // for example, stopping threads, etc.
      // You do not have to save persistency here. That only needs to be done in the Save() function, see above.
      public virtual void Closed() {}

      #endregion

      #region notifications from ComponentManager

      /// <summary>
      /// When this function gets called, your class gets informed about the addition of a new component in the system.
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

