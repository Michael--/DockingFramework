using System.Collections.Generic;

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

      /// <summary>
      /// Component info used to create this instance
      /// </summary>
      public ComponentFactoryInformation ComponentInfo { get; set; }

      // Returns true if this component currently is selected inits containing ComponentManager.
      // Note that this is something different than the focus.
      // The focus can be at some text edit control etc.      
      public bool IsCurrentDockItem { get
      {
         return ComponentManager!=null
             && ComponentManager.CurrentDockItem!=null
             && ComponentManager.CurrentDockItem==this.DockItem;
      }}

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
      public virtual void Loaded() { }

      // Will get called when this component is about to be closed (for example by the user or on application shutdown).
      // The component shall return true when it agrees to the closure and false if not.
      // For example, if the component is an editor with unsaved changes,
      // this function is the one to prompt the user for saving with a message box YES/NO/CANCEL,
      // and on CANCEL return false here.
      // Note that the component should NOT YET shutdown itself. That must happen in the "Close" call
      // which will occur subsesquently.
      public virtual bool IsCloseOK() { return true; }

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
      public virtual void ComponentAdded(object component)
      {
         if (component is IPropertyViewer)
         {
            IPropertyViewer property = component as IPropertyViewer;

            m_PropertyViewer.Add(property);
            property.PropertyChanged += PropertyChangedEventHandler;
         }
      }

      /// <summary>
      /// This function informs you about that a component has been removed from the framework.
      /// If your implementation has a reference in its implementation to that component,
      /// this is the time to assign null to that.
      /// </summary>
      public virtual void ComponentRemoved(object component)
      {
         if (component is IPropertyViewer)
         {
            IPropertyViewer property = component as IPropertyViewer;
            property.PropertyChanged -= PropertyChangedEventHandler;
            m_PropertyViewer.Remove(property);
         }
      }

      /// <summary>
      /// The current visibility state of some component has been changed.
      /// </summary>
      public virtual void VisibilityChanged(object component, bool visible) {}

      /// <summary>
      /// Current dock item has been changed.
      /// If item == this, your component is now the current one having the focus.
      /// For example this means it's time to update the properties list component.
      /// </summary>
      public virtual void FocusChanged(object component)
      {
         if (component == this && m_PropertyObject != null)
         {
            if (m_PropertyObject.GetType().IsArray)
            {
               foreach (IPropertyViewer p in m_PropertyViewer)
                  p.SetObject(m_PropertyObject, (object[])m_PropertyObject);
            }
            else
            {
               foreach (IPropertyViewer p in m_PropertyViewer)
                  p.SetObject(m_PropertyObject);
            }
         }
      }

      /// <summary>
      /// set any object to display with the known property viewer
      /// </summary>
      public void SetPropertyObject(object value)
      {
         m_PropertyObject = value;

         if (m_PropertyObject != null && m_PropertyObject.GetType().IsArray)
         {
            foreach (IPropertyViewer p in m_PropertyViewer)
               p.SetObject(m_PropertyObject, (object[])m_PropertyObject);
         }
         else
         {
            foreach (IPropertyViewer p in m_PropertyViewer)
               p.SetObject(m_PropertyObject);
         }
      }


      /// <summary>
      /// get the property object previously set
      /// </summary>
      public T GetPropertyObject<T>() { return (T)m_PropertyObject; }

      /// <summary>
      /// Get all available property viewer
      /// </summary>
      public IEnumerable<IPropertyViewer> PropertyViewer { get { return m_PropertyViewer; } }

      /// <summary>
      /// Called when your property object has been changed
      /// </summary>
      public virtual void PropertyChanged() { }

      #region property convinience private details

      private List<IPropertyViewer> m_PropertyViewer = new List<IPropertyViewer>();
      private object m_PropertyObject = null;

      // called whenever the property object has been changed by any property viewer
      private void PropertyChangedEventHandler(PropertyChangedEventArgs e)
      {
         if (e.Object == m_PropertyObject && m_PropertyObject != null)
         {
            PropertyChanged();
         }
      }

      #endregion

      #endregion
   }
}

