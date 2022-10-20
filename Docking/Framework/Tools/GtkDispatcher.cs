﻿
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Docking.Framework.Tools
{
   /// <summary>
   /// The GtkDispatcher class is a slim wrappper around Gtk.Application class.
   /// </summary>
   public class GtkDispatcher
   {
      private int                      mMainThreadID      = 0;
      private TaskScheduler            mMainTaskScheduler = null;
      private Func<bool, Action, bool> mShutdownAction;

      private GtkDispatcher()
      {
         IsShutdown = false;
      }

      public static readonly GtkDispatcher Instance = new GtkDispatcher();

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

      internal bool IsMainThread
      {
         get { return Thread.CurrentThread.ManagedThreadId == mMainThreadID; }
      }

      public bool IsShutdown { get; internal set; }

      internal void RegisterShutdownHandler(Func<bool, Action, bool> shutdownAction)
      {
         mShutdownAction += shutdownAction;
      }

      public void InitiateShutdown(bool savePersistence, Action action = null)
      {
         if (!IsShutdown)
         {
            Invoke(() => mShutdownAction(savePersistence, action));
         }
      }

      public void Invoke(Action handler, [CallerFilePath] string callerFileName = "", [CallerLineNumber] int callerLineNumber = 0)
      {
         Invoke((sender, args) => handler() , callerFileName, callerLineNumber);
      }

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
   }
}
