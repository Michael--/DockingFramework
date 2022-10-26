
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

namespace Docking.Components
{
   public class PythonScriptEngine
   {
      private          ComponentManagerScripting m_ScriptingInstance;
      private          ScriptEngine              mScriptEngine;
      private          ScriptScope               mScriptScope;
      private readonly ComponentManager          mManager;

      /// <summary>
      /// Initializes a new instance
      /// </summary>
      internal PythonScriptEngine(ComponentManager manager)
      {
         mManager = manager;
      }

      public void Initialize(string pythonBaseVariableName)
      {
         mScriptEngine = Python.CreateEngine();
         mScriptScope  = mScriptEngine.CreateScope();

         // override import
         //mScriptScope scope = IronPython.Hosting.Python.GetBuiltinModule(mScriptEngine);
         //scope.SetVariable("__import__", new ImportDelegate(DoPythonModuleImport));

         // access to this using "ComponentManager"
         m_ScriptingInstance = new ComponentManagerScripting(mManager);
         mScriptScope.SetVariable(pythonBaseVariableName, m_ScriptingInstance);

         try
         {
            // add Python commands like "message(...)"
            Execute(mManager.ReadResource("cm.py").Replace("[INSTANCE]", pythonBaseVariableName));
         }
         catch(Exception e)
         {
            mManager.MessageWriteLine("Error in cm.py:\n" + e.ToString());
         }
      }

      public CompiledCode Compile(String code)
      {
         if (mScriptEngine == null)
         {
            return null;
         }

         ScriptSource source = mScriptEngine.CreateScriptSourceFromString(code, SourceCodeKind.AutoDetect);
         return source.Compile();
      }

      public dynamic Execute(CompiledCode compiled, List<KeyValuePair<string, object>> global_variables = null)
      {
         setGlobalVariables(global_variables);
         return compiled.Execute(mScriptScope);
      }

      public dynamic Execute(String code, List<KeyValuePair<string, object>> global_variables = null)
      {
         CompiledCode compiled = Compile(code);
         if (compiled == null)
         {
            return null;
         }

         setGlobalVariables(global_variables);
         try
         {
            return compiled.Execute(mScriptScope);
         }
         catch
         {
            return null;
         }
      }

      public dynamic ExecuteFile(String filename, List<KeyValuePair<string, object>> global_variables = null)
      {
         string code = File.ReadAllText(filename, Encoding.UTF8);
         return Execute(code, global_variables);
      }

      public object DoPythonModuleImport(CodeContext context, string moduleName, PythonDictionary globals, PythonDictionary locals, PythonTuple tuple)
      {
         // test, may useful to import py from embedded resource
#if false
            string py = ReadResource(moduleName);
            if (py != null)
            {
                //var scope = Execute(py);
                //ScriptSource source = mScriptEngine.CreateScriptSourceFromString(py);
                //mScriptScope scope = mScriptEngine.CreateScope();
                var scope = mScriptScope;
                mScriptEngine.Execute(py, scope);
                Microsoft.Scripting.Runtime.Scope ret = Microsoft.Scripting.Hosting.Providers.HostingHelpers.GetScope(scope);
                mScriptScope.SetVariable(moduleName, ret);
                return ret;
            }
            else
            {   // fall back on the built-in method
                return IronPython.Modules.Builtin.__import__(context, moduleName);
            }
#else
         return IronPython.Modules.Builtin.__import__(context, moduleName);
#endif
      }

      private delegate object ImportDelegate(CodeContext context, string moduleName, PythonDictionary globals, PythonDictionary locals, PythonTuple tuple);

      private void setGlobalVariables(List<KeyValuePair<string, object>> global_variables)
      {
         if (global_variables != null && mScriptScope != null)
         {
            global_variables.ForEach(v => mScriptScope.SetVariable(v.Key, v.Value));
         }
      }
   }

   /// <summary>
   /// Adapter class encapsulate access to Docking.Components.ComponentManager
   /// </summary>
   internal class ComponentManagerScripting
   {
      public ComponentManagerScripting(ComponentManager cm)
      {
         ComponentManager = cm;
      }

      private ComponentManager ComponentManager { get; set; }

