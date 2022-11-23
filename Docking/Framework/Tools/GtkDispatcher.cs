
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Docking.Tools;

namespace Docking.Framework.Tools
{
   /// <summary>
   /// The GtkDispatcher is a wrappper around Gtk.Application.Invoke() call.
   /// </summary>
   public class GtkDispatcher
   {
      private int                      mMainThreadID      = 0;
      private TaskScheduler            mMainTaskScheduler = null;
      private Func<bool, Action, bool> mShutdownAction;

      private readonly QueueInvokeList muQueueInvokeList = new QueueInvokeList();
      private readonly DispenseInvoke  mDispenseInvoke   = new DispenseInvoke();

      /// <summary>
      /// Initialies a new instance.
      /// </summary>
      private GtkDispatcher()
      {
         IsShutdown = false;
      }

      /// <summary>
      /// The singleton instance
      /// </summary>
      public static readonly GtkDispatcher Instance = new GtkDispatcher();

      /// <summary>
      /// Captures and sets the <seealso cref="SynchronizationContext"/> and threadId of thread this method is called on.
      /// </summary>
      public void CaptureMainTaskScheduler()
      {
         if (mMainThreadID != 0)
         {
            throw new InvalidOperationException("Main-thread Task Scheduler is already set !");
         }

         mMainThreadID        = Thread.CurrentThread.ManagedThreadId;

         SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
         mMainTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
      }

      /// <summary>
      /// Is current thread the main thread, that is the thread UI controls must be updated on.
      /// </summary>
      internal bool IsMainThread
      {
         get { return Thread.CurrentThread.ManagedThreadId == mMainThreadID; }
      }

      /// <summary>
      /// Indicates whether application is shutting down.
      /// </summary>
      public bool IsShutdown { get; internal set; }

      /// <summary>
      /// Registers callbacks which will be invoked when application's shutdown initiated.
      /// </summary>
      /// <param name="shutdownAction">Handler to be registered</param>
      internal void RegisterShutdownHandler(Func<bool, Action, bool> shutdownAction)
      {
         mShutdownAction += shutdownAction;
      }

      /// <summary>
      /// Begins application shutdown procedure.
      /// </summary>
      /// <param name="savePersistence">Controls whether persistency settings must be saved before shutdown</param>
      /// <param name="action">Handler to be invoked during shutdown</param>
      public void InitiateShutdown(bool savePersistence, Action action = null)
      {
         if (!IsShutdown)
         {
            Invoke2(() => mShutdownAction(savePersistence, action));
         }
      }

      /// <summary>
      /// Invokes delegate on main thread.
      /// </summary>
      /// <param name="handler">Delegate to be invoked</param>
      /// <param name="callerFileName">Name of file from which this method was invoked </param>
      /// <param name="callerLineNumber">Line number of file this method was invoked on</param>
      public void Invoke2(Action handler, [CallerFilePath] string callerFileName = "", [CallerLineNumber] int callerLineNumber = 0)
      {
         Invoke((sender, args) => handler() , callerFileName, callerLineNumber);
      }

      /// <summary>
      /// Invokes delegate on main thread.
      /// </summary>
      /// <param name="handler">Delegate to be invoked</param>
      /// <param name="callerFileName">Name of file from which this method was invoked </param>
      /// <param name="callerLineNumber">Line number of file this method was invoked on</param>
      public void Invoke(EventHandler handler, [CallerFilePath] string callerFileName = "", [CallerLineNumber] int callerLineNumber = 0)
      {
         if (IsMainThread)
         {
            handler.Invoke(null, EventArgs.Empty);
         }
         else
         {
            Gtk.Application.Invoke(handler);
         }
      }


      /// <summary>
      /// Invokes delegate using a <see cref="Task"/> asynchronously on main thread.
      /// </summary>
      /// <typeparam name="TResult">The return type of delegate</typeparam>
      /// <param name="handler">The delegate to be invoked</param>
      /// <param name="callerFileName">Name of file from which this method was invoked </param>
      /// <param name="callerLineNumber">Line number of file this method was invoked on</param>
      /// <returns>The <seealso cref="Task{TResult}"/> representing the executing delegate.</returns>
      public Task<TResult> InvokeAsync<TResult>(Func<TResult> handler, [CallerFilePath] string callerFileName = "", [CallerLineNumber] int callerLineNumber = 0)
      {
         if (IsMainThread)
         {
            return Task.FromResult(handler.Invoke());
         }
         else
         {
            var task = new Task<TResult>(handler);
            Gtk.Application.Invoke(delegate
            {
               task.RunSynchronously(mMainTaskScheduler);
            });

            return task;
         }
      }

      /// <summary>
      /// Invokes only the last queued delegate on the main thread.
      /// In case multiple delegates are queued in UI dispatcher for execution only the
      /// last queued delegate will run. All other queued calls are disregared and therefore wont run.
      /// This is a suppose to reduced the load of main thread dispatcher queue.
      /// </summary>
      /// <param name="handler">The delegate to be invoked</param>
      /// <param name="callerFileName">Name of file from which this method was invoked </param>
      /// <param name="callerLineNumber">Line number of file this method was invoked on</param>
      public void InvokeLast(EventHandler handler, [CallerFilePath] string callerFileName = "", [CallerLineNumber] int callerLineNumber = 0)
      {
         muQueueInvokeList.GetInstance(callerFileName, callerLineNumber).InvokeLast(handler);
      }

      /// <summary>
      /// Invokes the handler delegate on main thread.
      /// Limit dispatching calls when more than 100 calls are pending for execution by main thread.
      /// Below this threshold queued calls will be continuously posted to main thread dispatcher.
      /// </summary>
      /// <param name="handler">The delegate to be invoked.</param>
      public void InvokeDispense(EventHandler handler)
      {
         mDispenseInvoke.Invoke(handler);
      }
   }
}
