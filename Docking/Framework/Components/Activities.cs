using System;
using System.Collections.Generic;
using Docking.Components;
using Docking.Tools;
using Gtk;

namespace Docking.Components
{
    [System.ComponentModel.ToolboxItem(false)]
    public partial class Activities : Component, ILocalizableComponent
    {
        Gtk.ListStore listStore;

        const int COLUMN_JOB_INFORMATION = 0;
        const int COLUMN_NAME            = 1;
        const int COLUMN_DESCRIPTION     = 2;
        const int COLUMN_STATUS          = 3;

        public Activities ()
        {
            this.Build();

            Gtk.TreeViewColumn columnName   = new TreeViewColumnLocalized() { Title = "Activity",    Sizing = Gtk.TreeViewColumnSizing.Fixed, FixedWidth = 100 };
            Gtk.TreeViewColumn columnDesc   = new TreeViewColumnLocalized() { Title = "Description", Sizing = Gtk.TreeViewColumnSizing.Fixed, FixedWidth = 200 };
            Gtk.TreeViewColumn columnStatus = new TreeViewColumnLocalized() { Title = "Status",      Sizing = Gtk.TreeViewColumnSizing.Fixed, FixedWidth = 100 };

            treeview1.AppendColumn(columnName);
            treeview1.AppendColumn(columnDesc);
            treeview1.AppendColumn(columnStatus);

            Gtk.CellRendererText     rendererName   = new Gtk.CellRendererText ();
            Gtk.CellRendererText     rendererDesc   = new Gtk.CellRendererText ();
            Gtk.CellRendererProgress rendererStatus = new Gtk.CellRendererProgress ();

            columnName.PackStart(rendererName, true);
            columnDesc.PackStart(rendererDesc, true);
            columnStatus.PackStart(rendererStatus, true);

            columnName.AddAttribute(rendererName, "text", COLUMN_NAME);
            columnDesc.AddAttribute(rendererDesc, "text", COLUMN_DESCRIPTION);
            columnStatus.AddAttribute(rendererStatus, "value", COLUMN_STATUS);

            listStore = new Gtk.ListStore (typeof(JobInfo), typeof (string), typeof (string), typeof (int));
            treeview1.Model = listStore;
            treeview1.CursorChanged += HandleCursorChanged;
        }

        public override void Loaded(DockItem item)
        {
            base.Loaded(item);

            JobInfo.Added += HandleAdded;
            JobInfo.Removed += HandleRemoved;
            Initialize();
        }

        #region ILocalizable

        string ILocalizableComponent.Name { get { return "Activities"; } }

        void ILocalizableComponent.LocalizationChanged(Docking.DockItem item)
        {
           Initialize();
        }
        #endregion


        void Initialize()
        {
           lock(listStore)
           { 
              listStore.Clear();
               JobInfo[] jobs = JobInfo.GetJobs();
               foreach(JobInfo job in jobs)
                  AddJob(job);            
           }
        }

        TreeIter FindJob(JobInfo job)
        {
            TreeIter iter;
            listStore.GetIterFirst(out iter);
            while(!iter.Equals(TreeIter.Zero))
            {
               if(listStore.GetValue(iter, COLUMN_JOB_INFORMATION)==job)
               {
                  return iter;
               }
               listStore.IterNext(ref iter);
            }
            return TreeIter.Zero;
        }

        TreeIter FindJob(Gtk.TreeSelection selection)
        {
            Gtk.TreeModel model;
            Gtk.TreeIter iter;
            return (selection!=null && selection.GetSelected(out model, out iter)) ? iter : TreeIter.Zero;        
        }

        void HandleCursorChanged(object sender, EventArgs e)
        {
           lock(listStore)
           { 
               Gtk.TreeIter iter = FindJob(treeview1.Selection);
               if(!iter.Equals(TreeIter.Zero))
               {
                  JobInfo job = (JobInfo)listStore.GetValue(iter, COLUMN_JOB_INFORMATION);
                  buttonCancel.Sensitive = job.CancelationSupported;
               }
               else
               {
                  buttonCancel.Sensitive = false;
               }
           }
        }

        void HandleAdded (object sender, JobInformationEventArgs e)
        {
            Gtk.Application.Invoke(delegate { AddJob(e.JobInformation); });
        }

        void HandleRemoved (object sender, JobInformationEventArgs e)
        {
            Gtk.Application.Invoke(delegate { RemoveJob(e.JobInformation); });
        }

        void AddJob(JobInfo job)
        {
           lock(listStore)
           {
              Gtk.TreeIter iter = listStore.AppendValues(job, job.Name, job.Description, 0);
              job.ProgressChanged += HandleProgressChanged; 
           }
        }

        void RemoveJob(JobInfo job)
        {
           lock(listStore)
           { 
               TreeIter iter = FindJob(job);
               if(!iter.Equals(TreeIter.Zero))
                  listStore.Remove(ref iter);
           }
        }

        void HandleProgressChanged (object sender, JobInformationEventArgs e)
        {
            Gtk.Application.Invoke(delegate
            {
                lock(listStore)
                {
                   TreeIter iter = FindJob(e.JobInformation);
                   if(!iter.Equals(TreeIter.Zero))
                     listStore.SetValue(iter, COLUMN_STATUS, e.JobInformation.Progress);
                }
            });
        }

        protected void OnButtonCancelClicked(object sender, EventArgs e)
        {
           lock(listStore)
           { 
              Gtk.TreeIter iter = FindJob(treeview1.Selection);
              if(!iter.Equals(TreeIter.Zero))
              {
                 JobInfo job = listStore.GetValue(iter, COLUMN_JOB_INFORMATION) as JobInfo;
                 job.Cancel();
              }
            }
        }
    }

    #region Starter / Entry Point
    public class Factory : ComponentFactory
    {
        public override Type TypeOfInstance { get { return typeof(Activities); } }
        public override String MenuPath { get { return @"View\Infrastructure\Activities"; } }
        public override String Comment { get { return "Show a list of all activities"; } }
        public override Mode Options { get { return Mode.CloseOnHide; } }
        public override Gdk.Pixbuf Icon { get { return Gdk.Pixbuf.LoadFromResource ("Docking.Framework.Resources.Activities-16.png"); } }
    }
    #endregion
}

