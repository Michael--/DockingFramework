
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Docking.Tools
{
   /// <summary>
   /// Encapsulate GtkDispatcher.Instance.Invoke and ensure that massive call invoke to main thread time by time.
   /// Avoid to bother the main thread with a high queue of tasks.
   /// Helpful to ensure an active UI
   /// </summary>
   internal class DispenseInvoke
   {
      private                 int                 mCount    = 0; // total count of Invoke calls
      private readonly        Queue<EventHandler> mEvents   = new Queue<EventHandler>();
      private                 Task                mTask     = null;

      /// <summary>
      /// Initializes a new instance.
      /// </summary>
      public DispenseInvoke()
      { }

      public void Invoke(EventHandler newHandler)
      {
         Monitor.Enter(mEvents);

         mEvents.Enqueue(newHandler);

         if (mTask == null || mTask.IsCompleted)
         {
            mTask = Task.Run(() =>
            {
               while (true)
               {
                  //Spinwait til UI dispatcher has run a couple more delegates ..
                  while (mCount > 100)
                  {
                     Thread.Sleep(1);
                  }

                  Monitor.Enter(mEvents);
                  if (mEvents.Count == 0)
                  {
                     Monitor.Exit(mEvents);
                     return;
                  }

                  var handler = mEvents.Dequeue();
                  Monitor.Exit(mEvents);

                  Interlocked.Increment(ref mCount);
                  Docking.Framework.Tools.GtkDispatcher.Instance.Invoke((o, e) =>
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
