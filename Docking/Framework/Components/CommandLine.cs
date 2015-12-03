using System;
using System.Text;
using MonoDevelop.Components;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Docking.Components
{
   [System.ComponentModel.ToolboxItem(false)]
   public partial class CommandLine : Component, ILocalizableComponent
   {
      public override void Loaded(DockItem item)
      {
         base.Loaded(item);

         mPersistence = (CommandLinePersistence)ComponentManager.LoadObject("CommandLine", typeof(CommandLinePersistence), item);

         Task.Factory.StartNew(() =>
         {
            // must not rename the thread in a thread pool: System.Threading.Thread.CurrentThread.Name = "CommandLine.Loaded";
            if (mPersistence == null)
               mPersistence = new CommandLinePersistence() { Script = "" }; // TODO: set a default script here

            try
            {
               ComponentManager.Execute(mPersistence.Script);
            }
            catch (Exception ex)
            {
               consoleview.WriteOutput("Error: " + ex.Message);
               consoleview.Prompt(true);
            }

            m_ScriptingInstance = new CommandScript(this, consoleview, ComponentManager);

            // output can be redirected to any object which implements method write and property softspace
            string[] phython_script_for_redirecting_stdout_and_stderr = new string[] 
            { 
               "import sys",
               "cmd=app().GetInstance(\""+(this as ILocalizableComponent).Name+"\")",
               "sys.stderr=cmd",
               "sys.stdout=cmd"
            };

            ComponentManager.Execute(String.Join("\r\n", phython_script_for_redirecting_stdout_and_stderr));
         });
      }

      CommandLinePersistence mPersistence; // TODO early prototype - abolish, implement IPersistable instead!

      public override void Save()
      {
         base.Save();

         ComponentManager.SaveObject("CommandLine", mPersistence, DockItem);
      }

      #region Component - Interaction


      List<IScript> m_ScriptInterface = new List<IScript>();
      public override void ComponentAdded(object item)
      {
         base.ComponentAdded(item);
         if (item is IScript)
         {
            IScript script = item as IScript;
            m_ScriptInterface.Add(script);

            script.ScriptChanged += ScriptChanged;

         }
      }

      public override void ComponentRemoved(object item)
      {
         base.ComponentRemoved(item);
         if (item is IScript)
            m_ScriptInterface.Remove(item as IScript);
      }

      public override void FocusChanged(object item)
      {
         base.FocusChanged(item);
         if (this == item)
         {
            foreach (IScript it in m_ScriptInterface)
               it.SetScript(this, mPersistence.Script);
         }
      }

      #endregion

      #region Python command extensions e.g. print

      CommandScript m_ScriptingInstance;

      public override object GetScriptingInstance()
      {
         return m_ScriptingInstance;
      }

      public bool DisableInvokeOnAccessingConsoleView { get; set; }

      // encapsulate python access to c#, reduce access to well known methods
      public class CommandScript
      {
         private CommandLine      CommandLine      { get; set; }
         private ConsoleView      ConsoleView      { get; set; }
         private ComponentManager ComponentManager { get; set; }

         public CommandScript(CommandLine cmdline, ConsoleView cv, ComponentManager cm)
         {
            CommandLine = cmdline;
            ConsoleView = cv;
            ComponentManager = cm;
         }

         public void write(string s)
         {
            if (CommandLine.DisableInvokeOnAccessingConsoleView)
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

         public void quit()
         {
            Quit();
         }

         public void Quit()
         {
            ComponentManager.Quit(true);
         }

      }

      #endregion

      #region MAIN
      public CommandLine()
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

            consoleview.WriteOutput("Congratulations, you have found the help function :)"                             + "\n" +
                                    "This window is an interactive Python command line. You can input Python commands" + "\n" + 
                                    "and will see their outputs directly." + "\n" + 
                                    "To get more help, use the help() function, taking 1 parameter."                   + "\n" + 
                                    "It will show help for that parameter, including for example all its methods etc." + "\n" + 
                                    "To get a list of all available such parameters, you can use"                      + "\n" +
                                    "   print dir()"                                                                   + "\n" +
                                    "That command returns a list of all available global objects. The most interesting one in there is" + "\n" +
                                    "app(), which is the invocation of a getter for the main application object." + "\n" + 
                                    "To see which methods that object offers, run" + "\n" +
                                    "   help(app())"+ "\n" +
                                    "Especially, this object has 4 interesting methods:" + "\n" +
                                    "   print app().ListInstances()"+ "\n" +
                                    "prints a list of all currently instantiated components." + "\n" +
                                    "Each of them can be accessed by" + "\n" +
                                    "   app().GetInstance(\"xyz\")" + "\n" +
                                    ", for example" + "\n" +
                                    "   app().GetInstance(\"Map Explorer 2\")" + "\n" +
                                    "You can also instantiate new components: To get a list of the available ones, run" + "\n" +
                                    "   print app().ListComponentTypes()" + "\n" +
                                    "Creation then works like this:" + "\n" +
                                    "   print app().CreateComponent(\"xyz\")"
                                   );
            consoleview.Prompt(true);
            return;
         }

         bool ok = true;

         DisableInvokeOnAccessingConsoleView = true;
         try { ComponentManager.Execute(input); }
         catch(Exception ex)
         {
            consoleview.WriteOutput("Error: " + ex.Message);               
            ok = false;
         }
         DisableInvokeOnAccessingConsoleView = false;

         if(ok)
            consoleview.Prompt(false);
         else
            consoleview.Prompt(true);
      }
      #endregion

      #region ILocalizableComponent

      string ILocalizableComponent.Name { get { return "Command Line"; } }

      void ILocalizableComponent.LocalizationChanged(Docking.DockItem item)
      {}
      #endregion

   }

   [Serializable]
   public class CommandLinePersistence
   {
      public string Script { get; set; }
   }



   #region Starter / Entry Point

   public class CommandFactory : ComponentFactory
   {
      public override Type TypeOfInstance { get { return typeof(CommandLine); } }
      public override String Name { get { return "Command Line"; } }
      public override String MenuPath { get { return @"View\Infrastructure\Command Line"; } }
      public override String Comment { get { return "interactive input of python commands"; } }
      public override Gdk.Pixbuf Icon { get { return Docking.Tools.ResourceLoader_Docking.LoadPixbuf("Messages-16.png"); } }
   }

   #endregion

}

