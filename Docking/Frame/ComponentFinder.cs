using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Docking;

namespace Docking.Components
{
	public class ComponentFinder
	{
		public class ComponentFactoryInformation
		{
			public ComponentFactoryInformation(String filename, Type[] types, ComponentFactory component, bool active)
			{
				Filename = filename;
				Types = types;
				ComponentFactory = component;
				Typename = component.ToString();
				Active = active;
			}
			
			public bool Valid
			{
				get
				{
					if (ComponentFactory != null)
						return true;
					return false;
				}
			}

			public Gtk.Bin CreateInstance(DockFrame frame)
			{
				Gtk.Bin dc;
				try
				{
					dc = (Gtk.Bin)Activator.CreateInstance(AddInType, new Object[] {frame});
				}
				catch(Exception e)
				{
					Console.WriteLine(e.ToString());
					return null;
				}
				return dc;
			}
			
			public Type FactoryType
			{
				get
				{
					if (ComponentFactory != null)
						return ComponentFactory.GetType();
					return null;
				}
			}
			
			public Type AddInType
			{
				get
				{
					if (ComponentFactory != null)
						return ComponentFactory.TypeOfInstance;
					return null;
				}
			}
			
			public String Comment
			{
				get
				{
					if (ComponentFactory != null)
						return ComponentFactory.Comment;
					return null;
				}
			}
			
			public String MenuPath
			{
				get
				{
					if (ComponentFactory != null)
						return ComponentFactory.MenuPath;
					return null;
				}
			}
			
			public bool IsSingleInstance
			{
				get
				{
					if (ComponentFactory != null)
						return (ComponentFactory.Options & ComponentFactory.Mode.MultipleInstance) !=ComponentFactory.Mode.MultipleInstance;
					return true; // should never occur
				}
			}
			
			public bool InstanceMustExist
			{
				get
				{
					if (ComponentFactory != null)
						return (ComponentFactory.Options & ComponentFactory.Mode.AutoCreate) == ComponentFactory.Mode.AutoCreate;
					return false; // should never occur
				}
			}
			
			public bool HideOnCreate
			{
				get
				{
					if (ComponentFactory != null)
						return (ComponentFactory.Options & ComponentFactory.Mode.Hidden) == ComponentFactory.Mode.Hidden;
					return false; // should never occur
				}
			}
			
			public String Filename { get; private set; }
			public Type[] Types { get; private set; }
			ComponentFactory ComponentFactory { get; set; }
			
			
			public String Typename { get; private set; }
			public bool Active { get; set; }
			public Gtk.Bin DockWidget { get; set; }
		}

		private List<ComponentFactoryInformation> mComponents = new List<ComponentFactoryInformation>();
		
		public ComponentFactoryInformation[] ComponentInfos
		{
			get { return mComponents.ToArray(); }
		}

		Gtk.Bin CreateInstance(ComponentFactoryInformation info, DockFrame frame)
		{
			Gtk.Bin dc = info.CreateInstance(frame);
			
			// announce types
			if (dc != null)
			{
				ITypes typeInterface = dc as ITypes;
				if (typeInterface != null)
				{
					// collect all interface from all components first before use
					// avoid double announce of interfaces
					// TODO: may this loop could be placed at other location
					Dictionary<Type, Type> knownTypes = new Dictionary<Type, Type>();
					foreach (ComponentFactoryInformation i in mComponents)
					{
						foreach(Type t in i.Types)
						{
							if (!knownTypes.ContainsKey(t))
								knownTypes.Add(t, t);
						}
					}
					Type[] allTypes = new Type[knownTypes.Count];
					knownTypes.Values.CopyTo(allTypes, 0);
					
					// annouce all interface of all other components in one step
					typeInterface.Type(allTypes);
				}
			}
			return dc;
		}
		
		public ComponentFactoryInformation FindEntryPoint(Type t)
		{
			foreach (ComponentFactoryInformation info in mComponents)
			{
				if (t == info.AddInType)
					return info;
			}
			return null;
		}
		
		private ComponentFactoryInformation FindComponent(String filename, String typename)
		{
			foreach (ComponentFactoryInformation info in mComponents)
			{
				if (Path.GetFileName(info.Filename) == Path.GetFileName(filename))
				{
					if (info.Typename.Length == 0)
						return info;
					if (typename == info.Typename)
						return info;
				}
			}
			return null;
		}

		public Gtk.Bin CreateInstance(Type type, DockFrame frame)
		{
			foreach (ComponentFactoryInformation info in mComponents)
			{
				Type t = info.AddInType;
				if (t != null)
				{
					if(t==type)
					{
						info.DockWidget = CreateInstance(info, frame);
						return info.DockWidget;
					}
					Type[] myInterfaces = t.FindInterfaces(mTypeFilter, type);
					if (myInterfaces.Length > 0)
					{
						info.DockWidget = CreateInstance(info, frame);
						return info.DockWidget;
					}
				}
			}
			return null;
		}
		
		public Gtk.Bin FindInstance(Type type)
		{
			foreach (ComponentFactoryInformation info in mComponents)
			{
				Type t = info.AddInType;
				if (t != null)
				{
					if (t == type)
					{
						return info.DockWidget;
					}
					Type[] myInterfaces = t.FindInterfaces(mTypeFilter, type);
					if(myInterfaces.Length>0 && info.DockWidget!=null)
					{
						return info.DockWidget;
					}
				}
			}
			return null;
		}

		TypeFilter mTypeFilter = new TypeFilter(InterfaceFilterCallback);
		private static bool InterfaceFilterCallback(Type typeObj, Object criteriaObj)
		{
			return typeObj == (Type)criteriaObj;
		}
		
		public void AddComponent(String filename)
		{
			try
			{
				Assembly asm = Assembly.LoadFrom(filename);
				Type[] types = asm.GetExportedTypes();
				foreach (Type t in types)
				{
					if (t.IsClass)
					{
						// Add in instance generation
						Type[] myInterfaces = t.FindInterfaces(mTypeFilter, typeof(IComponentFactory));
						if (myInterfaces.Length > 0)
						{
							try
							{
								ComponentFactory cf = (ComponentFactory)Activator.CreateInstance(t);
								mComponents.Add(new ComponentFactoryInformation(filename, types, cf, true));
							}
							catch (InvalidCastException)
							{
								// Console.WriteLine("{0}", e.ToString());
							}
							catch (MissingMethodException)
							{
								// Console.WriteLine("{0}", e.ToString());
							}
							catch (ReflectionTypeLoadException)
							{
								// Console.WriteLine("{0}", e.ToString());
							}
						}
					}
				}
			}
			catch (TypeLoadException)
			{
			}
			catch (BadImageFormatException)
			{
			}
			catch (ReflectionTypeLoadException)
			{
			}
			catch (FileLoadException)
			{
			}
			catch (MissingMethodException)
			{
			}
		}
		
		public void SearchForComponents(String folder)
		{
			//string[] pluginFiles = Directory.GetFiles(folder, "*.dll");
			string[] pluginFiles = Directory.GetFiles(folder, "*.exe");

			foreach (String s in pluginFiles)
				AddComponent(Path.GetFullPath(s));
		}
		
		public void OpenMustExists(DockFrame frame)
		{
			foreach (ComponentFactoryInformation info in mComponents)
			{
				if (info.InstanceMustExist && info.DockWidget == null)
				{
					info.DockWidget = info.CreateInstance(frame);
					info.DockWidget.Show();
					
					if (info.HideOnCreate)
						info.DockWidget.Hide();
				}
			}
		}
	}

	public interface ITypes
	{
		void Type(Type[] types);
	}

}

