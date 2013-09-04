using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Docking.Tools
{
    /// <summary>
    /// Job information about any running thread or task.
    /// Contains basic information like name, start time, etc.
    /// Additionally information about progress if available
    /// and steering possibilities like pause and abort if supported.
    /// </summary>
    public abstract class JobInformation
    {
        public JobInformation (String name, String description)
        {
            Name = name;
            Description = description;
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
        
        // Occurs when a new job is added to the list of all jobs
        public static event  JobInformationAddedEventHandler Added;
        
        // Occurs when a job is removed from the list of all jobs
        public static event  JobInformationRemovedEventHandler Removed;
        
        // Occurs when job progress changed
        public event JobInformationProgressEventHandler ProgressChanged;
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
}

