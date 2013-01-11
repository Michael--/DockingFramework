using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;

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
            
        WorkState mState = WorkState.NotStarted;

        WorkState State { get { return mState; } }
            
        public bool CancellationPending
        {
            get { return mState == WorkState.CancelationPending; }
            private set {
                // only be set is possible, can't reverted
                if (WorkerSupportsCancellation && mState < WorkState.CancelationPending)
                    mState = WorkState.CancelationPending;
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
            if (WorkerReportsProgress && ProgressChanged != null)
                ProgressChanged(percent, new ProgressChangedEventArgs(percent, this));
        }
            
        public ThreadWorker ()
        {
            mThread = new Thread(ThreadHull);
        }
            
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
                mState = WorkState.Canceled;
            else
                mState = WorkState.Completed;
                
            if (RunWorkerCompleted != null)
                RunWorkerCompleted(this, new RunWorkerCompletedEventArgs(theArgs.Result, null, theArgs.Cancel));
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
}

