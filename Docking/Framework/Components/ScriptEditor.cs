using System;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting;

namespace Docking.Components
{
   [System.ComponentModel.ToolboxItem(false)]
   public partial class ScriptEditor : Gtk.Bin, IScript, IComponent, ILocalizableComponent
   {
      #region MAIN

      Mono.TextEditor.TextEditor textEditor;
      System.Timers.Timer m_TextChangedTimer;

      public ScriptEditor()
      {
         this.Build();
         textEditor = new Mono.TextEditor.TextEditor();
         scrolledwindow4.Child = textEditor;

         // use a timer to catch multiple LineChange events in very short time 
         m_TextChangedTimer = new System.Timers.Timer();
         m_TextChangedTimer.Elapsed += ContentChanged;
         m_TextChangedTimer.Interval = 50;
         m_TextChangedTimer.Enabled = false;

         // see also http://monodevelop.com/Developers/Articles/Language_Addins
         // to enable code completion, smart indent and such like this additional code is necessary
         textEditor.Document.MimeType = "text/x-python";
         textEditor.Options.ColorScheme = "Tango"; //  TODO: user could select one
         textEditor.Options.DrawIndentationMarkers = true;
         textEditor.Options.EnableSyntaxHighlighting = true;
         textEditor.Options.HighlightCaretLine = true;
         textEditor.Options.HighlightMatchingBracket = true;
         textEditor.Options.IndentStyle = Mono.TextEditor.IndentStyle.Auto;
         textEditor.Options.ShowFoldMargin = true;
         textEditor.Options.ShowRuler = true;
         textEditor.Sensitive = false; // will be enabled on request
         textEditor.Text = "# input disabled ...";

         textEditor.Document.LineChanged += (object sender, Mono.TextEditor.LineEventArgs e) =>
         {
            if (!m_TextChangedTimer.Enabled)
               m_TextChangedTimer.Enabled = true;
         };
      }

      void ContentChanged(object sender, System.Timers.ElapsedEventArgs e)
      {
         string script = textEditor.Text;
         m_TextChangedTimer.Enabled = false;

         CompiledCode code = null;
         try
         {
            code = ComponentManager.Compile(script);
            Message("");
         }
         catch (SyntaxErrorException ex)
         {
            Message(string.Format("Line {0}/{1}: {2}", ex.Line, ex.Column, ex.Message));
         }
         catch (Exception ex)
         {
            Message(ex.Message);
         }
         finally
         {
            if (ScriptChangedHandler != null)
               ScriptChangedHandler(new ScriptChangedEventArgs(m_Reference, script, code));
         }
      }

      void Message(string msg)
      {
         textMessage.Buffer.Clear();
         Gtk.TextIter iter = textMessage.Buffer.EndIter;
         textMessage.Buffer.Insert(ref iter, msg);
      }

      #endregion

      #region implement IScript

      void IScript.SetScript(object reference, string script)
      {
         if (m_Reference == reference)
            return;

         m_Reference = reference;
         if (script != null)
         {
            textEditor.Sensitive = true;
            textEditor.Text = script;
         }
         else
         {
            textEditor.Sensitive = false;
            textEditor.Text = "# input disabled ...";
         }
      }

      void IScript.RemoveScript(object reference)
      {
         if (m_Reference != reference)
            return;

         m_Reference = null;
         textEditor.Sensitive = false;
         textEditor.Text = "# input disabled ...";
      }

      void IScript.SetMessage(object reference, string msg)
      {
         if (m_Reference != reference)
            return;

         Message(msg);
      }

      /// <summary>
      /// Get an event on any property changes
      /// </summary>
      ScriptChangedEventHandler IScript.ScriptChanged
      {
         get { return ScriptChangedHandler; }
         set { ScriptChangedHandler = value; }
      }

      object m_Reference = null;
      private ScriptChangedEventHandler ScriptChangedHandler;

      #endregion

      #region implement IComponent
      public ComponentManager ComponentManager { get; set; }

      void IComponent.Loaded(DockItem item)
      {
         mPersistence = (ScriptPersistence)ComponentManager.LoadObject("ScriptEditor", typeof(ScriptPersistence));
         if (mPersistence == null)
            mPersistence = new ScriptPersistence();

         // set vpaned position delayed when really possible, TODO: find event called only once, better as ExposeEvent
         vpaned1.ExposeEvent += new Gtk.ExposeEventHandler(vpaned1_ExposeEvent);

         // Iterate over all loaded styles. TODO: user should select from available styles
         // foreach (string s in Mono.TextEditor.Highlighting.SyntaxModeService.Styles)
         //    ComponentManager.MessageWriteLine(s);
      }

      bool mSetPositionOnlyOnce = true;
      ScriptPersistence mPersistence;

      void vpaned1_ExposeEvent(object o, Gtk.ExposeEventArgs args)
      {
         if (mSetPositionOnlyOnce)
         {
            mSetPositionOnlyOnce = false;

            // first calculate an useful position
            if (mPersistence.VPanedPosition == 0)
            {
               int width, height;
               this.GdkWindow.GetSize(out width, out height);
               mPersistence.VPanedPosition = height - 50;
               if (mPersistence.VPanedPosition <= 0)
                  mPersistence.VPanedPosition = height;
            }

            vpaned1.Position = mPersistence.VPanedPosition;
         }
      }

      void IComponent.Save()
      {
         mPersistence.VPanedPosition = vpaned1.Position;
         ComponentManager.SaveObject("ScriptEditor", mPersistence);
      }
      #endregion

      #region implement  ILocalizable

      // set the displayed name of the widget
      string ILocalizableComponent.Name { get { return "Script Editor"; } }

      void ILocalizableComponent.LocalizationChanged(Docking.DockItem item)
      {
      }
      #endregion

   }

   [Serializable]
   public class ScriptPersistence
   {
      public int VPanedPosition { get { return m_VPaned_Position; } set { m_VPaned_Position = value; } }
      int m_VPaned_Position;
   }

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


   #region Starter / Entry Point

   public class ScriptEditorFactory : ComponentFactory
   {
      public override Type TypeOfInstance { get { return typeof(ScriptEditor); } }
      public override String MenuPath { get { return @"View\Infrastructure\Script Editor"; } }
      public override String Comment { get { return "Show selected script"; } }
      public override Mode Options { get { return Mode.CloseOnHide; } }
      public override Gdk.Pixbuf Icon { get { return Gdk.Pixbuf.LoadFromResource("Docking.Framework.Components.Messages-16.png"); } }
   }
   #endregion

}

