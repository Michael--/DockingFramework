
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Globalization;
using Docking.Tools;
using Docking.Widgets;
using Docking.Framework;
using Docking.Framework.Interfaces;
using Docking.Framework.Tools;
using Gtk;
using Docking.Helper;

namespace Docking.Components
{
   // This is a workaround for the problem that we cannot write "blablabla".Localized(this) inside class "ComponentManager",
   // Because that would yield the wrong namespace: not "Docking.Components", but the one of the inherited main application class.
   internal static class StringLoc
   {
      public static string L(this string s)
      {
         return s.Localized("Docking.Components");
      }
   }

   public class ComponentManager : IMessageWriteLine, ICut, ICopy, IPaste
   {
      public  int             MaxRecentFiles                    = 9;
      private bool            mInitialLoadOfComponentsCompleted = false;

      private System.Action           mSavePersistencyOnShutdownAction = () => { };
      private System.Func<Gdk.Pixbuf> mScreenshotAction       = () => { return new Gdk.Pixbuf(IntPtr.Zero); };

      private ScrollFunc mRegisteredScrollFuncs = (p1, p2, p3, p4, p5) => { return true; };

      private DockVisualStyle mSelectedStyle;
      private DockVisualStyle mNormalStyle;
      /// <summary>
      /// Relation between any object and its parent DockItem
      /// Need to find fast the DockItem for any user selection of an object
      /// Objects are normally widgets and similar
      /// </summary>
      private readonly Dictionary<object, DockItem> mSelectRelation = new Dictionary<object, DockItem>();

      private readonly List<IPropertyViewer> mPropertyInterfaces = new List<IPropertyViewer>();
      private readonly List<IScript>         mScriptInterfaces   = new List<IScript>();

      private readonly List<object> mComponents = new List<object>();

      /// <summary>
      /// Initializes a new instance
      /// Note: Make sure that you construct this class from the main thread!
      /// </summary>
      internal ComponentManager()
      {
         Settings1 = new TGSettings();

         AccelGroup      = new AccelGroup();
         LicenseFile     = new LicenseFile();
         LicenseGroup    = new LicenseGroup();
         ComponentFinder = ComponentFinderHelper.Instance;
         ScriptEngine    = new PythonScriptEngine(this);
         LogWriter       = new LogWriter();

         GtkDispatcher.Instance.RegisterShutdownHandler(QuitInternal);
      }

      public bool IsMainThread
      {
         get { return GtkDispatcher.Instance.IsMainThread; }
      }

      public LogWriter LogWriter { get; private set; }

      public LicenseFile LicenseFile { get; set; }

      public PythonScriptEngine ScriptEngine { get; private set; }

      public string ApplicationName { get; private set; }

      public bool Visible
      {
         get { return MainWindowBase.Visible; }
         set
         {
            MainWindowBase.Visible      = value;
            LogWriter.EnableLogging = value;
         }
      }

      public IReadOnlyCollection<object> Components { get { return mComponents; } }

      public Gtk.Window MainWindow { get { return MainWindowBase; } }

      internal MainWindowBase MainWindowBase { get; set; }

      public IMenuService MenuService { get { return MainWindowBase; } }

      #region private properties

      public TGSettings Settings1 { get; set; }

      public IPersistency Persistence { get { return Settings1 as IPersistency; } }

      public delegate bool ScrollFunc(object sender, double lat, double lon, int percent, bool repaint);
      public void RegisterScrollToHandler(ScrollFunc fn)
      {
         mRegisteredScrollFuncs += fn;
      }

      public void RegisterSavePersistencyOnShutdownHandler(System.Action handler)
      {
         mSavePersistencyOnShutdownAction = handler;
      }

      public void RegisterScreenshotHandler(System.Func<Gdk.Pixbuf> handler)
      {
         mScreenshotAction = handler;
      }

      public DialogProvider DialogProvider { get; internal set; }

      #endregion

      #region public properties

      public AccelGroup AccelGroup { get; private set; }
      internal DockItem CurrentDockItem { get; private set; }
      internal DockFrame DockFrame { get; private set; }

      public ComponentFinder ComponentFinder { get; private set; }
      public LicenseGroup LicenseGroup { get; private set; }

      public Localization Localization { get; internal set; }
      public bool OperateInBatchMode { get; set; }

      public bool PowerDown
      {
         get { return GtkDispatcher.Instance.IsShutdown; }
      }

