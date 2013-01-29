using System;
using Docking.Components;
using Docking;
using Docking.Tools;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Examples.Threading
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class ThreadWorkerWidget : Gtk.Bin, Docking.Components.IComponent
    {
        public ThreadWorkerWidget ()
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
        private ThreadWorker theWorker = null;
        String myThreadHeader;
        static int instances = 0;
        int myThreadId = 0;
        int myTaskId = 0;
        int countTasks = 0;
        static Random rnd = new Random();

        void StartNewThread()
        {
            if (ComponentManager.PowerDown || m_Destroyed)
                return;

            // start a new thread
            myThreadId++;
            String name = String.Format("{0}:{1}", myThreadHeader, myThreadId);
            String description = "Example how to use ThreadWorker";
            Message(String.Format("Thread {0} started", name));
            theWorker = new ThreadWorker(name, description);
            theWorker.WorkerSupportsCancellation = true;
            theWorker.WorkerReportsProgress = true;
            theWorker.DoWork += new DoWorkEventHandler(Worker);
            theWorker.ProgressChanged += new ProgressChangedEventHandler(ProgressChanged);
            theWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(RunCompleted);
            theWorker.RunWorkerAsync(ThreadPriority.BelowNormal);
        }

        public void RequestStop()
        {
            if(theWorker != null)
                theWorker.CancelAsync();
            foreach(CancellationTokenSource t in cancelTokenList)
                t.Cancel();
        }
        
        // complete message
        private void RunCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Message(String.Format("Thread {0}:{1} {2}",
                                  myThreadHeader, myThreadId,
                                  e.Cancelled ? "Canceled" : "completed"));
            theWorker = null;

            if (checkbutton1.Active)
                StartNewThread();
        }
        
        // progress message
        private void ProgressChanged(object sender, ProgressChangedEventArgs e)
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
        
        private void Worker(object sender, DoWorkEventArgs e)
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

        #region implement IComponent

        public ComponentManager ComponentManager { get; set; }
        
        void Docking.Components.IComponent.Loaded(DockItem item)
        {
            if (checkbutton1.Active)
                StartNewThread();
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

        protected void OnCheckbutton1Toggled(object sender, EventArgs e)
        {
            if (checkbutton1.Active && theWorker == null)
                StartNewThread();
        }
    }

    #region Starter / Entry Point
    
    public class ThreadWorkerWidgetFactory : ComponentFactory
    {
        public override Type TypeOfInstance { get { return typeof(ThreadWorkerWidget); } }
        public override String MenuPath { get { return @"File\New\Examples\ThreadWorkerWidget"; } }
        public override String Comment { get { return "Example thread worker widget, start some worker thread"; } }
        public override Mode Options { get { return Mode.MultipleInstance; } }
    }
    
    #endregion
}

