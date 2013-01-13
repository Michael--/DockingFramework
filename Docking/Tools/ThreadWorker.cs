using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Docking.Tools
{
    /**
     * Use the ThreadWorker as an alternate to the BackgroundWorker.
     * The interface is near the same.
     * Is is obligatory to choise a thread priority (not possible with BW)
     * This worker are explicitly for long processing time.
     * For short calculation time use the BW instead.
     * Basic difference to BackgroundWorker is that the ThreadWorker use an own thread.
     */ 
    public class ThreadWorker : Object
    {
        internal enum WorkState
        {
            /// The work item is in the queue.
            NotStarted,
                
            /// A thread is executing the work item.
            InProgress,
                
            /// Cancelation pending
            CancelationPending,
                
            /// The work item execution has been completed.
            Completed,
                
            /// The work item has been cancelled.
            Canceled,
        }
            
        private WorkState State { get; set; }
        JobInformation m_JobInformation;
            
        public bool CancellationPending
        {
            get { return State == WorkState.CancelationPending; }
            private set {
                // only be set is possible, can't reverted
                if (WorkerSupportsCancellation && State < WorkState.CancelationPending)
                    State = WorkState.CancelationPending;
            }
        }
            
        bool mReportsProgress = false;

        public bool WorkerReportsProgress
        {
            get { return mReportsProgress; }
            set { mReportsProgress = value; }
        }
            
        /// <summary>
        /// I think its important that cancellation possibility is default true (other as BackgroundWorker)
        /// </summary>
        bool mSupportCancellation = true;

        public bool WorkerSupportsCancellation
        {
            get { return mSupportCancellation; }
            set { mSupportCancellation = value; }
        }
            
        public void ReportProgress(int percent)
        {
            m_JobInformation.Progress = percent;
            if (WorkerReportsProgress && ProgressChanged != null)
                ProgressChanged(percent, new ProgressChangedEventArgs(percent, this));
        }
            
        public ThreadWorker (String name, String description)
        {
            State = WorkState.NotStarted;
            mThread = new Thread(ThreadHull);

            m_JobInformation = ThreadWorkerInformation.Create(this, name, description);
        }
            
        /// <summary>
        /// Short description to identify thread in any view 
        /// </summary>
        private Thread mThread;
        DoWorkEventArgs theArgs;
                    
        internal void ThreadHull(object sender)
        {
            // ThreadWorker thread = sender as ThreadWorker;
                
            if (DoWork != null)
            {
                // the class Background worker use DoWork.BeginInvoke()
                // inside RunWorkerAsync() without using an explicit new thread
                DoWork.Invoke(this, theArgs);
            }
                
            if (theArgs.Cancel)
                State = WorkState.Canceled;
            else
                State = WorkState.Completed;
                
            if (RunWorkerCompleted != null)
                RunWorkerCompleted(this, new RunWorkerCompletedEventArgs(theArgs.Result, null, theArgs.Cancel));
            m_JobInformation.Destroy();
        }
            
        public void RunWorkerAsync(ThreadPriority priority, object args)
        {
            theArgs = new DoWorkEventArgs(args);
                
            /// note: following next 2 code lines in opposite order can cause a "ThreadStateException" (Remember "Thread is dead ...")
            mThread.Priority = priority;
            mThread.Start(this);
        }
            
        public void RunWorkerAsync(ThreadPriority priority)
        {
            RunWorkerAsync(priority, null);
        }
            
        public void RunWorkerAsync()
        {
            RunWorkerAsync(ThreadPriority.Normal, null);
        }
            
        public bool IsBusy
        {
            get {
                if (State == WorkState.Completed || State == WorkState.Canceled)
                    return false;
                return true;
            }
        }
            
        // be careful using this function
        public void ForceCancel(int msec)
        {
            CancelAsync();
                
            Thread.Sleep(20);
                
            if (!this.mThread.Join(msec))
                this.mThread.Abort();
                
        }
            
        public void CancelAsync()
        {
            CancellationPending = true; // note: false do the same
        }
            
        public event DoWorkEventHandler DoWork;
            
        // Occurs when System.ComponentModel.ThreadWorker.ReportProgress(System.Int32) is called.
        public event ProgressChangedEventHandler ProgressChanged;
            
        // Occurs when the background operation has completed/canceled
        public event RunWorkerCompletedEventHandler RunWorkerCompleted;
    }

    public class JobInformationEventArgs : EventArgs
    {
        public JobInformationEventArgs (JobInformation job)
        {
            JobInformation = job;
        }
        public JobInformation JobInformation { get; private set; }
    }

    public delegate void JobInformationAddedEventHandler (object sender, JobInformationEventArgs e);
    public delegate void JobInformationRemovedEventHandler (object sender, JobInformationEventArgs e);
    public delegate void JobInformationProgressEventHandler (object sender, JobInformationEventArgs e);


    /// <summary>
    /// Job information about any running thread or task.
    /// Contains basic information like name, start time, etc.
    /// Additionally information about progress if available
    /// and steering possibilities like pause and abort if supported.
    /// </summary>
    public abstract class JobInformation
    {
        public JobInformation (String name, String desciption)
        {
            Name = name;
            Description = desciption;
            StartTime = DateTime.Now;
            Id = LastId++;
        }

        public void Destroy()
        {
            JobInformation.RemoveJob(this);
        }

        public String Name { get; private set; }
        public String Description { get; private set; }
        public DateTime StartTime { get; private set; }
        public int Id { get; private set; }
        private static int LastId = 0;
        abstract public bool CancelationSupported { get; }
        public bool ProgressSupported { get; private set; }
        public int Progress
        {
            get { return m_Progress; }
            set
            {
                Debug.Assert(value >= 0 && value <= 100);
                m_Progress = value;
                ProgressSupported = true;
                if (ProgressChanged != null)
                    ProgressChanged(null, new JobInformationEventArgs(this));
            }
        }
        private int m_Progress;

        public virtual void Cancel() {}

        protected static void AddJob(JobInformation job)
        {
            lock(m_Jobs)
                m_Jobs.Add(job);
            if (Added != null)
                Added(null, new JobInformationEventArgs(job));
        }

        private static void RemoveJob(JobInformation job)
        {
            lock(m_Jobs)
                m_Jobs.Remove(job);
            if (Removed != null)
                Removed(null, new JobInformationEventArgs(job));
        }

        // the list of all existing jobs
        static List<JobInformation> m_Jobs = new List<JobInformation>();
        public static JobInformation[] GetJobs()
        { 
            lock(m_Jobs)
                return m_Jobs.ToArray();
        }

        // Occurs when a new job added to the list of all jobs
        public static event  JobInformationAddedEventHandler Added;
        
        // Occurs when a job removed from the list of all jobs
        public static event  JobInformationRemovedEventHandler Removed;

        // Occurs when job progress changed
        public event JobInformationProgressEventHandler ProgressChanged;
    }

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

    public class TaskInformation : JobInformation
    {
        private TaskInformation (String name, String description)
            : base(name, description)
        {
        }

        public static TaskInformation Create(String name, String description)
        {
            TaskInformation job = new TaskInformation(name, description);
            JobInformation.AddJob(job);
            return job;
        }

        public override bool CancelationSupported
        {
            get { return false; }
        }
        
        public override void Cancel()
        {
            // m_Worker.CancelAsync();
        }
    }

}

