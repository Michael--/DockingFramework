
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Docking.Tools;
using Gtk;

namespace Docking.Components
{
   public class ComponentFinder
   {
      private readonly List<ComponentFactoryInformation> mComponents = new List<ComponentFactoryInformation>();

      private readonly TypeFilter mTypeFilter = new TypeFilter(InterfaceFilterCallback);
      private readonly List<Type> mTypes      = new List<Type>();

      /// <summary>
      /// Default ctor Initializes a new ComponentFinder instance.
      /// </summary>
      public ComponentFinder()
      {
         //nothing to do
      }

      public ComponentFactoryInformation[] ComponentInfos
      {
         get { return mComponents.ToArray(); }
      }

      public ComponentFactoryInformation FindEntryPoint(Type t)
      {
         foreach (ComponentFactoryInformation info in mComponents)
         {
            if (t == info.ComponentType)
            {
               return info;
            }
         }

         return null;
      }

      public ComponentFactoryInformation FindComponent(String typename)
      {
         foreach (ComponentFactoryInformation info in mComponents)
         {
            try
            {
               if (typename == info.ComponentType.ToString())
               {
                  return info;
               }
            }
            catch(Exception)
            {
               // NOP
            }
         }

         return null;
      }

      public Widget FindInstance(Type type)
      {
         foreach (ComponentFactoryInformation info in mComponents)
         {
            Type t = info.ComponentType;
            if (t != null)
            {
               if (t == type)
               {
                  return info.DockWidget;
               }

               Type[] myInterfaces = t.FindInterfaces(mTypeFilter, type);
               if (myInterfaces.Length > 0 && info.DockWidget != null)
               {
                  return info.DockWidget;
               }
            }
         }

         return null;
      }

      public void SearchForComponents(String[] list_of_pathes_with_wildcards = null)
      {
         if (list_of_pathes_with_wildcards == null)
         {
            list_of_pathes_with_wildcards = new string[]
            {
               Path.Combine(AssemblyInfoExt.Directory, "*.exe"),
               Path.Combine(AssemblyInfoExt.Directory, "*.dll")
            };
         }

         List<String> found_files = new List<string>();
         foreach (String s in list_of_pathes_with_wildcards)
         {
            String folder = Path.GetDirectoryName(s);
            String wildcard = Path.GetFileName(s);
            string[] files = Directory.GetFiles(folder, wildcard);
            found_files.AddRange(files);
         }

         foreach (String s in found_files)
         {
            CollectTypes(Path.GetFullPath(s));
         }

         SearchComponents();
      }

      /// <summary>
      /// Searches for requested type in all available components DLLs
      /// The searched type could be a class, an abstract class or an interface
      /// </summary>
      public Type[] SearchForTypes(Type search)
      {
         List<Type> theList = new List<Type>();
         foreach (Type type in mTypes)
         {
            if (!type.IsAbstract && type.IsClass)
            {
               // check if requested interface implemented
               if (search.IsInterface)
               {
                  try
                  {
                     if (search.IsAssignableFrom(type))
                     {
                        if (!theList.Contains(type)) // avoid duplicates
                        {
                           theList.Add(type);
                        }
                     }
                  }
                  catch(Exception)
                  {
                     // This is a workaround to catch the exception:
                     // Could not load type of field 'Docking.Components.ScriptEditor:m_TextEditor' (0) due to: Could not load file or assembly 'Mono.TextEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null' or one of its dependencies.
                  }
               }

               // test current type and search also in the base class tree
               else
               {
                  for (Type t = type; t != null; t = t.BaseType)
                  {
                     if (t.Name == search.Name)
                     {
                        if (!theList.Contains(type)) // avoid duplicates
                        {
                           theList.Add(type);
                        }

                        break;
                     }
                  }
               }
            }
         }

         return theList.ToArray();
      }

      public List<ComponentFactoryInformation> GetAutoCreateList(ComponentManager cm)
      {
         List<ComponentFactoryInformation> result = new List<ComponentFactoryInformation>();
         foreach (ComponentFactoryInformation info in mComponents)
         {
            if (info.AutoCreate)
            {
               result.Add(info);
            }
         }

         return result;
      }

      private Component CreateInstance(ComponentFactoryInformation info, ComponentManager cm)
      {
         Component component = info.CreateInstance(cm);
         return component;
      }

      private static bool InterfaceFilterCallback(Type typeObj, System.Object criteriaObj)
      {
         return typeObj == (Type)criteriaObj;
      }

