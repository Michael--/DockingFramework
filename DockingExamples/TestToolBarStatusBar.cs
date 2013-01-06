using System;
using Docking.Components;
using Docking;
using Gtk;
using System.Collections.Generic;

namespace Examples.TestToolAndStatusBar
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class TestToolBarStatusBar : Gtk.Bin, IComponent
    {
        public TestToolBarStatusBar ()
        {
            this.Build ();
        }

        #region implement IComponent
        public ComponentManager ComponentManager { get; set; }

        void IComponent.Loaded(DockItem item)
        {
            ToolButton push = new ToolButton("Push");
            push.Label = "Push";
            push.Clicked += (sender, e) => 
            {
                String text = String.Format("Hello {0} at {1}", ++mTextCounter, DateTime.Now.ToLongTimeString());
                uint id = ComponentManager.PushStatusbar(text);
                mStack.Push(id);
            };

            ToolButton pop = new ToolButton("Pop");
            pop.Label = "Pop";
            pop.Clicked += (sender, e) => 
            {
                if (mStack.Count > 0)
                    ComponentManager.PopStatusbar(mStack.Pop ());
            };

            ComponentManager.AddToolItem(push);
            ComponentManager.AddToolItem(pop);
        }

        int mTextCounter = 0;
        Stack<uint> mStack = new Stack<uint>();

        
        void IComponent.Save()
        {
        }
        #endregion

    }

    #region Starter / Entry Point
    
    public class Factory : ComponentFactory
    {
        public override Type TypeOfInstance { get { return typeof(TestToolBarStatusBar); } }
        public override String MenuPath { get { return @"Components\Examples\Test Tool- and Status bar"; } }
        public override String Comment { get { return "Example using tool bat and status bar"; } }
    }
    
    #endregion

}