      // default font for all languages except Arabic and Hebrew
      // @see DEFAULT_FONT_ARAB
      // @see DEFAULT_FONT_HEBR
      public static string DEFAULT_FONT  { get { return PlatformFonts.DEFAULT_FONT; } }

      // default font for Arabic
      // @see DEFAULT_FONT
      // @see DEFAULT_FONT_HEBR
      public static string DEFAULT_FONT_ARAB  {  get { return PlatformFonts.DEFAULT_FONT_ARAB; } }

      // default font for Hebrew
      // @see DEFAULT_FONT
      // @see DEFAULT_FONT_ARAB
      public static string DEFAULT_FONT_HEBR  { get { return PlatformFonts.DEFAULT_FONT_HEBR; } }

      #endregion

      public IArchive Archive { get; set; }

      public void SetDockFrame(DockFrame df)
      {
         DockFrame = df;
         DockFrame.DockItemRemoved += HandleDockItemRemoved;
         DockFrame.CreateItem = this.CreateItem;

         var style = new DockVisualStyle();
         style.PadTitleLabelColor = Styles.PadLabelColor;
         style.PadBackgroundColor = Styles.PadBackground;
         style.InactivePadBackgroundColor = Styles.InactivePadBackground;
         DockFrame.DefaultVisualStyle = style;

         mNormalStyle = DockFrame.DefaultVisualStyle;
         mSelectedStyle = DockFrame.DefaultVisualStyle.Clone();
         mSelectedStyle.PadBackgroundColor = new Gdk.Color(100, 160, 255);
      }

      public void AddLayout(string name, bool copyCurrent)
      {
         if (!DockFrame.HasLayout(name))
         {
            DockFrame.CreateLayout(name, copyCurrent);
         }
      }

      private void DeleteLayout(string name)
      {
         if(name != DockFrame.CurrentLayout)
         {
            DockFrame.DeleteLayout(name);
         }
      }

      void ICut.Cut()
      {
         if (CurrentDockItem != null && CurrentDockItem.Content != null)
         {
            if (CurrentDockItem.Content is ICut)
               (CurrentDockItem.Content as ICut).Cut();
            else
               MessageWriteLine("current component does not implement interface ICut");
         }
      }

      void ICopy.Copy()
      {
         if (CurrentDockItem != null && CurrentDockItem.Content != null)
         {
            if (CurrentDockItem.Content is ICopy)
               (CurrentDockItem.Content as ICopy).Copy();
            else
               MessageWriteLine("current component does not implement interface ICopy");
         }
      }

      void IPaste.Paste()
      {
         if (CurrentDockItem != null && CurrentDockItem.Content != null)
         {
            if (CurrentDockItem.Content is IPaste)
               (CurrentDockItem.Content as IPaste).Paste();
            else
               MessageWriteLine("current component does not implement interface IPaste");
         }
      }


