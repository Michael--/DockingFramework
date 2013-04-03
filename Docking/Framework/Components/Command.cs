using System;
using System.Text;
using MonoDevelop.Components;
using System.Threading;

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
            command = new _Command(this, consoleview, ComponentManager);
            ComponentManager.ScriptScope.SetVariable("cmd", command);
            ComponentManager.Execute(String.Join("\r\n", pyPrint));
        }

        void IComponent.Save()
        {
        }
        
        #endregion

        #region Python command extensions e.g. print
        string []pyPrint = new string[] 
        { 
            "#output can be redirected to any object which implement method write and property softspace",
            "import sys",
            "sys.stderr=cmd",
            "sys.stdout=cmd"
        };

        _Command command;
        public bool DisableInvoke { get; set; }

        // encapsulate python access to c#, reduce access to well known methods
        public class _Command
        {
            public _Command(Command cmd, ConsoleView cv, ComponentManager cm)
            {
                Command = cmd;
                ConsoleView = cv;
                ComponentManager = cm;
            }

            private Command Command { get; set; }
            private ConsoleView ConsoleView { get; set; }
            private ComponentManager ComponentManager { get; set; }

            public void write(string s)
            {
                if (Command.DisableInvoke)
                {
                    ConsoleView.WriteOutput(s);
                }
                else
                {
                    Gtk.Application.Invoke(delegate
                    {
                        ConsoleView.WriteOutput(s);
                    });
                }
            }
            
            public int softspace { get; set; }
            
            /// <summary>
            /// exit application
            /// </summary>
            public void quit()
            {
                ComponentManager.quit();
            }
        }

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
                    DisableInvoke = true;
                    ComponentManager.Execute(input);
                    DisableInvoke = true;
                    consoleview.Prompt(false);
                }
                catch (Exception ex)
                {
                    consoleview.WriteOutput("Error: " + ex.Message);
                    consoleview.Prompt(true);
                }
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

