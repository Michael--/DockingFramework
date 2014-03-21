using System;
using System.Text;
using MonoDevelop.Components;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Docking.Components
{
   [System.ComponentModel.ToolboxItem(false)]
   public partial class Command : Component, ILocalizableComponent
   {
      public override void Loaded(DockItem item)
      {
         base.Loaded(item);

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
            m_ScriptingInstance = new CommandScript(this, consoleview, ComponentManager);
            ComponentManager.Execute(String.Join("\r\n", pyPrint));
         });
      }
      CommandPersistence mPersistence;

      public override void Save()
      {
         base.Save();

         ComponentManager.SaveObject("Command", mPersistence);
      }

      #region Component - Interaction


      List<IScript> m_ScriptInterface = new List<IScript>();
      public override void ComponentAdded(object item)
      {
         if (item is IScript)
         {
            IScript script = item as IScript;
            m_ScriptInterface.Add(script);

            script.ScriptChanged += ScriptChanged;

         }
      }

      public override void ComponentRemoved(object item)
      {
         if (item is IScript)
            m_ScriptInterface.Remove(item as IScript);
      }

      public override void FocusChanged(object item)
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
            "cmd=app().GetInstance(\"Command\")",
            "sys.stderr=cmd",
            "sys.stdout=cmd"
        };

      CommandScript m_ScriptingInstance;

      public override object GetScriptingInstance()
      {
         return m_ScriptingInstance;
      }

      public bool DisableInvoke { get; set; }

      // encapsulate python access to c#, reduce access to well known methods
      public class CommandScript
      {
         public CommandScript(Command cmd, ConsoleView cv, ComponentManager cm)
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
            ComponentManager.Quit(true);
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
         if(input==null)
         {
            consoleview.Prompt(true);
            return;
         }

         string inputL = input.Trim().ToLowerInvariant();
         if(inputL=="help"   || 
            inputL=="help()" ||
            inputL.StartsWith("?")) 
         {
            // treat this input specially to help the user. we do not pass this line to the python interpreter.

            consoleview.WriteOutput("Congratulations, you have found the help function :)"                   + "\n" +
                                    "To get more help, use the help() function, which takes 1 parameter."    + "\n" + 
                                    "It will show help for that, including for example all its methods etc." + "\n" + 
                                    "To get a list of all available such parameters, you can use"            + "\n" +
                                    "   print dir()"                                                      
                                   );
            consoleview.Prompt(true);
            return;
         }

         bool ok = true;

         DisableInvoke = true;
         try { ComponentManager.Execute(input); }
         catch(Exception ex)
         {
            consoleview.WriteOutput("Error: " + ex.Message);               
            ok = false;
         }
         DisableInvoke = false;

         if(ok)
            consoleview.Prompt(false);
         else
            consoleview.Prompt(true);
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