      public bool OpenFile(string filename, bool synchronous = false, object archiveHandle = null)
      {
         if (archiveHandle == null && Directory.Exists(filename))
         {
            MessageBox.Show("Opening whole directories like\n'{0}'\nis not supported. Please choose individual files instead.".FormatLocalizedWithPrefix("Docking.Components", filename));
            return false;
         }

         List<Component> existing_components = new List<Component>();

         bool isArchive = archiveHandle == null && Archive != null && Archive.IsArchive(filename);
         int openedArchiveFiles = 0;
         if (isArchive)
         {
            var handle = Archive.Open(filename);
            if (handle != null)
            {
               var files = Archive.GetFileNames(handle);
               foreach (var s in files)
               {
                  if (OpenFile(s, synchronous, handle))
                     openedArchiveFiles++;
               }
               Archive.Close(handle);
            }
         }
         else
         {
            // open file from archive need optionally copy of file
            if (archiveHandle != null)
            {
               filename = Archive.Extract(archiveHandle, filename);
            }

            if (!File.Exists(filename))
            {
               MessageWriteLine("File {0} does not exist".FormatLocalizedWithPrefix("Docking.Components", filename));
               return false;
            }

            // TODO: consider all and let the user pick which one
            /*if (!isArchive)
            {
               MessageWriteLine(Localization.Format("Docking.Components.Opening file {0} as {1}..."), filename, openers[0].Value);
               bool success = openers[0].Key.OpenFile(filename);
               MessageWriteLine(success ? "File opened successfully" : "File opening failed");

            }**/

            // search already created instances of components if they can handle this file:
            foreach (DockItem item in DockFrame.GetItems())
            {
               Component c = item.Content as Component;
               if (c is IFileOpen && (c as IFileOpen).CanOpenFile(filename))
               {
                  existing_components.Add(c);
               }
            }

            // search for available components
            List<ComponentFactoryInformation> available_components = new List<ComponentFactoryInformation>();
            foreach (ComponentFactoryInformation cfi in ComponentFinder.ComponentInfos)
            {
               if (typeof(IFileOpen).IsAssignableFrom(cfi.ComponentType) && cfi.SupportsFile(filename) && LicenseGroup.IsEnabled(cfi.LicenseGroup))
               {
                  // check if this is a single instance type and an instance is already open
                  var ec = existing_components.Where(x => x.ComponentInfo.ComponentType==cfi.ComponentType && !cfi.MultiInstance);
                  if (ec.Count() == 0)
                     available_components.Add(cfi);
               }
            }

            if(available_components.Count + existing_components.Count > 1) // show a dialog and let the user choose
            {
               // create a dialog and update the internal component model
               int result = DialogProvider.SelectComponentDialog(ref available_components, ref existing_components);

               // show to let the user choose how to open that file
               if(result==(int)ResponseType.Ok)
               {
                  bool success = true;
                  // create a instance for each selected component
                  foreach ( ComponentFactoryInformation cfi in available_components )
                  {
                     var component = CreateComponent(cfi, true ).Content;
                     if (component is IFileOpen)
                     {
                        MessageWriteLine(Localization.Format("Docking.Components.Opening file {0} in component {1}..."), filename, cfi.Name );
                        success &= (component as IFileOpen).OpenFile(filename, synchronous);
                        MessageWriteLine(success ? "File opened successfully" : "File opening failed");
                     }
                  }

                  // forward this file to all selected instances
                  foreach (Component component in existing_components)
                  {
                     MessageWriteLine(Localization.Format("Docking.Components.Opening file {0} in component {1}..."), filename, component.Name);
                     success &= (component as IFileOpen).OpenFile(filename, synchronous);
                     MessageWriteLine(success ? "File opened successfully" : "File opening failed");
                  }

                  if (success)
                  {
                     MenuService.AddRecentFile(filename);
                  }
                  return success;
               }
            }
            else if(existing_components.Count==1 && available_components.Count==0) // use an existing component
            {
               MessageWriteLine(Localization.Format("Docking.Components.Opening file {0} in component {1}..."), filename, existing_components[0].Name);
               bool success = (existing_components[0] as IFileOpen).OpenFile(filename, synchronous);
               if (success)
               {
                  MenuService.AddRecentFile(filename);
               }
               MessageWriteLine(success ? "File opened successfully" : "File opening failed");
               return success;

            }
            else if(existing_components.Count==0 && available_components.Count==1) // instantiate a new component
            {
               var component = CreateComponent(available_components[0], true).Content;
               if (component is IFileOpen)
               {
                  MessageWriteLine(Localization.Format("Docking.Components.Opening file {0} in component {1}..."), filename, available_components[0].Name);
                  bool success = (component as IFileOpen).OpenFile(filename, synchronous);
                  if (success)
                  {
                     MenuService.AddRecentFile(filename);
                  }
                  MessageWriteLine(success ? "File opened successfully" : "File opening failed");
                  return success;
               }
            }
            else // no component available at all
            {
               MessageBox.Show("Could not find any component which can handle file '{0}'".FormatLocalizedWithPrefix("Docking.Components"), filename);
               return false;
            }
         }

         // any archive opening
         if (openedArchiveFiles > 0)
            MenuService.AddRecentFile(filename);

         return openedArchiveFiles > 0;
      }

      #region Configuration

      public void AddComponent(object o)
      {
         if (!mComponents.Contains(o))
            mComponents.Add(o);

         if(mInitialLoadOfComponentsCompleted)
         {
            foreach(var item in mComponents)
            {
               if(item==o) // objects do not need to be notified of their own birth :)
                  continue;
               if(item is Component)
                  (item as Component).ComponentAdded(o);
               if(o is Component)
                  (o as Component).ComponentAdded(item);
            }
         }
      }