      /// <summary>
      /// set the visibility of the main window
      /// </summary>
      public bool Visible
      {
         get { return ComponentManager.Visible; }
         set { ComponentManager.Visible = value; }
      }

      /// <summary>
      /// exit application immediately
      /// </summary>
      public void Quit()
      {
         ComponentManager.Quit(true);
      }

      /// <summary>
      /// Write a message to the message window (if exist)
      /// </summary>
      public void MessageWriteLine(String message)
      {
         ComponentManager.MessageWriteLine(message);
      }

      /// <summary>
      /// Opens the file.
      /// </summary>
      public bool OpenFile(string filename, bool syncronous = false)
      {
         return ComponentManager.OpenFile(filename, syncronous);
      }

      /// <summary>
      /// Opens the file dialog.
      /// </summary>
      public String OpenFileDialog(string prompt)
      {
         return ComponentManager.DialogProvider.OpenFileDialog(prompt);
      }

      /// lists all available component types which you can instantiate using CreateComponent()
      public List<string> ListComponentTypes()
      {
         List<string> result = new List<string>();
         foreach (ComponentFactoryInformation info in ComponentManager.ComponentFinder.ComponentInfos)
         {
            result.Add(info.ComponentType.ToString());
         }

         return result;
      }

      /// Creates a new component instance. The given parameter must be one of the available types returned by ListAvailableComponentTypes().
      /// Returned is the unique instance identification string.
      public string CreateComponent(string s)
      {
         foreach (ComponentFactoryInformation info in ComponentManager.ComponentFinder.ComponentInfos)
         {
            if (info.ComponentType.ToString() == s)
            {
               DockItem item = ComponentManager.CreateComponent(info, true);
               return GetComponentIdentifier(item);
            }
         }

         return null;
      }

      /// <summary>
      /// Returns a list of all currently instantiated components (including hidden ones).
      /// </summary>
      public List<string> ListInstances()
      {
         List<string> result = new List<string>();
         foreach (DockItem item in ComponentManager.DockFrame.GetItems())
         {
            if (item.Content == null)
            {
               continue;
            }

            Component component = item.Content as Component;
            if (component == null || component.GetScriptingInstance() == null)
            {
               continue;
            }

            string cid = GetComponentIdentifier(item);
            if (cid != null)
            {
               result.Add(cid);
            }
         }

         return result;
      }

      /// <summary>
      /// Returns a specific component instance, identified by its brief, unique instance identifier string.
      /// </summary>
      public object GetInstance(string identifier)
      {
         foreach (DockItem item in ComponentManager.DockFrame.GetItems())
         {
            if (item.Content == null)
            {
               continue;
            }

            Component component = item.Content as Component;
            if (component == null || component.GetScriptingInstance() == null)
            {
               continue;
            }

            string cid = GetComponentIdentifier(item);
            if (cid != null && cid == identifier)
            {
               return component.GetScriptingInstance();
            }
         }

         return null;
      }

      /// <summary>
      /// Get an array with the names of all available python scripting objects
      /// </summary>
      /// <returns></returns>
      public string[] GetInstances()
      {
         List<string> result = new List<string>();
         foreach (DockItem item in ComponentManager.DockFrame.GetItems())
         {
            if (item.Content == null)
            {
               continue;
            }

            Component component = item.Content as Component;
            if (component.GetScriptingInstance() == null)
            {
               continue;
            }

            string cid = GetComponentIdentifier(item);
            if (cid != null)
            {
               result.Add(cid);
            }
         }

         return result.ToArray();
      }

      /// <summary>
      /// Gets a simple component instance identification string.
      /// This normally is identical to the component window title.
      /// For non-multi-instance components, this normally is a human-readable text like "Map Explorer".
      /// For multi-instance components, this at the end has a number, for example "Map Explorer 2".
      /// </summary>
      private string GetComponentIdentifier(DockItem item)
      {
         if (item == null)
         {
            return null;
         }

         Component comp = item.Content as Component;
         if (comp == null)
         {
            return null;
         }

         string name = comp is ILocalizableComponent
                          ? (comp as ILocalizableComponent).Name
                          : item.Content.Name;

         return item.InstanceIndex > 1
                   ? (name + " " + item.InstanceIndex)
                   : name;
      }
   }
}
