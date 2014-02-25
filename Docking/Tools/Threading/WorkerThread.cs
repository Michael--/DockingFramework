using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Docking.Tools
{
    // [Obsolete("use Task.Factory.StartNew() instead", false)]
    public class WorkerThread : Object
    {
        public enum WorkState
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
            
        public WorkState State { get; protected set; }
        JobInfo m_JobInformation;
            
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
            
        public WorkerThread (String name, String description)
        {
            State = WorkState.NotStarted;
            mThread = new Thread(ThreadHull);

            m_JobInformation = WorkerThreadInfo.Create(this, name, description);
        }
            
        /// <summary>
        /// Short description to identify thread in any view 
        /// </summary>
        private Thread mThread;
        DoWorkEventArgs theArgs;
                    
        internal void ThreadHull(object sender)
        {
            State = WorkState.InProgress;

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

            mThread.Priority = priority; // priority must be set _before_ .Start(), otherwise of course the thread might be finished when that assignment is tried, and that will result in an exception
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
            get
            {
               return State==WorkState.InProgress
                   || State==WorkState.CancelationPending;
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
            
        // Occurs when System.ComponentModel.WorkerThread.ReportProgress(System.Int32) is called.
        public event ProgressChangedEventHandler ProgressChanged;
            
        // Occurs when the background operation has completed/canceled
        public event RunWorkerCompletedEventHandler RunWorkerCompleted;
    }
}

