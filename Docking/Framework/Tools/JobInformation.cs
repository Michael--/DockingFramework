
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Docking.Tools
{
   /// <summary>
   /// Job information about any running thread or task.
   /// Contains basic information like name, start time, etc.
   /// Additionally information about progress if available
   /// and steering possibilities like pause and abort if supported.
   /// </summary>
   public abstract class JobInfo
   {
      private static int LastId = 0;

      // the list of all existing jobs
      private static readonly List<JobInfo> m_Jobs = new List<JobInfo>();
      private                 int           m_Progress;

      public JobInfo(String name, String description)
      {
         Name        = name;
         Description = description;
         StartTime   = DateTime.Now;
         Id          = LastId++;
      }

      public string Name         { get; private set; }
      public string Description  { get; private set; }
      public DateTime StartTime  { get; private set; }
      public int Id              { get; private set; }

      public abstract bool CancelationSupported { get; }
      public bool ProgressSupported { get; private set; }

      public int Progress
      {
         get { return m_Progress; }
         set
         {
            Debug.Assert(value >= 0 && value <= 100);
            m_Progress        = value;
            ProgressSupported = true;
            if (ProgressChanged != null)
            {
               ProgressChanged(null, new JobInformationEventArgs(this));
            }
         }
      }

      // Occurs when a new job is added to the list of all jobs
      public static event JobInformationAddedEventHandler Added;

      // Occurs when a job is removed from the list of all jobs
      public static event JobInformationRemovedEventHandler Removed;

      // Occurs when job progress changed
      public event JobInformationProgressEventHandler ProgressChanged;

      public virtual void Cancel()
      { }

      public void Destroy()
      {
         RemoveJob(this);
      }

      public static JobInfo[] GetJobs()
      {
         lock(m_Jobs)
         {
            return m_Jobs.ToArray();
         }
      }

      protected static void AddJob(JobInfo job)
      {
         lock(m_Jobs)
         {
            m_Jobs.Add(job);
         }

         if (Added != null)
         {
            Added(null, new JobInformationEventArgs(job));
         }
      }

      private static void RemoveJob(JobInfo job)
      {
         lock(m_Jobs)
         {
            m_Jobs.Remove(job);
         }

         if (Removed != null)
         {
            Removed(null, new JobInformationEventArgs(job));
         }
      }
   }

   public class JobInformationEventArgs : EventArgs
   {
      public JobInformationEventArgs(JobInfo job)
      {
         JobInformation = job;
      }

      public JobInfo JobInformation { get; private set; }
   }

   public delegate void JobInformationAddedEventHandler(object sender, JobInformationEventArgs e);
   public delegate void JobInformationRemovedEventHandler(object sender, JobInformationEventArgs e);
   public delegate void JobInformationProgressEventHandler(object sender, JobInformationEventArgs e);
}
