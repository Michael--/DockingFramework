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
            ComponentManager.ScriptScope.SetVariable("cmd", this);
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

        public void write(string s)
        {
            consoleview.WriteOutput(s);
        }
        
        public int softspace { get; set; }

        /// <summary>
        /// exit application
        /// </summary>
        public void quit()
        {
            ComponentManager.quit();
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
                    ComponentManager.Execute(input);
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

