using System;

namespace Docking.Tools
{
    public class WorkerThreadInfo : JobInfo
    {
        private WorkerThreadInfo (String name, String description)
            : base(name, description)
        {
        }
        
        public static WorkerThreadInfo Create(WorkerThread worker, String name, String description)
        {
            WorkerThreadInfo job = new WorkerThreadInfo(name, description);
            job.m_Worker = worker;
            JobInfo.AddJob(job);
            return job;
        }
        
        WorkerThread m_Worker;
        
        public override bool CancelationSupported
        {
            get { return m_Worker.WorkerSupportsCancellation; }
        }
        
        public override void Cancel()
        {
            m_Worker.CancelAsync();
        }
    }
}

