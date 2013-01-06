using System;
using Docking.Components;
using Docking;
using Gtk;
using System.Collections.Generic;

namespace Examples.TestToolAndStatusBar
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class TestToolBarStatusBar : Gtk.Bin, IComponent, IComponentInteract
    {
        public TestToolBarStatusBar ()
        {
            this.Build ();
        }

        #region implement IComponent
        public ComponentManager ComponentManager { get; set; }

        void IComponent.Loaded(DockItem item)
        {
            push = new ToolButton("Push");
            push.Label = "Push";
            push.Clicked += (sender, e) => 
            {
                String text = String.Format("Hello {0} at {1}", ++mTextCounter, DateTime.Now.ToLongTimeString());
                uint id = ComponentManager.PushStatusbar(text);
                mStack.Push(id);
            };

            pop = new ToolButton("Pop");
            pop.Label = "Pop";
            pop.Clicked += (sender, e) => 
            {
                if (mStack.Count > 0)
                    ComponentManager.PopStatusbar(mStack.Pop ());
            };
        }

        void IComponent.Save()
        {
        }
        #endregion

        #region implement IComponentInteract
        void IComponentInteract.Added(object item)
        {
        }
        
        void IComponentInteract.Removed(object item)
        {
        }
        
        void IComponentInteract.Visible(object item, bool visible)
        {
            if (push == null || pop == null)
                return;

            if (visible)
            {
                ComponentManager.AddToolItem (push);
                ComponentManager.AddToolItem (pop);
            }
            else
            {
                ComponentManager.RemoveToolItem (push);
                ComponentManager.RemoveToolItem (pop);
                while (mStack.Count > 0)
                    ComponentManager.PopStatusbar(mStack.Pop ());
            }
        }
        
        #endregion

        #region varibables, properties
        ToolButton push, pop;
        int mTextCounter = 0;
        Stack<uint> mStack = new Stack<uint>();
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