      public void RemoveComponent(object o)
      {
         if(mComponents.Contains(o))
            mComponents.Remove(o);

         if(mInitialLoadOfComponentsCompleted)
            foreach(object item in mComponents)
               if(item!=o && (item is Component)) // // objects do not need to be notified of their own death :)
                  (item as Component).ComponentRemoved(o);
      }


      public void ComponentsLoaded()
      {
         mDisableHandleVisibleChanged = false;

         // startup time measurement
         System.Diagnostics.Stopwatch total = new System.Diagnostics.Stopwatch();
         total.Start();

         // ensure that all components which have .AutoCreate set are present
         List<ComponentFactoryInformation> autocreate = ComponentFinder.GetAutoCreateList(this);
         foreach(ComponentFactoryInformation cfi in autocreate)
         {
            bool found = false;
            foreach(DockItem item in DockFrame.GetItems())
            {
               if(item.Content!=null && item.Content.GetType()==cfi.ComponentType)
               {
                  found = true;
                  break;
               }
            }
            if(!found)
            {
               if (LicenseGroup.IsEnabled(cfi.LicenseGroup))
               {
                  DockItem item = CreateComponent(cfi, false);
                  if (item != null && cfi.HideOnCreate)
                     item.Visible = false;
               }
            }
         }

         // tell all components about load state
         // time for late initialization and/or loading persistence
          foreach(DockItem item in DockFrame.GetItems())
          {
             if (item.Content is Component)
             {
                var component = item.Content as Component;
                System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
                w.Start();
                var allComponents = AllComponentsOfType<Component>(component);
                var allIPersistable = AllComponentsOfType<IPersistable>(component);

                foreach (var c in allComponents)
                   c.DockItem = item;
                foreach (var c in allIPersistable)
                {
                   try
                   {
                      c.LoadFrom(Settings1);
                   }
                   catch (Exception e)
                   {
                      MessageWriteLine("{0}.LoadFrom() Exception:{1}", c.GetType(), e);
                   }
                }

                w.Stop();

                #if DEBUG
                {
                   if (w.ElapsedMilliseconds > 300) // goal limit: 25, 300 is just to reduce current clutter
                      MessageWriteLine("Invoking IComponent.Loaded() for component {0} took {1:0.00}s", item.Id, w.Elapsed.TotalSeconds);
                }
                #endif

                component.VisibilityChanged(item.Content, item.Visible);
             }

             if(item.Content is IPropertyViewer)
                mPropertyInterfaces.Add(item.Content as IPropertyViewer);

             if(item.Content is IScript)
                mScriptInterfaces.Add(item.Content as IScript);

             if(item.Content is Component)
                AddComponent(item.Content as Component);
          }

         mInitialLoadOfComponentsCompleted = true;
         List<object> components = new List<object>(mComponents);

         mComponents.Clear();

         foreach (object o in components)
         {
            AddComponent(o);
         }

         foreach (DockItem item in DockFrame.GetItems())
         {
            if (item.Content is Component)
            {
               var component = item.Content as Component;
               var allComponents = AllComponentsOfType<Component>(component);
               foreach (var c in allComponents)
               {
                  c.Loaded();
               }
            }
         }

         total.Stop();

         #if DEBUG
         if(total.ElapsedMilliseconds>1500)
            MessageWriteLine("ComponentsLoaded() total time = {0}s", total.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture));
         #endif
      }

      private void ComponentsSave()
      {
         foreach (DockItem item in DockFrame.GetItems())
         {
            var allIPersistable = AllComponentsOfType<IPersistable>(item.Content);
            foreach (var p in allIPersistable)
            {
               try
               {
                  p.SaveTo(Settings1);
               }
               catch (Exception e)
               {
                  MessageWriteLine("{0}.SaveTo() Exception:{1}", p.GetType(), e);
               }
            }
         }

         Settings1.SaveLayout(DockFrame);
      }

      private void ComponentsRemove()
      {
         foreach (DockItem item in DockFrame.GetItems())
         {
            if (item.Content is Component)
            {
               foreach (DockItem other in DockFrame.GetItems())
               {
                  if (other != item)
                  {
                     (item.Content as Component).ComponentRemoved(other);
                  }
               }
            }
         }
      }

      public void LoadConfigurationFile()
      {
         Settings1.LoadConfigurationFile();
      }

      public void SaveConfigurationFile()
      {
         ComponentsSave();
         Localization.WriteChangedResourceFiles();

         Settings1.SaveConfigurationFile();
      }