      private void SearchComponents()
      {
         // find all non-abstract classes which inherit from interface 'IComponentFactory'
         Type[] factories = SearchForTypes(typeof(IComponentFactory));

         foreach (Type t in factories)
         {
            try
            {
               IComponentFactory cf = Activator.CreateInstance(t) as IComponentFactory;
               if (cf == null)
               {
                  continue;
               }

               mComponents.Add(new ComponentFactoryInformation(cf, true));
            }
            catch(InvalidCastException /*e*/)
            {
               // Console.WriteLine("{0}", e.ToString());
            }
            catch(MissingMethodException /*e*/)
            {
               // Console.WriteLine("{0}", e.ToString());
            }
            catch(ReflectionTypeLoadException /*e*/)
            {
               // Console.WriteLine("{0}", e.ToString());
            }
            catch(Exception /*e*/)
            {
               // Console.WriteLine("{0}", e.ToString());
            }
         }
      }

      private void CollectTypes(String filename)
      {
         // save runtime Exceptions by ignoring known problematic files
         List<string> filenames_to_skip = new List<string>()
         {
            "SQLiteNetExtensions.dll",
            "sqlite3.dll"
         };
         foreach (string s in filenames_to_skip)
         {
            if (filename.ToLowerInvariant().EndsWith(Path.DirectorySeparatorChar + s.ToLowerInvariant()))
            {
               return;
            }
         }

         try
         {
            Assembly asm = Assembly.LoadFrom(filename);
            Type[] types = asm.GetExportedTypes();
            mTypes.AddRange(types);
         }
         catch(FileNotFoundException)
         { } // cheap
         catch(BadImageFormatException)
         { } // cheap
         catch(ReflectionTypeLoadException)
         { } // cheap
         catch(MissingMethodException)
         { } // cheap
         catch(TypeLoadException)
         { } // cheap
#if DEBUG
         catch(Exception e) // runtime expensive! avoid getting here to have a speedy start!
         {
            Console.WriteLine("cannot load framework components DLL '{0}':", filename);
            Console.WriteLine("   " + e.ToString());
         }
#else
         catch(Exception)                      // runtime expensive! avoid getting here to have a speedy start!
         { /* NOP */ }
#endif
      }
   }


   public static class ComponentFinderHelper
   {
      public static readonly ComponentFinder Instance = new ComponentFinder();
   }

   public class ComponentFactoryInformation
   {
      public ComponentFactoryInformation(IComponentFactory factory, bool active)
      {
         Debug.Assert(factory != null);
         ComponentFactory = factory;
         Active           = active;
      }

      public Type FactoryType
      {
         get { return ComponentFactory.GetType(); }
      }

      public Type ComponentType
      {
         get
         {
            try
            {
               return ComponentFactory.TypeOfInstance;
            }
            catch(Exception)
            {
               return null;
            }
         }
      }

      public String Comment
      {
         get { return ComponentFactory.Comment; }
      }

      public String MenuPath
      {
         get { return ComponentFactory.MenuPath; }
      }

      public String LicenseGroup
      {
         get { return ComponentFactory.LicenseGroup; }
      }

      public String Name
      {
         get { return ComponentFactory.Name; }
      }

      public List<FileFilterExt> SupportedFileTypes
      {
         get { return ComponentFactory.SupportedFileTypes; }
      }

      public bool MultiInstance
      {
         get { return (ComponentFactory.Options & Mode.MultiInstance) != 0; }
      }

      public bool AutoCreate
      {
         get { return (ComponentFactory.Options & Mode.AutoCreate) != 0; }
      }

      public bool HideOnCreate
      {
         get { return (ComponentFactory.Options & Mode.HideOnCreate) != 0; }
      }

      public bool PreventClosing
      {
         get { return (ComponentFactory.Options & Mode.PreventClosing) != 0; }
      }

      public Gdk.Pixbuf Icon
      {
         get { return ComponentFactory.Icon; }
      }

      public IComponentFactory ComponentFactory { get; set; }

      public bool Active { get; set; }

      public Widget DockWidget { get; set; }

      public bool SupportsFile(string _file)
      {
         if (null != ComponentFactory.SupportedFileTypes)
         {
            foreach (FileFilterExt file_matcher in ComponentFactory.SupportedFileTypes)
            {
               if (file_matcher.Matches(_file))
               {
                  return true;
               }
            }
         }

         return false;
      }

      public Component CreateInstance(ComponentManager cm)
      {
         if (cm == null)
         {
            return null;
         }

         if (!cm.LicenseGroup.IsEnabled(LicenseGroup))
         {
            return null;
         }

         Component component;
         try
         {
            component = Activator.CreateInstance(ComponentType) as Component;
         }
         catch(Exception e)
         {
            cm.MessageWriteLine(e.ToString());
            return null;
         }

         if (component == null)
         {
            cm.MessageWriteLine("Error: class '{0}' does not inherit from 'Component'", ComponentType.ToString());
            return null;
         }

         component.ComponentManager = cm;
         return component;
      }
   }
}
