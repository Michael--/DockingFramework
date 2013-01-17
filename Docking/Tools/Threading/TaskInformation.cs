using System;
using System.Threading;

namespace Docking.Tools
{
    public class TaskInformation : JobInformation
    {
        private TaskInformation (CancellationTokenSource token, String name, String description)
            : base(name, description)
        {
            m_TokenSource = token;
        }
        
        CancellationTokenSource m_TokenSource;
        
        public static TaskInformation Create(CancellationTokenSource token, String name, String description)
        {
            TaskInformation job = new TaskInformation(token, name, description);
            JobInformation.AddJob(job);
            return job;
        }
        
        public override bool CancelationSupported
        {
            get { return m_TokenSource != null; }
        }
        
        public override void Cancel()
        {
            if (m_TokenSource != null)
                m_TokenSource.Cancel();
        }
    }
}

