using System;
using Docking.Components;
using Docking;
using System.IO;

namespace Examples
{
    [System.ComponentModel.ToolboxItem(false)]
    public partial class TextViewer : Gtk.Bin, IComponent, IFileOpen
    {       
        #region implement IComponent
        public ComponentManager ComponentManager { get; set; }
        
        void IComponent.Loaded(DockItem item)
        {
        }
        
        void IComponent.Save()
        {
        }
        
        #endregion

        #region implement IFileOpen
        String IFileOpen.TryOpenFile(String filename)
        {
            if (!File.Exists(filename))
                return null;

            String ext = System.IO.Path.GetExtension(filename);
            
            if (ext.ToLower() == ".txt")
                return "text file";

            return null;
        }
        
        void IFileOpen.OpenFile(String filename)
        {
            if (!File.Exists(filename))
                return;

            using (System.IO.StreamReader reader = new System.IO.StreamReader(filename))
            {
                if (reader != null)
                {
                    string txt = reader.ReadToEnd();
                    textview.Buffer.Clear();
                    textview.Buffer.InsertAtCursor(txt);
                }
            }
        }
        #endregion

        #region MAIN
        public TextViewer()
        {
            this.Build();
        }
        #endregion
    }

    public class ExampleTextViewerFactory : ComponentFactory
    {
        public override Type TypeOfInstance { get { return typeof(TextViewer); } }
        public override String MenuPath { get { return @"View\Examples\TextViewer"; } }
        public override String Comment { get { return "Load *.txt files"; } }
        public override Mode Options { get { return Mode.CloseOnHide; } }
        public override Gdk.Pixbuf Icon { get { return Gdk.Pixbuf.LoadFromResource ("Examples.HelloWorld-16.png"); } }
    }

}

