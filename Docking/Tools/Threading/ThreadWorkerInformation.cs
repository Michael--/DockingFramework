using System;

namespace Docking.Tools
{
    public class ThreadWorkerInformation : JobInformation
    {
        private ThreadWorkerInformation (String name, String description)
            : base(name, description)
        {
        }
        
        public static ThreadWorkerInformation Create(ThreadWorker worker, String name, String description)
        {
            ThreadWorkerInformation job = new ThreadWorkerInformation(name, description);
            job.m_Worker = worker;
            JobInformation.AddJob(job);
            return job;
        }
        
        ThreadWorker m_Worker;
        
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

