using System;
using Docking.Components;
using Docking.Tools;
using System.Collections.Generic;

namespace Docking.Components
{
    [System.ComponentModel.ToolboxItem(false)]
    public partial class Activities : Gtk.Bin, IComponent
    {
        public Activities ()
        {
            this.Build();
            this.Name = "Activities";

            Gtk.TreeViewColumn activityColumn = new Gtk.TreeViewColumn ();
            activityColumn.Title = "Activity";
            activityColumn.Resizable = true;
            activityColumn.Sizing = Gtk.TreeViewColumnSizing.Fixed;
            activityColumn.FixedWidth = 100;

            Gtk.TreeViewColumn desciptionColumn = new Gtk.TreeViewColumn ();
            desciptionColumn.Title = "Desciption";
            desciptionColumn.Resizable = true;
            desciptionColumn.Sizing = Gtk.TreeViewColumnSizing.Fixed;
            desciptionColumn.FixedWidth = 200;

            Gtk.TreeViewColumn statusColumn = new Gtk.TreeViewColumn ();
            statusColumn.Title = "Status";
            statusColumn.Resizable = true;
            statusColumn.Sizing = Gtk.TreeViewColumnSizing.Fixed;
            statusColumn.FixedWidth = 100;

            // Add the columns to the TreeView
            treeview1.AppendColumn (activityColumn);
            treeview1.AppendColumn (desciptionColumn);
            treeview1.AppendColumn (statusColumn);

            // Create the text cells that will display the content
            Gtk.CellRendererText componentsCell = new Gtk.CellRendererText ();
            Gtk.CellRendererText descriptionCell = new Gtk.CellRendererText ();
            Gtk.CellRendererProgress statusCell = new Gtk.CellRendererProgress ();

            activityColumn.PackStart (componentsCell, true);
            desciptionColumn.PackStart (descriptionCell, true);
            statusColumn.PackStart (statusCell, true);

            activityColumn.AddAttribute (componentsCell, "text", activityIndex);
            desciptionColumn.AddAttribute (descriptionCell, "text", descriptionIndex);
            statusColumn.AddAttribute (statusCell, "value", statusIndex);

            // Create a model that will hold the content, assign the model to the TreeView
            listStore = new Gtk.ListStore (typeof(JobInformation), typeof (string), typeof (string), typeof (int));
            treeview1.Model = listStore;
        }
        Gtk.ListStore listStore;
        const int jobInformationIndex = 0;
        const int activityIndex = 1;
        const int descriptionIndex = 2;
        const int statusIndex = 3;

        #region implement IComponent
        public ComponentManager ComponentManager { get; set; }

        void IComponent.Loaded(DockItem item)
        {
            // note: job events could be happen while initializing
            JobInformation.Added += HandleAdded;
            JobInformation.Removed += HandleRemoved;
            Initialize();
        }

        void IComponent.Save()
        {
        }
        #endregion

        void Initialize()
        {
            JobInformation[] jobs = JobInformation.GetJobs();
            foreach(JobInformation job in jobs)
                AddJob(job);

            treeview1.CursorChanged += HandleCursorChanged;
        }

        void HandleCursorChanged(object sender, EventArgs e)
        {
            Gtk.TreeSelection selection = (sender as Gtk.TreeView).Selection;

            Gtk.TreeModel model;
            Gtk.TreeIter iter;
            if (selection.GetSelected(out model, out iter))
            {
                lock (TreeIterHelper)
                {
                    JobInformation job = (JobInformation) listStore.GetValue(iter, jobInformationIndex);
                    buttonCancel.Sensitive = job.CancelationSupported;
                }
            }
            else
                buttonCancel.Sensitive = false;
        }

        void HandleAdded (object sender, JobInformationEventArgs e)
        {
            Gtk.Application.Invoke(delegate { AddJob(e.JobInformation); });
        }

        void HandleRemoved (object sender, JobInformationEventArgs e)
        {
            Gtk.Application.Invoke(delegate { RemoveJob(e.JobInformation); });
        }

        /// <summary>
        /// Normally I would add a tag to each row, but as a GTK beginner I currently
        /// don't know how.
        /// I will refacture when I found the solution I looking for.
        /// </summary>
        Dictionary<JobInformation, Gtk.TreeIter> TreeIterHelper = new Dictionary<JobInformation, Gtk.TreeIter>();

        void AddJob(JobInformation job)
        {
            Gtk.TreeIter iter = listStore.Append();
            listStore.SetValue(iter, jobInformationIndex, job);
            listStore.SetValue(iter, activityIndex, job.Name);
            listStore.SetValue(iter, descriptionIndex, job.Description);
            listStore.SetValue(iter, statusIndex, 0);

            lock (TreeIterHelper)
            {
                TreeIterHelper.Add(job, iter);
            }
            job.ProgressChanged += HandleProgressChanged;
        }

        void RemoveJob(JobInformation job)
        {
            lock (TreeIterHelper)
            {
                Gtk.TreeIter iter;
                if (TreeIterHelper.TryGetValue(job, out iter))
                {
                    TreeIterHelper.Remove(job);
                    listStore.Remove(ref iter);
                }
            }
            HandleCursorChanged(treeview1, null);
        }

        void HandleProgressChanged (object sender, JobInformationEventArgs e)
        {
            Gtk.Application.Invoke(delegate
            {
                int progress = e.JobInformation.Progress;
                lock(TreeIterHelper)
                {
                    Gtk.TreeIter iter;
                    if (TreeIterHelper.TryGetValue(e.JobInformation, out iter))
                    {
                        listStore.SetValue(iter, statusIndex, progress);
                    }
                }
            });
        }

        protected void OnButtonCancelClicked(object sender, EventArgs e)
        {
            Gtk.TreeSelection selection = treeview1.Selection;
            Gtk.TreeModel model;
            Gtk.TreeIter iter;
            if (selection.GetSelected(out model, out iter))
            {
                lock (TreeIterHelper)
                {
                    JobInformation job = (JobInformation) listStore.GetValue(iter, jobInformationIndex);
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
        public override Gdk.Pixbuf Icon { get { return Gdk.Pixbuf.LoadFromResource ("Docking.Framework.Components.Activities-16.png"); } }
    }
    #endregion
}