      public void Quit(bool save_persistency, System.Action beforeShutdownAction = null)
      {
         GtkDispatcher.Instance.InitiateShutdown(save_persistency, beforeShutdownAction);
      }

      private bool QuitInternal(bool save_persistency, System.Action beforeShutdown = null)
      {
         foreach (DockItem item in DockFrame.GetItems())
            if ((item.Content != null) && (item.Content is Component))
               if (!(item.Content as Component).IsCloseOK())
                  return false; // close has been canceled, for example by a dialog prompt which asks for saving an edited document

         // from here on, shutdown activity goes on, so returning false must not happen from here on!

         if (save_persistency)
         {
            mSavePersistencyOnShutdownAction();

            if(Settings1.IsReadonly)
               MessageWriteLine("skipping configuration saving because it is readonly");
            else
               SaveConfigurationFile();

            ComponentsRemove();
         }

         foreach (DockItem item in DockFrame.GetItems())
            if ((item.Content != null) && (item.Content is Component))
               (item.Content as Component).Closed();

         GtkDispatcher.Instance.IsShutdown = true;

         if(beforeShutdown!=null)
            beforeShutdown();

         Gtk.Application.Quit();
         return true;
      }

      public void OnDeleteEvent(object sender, DeleteEventArgs a)
      {
         Quit(true);
         a.RetVal = true;
      }

      #endregion

      #region Docking

      /// <summary>
      /// Searches for requested type in all available components DLLs
      /// </summary>
      public Type[] SearchForTypes(Type search)
      {
         return ComponentFinder.SearchForTypes(search);
      }


      public bool CurrentLicenseCoversTheCreationOfComponent(string typename)
      {
         ComponentFactoryInformation cfi = ComponentFinder.FindComponent(typename);
         return cfi!=null && LicenseGroup.IsEnabled(cfi.LicenseGroup);
      }

      public DockItem CreateComponent(string typename, bool initCalls)
      {
         ComponentFactoryInformation cfi = ComponentFinder.FindComponent(typename);
         return cfi==null ? null : CreateComponent(cfi, initCalls);
      }

      public DockItem CreateComponent(ComponentFactoryInformation cfi, bool initCalls)
      {
         if (!LicenseGroup.IsEnabled(cfi.LicenseGroup))
            return null;

         String name = cfi.ComponentType.ToString();

         // Find already existing - potentially invisible - instances for activation.
         // We'll only create a new instance if no such is found.
         if(!cfi.MultiInstance)
         {
            DockItem di = DockFrame.GetItem(name);
            if(di==null)
               di = DockFrame.GetItem(name+"-1"); // this can occur when a component's setting MultiInstance is enabled/disabled during debugging
            if(di!=null)
            {
               if(!di.Visible)
                  di.Visible = true;
               di.Present(true);
               return di;
            }

         }
         else
         {
            DockItem[] dis = DockFrame.GetItemsContainingSubstring(name); // TODO make a better, more precise match, see TODO there
            foreach(DockItem di in dis)
            {
               if(!di.Visible)
               {
                  di.Visible = true;
                  return di;
               }
            }

            // adjust the name string so it becomes a new, unique instance we'll create below
            int instance = 0;
            do
            {
               instance++;
               name = cfi.ComponentType.ToString() + "-" + instance.ToString();
            }
            while(DockFrame.GetItem(name)!=null);
         }

         // when we get here, a new instance of the desired component needs to be created

         DockItem item = CreateItem(cfi, name);
         if(item==null)
         {
            MessageWriteLine("ERROR: cannot instantiate component " + name);
            return null;
         }

         Localization.LocalizeControls(item.Content.GetType().Namespace, item.Widget);

         item.DefaultVisible = false;
         //item.DrawFrame = true;
         item.Visible = true;

         // call initialization of new created component
         if(initCalls)
         {
            if (item.Content is Component)
            {
               var component = item.Content as Component;
               var allComponents = AllComponentsOfType<Component>(component);
               var allIPersistable = AllComponentsOfType<IPersistable>(component);

               foreach (var c in allComponents)
                  c.DockItem = item;
               foreach (var c in allIPersistable)
               {
                  try
                  {
                     c.LoadFrom(Settings1);
                  }
                  catch (Exception e)
                  {
                     MessageWriteLine("{0}.LoadFrom() Exception:{1}", c.GetType(), e);
                  }
               }

               AddComponent(component);

               foreach (var c in allComponents)
               {
                  try
                  {
                     c.Loaded();
                  }
                  catch (Exception e)
                  {
                     MessageWriteLine("{0}.Loaded() Exception:{1}", c.GetType(), e);
                  }
               }
            }

            if (item.Content is IPropertyViewer)
               mPropertyInterfaces.Add(item.Content as IPropertyViewer);

            if (item.Content is IScript)
               mScriptInterfaces.Add(item.Content as IScript);
         }
         return item;
      }

