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
   /// Encapsulate Gtk.Application.Invoke and ensure that massive call invoke to main thread time by time.
   /// Avoid to bother the main thread with a high queue of tasks.
   /// Helpful to ensure an active UI
   /// </summary>
   public class DispenseInvoke
   {
      private DispenseInvoke()
      {
      }

      static DispenseInvoke mInstance = new DispenseInvoke();
      Task mTask = null;
      Queue<EventHandler> mEvents = new Queue<EventHandler>();
      int mCount = 0; // total count of Invoke calls

      /// <summary>
      /// Invoke call, perform handler in the main GUI thread, comparable to Gtk.Application.Invoke, but invoke will be dispensed by load of main thread
      /// </summary>
      /// <param name="handler"></param>
      public static void Invoke(EventHandler handler, [CallerFilePathAttribute] string callerFileName = "", [CallerLineNumberAttribute] int callerLineNumber = 0)
      {
         mInstance.Invoke(handler);
      }

      void Invoke(EventHandler newHandler)
      {
         Monitor.Enter(mEvents);
         mEvents.Enqueue(newHandler);
         if (mTask == null || mTask.IsCompleted)
         {
            mTask = Task.Run(() =>
            {
               while (true)
               {
                  // allow only some pending Gtk.Application.Invoke until invoke next from queue
                  while (mCount > 100)
                     Thread.Sleep(1);

                  Monitor.Enter(mEvents);
                  if (mEvents.Count == 0)
                  {
                     Monitor.Exit(mEvents);
                     return;
                  }
                  var handler = mEvents.Dequeue();
                  Monitor.Exit(mEvents);

                  Interlocked.Increment(ref mCount);
                  Gtk.Application.Invoke((o, e) =>
                  {
                     handler(o, e);
                     Interlocked.Decrement(ref mCount);
                  });
               }
            });
         }
         Monitor.Exit(mEvents);
      }
   }
}
