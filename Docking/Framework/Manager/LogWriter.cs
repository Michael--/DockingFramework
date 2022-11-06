
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Docking.Framework.Tools;
using Docking.Framework;

namespace Docking.Components
{
   /// <summary>
   /// The LogWriter writes lifecycle message to file and console
   /// </summary>
   public class LogWriter : IMessageWriteLine
   {
      private readonly List<String>                 mMessageQueue = new List<string>();
      private readonly Dictionary<string, IMessage> mReceivers    = new Dictionary<string, IMessage>();
      private          TextWriter                   mLogFile      = null;

      internal LogWriter()
      {
         Title         = String.Empty;
         EnableLogging = false;
      }

      public string Title { get; set; }

      public bool EnableLogging { get; set; }

      public void OpenFile(string filename, bool append)
      {
         if (!string.IsNullOrEmpty(filename))
         {
            try
            {
               mLogFile = new StreamWriter(filename, append, new UTF8Encoding(false));
            }
            catch(Exception e)
            {
               string errmsg = String.Format("cannot open log file '{0}' for writing: {1}", filename, e);

               Console.Error.WriteLine(errmsg);
               Console.Error.Flush();

               MessageWriteLine(errmsg);
               mLogFile = null;

               throw new Exception(errmsg, e);
            }

            MessageWriteLine("=== {0} === {1} ===============================================================================",
                             DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Title);
         }
      }

      public void AddMessageReceiver(string id, IMessage message)
      {
         mReceivers.Add(id, message);

         // push all queued messages
         foreach (string m in mMessageQueue)
         {
            message.WriteLine(m);
         }
      }

      public void RemoveMessageReceiver(string id)
      {
         mReceivers.Remove(id);
      }


      #region IMessageWriteLine

      public void MessageWriteLine(String format, params object[] args)
      {
         if (format == null)
         {
            return;
         }

         string message;
         try
         {
            if (args.Count() == 0)
            {
               message = format;
            }
            else
            {
               message = String.Format(format, args);
            }
         }
         catch(FormatException)
         {
            message = "(invalid format string)";
         }

         if (!EnableLogging)
         {
            Console.WriteLine(message);
         }

         if (mLogFile != null)
         {
            mLogFile.WriteLine(message);
            mLogFile.Flush();
         }

         if (GtkDispatcher.Instance.IsShutdown)
         {
            return;
         }

         if (EnableLogging)
         {
            GtkDispatcher.Instance.Invoke(() => SendToReceivers(message));
         }
      }

      private void SendToReceivers(String message)
      {
         foreach (KeyValuePair<string, IMessage> kvp in mReceivers)
         {
            kvp.Value.WriteLine(message);
         }

         // queue all messages for new not yet existing receiver
         if (mReceivers.Count == 0)
         {
            mMessageQueue.Add(message);
         }
      }

      #endregion
   }
}