      private static IEnumerable<T> AllComponentsOfType<T>(Widget widget)
      {
         var list = new List<T>();
         AllComponentsOfType<T>(widget, list);
         return list;
      }

      private static void AllComponentsOfType<T>(object widget, List<T> list)
      {
         if (widget is T)
            list.Add((T)widget);
         if (widget is Container)
         {
            foreach (var c in (widget as Container).AllChildren)
               if (c is Container)
                  AllComponentsOfType<T>(c as Container, list);
         }
      }

      private void HandleDockItemRemoved(DockItem item)
      {
         if(item.Content is IPropertyViewer)
            mPropertyInterfaces.Remove(item.Content as IPropertyViewer);

         // tell all other about current item changed if it the removed component
         if(CurrentDockItem == item)
         {
            // care all IPropertyViewers
            foreach(IPropertyViewer ip in mPropertyInterfaces)
               ip.SetObject(null);

            // care all IScript Widgets
            foreach(IScript isc in mScriptInterfaces)
               isc.SetScript(null, null);

            CurrentDockItem = null;
            foreach(DockItem other in DockFrame.GetItems())
            {
               if(other != item && other.Content is Component)
                  (other.Content as Component).FocusChanged(null);
            }
         }

         // tell all other about removed component
         foreach(DockItem other in DockFrame.GetItems())
         {
            if(other != item && other.Content is Component)
               (other.Content as Component).ComponentRemoved(item.Content);
         }

         // remove from message dictionary
         if (item.Content is IMessage)
         {
            LogWriter.RemoveMessageReceiver(item.Id);
         }

         // tell component about it instance itself has been removed from dock container
         if(item.Content is Component)
            (item.Content as Component).ComponentRemoved(item.Content);

         // item.Widget.Destroy();
      }

      bool mDisableHandleVisibleChanged = true; // startup lock

      private void HandleVisibleChanged(object sender, EventArgs e)
      {
         if(mDisableHandleVisibleChanged)
            return;

         DockItem item = sender as DockItem;
         if(item.Content is Component)
            (item.Content as Component).VisibilityChanged(item, item.Visible);
      }

      /// <summary>
      /// Create new item. This may be triggered by
      /// - a user interaction via the "View" menu
      /// - loading a persistence
      /// - running a Python script
      /// DO NOT INVOKE THIS METHOD from outside the component manager.
      /// If you want to create a component from outside, use ComponentManager.CreateComponent() instead!
      /// </summary>
      private DockItem CreateItem(ComponentFactoryInformation cfi, String id)
      {
         Component component = cfi.CreateInstance(this);
         if(component==null)
            return null;

         // keep component factory info of creation
         component.ComponentInfo = cfi;

         DockItem item = new DockItem(DockFrame, id, component);
         DockFrame.AddItem(item);

         AddSelectNotifier(item, item.Content);
         AddSelectNotifier(item, item.TitleTab);

         item.DefaultVisible = false;
         item.VisibleChanged += HandleVisibleChanged;

         item.Behavior = DockItemBehavior.Normal;
         /*if(cfi.CloseOnHide)*/ // No "if CloseOnHide" here anymore. "CloseOnHide" is now the implicit, implemented default UNLESS the user has explicitly set "PreventClosing" (see there).
         if(!cfi.PreventClosing)
            item.Behavior |= DockItemBehavior.CloseOnHide;

         if(item.Content is IMessage)
         {
            LogWriter.AddMessageReceiver(item.Id, item.Content as IMessage);
         }

         return item;
      }

      /// <summary>
      /// Create new item, called from persistence
      /// DO NOT INVOKE THIS METHOD from outside the component manager.
      /// If you want to create a component from outside, use ComponentManager.CreateComponent() instead!
      /// </summary>
      private DockItem CreateItem(string id)
      {
         String[] m = id.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
         if(m.Length == 0)
            return null;
         String typename = m[0];

         ComponentFactoryInformation cfi = ComponentFinder.FindComponent(typename);
         if(cfi == null)
            return null;

         if (!LicenseGroup.IsEnabled(cfi.LicenseGroup))
            return null;

         return CreateItem(cfi, id);
      }

