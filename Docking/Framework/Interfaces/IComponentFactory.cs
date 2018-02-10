using System;
using System.Collections.Generic;

using Docking.Tools;

namespace Docking.Components
{
   [Flags]
   public enum Mode
   {
      None = 0x00,

      /// <summary>
      /// By default, all components only exist in single-instance mode.
      /// This means that different workspace layouts etc. all share the same instance.
      /// For some components, it makes sense to allow multi-instance mode,
      /// for example, multiple log file viewer instances can be used to show different parts of one log.
      /// </summary>
      MultiInstance = 0x01,

      /// <summary>
      /// Normally, new component instances will only be created when the user instantiates them.
      /// By setting this flag, the framework will instantiate a (hidden) instance automatically on startup
      /// if it does not already exist.
      /// This is necessary for some infrastructure components which need to be present all the time, for example,
      /// the messages log window.
      /// </summary>
      AutoCreate = 0x02,

      /// <summary>
      /// Normally, a component is visible by default.
      /// This flag exists to allow the creation of hidden "background worker" components (together with "AutoCreate").
      /// </summary>
      HideOnCreate = 0x04,

#if false
         // Obsolete. Do not re-use this bit. (For the curious: it used to be "CloseOnHide" in the past.)
         Reserved0x08 = 0x08,
#endif

      /// <summary>
      /// When this flag is set, each try to "close" the component just hides it. It stays alive hidden, invisible.
      /// This is necessary for infrastructure windows like the message logger etc.
      /// </summary>
      PreventClosing = 0x10
   }

   /// <summary>
   /// The framework will search for this interface inside DLLs.
   /// When found, it can instantiate it and this way construct addin components at runtime.
   /// Just some abstract methods must be overwritten to define a new AddIn.
   /// 
   /// This interface is empty for 2 reasons:
   /// 1. function SearchForTypes(), which searches for it, is faster for interfaces than for abstract classes (TODO really? measure the time in experiments)
   /// 2. In future, totally different child classes might inherit from this, not having anything in common except for the name.
   ///    We want to be able to totally refacture this and still keep the name IComponentFactory to support old DLLs.
   /// </summary>
   public interface IComponentFactory 
   {
      /// <summary>
      /// Returns a list of file type supported to open by this component
      /// </summary>
      List<FileFilterExt> SupportedFileTypes { get; }

      /// <summary>
      /// Get a short description of the component.
      /// </summary>
      String Comment { get; }

      /// <summary>
      /// Get a readable name of this component to show within the selector dialog
      /// </summary>
      String Name { get; }

      /// <summary>
      /// The menu path of the component. Return null if no menu is necessary (e.g. hidden components).
      /// </summary>
      String MenuPath { get; }

      /// <summary>
      /// The type of the instance to create. Needed to check existing instances and creating new instances.
      /// The class must derive from 'DockContent' and support a constructor with 'IFrame' paramater
      /// Example:
      ///     public class MainPanel : DockContent
      ///     {
      ///         public MainPanel(IFrame frame){}
      ///         ...
      ///     }
      /// </summary>
      Type TypeOfInstance { get; }

      /// <summary>
      /// Get the default open mode.
      /// A single instance window created only on demand.
      /// </summary>
      Mode Options { get; }

      /// <summary>
      /// Gets the icon displayed on menu, the tab, ... default is no icon (null)
      /// </summary>
      Gdk.Pixbuf Icon { get; }

      /// <summary>
      /// Gets the license group name
      /// </summary>
      string LicenseGroup { get; }
   }

   /// <summary>
   /// The basic component factory base class. Each component factory shall derive from this.
   /// Provides default implementations for some IComponentFactory properties and methods.
   /// </summary>
   public abstract class ComponentFactory : IComponentFactory
   {
      public abstract String MenuPath       { get;               }
      public abstract Type   TypeOfInstance { get;               }
      public virtual  String Comment        { get { return ""; } }

      /// <summary>
      /// List of file which can opened by this component. The component factory use this 
      /// information to select one or more components while opening a file by the file menu.
      /// </summary>
      public virtual List<FileFilterExt> SupportedFileTypes { get { return null; } }

      /// <summary>
      /// Returns a use readable name of the component to create by the component factory while open a file for this component
      /// </summary>
      /// <value>The name.</value>
      public virtual String Name    { get { return string.Empty; } }

      public virtual  Mode       Options        { get { return Mode.None; } }
      public virtual  Gdk.Pixbuf Icon           { get { return null;      } }
      public virtual  string     LicenseGroup   { get { return null;      } }
   }
}

