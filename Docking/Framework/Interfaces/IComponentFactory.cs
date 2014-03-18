using System;

namespace Docking.Components
{
   /// <summary>
   /// The framework will search for this interface inside DLLs.
   /// When found, it can instantiate it and this way construct addin components at runtime.
   /// Just some abstract methods must be overwritten to define a new AddIn.
   /// </summary>
   public interface IComponentFactory {}

   /// <summary>
   /// The basic component factory base class. Each component factory derive from this.
   /// </summary>
   public abstract class ComponentFactory : IComponentFactory
   {
      /// <summary>
      /// Get a short description of the component.
      /// </summary>
      public abstract String Comment { get; }

      /// <summary>
      /// The menu path of the component. Return null if no menu is necessary (e.g. hidden components).
      /// </summary>
      public abstract String MenuPath { get; }

      /// <summary>
      /// The type of the instance to create. Needable to check existing instances and creating new instances.
      /// The class must derived from 'DockContent' and support a constructor with 'IFrame' paramater
      /// Example:
      ///     public class MainPanel : DockContent
      ///     {
      ///         public MainPanel(IFrame frame){}
      ///         ...
      ///     }
      /// </summary>
      public abstract Type TypeOfInstance { get; }

      /// <summary>
      /// Get the default open mode.
      /// A single instance window created only on demand.
      /// </summary>
      public virtual Mode Options { get { return Mode.None; } }

      /// <summary>
      /// Gets the icon displayed on menu, the tab, ... default is no icon (null)
      /// </summary>
      public virtual Gdk.Pixbuf Icon { get { return null; } }

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
         MultipleInstance = 0x01,

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
         Hidden = 0x04,

         /// <summary>
         /// Close on hide option.
         /// Components with MultipleInstance option are automatically
         /// closed on hide, SingleInstance components only optionally
         /// with this option.
         /// Closed windows are removed from memory.
         /// Hidden windows are only hidden, content existing and persistent.
         /// 
         /// TODO This concept needs refacturing. The different default behaviours for "Close"/"Hide" of "single instance"
         /// vs "multiple instance" are very confusing. Additionally, on "Close", a persistency saving should occur, which is currently missing.
         /// </summary>
         CloseOnHide = 0x08
      }
   }
}

