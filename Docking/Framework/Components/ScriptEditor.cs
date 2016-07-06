using System;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting;
using System.Threading.Tasks;

namespace Docking.Components
{
   [System.ComponentModel.ToolboxItem(false)]
   public partial class ScriptEditor : Component, IScript, ILocalizableComponent, IPersistable
   {
      #region MAIN

      public ScriptEditor()
      {
         this.Build();
         m_TextEditor = new Mono.TextEditor.TextEditor();
         scrolledwindow4.Child = m_TextEditor;

         // see also http://monodevelop.com/Developers/Articles/Language_Addins
         // to enable code completion, smart indent and such like this additional code is necessary
         m_TextEditor.Document.MimeType = "text/x-python";
         m_TextEditor.Options.ColorScheme = "Tango"; //  TODO: user could select one
         m_TextEditor.Options.DrawIndentationMarkers = true;
         m_TextEditor.Options.EnableSyntaxHighlighting = true;
         m_TextEditor.Options.HighlightCaretLine = true;
         m_TextEditor.Options.HighlightMatchingBracket = true;
         m_TextEditor.Options.IndentStyle = Mono.TextEditor.IndentStyle.Auto;
         m_TextEditor.Options.ShowFoldMargin = true;
         m_TextEditor.Options.ShowRuler = true;
         m_TextEditor.Sensitive = false; // will be enabled on request
         m_TextEditor.Text = "# input disabled ...";

         m_TextEditor.Document.LineChanged += (object sender, Mono.TextEditor.LineEventArgs e) =>
         {
            // CompileScript(); // NOTE: a lot of compile calls occured on any text change, not really good
         };

         buttonOK.Clicked += (o, e) =>
         {
            CompileScript();
         };

         vpaned1.ExposeEvent += (o, e) =>
         {
            if (mSetPositionOnlyOnce)
            {
               mSetPositionOnlyOnce = false;

               // first calculate an useful position
               if (VPanedPosition == 0)
               {
                  int width, height;
                  this.GdkWindow.GetSize(out width, out height);
                  VPanedPosition = height - 50;
                  if (VPanedPosition <= 0)
                     VPanedPosition = height;
               }

               vpaned1.Position = VPanedPosition;
            }
         };
      }

      Mono.TextEditor.TextEditor m_TextEditor;
      Task m_CompileTask = null;
      bool m_PendingRequest = false;

      void CompileScript()
      {
         // try start compiling current edited script
         if (m_CompileTask == null || m_CompileTask.IsCompleted)
         {
            string script = m_TextEditor.Text;
            m_CompileTask = Task.Factory.StartNew(() =>
            {
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

               // if any last requested frame exist, start loading of this frame at least
               if (m_PendingRequest)
               {
                  m_PendingRequest = false;
                  CompileScript();
               }
            });
         }
         // because compiling is in progress, save last request
         else
         {
            m_PendingRequest = true;
         }
      }

      void Message(string msg)
      {
         textMessage.Buffer.Clear();
         Gtk.TextIter iter = textMessage.Buffer.EndIter;
         textMessage.Buffer.Insert(ref iter, msg);
      }

      #endregion

      #region IScript

      void IScript.SetScript(object reference, string script)
      {
         if (m_Reference == reference)
            return;

         m_Reference = reference;
         if (script != null)
         {
            m_TextEditor.Sensitive = true;
            m_TextEditor.Text = script;
         }
         else
         {
            m_TextEditor.Sensitive = false;
            m_TextEditor.Text = "# input disabled ...";
         }
      }

      void IScript.RemoveScript(object reference)
      {
         if (m_Reference != reference)
            return;

         m_Reference = null;
         m_TextEditor.Sensitive = false;
         m_TextEditor.Text = "# input disabled ...";
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

      public override void Loaded()
      {
         base.Loaded();

         // set vpaned position delayed when really possible, TODO: find event called only once, better as ExposeEvent
         vpaned1.Position = VPanedPosition;

         // Iterate over all loaded styles. TODO: user should select from available styles
         // foreach (string s in Mono.TextEditor.Highlighting.SyntaxModeService.Styles)
         //    ComponentManager.MessageWriteLine(s);
      }

      bool mSetPositionOnlyOnce = true;
      public int VPanedPosition { get; set; }

      public override bool IsCloseOK()
      {
         // TODO if the current script is unsaved, prompt the user here to save it and offer him a "Cancel" button.
         // When the user presses "Cancel", return false from this function to prevent the closing from happening.
         return true;
      }

      #region IPersistable

      void IPersistable.SaveTo(IPersistency persistency)
      {
         string instance = DockItem.Id.ToString();
         VPanedPosition = vpaned1.Position;
         persistency.SaveSetting(instance, "VPanedPosition", VPanedPosition);
      }

      void IPersistable.LoadFrom(IPersistency persistency)
      {
         string instance = DockItem.Id.ToString();
         VPanedPosition = persistency.LoadSetting(instance, "VPanedPosition", VPanedPosition);
      }

      #endregion


      #region ILocalizableComponent

      string ILocalizableComponent.Name { get { return "Script Editor"; } }

      void ILocalizableComponent.LocalizationChanged(Docking.DockItem item)
      {}
      #endregion

   }

   #region Starter / Entry Point

   public class ScriptEditorFactory : ComponentFactory
   {
      public override Type TypeOfInstance { get { return typeof(ScriptEditor); } }
      public override String Name { get { return "Script Editor"; } }
      public override String MenuPath { get { return @"View\Infrastructure\Script Editor"; } }
      public override String Comment { get { return "Show selected script"; } }
      public override Gdk.Pixbuf Icon { get { return Docking.Tools.ResourceLoader_Docking.LoadPixbuf("Messages-16.png"); } }
   }
   #endregion

}
