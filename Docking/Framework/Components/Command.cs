using System;

namespace Docking.Components
{
    [System.ComponentModel.ToolboxItem(false)]
    public partial class Command : Gtk.Bin, IComponent
    {
        #region implement IComponent
        public ComponentManager ComponentManager { get; set; }
        
        void IComponent.Loaded(DockItem item)
        {
        }

        void IComponent.Save()
        {
        }
        
        #endregion

        #region MAIN
        public Command()
        {
            this.Build();
            consoleview.ConsoleInput += HandleConsoleInput;
        }

        void HandleConsoleInput (object sender, MonoDevelop.Components.ConsoleInputEventArgs e)
        {
            string input = e.Text;
            if (input != null)
            {
                //consoleview.WriteOutput("Echo: " + input);
                //consoleview.Prompt(true);
                consoleview.Prompt(false);
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

