using System;
using Gtk;
using Docking.Tools;


namespace Docking.Components
{
   public class MessageBox
   {
      public static ComponentManager ComponentManager { get; private set; }

      private static Gdk.Pixbuf PIXBUF_INFO;
      private static Gdk.Pixbuf PIXBUF_WARNING;
      private static Gdk.Pixbuf PIXBUF_QUESTION;
      private static Gdk.Pixbuf PIXBUF_ERROR;

      public static void Init(ComponentManager cm)
      {
         ComponentManager = cm;

         if(Platform.IsWindows)
         {
            PIXBUF_INFO     = SystemDrawing_vs_GTK_Conversion.Bitmap2Pixbuf(System.Drawing.SystemIcons.Information);
            PIXBUF_WARNING  = SystemDrawing_vs_GTK_Conversion.Bitmap2Pixbuf(System.Drawing.SystemIcons.Warning);
            PIXBUF_QUESTION = SystemDrawing_vs_GTK_Conversion.Bitmap2Pixbuf(System.Drawing.SystemIcons.Question);
            PIXBUF_ERROR    = SystemDrawing_vs_GTK_Conversion.Bitmap2Pixbuf(System.Drawing.SystemIcons.Error);
         }
      }

      public static ResponseType Show(Window parent, MessageType msgtype, ButtonsType buttontype, string format, params object[] args)
      {
         if(ComponentManager!=null && !ComponentManager.IsMainThread)
            throw new Exception("message boxes may only be popped up from the main GUI thread, i.e., they need a surrounding Gtk.Application.Invoke(delegate {}); block");

         if(ComponentManager!=null)
            ComponentManager.MessageWriteLine(format, args);

         #region treat batch mode case
         if(ComponentManager!=null && ComponentManager.OperateInBatchMode && ComponentManager.Visible==false)
         {
            switch(buttontype)
            {
            case ButtonsType.None:
               ComponentManager.MessageWriteLine("[NONE]"); 
               return ResponseType.None;

            case ButtonsType.Ok:
               ComponentManager.MessageWriteLine("[OK]");
               return ResponseType.Ok;

            case ButtonsType.Close:
               ComponentManager.MessageWriteLine("[CLOSE]"); 
               return ResponseType.Close;

            case ButtonsType.Cancel:
               ComponentManager.MessageWriteLine("[CANCEL]");
               return ResponseType.Cancel;

            case ButtonsType.YesNo:    
               throw new Exception("sorry, but a yes/no messagebox cannot be decided in batch mode :(");

            case ButtonsType.OkCancel:
               #if true
               throw new Exception("are we sure here we want an automatic [OK] click in batch mode???");
               #else
               // be BOLD and assume an automated "OK" in script mode???? hmmm........... might be dangerous.....
               // "Do you really want to delete the internet? [OK]/[CANCEL]"......
               ComponentManager.MessageWriteLine("[OK]");               
               return ResponseType.Ok;
               #endif               
            }
            throw new Exception("unknown message box button type");
         }
         #endregion

         if(parent==null && ComponentManager!=null)
            parent = ComponentManager;
         MessageDialog md = new MessageDialog(parent, DialogFlags.Modal, msgtype, buttontype, format, args);

         if(Platform.IsWindows) // replace Gtk's private icons by Windows standard icons
         {
            switch(msgtype)
            {
            case MessageType.Info:     ((Gtk.Image) md.Image).Pixbuf = PIXBUF_INFO;     break;
            case MessageType.Warning:  ((Gtk.Image) md.Image).Pixbuf = PIXBUF_WARNING;  break;
            case MessageType.Question: ((Gtk.Image) md.Image).Pixbuf = PIXBUF_QUESTION; break;
            case MessageType.Error:    ((Gtk.Image) md.Image).Pixbuf = PIXBUF_ERROR;    break;
            }
         }

         md.SetPosition(parent==null ? WindowPosition.Center : WindowPosition.CenterOnParent);
         if(ComponentManager!=null)
         {
            md.Title = ComponentManager.ApplicationName;
            md.Icon  = ComponentManager.Icon;
         }
         else
         {
            md.Title = "";
            md.Icon  = null;
         }

         // localize button texts
         foreach(Gtk.Widget w in md.ActionArea.Children)
         {
            Gtk.Button b = w as Gtk.Button;
            if(b!=null)
               b.Label = b.Label.Localized("Docking.Components");
         }

         ResponseType result = (ResponseType) md.Run();
         md.Destroy();
         return result;
      }

      public static ResponseType Show(MessageType msgtype, ButtonsType buttontype, string format, params object[] args)
      {
         return Show(null, msgtype, buttontype, format, args);
      }

      public static ResponseType Show(MessageType msgtype, ButtonsType buttontype, string s)
      {
         return Show(null, msgtype, buttontype, "{0}", s);
      }

      public static ResponseType Show(MessageType msgtype, string s)
      {
         return Show(null, msgtype, ButtonsType.Ok, "{0}", s);
      }

      public static ResponseType Show(MessageType msgtype, string format, params object[] args)
      {
         return Show(null, msgtype, ButtonsType.Ok, format, args);
      }

      public static ResponseType Show(string format, params object[] args)
      {
         return Show(null, MessageType.Warning, ButtonsType.Ok, format, args);
      }

      public static ResponseType Show(string s)
      {
         return Show(null, MessageType.Warning, ButtonsType.Ok, "{0}", s);
      }
   }
}

