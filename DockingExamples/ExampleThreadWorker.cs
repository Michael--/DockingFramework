using System;
using Docking.Components;
using Docking;
using Docking.Tools;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Examples
{
    [System.ComponentModel.ToolboxItem(false)]
    public partial class ExampleThreadWorker : Gtk.Bin, Docking.Components.IComponent
    {
        public ExampleThreadWorker ()
        {
            this.Build();
            this.Name = "Threading";
            myThreadHeader = "Test" + instances++.ToString();
            progressbar1.Adjustment = new Gtk.Adjustment(0, 0, 100, 1, 1, 10);
            progressbar1.Adjustment.Lower = 0;
            progressbar1.Adjustment.Upper = 100;
        }

        bool m_Destroyed = false; // todo: should be replaced by an widget property, but which ?
        protected override void OnDestroyed()
        {
            m_Destroyed = true;
            base.OnDestroyed();
            RequestStop();
        }

        private List<CancellationTokenSource> cancelTokenList = new List<CancellationTokenSource>();

        private ThreadWorker mThreadWorker = null;
        private ThreadWorker mThreadEndless = null;
        private Object mThreadWorkerSemaphore = new object();
        private Object mThreadEndlessSemaphore = new object();

        String myThreadHeader;
        static int instances = 0;
        int myThreadId = 0;
        int myTaskId = 0;
        int countTasks = 0;
        static Random rnd = new Random();

        void StartNewThread()
		{
			if(ComponentManager.PowerDown || m_Destroyed)
				return;

            buttonStartThread.Sensitive = false;
            lock(mThreadWorkerSemaphore)
			{
				myThreadId++;
				String name = String.Format("{0}:{1}", myThreadHeader, myThreadId);
				String description = "Example how to use ThreadWorker";
				Message(String.Format("Thread {0} started", name));
				mThreadWorker = new ThreadWorker(name, description);
				mThreadWorker.WorkerSupportsCancellation = true;
				mThreadWorker.WorkerReportsProgress = true;
				mThreadWorker.DoWork += new DoWorkEventHandler(mThreadWorker_DoWork);
				mThreadWorker.ProgressChanged += new ProgressChangedEventHandler(mThreadWorker_ProgressChanged);
				mThreadWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(mThreadWorker_RunWorkerCompleted);
				mThreadWorker.RunWorkerAsync(ThreadPriority.BelowNormal);
			}
        }

        void StartEndlessThread()
        {
            if(ComponentManager.PowerDown || m_Destroyed)
                return;
            buttonEndlessStart.Sensitive = false;
            buttonEndlessStop.Sensitive = true;

            lock(mThreadEndlessSemaphore)
            {
                myThreadId++;
                String name = String.Format("Endless {0}:{1}", myThreadHeader, myThreadId);
                String description = "Endless ThreadWorker";
                Message(String.Format("Thread {0} started", name));
                mThreadEndless = new ThreadWorker(name, description);
                mThreadEndless.WorkerSupportsCancellation = true;
                mThreadEndless.WorkerReportsProgress = false;
                mThreadEndless.DoWork += new DoWorkEventHandler(mThreadEndless_DoWork);
                mThreadEndless.RunWorkerCompleted += new RunWorkerCompletedEventHandler(mThreadEndless_RunWorkerCompleted);
                mThreadEndless.RunWorkerAsync(ThreadPriority.BelowNormal);
            }
        }


        public void RequestStop()
		{
			lock(mThreadWorkerSemaphore)
			{
				if(mThreadWorker != null)
					mThreadWorker.CancelAsync();
				foreach(CancellationTokenSource t in cancelTokenList)
					t.Cancel();
			}
            lock(mThreadEndlessSemaphore)
            {
                if (mThreadEndless != null)
                    mThreadEndless.CancelAsync();
            }
        }

        // complete message
        private void mThreadWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			lock(mThreadWorkerSemaphore)
			{
				Message(String.Format("Thread {0}:{1} {2}",
	                                  myThreadHeader, myThreadId,
	                                  e.Cancelled ? "Canceled" : "completed"));
				mThreadWorker = null;
			}
            buttonStartThread.Sensitive = true;
        }

        // progress message
        private void mThreadWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Gtk.Application.Invoke(delegate
            {
                if (!ComponentManager.PowerDown && !m_Destroyed)
                    progressbar1.Adjustment.Value = e.ProgressPercentage;
            });
        }

        private void Message(String message)
        {
            if (ComponentManager.PowerDown)
                return;

            Gtk.Application.Invoke(delegate {
                ComponentManager.MessageWriteLine(message);
            });
        }

        private void mThreadWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            ThreadWorker worker = sender as ThreadWorker;

            int duration = rnd.Next() % 10000 + 10000;
            int steps = 100;
            int onesleep = duration / steps;
            int proceeded = 0;
            while(proceeded < duration)
            {
                proceeded += onesleep;
                Thread.Sleep(onesleep);

                worker.ReportProgress(proceeded * 100 / duration);

                if(worker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
            }
        }

        // complete message
        private void mThreadEndless_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            lock(mThreadEndlessSemaphore)
            {
                Message(String.Format("Thread {0}:{1} {2}",
                                      myThreadHeader, myThreadId,
                                      e.Cancelled ? "Canceled" : "completed"));
                mThreadEndless = null;
            }
            buttonEndlessStart.Sensitive = true;
            buttonEndlessStop.Sensitive = false;
        }

        private void mThreadEndless_DoWork(object sender, DoWorkEventArgs e)
        {
            ThreadWorker worker = sender as ThreadWorker;
            
            while(true)
            {
                Thread.Sleep(50);
                
                if(worker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
            }
        }


        #region implement IComponent

        public ComponentManager ComponentManager { get; set; }

        void Docking.Components.IComponent.Loaded(DockItem item)
        {
        }

        void Docking.Components.IComponent.Save()
        {
            RequestStop();
        }

        #endregion

        protected void OnButton1Clicked(object sender, EventArgs e)
        {
            // start a new task with Task.Factory
            // this is a very common method to work on something in the background
            // use TaskInformation to observe this new task

            CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
            lock (cancelTokenList)
                cancelTokenList.Add(cancelTokenSource);

            Task.Factory.StartNew(() =>
            {
                myTaskId++;
                String name = String.Format("{0}:{1}", myThreadHeader, myTaskId);
                String description = "Example how to use a Task";
                Message(String.Format("Task {0} started", name));

                Gtk.Application.Invoke(delegate {
                    labelTaskCount.Text = String.Format("Running count: {0}", ++countTasks);
                });
                TaskInformation ti = TaskInformation.Create(cancelTokenSource, name, description);

                int duration = rnd.Next() % 5000 + 5000;
                int steps = 100;
                int onesleep = duration / steps;
                int proceeded = 0;
                while(proceeded < duration)
                {
                    proceeded += onesleep;
                    Thread.Sleep(onesleep);
                    ti.Progress = proceeded * 100 / duration;
                    if (cancelTokenSource.IsCancellationRequested)
                        break;
                }
                Message(String.Format("Task {0} {1}", name,
                        cancelTokenSource.IsCancellationRequested ? "cancelled" : "finished"));
                ti.Destroy();
                Gtk.Application.Invoke(delegate {
                    labelTaskCount.Text = String.Format("Running count: {0}", --countTasks);
                });
                lock (cancelTokenList)
                    cancelTokenList.Remove(cancelTokenSource);
            }, cancelTokenSource.Token);
        }

  
        protected void OnButtonStartThreadClicked (object sender, EventArgs e)
        {
            if (mThreadWorker == null)
                StartNewThread();
        }

        protected void OnButtonEndlessStartClicked (object sender, EventArgs e)
        {
            if (mThreadEndless == null)
                StartEndlessThread();
        }

        protected void OnButtonEndlessStopClicked (object sender, EventArgs e)
        {
            if (mThreadEndless != null)
                mThreadEndless.CancelAsync();
        }
    }

    #region Starter / Entry Point

	public class ExampleThreadWorkertFactory : ComponentFactory
    {
        public override Type TypeOfInstance { get { return typeof(ExampleThreadWorker); } }
        public override String MenuPath { get { return @"View\Examples\Thread Worker"; } }
        public override String Comment { get { return "Example thread worker widget, starts some worker thread(s)"; } }
        public override Mode Options { get { return Mode.MultipleInstance; } }
        public override Gdk.Pixbuf Icon { get { return Gdk.Pixbuf.LoadFromResource ("Examples.ThreadWorker-16.png"); } }
    }

    #endregion
}

