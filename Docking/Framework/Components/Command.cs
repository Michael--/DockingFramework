using System;
using System.Text;
using MonoDevelop.Components;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Docking.Components
{
   [System.ComponentModel.ToolboxItem(false)]
   public partial class Command : Component, IComponent, IComponentInteract, ILocalizableComponent
   {
      #region implement IComponent
      public ComponentManager ComponentManager { get; set; }

      void IComponent.Loaded(DockItem item)
      {
         mPersistence = (CommandPersistence)ComponentManager.LoadObject("Command", typeof(CommandPersistence), item);

         Task.Factory.StartNew(() =>
         {
            if (mPersistence == null)
               mPersistence = new CommandPersistence() { Script = "" }; // TODO: set a default script here

            try
            {
               ComponentManager.Execute(mPersistence.Script);
            }
            catch (Exception ex)
            {
               consoleview.WriteOutput("Error: " + ex.Message);
               consoleview.Prompt(true);
            }

            // redirect print message and access to this using "command"
            command = new _Command(this, consoleview, ComponentManager);
            ComponentManager.ScriptScope.SetVariable("cmd", command);
            ComponentManager.Execute(String.Join("\r\n", pyPrint));
         });
      }
      CommandPersistence mPersistence;

      void IComponent.Save()
      {
         ComponentManager.SaveObject("Command", mPersistence);
      }

      #endregion

      #region implement IComponentInteract

      List<IScript> m_ScriptInterface = new List<IScript>();
      void IComponentInteract.Added(object item)
      {
         if (item is IScript)
         {
            IScript script = item as IScript;
            m_ScriptInterface.Add(script);

            script.ScriptChanged += ScriptChanged;

         }
      }

      void IComponentInteract.Removed(object item)
      {
         if (item is IScript)
            m_ScriptInterface.Remove(item as IScript);
      }

      void IComponentInteract.Visible(object item, bool visible)
      {
      }

      void IComponentInteract.Current(object item)
      {
         if (this == item)
         {
            foreach (IScript it in m_ScriptInterface)
               it.SetScript(this, mPersistence.Script);
         }
      }

      #endregion

      #region Python command extensions e.g. print
      string[] pyPrint = new string[] 
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
         consoleview.ConsoleInput += HandleConsoleInput;
      }

      void ScriptChanged(ScriptChangedEventArgs e)
      {
         if (e.Reference == this)
         {
            mPersistence.Script = e.Script;
            try
            {
               if (e.Code != null)
                  ComponentManager.Execute(e.Code);
            }
            catch (Exception ex)
            {
               foreach (IScript i in m_ScriptInterface)
                  i.SetMessage(this, ex.Message);
            }
         }
      }

      void HandleConsoleInput(object sender, MonoDevelop.Components.ConsoleInputEventArgs e)
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

      #region ILocalizable

      string ILocalizableComponent.Name { get { return "Command"; } }

      void ILocalizableComponent.LocalizationChanged(Docking.DockItem item)
      {}
      #endregion

   }

   [Serializable]
   public class CommandPersistence
   {
      public string Script { get; set; }
   }



   #region Starter / Entry Point

   public class CommandFactory : ComponentFactory
   {
      public override Type TypeOfInstance { get { return typeof(Command); } }
      public override String MenuPath { get { return @"View\Infrastructure\Command"; } }
      public override String Comment { get { return "Command line"; } }
      public override Gdk.Pixbuf Icon { get { return Gdk.Pixbuf.LoadFromResource("Docking.Framework.Resources.Messages-16.png"); } }
   }

   #endregion

}

