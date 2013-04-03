using System;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting;

namespace Docking.Components
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ScriptEditor : Gtk.Bin, IScript, IComponent
	{
		#region MAIN
		public ScriptEditor()
		{
			this.Build();
			this.Name = "Script Editor";

			this.textSource.Buffer.Changed += ContentChanged;
		}

		void ContentChanged (object sender, EventArgs e)
		{
			Gtk.TextIter istart = textSource.Buffer.StartIter;
			Gtk.TextIter iend = textSource.Buffer.EndIter;
			string script = this.textSource.Buffer.GetText(istart, iend, true);

            CompiledCode code = null;
            try
            {
                code = ComponentManager.Compile(script);
                Message("");
            }
            catch (SyntaxErrorException ex)
            {
                Message(string.Format("Line {0}/{1}: {2}", ex.Line, ex.Column, ex.Message));
            }
            catch (Exception ex)
            {
                Message(ex.Message);
            }
			finally
			{
				if (ScriptChangedHandler != null)
                    ScriptChangedHandler(new ScriptChangedEventArgs(m_Reference, script, code));
			}
		}

        void Message(string msg)
        {
			textMessage.Buffer.Clear();
            Gtk.TextIter iter = textMessage.Buffer.EndIter;
            textMessage.Buffer.Insert(ref iter, msg);
        }

		#endregion

		#region implement IScript
		
		void IScript.SetScript(object reference, string script)
		{
            if (m_Reference == reference)
                return;

            m_Reference = reference;
			textSource.Buffer.Clear();
            if (script != null)
            {
                textSource.Sensitive = true;
                Gtk.TextIter iter = textSource.Buffer.EndIter;
                textSource.Buffer.Insert(ref iter, script);
            }
            else
            {
                textSource.Sensitive = false;
            }
		}

        void IScript.SetMessage(object reference, string msg)
        {
            if (m_Reference != reference)
                return;

            Message(msg);
        }
		
		/// <summary>
		/// Get an event on any property changes
		/// </summary>
		ScriptChangedEventHandler IScript.ScriptChanged
		{
			get { return ScriptChangedHandler; }
			set { ScriptChangedHandler = value; }
		}

        object m_Reference = null;
		private ScriptChangedEventHandler ScriptChangedHandler;
		
		#endregion


		#region implement IComponent
		public ComponentManager ComponentManager { get; set; }
		
		void IComponent.Loaded(DockItem item)
		{
            mPersistence = (ScriptPersistence)ComponentManager.LoadObject("ScriptEditor", typeof(ScriptPersistence));
            if (mPersistence == null)
                mPersistence = new ScriptPersistence();

            // set vpaned position delayed when really possible, TODO: find event called only once, better as ExposeEvent
            vpaned1.ExposeEvent += new Gtk.ExposeEventHandler(vpaned1_ExposeEvent);
		}

        bool mSetPositionOnlyOnce = true;
        ScriptPersistence mPersistence;

        void vpaned1_ExposeEvent(object o, Gtk.ExposeEventArgs args)
        {
            if (mSetPositionOnlyOnce)
            {
                mSetPositionOnlyOnce = false;

                // first calculate an useful position
                if (mPersistence.VPanedPosition == 0)
                {
                    int width, height;
                    this.GdkWindow.GetSize(out width, out height);
                    mPersistence.VPanedPosition = height - 50;
                    if (mPersistence.VPanedPosition <= 0)
                        mPersistence.VPanedPosition = height;
                }

                vpaned1.Position = mPersistence.VPanedPosition;
            }
        }

		void IComponent.Save()
		{
            mPersistence.VPanedPosition = vpaned1.Position;
            ComponentManager.SaveObject("ScriptEditor", mPersistence);
		}
		#endregion
	}

    public class ScriptPersistence
    {
        public int VPanedPosition { get { return m_VPaned_Position; } set { m_VPaned_Position = value; } }
        int m_VPaned_Position;
    }

	public class ScriptChangedEventArgs : EventArgs
	{
		public ScriptChangedEventArgs(object reference, string script, CompiledCode code)
		{
            Reference = reference;
			Script = script;
            Code = code;
		}
        public object Reference { get; private set; }
		public string Script { get; private set; }
        public CompiledCode Code { get; private set; }
	}
	
	
	public delegate void ScriptChangedEventHandler(ScriptChangedEventArgs e);
	
	public interface IScript
	{
		/// <summary>
		/// Sets the current script to display and edit
		/// </summary>
		void SetScript(object reference, string script);


        /// <summary>
        /// Show a message in the script editor message window
        /// </summary>
        void SetMessage(object reference, string msg);
		
		/// <summary>
		/// Get an event on any property changes
		/// </summary>
		ScriptChangedEventHandler ScriptChanged { get; set; }
	}

	#region Starter / Entry Point
	
	public class ScriptEditorFactory : ComponentFactory
	{
		public override Type TypeOfInstance { get { return typeof(ScriptEditor); } }
		public override String MenuPath { get { return @"View\Infrastructure\Script Editor"; } }
		public override String Comment { get { return "Show selected script"; } }
		public override Mode Options { get { return Mode.CloseOnHide; } }
		public override Gdk.Pixbuf Icon { get { return Gdk.Pixbuf.LoadFromResource ("Docking.Framework.Components.Messages-16.png"); } }
	}
	#endregion

}

