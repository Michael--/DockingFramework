using System;
using Docking.Components;
using Docking.Tools;
using System.Collections.Generic;

namespace Docking.Components
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class Activities : Gtk.Bin, IComponent
    {
        public Activities ()
        {
            this.Build();

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

            activityColumn.AddAttribute (componentsCell, "text", 0);
            desciptionColumn.AddAttribute (descriptionCell, "text", 1);
            statusColumn.AddAttribute (statusCell, "value", 2);

            // Create a model that will hold the content, assign the model to the TreeView
            listStore = new Gtk.ListStore (typeof (string), typeof (string), typeof (int));
            treeview1.Model = listStore;
        }
        Gtk.ListStore listStore;

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
        Dictionary<JobInformation, Gtk.TreeIter> tagHelper = new Dictionary<JobInformation, Gtk.TreeIter>();

        void AddJob(JobInformation job)
        {
            List<String> row = new List<string>();
            row.Add(job.Name);
            row.Add("todo: add description here");
            row.Add(job.Progress.ToString());
            Gtk.TreeIter it = listStore.AppendValues(row.ToArray());
            lock (tagHelper)
                tagHelper.Add(job, it);

            job.ProgressChanged += HandleProgressChanged;
        }

        void RemoveJob(JobInformation job)
        {
            lock (tagHelper)
            {
                Gtk.TreeIter it;
                if (tagHelper.TryGetValue(job, out it))
                {
                    listStore.Remove(ref it);
                    tagHelper.Remove(job);
                }
            }
        }

        void HandleProgressChanged (object sender, JobInformationEventArgs e)
        {
            Gtk.Application.Invoke(delegate 
            {
                int progress = e.JobInformation.Progress;
                lock(tagHelper)
                {
                    Gtk.TreeIter iter;
                    if (tagHelper.TryGetValue(e.JobInformation, out iter))
                    {
                        listStore.SetValue(iter, 2, progress);
                    }
                }
            });
        }
       
    }
    
    public class Factory : ComponentFactory
    {
        public override Type TypeOfInstance { get { return typeof(Activities); } }
        public override String MenuPath { get { return @"Components\Activities"; } }
        public override String Comment { get { return "Show a list of all activities"; } }
        public override Mode Options { get { return Mode.CloseOnHide; } }
    }
}

