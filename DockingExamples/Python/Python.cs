using System;
using Docking.Components;
using IronPython.Hosting;
using System.IO;
using System.Reflection;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Docking;


namespace Examples
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class PythonExample : Gtk.Bin, IComponent
    {
        #region implement IComponent
        public ComponentManager ComponentManager { get; set; }
        
        void IComponent.Loaded(DockItem item)
        {
            StartTests();
        }
        
        void IComponent.Save()
        {
        }
        
        #endregion

        public PythonExample()
        {
            this.Build();
            this.Name = "Python";
        }

        ScriptEngine pyEngine;
        ScriptScope pyScope;


        void CompileSourceAndExecute(String code)
        {
            try
            {
                ScriptSource source = pyEngine.CreateScriptSourceFromString(code, SourceCodeKind.AutoDetect);
                CompiledCode compiled = source.Compile();
                compiled.Execute(pyScope);
            }
            catch (Exception ex)
            {
                String []split = ex.ToString().Split(new char[]{ '\n' });
                foreach(string s in split)
                    ComponentManager.MessageWriteLine(s);
            }
        }


        void StartTests()
        {
            pyEngine = Python.CreateEngine();
            pyScope = pyEngine.CreateScope();
            pyScope.SetVariable("ComponentManager", ComponentManager);
            Test1();
            Test2();
        }

        void Test1()
        {
            CompileSourceAndExecute(ReadResource("Examples.Python.test1.py"));
        }

        void Test2()
        {
            CompileSourceAndExecute(ReadResource("Examples.Python.test2.py"));
        }

        String ReadResource(String id)
        {
            Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
            using (System.IO.Stream s = asm.GetManifestResourceStream(id))
            {
                if (s == null)
                    return null;
                using (System.IO.StreamReader reader = new System.IO.StreamReader(s))
                {
                    if (reader == null)
                        return null;
                    string result = reader.ReadToEnd();
                    return result;
                }
            }        
        }
    }

    #region Starter / Entry Point
    
    public class ExamplePythonFactory : ComponentFactory
    {
        public override Type TypeOfInstance { get { return typeof(PythonExample); } }
        public override String MenuPath { get { return @"View\Examples\Python"; } }
        public override String Comment { get { return "Python scriping example"; } }
        public override Mode Options { get { return Mode.CloseOnHide; } }
        public override Gdk.Pixbuf Icon { get { return Gdk.Pixbuf.LoadFromResource ("Examples.HelloWorld-16.png"); } }
    }
    
    #endregion

}

