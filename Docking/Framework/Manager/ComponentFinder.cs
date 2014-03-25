using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Docking;
using Gtk;
using System.Diagnostics;

namespace Docking.Components
{
   public class ComponentFinder
   {
      private List<Type> mTypes = new List<Type>();

      private List<ComponentFactoryInformation> mComponents = new List<ComponentFactoryInformation>();

      public ComponentFactoryInformation[] ComponentInfos { get { return mComponents.ToArray(); } }

      public ComponentFactoryInformation FindEntryPoint(Type t)
      {
         foreach (ComponentFactoryInformation info in mComponents)
         {
            if (t == info.ComponentType)
               return info;
         }
         return null;
      }

      public ComponentFactoryInformation FindComponent(String typename)
      {
         foreach (ComponentFactoryInformation info in mComponents)
         {
            if (typename == info.ComponentType.ToString())
               return info;
         }
         return null;
      }

      Widget CreateInstance(ComponentFactoryInformation info, ComponentManager cm)
      {
         Widget widget = info.CreateInstance(cm);
         return widget;
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

      TypeFilter mTypeFilter = new TypeFilter(InterfaceFilterCallback);

      private static bool InterfaceFilterCallback(Type typeObj, System.Object criteriaObj)
      {
         return typeObj == (Type)criteriaObj;
      }

      void SearchComponents()
      {
         // find all non-abstract classes which inherit from interface 'IComponentFactory'
         Type[] factories = SearchForTypes(typeof(IComponentFactory));

         foreach (Type t in factories)
         {
            try
            {
               ComponentFactory cf = Activator.CreateInstance(t) as ComponentFactory;
               if(cf==null)
                  continue;
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

      public void SearchForComponents(String search)
      {
         SearchForComponents(new String[] { search });
      }

      public void SearchForComponents(String[] search)
      {
         List<String> componentFiles = new List<string>();
         foreach (String s in search)
         {
            String folder = Path.GetDirectoryName(s);
            String name = Path.GetFileName(s);

            string[] files = Directory.GetFiles(folder, name);
            componentFiles.AddRange(files);
         }
         foreach (String s in componentFiles)
            CollectTypes(Path.GetFullPath(s));

         SearchComponents();
      }

      void CollectTypes(String filename)
      {
         try
         {
            Assembly asm = Assembly.LoadFrom(filename);
            Type[] types = asm.GetExportedTypes();
            mTypes.AddRange(types);
         }
         catch (FileNotFoundException) { } // cheap            
         catch (BadImageFormatException) { } // cheap
         catch (ReflectionTypeLoadException) { } // cheap
         catch (MissingMethodException) { } // cheap
         catch (TypeLoadException) { } // cheap
         catch (Exception e) // runtime expensive! avoid getting here to have a speedy start!
         {
            Console.WriteLine(e.ToString());
         }
      }

      /// <summary>
      /// Searches for requested type in all available components DLLs
      /// The searched type could be a class, an abstract class or an interface
      /// </summary>

      public Type[] SearchForTypes(Type search)
      {
         TypeFilter TypeFilter = new TypeFilter(InterfaceFilterCallback);
         List<Type> theList = new List<Type>();
         foreach (Type type in mTypes)
         {
            if (!type.IsAbstract && type.IsClass)
            {
               // check if requested interface implemented
               if (search.IsInterface)
               {
                  if (type.FindInterfaces(TypeFilter, search).Length > 0)
                  {
                     if (!theList.Contains(type)) // avoid duplicates
                        theList.Add(type);
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
                           theList.Add(type);
                        break;
                     }
                  }
               }

            }
         }
         return theList.ToArray();
      }


      public List<ComponentFactoryInformation> GetMustExistList(ComponentManager cm)
      {
         List<ComponentFactoryInformation> ac = new List<ComponentFactoryInformation>();

         foreach (ComponentFactoryInformation info in mComponents)
         {
            if (info.AutoCreate)
               ac.Add(info);
         }
         return ac;
      }
   }

   public class ComponentFactoryInformation
   {
      public ComponentFactoryInformation(ComponentFactory factory, bool active)
      {
         Debug.Assert(factory != null);
         ComponentFactory = factory;
         Active = active;
      }

      public Widget CreateInstance(ComponentManager cm)
      {
         Widget widget;
         try { widget = (Widget)Activator.CreateInstance(ComponentType); }
         catch (Exception e)
         {
            Console.WriteLine(e.ToString());
            return null;
         }
         if (widget is Component)
            (widget as Component).ComponentManager = cm;
         return widget;
      }

      public Type   FactoryType    { get { return ComponentFactory.GetType(); } }
      public Type   ComponentType  { get { return ComponentFactory.TypeOfInstance; } }
      public String Comment        { get { return ComponentFactory.Comment; } }
      public String MenuPath       { get { return ComponentFactory.MenuPath; } }

      public bool   MultiInstance  { get { return (ComponentFactory.Options & ComponentFactory.Mode.MultiInstance )!=0; } }
      public bool   AutoCreate     { get { return (ComponentFactory.Options & ComponentFactory.Mode.AutoCreate    )!=0; } }
      public bool   HideOnCreate   { get { return (ComponentFactory.Options & ComponentFactory.Mode.HideOnCreate  )!=0; } }
      public bool   PreventClosing { get { return (ComponentFactory.Options & ComponentFactory.Mode.PreventClosing)!=0; } }

      public Gdk.Pixbuf Icon { get { return ComponentFactory.Icon; } }

      public ComponentFactory ComponentFactory { get; set; }

      public bool Active { get; set; }

      public Widget DockWidget { get; set; }
   }
}

