using System;

namespace Docking.Components
{
	/// <summary>
	/// This 'empty' interface the is search root for any Nice-AddIn.
	/// If a class definition exist supporting this interface, it will be found
	/// automatically in any DLL and created by the NICE framework as an entry point.
	/// Only some abstract methods must be overwritten to define a new AddIn.
	/// </summary>
	public interface IComponentFactory  {}
	
	/// <summary>
	/// The basic AddIn factory base class. Each Nice-AddIn must be derived by this class.
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
			Hidden = 0x04
		}
	}
}

