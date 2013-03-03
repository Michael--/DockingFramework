using System;

namespace Docking.Components
{
	/// <summary>
	/// The framework will search for this interface inside DLLs.
	/// When found, it can instantiate it and this way construct addin components at runtime.
	/// Just some abstract methods must be overwritten to define a new AddIn.
	/// </summary>
	public interface IComponentFactory  {}

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
		/// The class must derived from 'DockContent' and support a contructor with 'IFrame' paramater
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
			/// A default instance can be exist only as a single instance.
			/// Some AddIn exist multiple, like some log file viewer to show different parts of one log.
			/// </summary>
			MultipleInstance = 0x01,

			/// <summary>
			/// Normally an instance will be created on demand, the user will open what he like.
			/// If an instance is mandatory, with this option the instance will be created at startup if not already exist.
			/// </summary>
			AutoCreate = 0x02,

			/// <summary>
			/// Each window is normally visible. Together with AutoCreate you can create hidden windows as a background worker.
			/// </summary>
			Hidden = 0x04,

            /// <summary>
            /// Close on hide option.
            /// Components with MultipleInstance option are automatically
            /// closed on hide, SingleInstance components only optionally
            /// with this option.
            /// Closed windows are removed from memory.
            /// Hidden windows are only hidden, content existing and persistent.
            /// </summary>
            CloseOnHide = 0x08
		}
	}
}

