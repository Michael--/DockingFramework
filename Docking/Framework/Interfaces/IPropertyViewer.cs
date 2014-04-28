using System;

namespace Docking.Components
{
	public delegate void PropertyChangedEventHandler(PropertyChangedEventArgs e);
	
	public interface IPropertyViewer
	{
		/// <summary>
		/// Sets the current object to display its properties
		/// </summary>
		void SetObject(Object obj);
		
		/// <summary>
		/// Sets the current object and display the properties of given providers
		/// Show the properties of more than one instance
		/// The base object is the anchor, also used to send PropertyChanged event
		/// </summary>
		void SetObject(Object obj, Object[] providers);
		
		/// <summary>
		/// Get an event on any property changes
		/// </summary>
		PropertyChangedEventHandler PropertyChanged { get; set; }
	}
}