      #endregion

      #region Select current DockItem

      /// <summary>
      /// Adds events for any child widget to find out which
      /// DockItem is selected by the user.
      /// Care the current selected DockItem, color the title bar.
      /// </summary>
      private void AddSelectNotifier(DockItem item, Widget w)
      {
         if(mSelectRelation.ContainsKey(w))
            return;

         mSelectRelation.Add(w, item);
         w.CanFocus = true;
         w.Events |= Gdk.EventMask.FocusChangeMask;
         w.FocusGrabbed += (object sender, EventArgs e) =>
         {
            SelectCurrentEvent(sender);
         };

         w.Events |= Gdk.EventMask.ButtonPressMask;
         w.ButtonPressEvent += (object sender, ButtonPressEventArgs args) =>
         {
            SelectCurrentEvent(sender);
         };

         if(w is Gtk.Container)
         {
            foreach(Widget xw in ((Gtk.Container) w))
            {
               AddSelectNotifier(item, xw);
            }
         }
#if false // TODO: could be needed
            if (w is TreeView)
            {
                foreach (TreeViewColumn twc in ((TreeView)w).Columns)
                {
                    twc.Clicked += (object sender, EventArgs e) =>
                    {
                        MessageWriteLine("{0} Test TreeViewColumn.Clicked {1}", qwe++, sender);
                    };
                }
            }
#endif
      }

      private void SelectCurrentEvent(object item)
      {
         DockItem select;
         if(mSelectRelation.TryGetValue(item, out select))
         {
            if(CurrentDockItem != select)
            {
               if(CurrentDockItem != null)
               {
                  CurrentDockItem.TitleTab.VisualStyle = mNormalStyle;
                  CurrentDockItem.Widget.VisualStyle = mNormalStyle;
                  CurrentDockItem.Widget.QueueDraw();
               }
               CurrentDockItem = select;
               CurrentDockItem.TitleTab.VisualStyle = mSelectedStyle;
               CurrentDockItem.Widget.VisualStyle = mSelectedStyle;
               CurrentDockItem.Widget.QueueDraw();

               MainWindowBase.MenuCut.Sensitive   = CurrentDockItem.Content is ICut;
               MainWindowBase.MenuCopy.Sensitive  = CurrentDockItem.Content is ICopy;
               MainWindowBase.MenuPaste.Sensitive = CurrentDockItem.Content is IPaste;

               // notify all IPropertyViewers
               if(!(CurrentDockItem.Content is IPropertyViewer))
               {
                  foreach(IPropertyViewer ip in mPropertyInterfaces)
                     ip.SetObject(null);
               }
               // notify all IScript Widgets
               if(!(CurrentDockItem.Content is IScript))
               {
                  foreach(IScript isc in mScriptInterfaces)
                     isc.SetScript(null, null);
               }

               // notify all other components that the current item changed
               foreach(DockItem other in DockFrame.GetItems())
               {
                  if(other != item && other.Content is Component)
                     (other.Content as Component).FocusChanged(CurrentDockItem.Content);
               }
            }
         }
      }

      #endregion
      #region IMessageWriteLine

      public void MessageWriteLine(String format, params object[] args)
      {
         LogWriter.MessageWriteLine(format, args);
      }

      #endregion

      #region ScrollTo globally

      // Set "percent" to zero if this always should happen.
      // If it is larger than zero, then the scroll only happens when the coordinate leaves that percentage range of the screen.
      // Returns true if such scroll has happened.
      public bool ScrollTo(object sender, double lat, double lon, int percent, bool triggerRepaint)
      {
         return mRegisteredScrollFuncs(sender, lat, lon, percent, triggerRepaint);
      }

      #endregion

      public string ReadResource(String id)
      {
         Assembly asm = Assembly.GetCallingAssembly();
         Stream s = asm.GetManifestResourceStream(id);
         if (s == null)
         {
            return null;
         }

         StreamReader reader = new StreamReader(s);
         if (reader == null)
         {
            return null;
         }

         return reader.ReadToEnd();
      }

      public Gdk.Pixbuf Screenshot()
      {
         return mScreenshotAction();
      }
   }
}

