
using Docking.Framework.Tools;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Docking.Tools
{
   /// <summary>
   /// Encapsulate GtkDispatcher.Instance.Invoke and ensure that only the last invoke in the queue will be processed.
   /// Other pending handler will be discarded.
   /// Some statistics are collected to have a look about queued invokes.
   /// </summary>
   internal class QueueInvoke
   {
      public int mCount         = 0; // total count of Invoke calls
      public int mQueueCount    = 0; // current queue count
      public int mQueueCountMax = 0; // max queue count
      public int mCancelCount   = 0; // total count of ignored calls due to shadowed by a newer call

      /// <summary>
      /// Initialies a new instance.
      /// </summary>
      public QueueInvoke()
      { }

      /// <summary>
      /// Invoke call, perform handler in the main GUI thread, comparable to GtkDispatcher.Instance.Invoke, but only last invoke in queue will be executed
      /// </summary>
      /// <param name="handler"></param>
      public void InvokeLast(EventHandler handler)
      {
         InvokeLastQueuedHandler(handler);
      }

      private void InvokeLastQueuedHandler(EventHandler handler)
      {
         // some statistic
         Interlocked.Increment(ref mQueueCount);
         mQueueCountMax = Math.Max(mQueueCountMax, mQueueCount);

         // save current point of queued handler
         int current = Interlocked.Increment(ref mCount);

         GtkDispatcher.Instance.Invoke((o, e) =>
         {
            // care statistic
            Interlocked.Decrement(ref mQueueCount);

            // call handler only of at top of queue
            if (current == mCount)
            {
               handler(o, e);
            }

            // other queued handler are ignored
            else
            {
               Interlocked.Increment(ref mCancelCount);
            }
         });
      }
   }


   /// <summary>
   /// List of <see cref="QueueInvoke"/> instances
   /// </summary>
   internal class QueueInvokeList
   {
      private struct CallerInfo
      {
         public string Filename { get; set; }
         public int Linenumber { get; set; }

         /// <inheritdoc />
         public override bool Equals(object obj)
         {
            if (obj is CallerInfo)
            {
               var info = (CallerInfo)obj;
               return info.Filename.Equals(Filename) &&
                      info.Linenumber.Equals(Linenumber);
            }
            else
            {
               return false;
            }
         }

         /// <inheritdoc />
         public override int GetHashCode()
         {
            return Filename.GetHashCode() ^ Linenumber.GetHashCode();
         }
      }

      private readonly Dictionary<CallerInfo, QueueInvoke> mQueueInvoke = new Dictionary<CallerInfo, QueueInvoke>(); // all instances created at while life time

      /// <summary>
      /// Initializes a new instance
      /// </summary>
      public QueueInvokeList()
      { }

      public IEnumerable<string> Statistic
      {
         get
         {
            lock (mQueueInvoke)
            {
               var result = new List<string>();
               foreach (var pair in mQueueInvoke)
               {
                  var info = string.Format("QueueInvoke({0}, {1}) calls={2} ignored={3} maxqueued={4}",
                                           pair.Key.Filename,
                                           pair.Key.Linenumber,
                                           pair.Value.mCount,
                                           pair.Value.mCancelCount,
                                           pair.Value.mQueueCountMax);
                  result.Add(info);
               }

               return result;
            }
         }
      }

      public QueueInvoke GetInstance(string callerFileName, int callerLineNumber)
      {
         lock (mQueueInvoke)
         {
            QueueInvoke value;
            var caller = new CallerInfo()
            {
               Filename = callerFileName,
               Linenumber = callerLineNumber
            };

            if (!mQueueInvoke.TryGetValue(caller, out value))
            {
               value = new QueueInvoke();
               mQueueInvoke.Add(caller, value);
            }

            return value;
         }
      }
   }
}
