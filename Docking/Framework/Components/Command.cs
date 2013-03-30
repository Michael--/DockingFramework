using System;
using System.Text;

namespace Docking.Components
{
    [System.ComponentModel.ToolboxItem(false)]
    public partial class Command : Gtk.Bin, IComponent
    {
        #region implement IComponent
        public ComponentManager ComponentManager { get; set; }
        
        void IComponent.Loaded(DockItem item)
        {
            // redirect print message and access to this using "command"
            ComponentManager.ScriptScope.SetVariable("command", this);
            ComponentManager.PythonExecute(String.Join("\r\n", pyPrint));
        }

        void IComponent.Save()
        {
        }
        
        #endregion

        #region Python print
        string []pyPrint = new string[] 
        { 
            "#output can be redirected to any object which implement method write and property softspace",
            "import sys",
            "sys.stderr=command",
            "sys.stdout=command"
        };

        StringBuilder mPrintBuilder = new StringBuilder();
        bool WritePromt { get; set; }
        public void write(string s)
        {
            if (s == "\n")
            {
                if (mPrintBuilder.Length > 0)
                {
                    consoleview.WriteOutput(mPrintBuilder.ToString());
                    consoleview.Prompt(true);
                    WritePromt = true;
                }
                mPrintBuilder.Clear();
            }
            else
            {
                mPrintBuilder.Append(s);
            }
        }
        
        public int softspace { get; set; }
        #endregion

        #region MAIN
        public Command()
        {
            this.Build();
            this.Name = "Command";
            consoleview.ConsoleInput += HandleConsoleInput;
        }

        void HandleConsoleInput (object sender, MonoDevelop.Components.ConsoleInputEventArgs e)
        {
            string input = e.Text;
            if (input != null)
            {
                try
                {
                    WritePromt = false;
                    ComponentManager.PythonExecute(input);
                    if (!WritePromt)
                        consoleview.Prompt(false);
                }
                catch (Exception ex)
                {
                    consoleview.WriteOutput("Error: " + ex.Message);
                    consoleview.Prompt(true);
                }
                finally
                {
                }

                //consoleview.WriteOutput("Echo: " + input);
                //consoleview.Prompt(true);
                //consoleview.Prompt(false);
            }
            else
            {
                consoleview.Prompt(false);
            }
        }
        #endregion
    }

    #region Starter / Entry Point
    
    public class CommandFactory : ComponentFactory
    {
        public override Type TypeOfInstance { get { return typeof(Command); } }
        public override String MenuPath { get { return @"View\Infrastructure\Command"; } }
        public override String Comment { get { return "Command line"; } }
        public override Gdk.Pixbuf Icon { get { return Gdk.Pixbuf.LoadFromResource ("Docking.Framework.Components.Messages-16.png"); } }
    }
    
    #endregion

}

