
using System;
using System.Threading;

namespace Docking.Tools
{
   public class TaskInformation : JobInfo
   {
      private readonly CancellationTokenSource m_TokenSource;

      private TaskInformation(CancellationTokenSource token, String name, String description)
         : base(name, description)
      {
         m_TokenSource = token;
      }

      public bool IsCancellationRequested
      {
         get { return m_TokenSource.IsCancellationRequested; }
      }

      public override bool CancelationSupported
      {
         get { return m_TokenSource != null; }
      }

      public static TaskInformation Create(CancellationTokenSource token, String name, String description)
      {
         TaskInformation job = new TaskInformation(token, name, description);
         AddJob(job);
         return job;
      }

      public override void Cancel()
      {
         if (m_TokenSource != null)
         {
            m_TokenSource.Cancel();
         }
      }
   }
}
