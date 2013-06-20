using System;
using Docking.Components;
using Docking.Tools;
using System.Collections.Generic;

namespace Docking.Components
{
    [System.ComponentModel.ToolboxItem(false)]
    public partial class Activities : Gtk.Bin, IComponent, ILocalizable
    {
        public Activities ()
        {
            this.Build();

            Gtk.TreeViewColumn activityColumn = new TreeViewColumnLocalized() { Title = "Activity", Sizing = Gtk.TreeViewColumnSizing.Fixed, FixedWidth = 100, Resizable = true };
            Gtk.TreeViewColumn descriptionColumn = new TreeViewColumnLocalized() { Title = "Description", Sizing = Gtk.TreeViewColumnSizing.Fixed, FixedWidth = 200, Resizable = true };
            Gtk.TreeViewColumn statusColumn = new TreeViewColumnLocalized() { Title = "Status", Sizing = Gtk.TreeViewColumnSizing.Fixed, FixedWidth = 100, Resizable = true };

            // Add the columns to the TreeView
            treeview1.AppendColumn (activityColumn);
            treeview1.AppendColumn (descriptionColumn);
            treeview1.AppendColumn (statusColumn);

            // Create the text cells that will display the content
            Gtk.CellRendererText componentsCell = new Gtk.CellRendererText ();
            Gtk.CellRendererText descriptionCell = new Gtk.CellRendererText ();
            Gtk.CellRendererProgress statusCell = new Gtk.CellRendererProgress ();

            activityColumn.PackStart (componentsCell, true);
            descriptionColumn.PackStart (descriptionCell, true);
            statusColumn.PackStart (statusCell, true);

            activityColumn.AddAttribute (componentsCell, "text", activityIndex);
            descriptionColumn.AddAttribute (descriptionCell, "text", descriptionIndex);
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

        #region implement  ILocalizable

        // set the displayed name of the widget
        string ILocalizable.Name { get { return "Activities"; } }

        void ILocalizable.LocalizationChanged(Docking.DockItem item)
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

            // if (!job.ProgressSupported) ...
            // -1 as ProgressValue will result in an empty progress bar display, but only if no other threads with active progress exist
            // in that case the progress copied from any other and totally flickering
            // also a lot of GLib-GObject-WARNING at runtime occur
            // TODO instead of displaying an empty progress bar, make it invisible or show a text widget saying something like "continuous", telling the user that this thread is never finished but instead always-working
            // unfortunately at statusIndex a CellRendererProgress is installed always in any row
            // we would need a special CellRenderer which could display its content as Text or Progress 

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
                        this.treeview1.QueueDraw(); // TODO don't know why this explicit call is necessary. without it, repainting of the progress bars is missing :((((
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

