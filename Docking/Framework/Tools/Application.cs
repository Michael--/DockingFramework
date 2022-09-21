
using System;
using System.Runtime.CompilerServices;
using Docking.Tools;
using System.Threading;

namespace Docking.Framework.Tools
{
   /// <summary>
   /// The Application class which uses Gtk.Application class.
   /// </summary>
   public class Application
   {
      internal static int mMainThreadID  = 0;

      internal static bool IsMainThread
      {
         get { return Thread.CurrentThread.ManagedThreadId == mMainThreadID; }
      }

      public static void Init()
      {
         Gtk.Application.Init();
      }

      public static void Run()
      {
         Gtk.Application.Run();
      }

      public static void Quit()
      {
         Gtk.Application.Quit();
      }

      public static void Invoke(Action handler, [CallerFilePath] string callerFileName = "", [CallerLineNumber] int callerLineNumber = 0)
      {
         Invoke((sender, args) => handler() , callerFileName, callerLineNumber);
      }

      public static void Invoke(EventHandler handler, [CallerFilePath] string callerFileName = "", [CallerLineNumber] int callerLineNumber = 0)
      {
         if (IsMainThread)
         {
            handler.Invoke(null, EventArgs.Empty);
         }
         else
         {
            QueueInvoke.Invoke(handler, callerFileName, callerLineNumber);
         }
      }
   }
}
