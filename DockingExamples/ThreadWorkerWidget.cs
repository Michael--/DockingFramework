using System;
using Docking.Components;
using Docking;
using Docking.Tools;
using System.ComponentModel;
using System.Threading;

namespace Examples.Threading
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class ThreadWorkerWidget : Gtk.Bin, Docking.Components.IComponent
    {
        public ThreadWorkerWidget ()
        {
            this.Build();
            myThreadHeader = "T" + instances++.ToString(); 
            progressbar1.Adjustment = new Gtk.Adjustment(0, 0, 100, 1, 1, 10);
            progressbar1.Adjustment.Lower = 0;
            progressbar1.Adjustment.Upper = 100;
        }

        private ThreadWorker theWorker = null;
        String myThreadHeader;
        static int instances = 0;
        int myThreadId = 0;
        static Random rnd = new Random();

        void StartNewThread()
        {
            if (ComponentManager.PowerDown)
                return;
            myThreadId++;
            Message(String.Format("Thread {0}:{1} started", myThreadHeader, myThreadId));
            theWorker = new ThreadWorker();
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
        }
        
        // complete message
        private void RunCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Message(String.Format("Thread {0}:{1} completed", myThreadHeader, myThreadId));
            theWorker = null;
            StartNewThread();
        }
        
        // progress message
        private void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (ComponentManager.PowerDown)
                return;

            Gtk.Application.Invoke(delegate {
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
            StartNewThread();
        }
        
        void Docking.Components.IComponent.Save()
        {
            RequestStop();
        }
        
        #endregion

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

