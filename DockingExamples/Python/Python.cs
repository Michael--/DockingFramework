using System;
using Docking.Components;
using IronPython.Hosting;
using System.IO;
using System.Reflection;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Docking;
using Gtk;
using System.Text;


namespace Examples
{
    [System.ComponentModel.ToolboxItem(false)]
    public partial class PythonExample : Gtk.Bin, IComponent
    {
        #region implement IComponent
        public ComponentManager ComponentManager { get; set; }
        
        void IComponent.Loaded(DockItem item)
        {
            InitTests();
        }
        
        void IComponent.Save()
        {
        }
        
        #endregion

        #region MAIN

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

        void InitTests()
        {
            combo.Changed += (object sender, EventArgs e) => 
            {
                ComboBox c = sender as ComboBox;
                if (c == null)
                    return;
                
                TreeIter iter;
                if (c.GetActiveIter (out iter))
                {
                    String s = (string) c.Model.GetValue (iter, 0);
                    String py = ComponentManager.ReadResource("Examples.Python." + s);
                    textview.Buffer.Clear ();
                    if (py != null)
                        textview.Buffer.InsertAtCursor(py);
                    InitPythonEngine(); // Init again to avoid side effects from other scripts 
                }
            };

            buttonExecute.Clicked += (sender, e) => 
            {
                CompileSourceAndExecute(textview.Buffer.Text);
            };

            combo.AppendText("test1.py");
            combo.AppendText("test2.py");
            combo.AppendText("test3.py");
            combo.AppendText("test4.py");
            combo.AppendText("test5.py");
            combo.AppendText("test6.py");
            TreeIter it;
            combo.Model.GetIterFirst(out it);
            combo.SetActiveIter(it);
        }

        void InitPythonEngine()
        {
            pyEngine = Python.CreateEngine();
            pyScope = pyEngine.CreateScope();
            pyScope.SetVariable("cm", ComponentManager);
            pyScope.SetVariable("cmd", this);
        }

        StringBuilder mPrintBuilder = new StringBuilder();
        public void write(string s)
        {
            if (s == "\n")
            {
                ComponentManager.MessageWriteLine(mPrintBuilder.ToString());
                mPrintBuilder.Clear();
            }
            else
            {
                mPrintBuilder.Append(s);
            }
        }

        public int softspace { get; set; }

        #endregion
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

