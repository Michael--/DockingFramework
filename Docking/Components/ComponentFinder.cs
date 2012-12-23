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
        public class ComponentFactoryInformation
        {
            public ComponentFactoryInformation (ComponentFactory factory, bool active)
            {
                Debug.Assert (factory != null);
                ComponentFactory = factory;
                Active = active;
            }
            
            public Widget CreateInstance(DockFrame frame)
            {
                Widget widget;
                try
                {
                    widget = (Widget)Activator.CreateInstance (ComponentType, new System.Object[] {frame});
                }
                catch (Exception e)
                {
                    Console.WriteLine (e.ToString ());
                    return null;
                }
                return widget;
            }
            
            public Type FactoryType { get { return ComponentFactory.GetType (); } }

            public Type ComponentType{ get { return ComponentFactory.TypeOfInstance; } }

            public String Comment{ get { return ComponentFactory.Comment; } }

            public String MenuPath{ get { return ComponentFactory.MenuPath; } }

            public bool IsSingleInstance{ get { return (ComponentFactory.Options & ComponentFactory.Mode.MultipleInstance) != ComponentFactory.Mode.MultipleInstance; } }

            public bool InstanceMustExist{ get { return (ComponentFactory.Options & ComponentFactory.Mode.AutoCreate) == ComponentFactory.Mode.AutoCreate; } }

            public bool HideOnCreate { get { return (ComponentFactory.Options & ComponentFactory.Mode.Hidden) == ComponentFactory.Mode.Hidden; } }

            ComponentFactory ComponentFactory { get; set; }

            public bool Active { get; set; }

            public Widget DockWidget { get; set; }
        }

        private List<Type> mTypes = new List<Type>();

        private List<ComponentFactoryInformation> mComponents = new List<ComponentFactoryInformation> ();
        
        public ComponentFactoryInformation[] ComponentInfos{ get { return mComponents.ToArray (); } }

        Widget CreateInstance(ComponentFactoryInformation info, DockFrame frame)
        {
            Widget widget = info.CreateInstance (frame);
            return widget;
        }
        
        public ComponentFactoryInformation FindEntryPoint(Type t)
        {
            foreach (ComponentFactoryInformation info in mComponents)
            {
                if (t == info.ComponentType)
                    return info;
            }
            return null;
        }
        
        private ComponentFactoryInformation FindComponent(String typename)
        {
            foreach (ComponentFactoryInformation info in mComponents)
            {
                if (typename == info.ComponentType.ToString())
                    return info;
            }
            return null;
        }

        public Widget CreateInstance(Type type, DockFrame frame)
        {
            foreach (ComponentFactoryInformation info in mComponents)
            {
                Type t = info.ComponentType;
                if (t != null)
                {
                    if (t == type)
                    {
                        info.DockWidget = CreateInstance (info, frame);
                        return info.DockWidget;
                    }
                    Type[] myInterfaces = t.FindInterfaces (mTypeFilter, type);
                    if (myInterfaces.Length > 0)
                    {
                        info.DockWidget = CreateInstance (info, frame);
                        return info.DockWidget;
                    }
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
                    Type[] myInterfaces = t.FindInterfaces (mTypeFilter, type);
                    if (myInterfaces.Length > 0 && info.DockWidget != null)
                    {
                        return info.DockWidget;
                    }
                }
            }
            return null;
        }

        TypeFilter mTypeFilter = new TypeFilter (InterfaceFilterCallback);

        private static bool InterfaceFilterCallback(Type typeObj, System.Object criteriaObj)
        {
            return typeObj == (Type)criteriaObj;
        }
        
        void SearchComponents()
        {
            foreach (Type t in mTypes)
            {
                // find all real classes which implement interface 'IComponentFactory'
                // and create an inctance of such 
                if (t.IsClass)
                {
                    Type[] myInterfaces = t.FindInterfaces (mTypeFilter, typeof(IComponentFactory));
                    if (myInterfaces.Length > 0)
                    {
                        try
                        {
                            ComponentFactory cf = (ComponentFactory)Activator.CreateInstance (t);
                            mComponents.Add (new ComponentFactoryInformation (cf, true));
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

        public void SearchForComponents(String search)
        {
            SearchForComponents (new String[] { search });
        }
        
        public void SearchForComponents(String[]search)
        {
            List<String> componentFiles = new List<string> ();
            foreach (String s in search)
            {
                String folder = Path.GetDirectoryName (s);
                String name = Path.GetFileName (s);

                string[] files = Directory.GetFiles (folder, name);
                componentFiles.AddRange (files);
            }
            foreach (String s in componentFiles)
                CollectTypes (Path.GetFullPath (s));

            SearchComponents();
        }

        void CollectTypes(String filename)
        {
            try
            {
                Assembly asm = Assembly.LoadFrom (filename);
                Type[] types = asm.GetExportedTypes ();
                mTypes.AddRange(types);
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
        
        public void OpenMustExists(DockFrame frame)
        {
            foreach (ComponentFactoryInformation info in mComponents)
            {
                if (info.InstanceMustExist && info.DockWidget == null)
                {
                    info.DockWidget = info.CreateInstance (frame);
                    info.DockWidget.Show ();
                    
                    if (info.HideOnCreate)
                        info.DockWidget.Hide ();
                }
            }
        }
    }
}

