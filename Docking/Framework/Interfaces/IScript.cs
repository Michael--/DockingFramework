using System;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting;

namespace Docking.Components
{
   public class ScriptChangedEventArgs : EventArgs
   {
      public ScriptChangedEventArgs(object reference, string script, CompiledCode code)
      {
         Reference = reference;
         Script = script;
         Code = code;
      }
      public object Reference { get; private set; }
      public string Script { get; private set; }
      public CompiledCode Code { get; private set; }
   }

   public delegate void ScriptChangedEventHandler(ScriptChangedEventArgs e);

   public interface IScript
   {
      /// <summary>
      /// Sets the current script to display and edit
      /// </summary>
      void SetScript(object reference, string script);

      /// <summary>
      /// Remove script if currently displayed by given reference
      /// </summary>
      void RemoveScript(object reference);

      /// <summary>
      /// Show a message in the script editor message window
      /// </summary>
      void SetMessage(object reference, string msg);

      /// <summary>
      /// Get an event on any property changes
      /// </summary>
      ScriptChangedEventHandler ScriptChanged { get; set; }
   }
}
