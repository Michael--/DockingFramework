using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Docking.Tools
{
   /// <summary>
   /// Encapsulate Gtk.Application.Invoke and ensure that only the last invoke in the queue will be processed.
   /// Other pending handler will be discarded.
   /// Some statistics are collected to have a look about queued invokes.
   /// </summary>
   public class QueueInvoke
   {
      private QueueInvoke(string callerFileName, int callerLineNumber)
      {
         CallerFileName = callerFileName;
         CallerLineNumber = callerLineNumber;

         Debug.Assert(!mQueueInvoke.ContainsKey(GetHashCode()), "Create only once");
         mQueueInvoke.Add(GetHashCode(), this);
      }

      private string CallerFileName { get;  set; }
      private int CallerLineNumber { get; set; }

      public override int GetHashCode()
      {
         return CalculateHashCode(CallerFileName, CallerLineNumber);
      }

      private static int CalculateHashCode(string callerFileName, int callerLineNumber)
      {
         return callerFileName.GetHashCode() ^ callerLineNumber.GetHashCode();
      }

      private int mCount = 0; // total count of Invoke calls
      private int mCancelCount = 0; // total count of ignored calls due to shadowed by a newer call
      private int mQueueCount = 0; // current queue count 
      private int mQueueCountMax = 0; // max queue count
      private static Dictionary<int, QueueInvoke> mQueueInvoke = new Dictionary<int, QueueInvoke>(); // all instances created at while life time

      private static QueueInvoke GetInstance(string callerFileName, int callerLineNumber)
      {
         QueueInvoke value;
         if (!mQueueInvoke.TryGetValue(CalculateHashCode(callerFileName, callerLineNumber), out value))
           value = new QueueInvoke(callerFileName, callerLineNumber);
         return value;
      }

      public static IEnumerable<string> Statistic
      {
         get
         {
            List<string> result = new List<string>();
            foreach (var s in mQueueInvoke.Values)
               result.Add(s.DebugInformation);
            return result;
         }
      }

      private string DebugInformation
      {
         get
         {
            return string.Format("QueueInvoke({0}, {1}) calls={2} ignored={3} maxqueued={4}", CallerFileName, CallerLineNumber, mCount, mCancelCount, mQueueCountMax);
         }
      }

      /// <summary>
      /// Invoke call, perform handler in the main GUI thread, comparable to Gtk.Application.Invoke, but only last invoke in queue will be executed
      /// </summary>
      /// <param name="handler"></param>
      public static void Invoke(EventHandler handler, [CallerFilePathAttribute] string callerFileName = "", [CallerLineNumberAttribute] int callerLineNumber = 0)
      {
         var instance = QueueInvoke.GetInstance(callerFileName, callerLineNumber);
         instance.Invoke(handler);
      }

      private void Invoke(EventHandler handler)
      {
         // some statistic
         Interlocked.Increment(ref mQueueCount);
         mQueueCountMax = Math.Max(mQueueCountMax, mQueueCount);

         // save current point of queued handler
         int current = Interlocked.Increment(ref mCount);

         Gtk.Application.Invoke((o, e) =>
         {
            // care statistic
            Interlocked.Decrement(ref mQueueCount);

            // call handler only of at top of queue
            if (current == mCount)
               handler(o, e);

            // other queued handler are ignored
            else
               Interlocked.Increment(ref mCancelCount);
         });
      }
   }
}
