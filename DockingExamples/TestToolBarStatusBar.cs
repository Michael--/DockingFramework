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
            mPush = new ToolButton("Push");
            mPush.Label = "Push";
            mPush.Clicked += (sender, e) => 
            {
                String text = String.Format("Hello {0} at {1}", ++mTextCounter, DateTime.Now.ToLongTimeString());
                uint id = ComponentManager.PushStatusbar(text);
                mStack.Push(id);
                UpdateMessageText();
            };

            mPop = new ToolButton("Pop");
            mPop.Label = "Pop";
            mPop.Clicked += (sender, e) => 
            {
                if (mStack.Count > 0)
                    ComponentManager.PopStatusbar(mStack.Pop ());
                UpdateMessageText();
            };
        }

        void IComponent.Save()
        {
        }


        void UpdateMessageText()
        {
            label3.Text = String.Format ("Messages pushed to status bar: {0}", mStack.Count);
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
            if (mPush == null || mPop == null)
                return;

            if (visible)
            {
                ComponentManager.AddToolItem (mPush);
                ComponentManager.AddToolItem (mPop);
            }
            else
            {
                ComponentManager.RemoveToolItem (mPush);
                ComponentManager.RemoveToolItem (mPop);
                while (mStack.Count > 0)
                    ComponentManager.PopStatusbar(mStack.Pop ());
            }
            UpdateMessageText();
        }
        
        #endregion

        #region varibables, properties
        ToolButton mPush;
        ToolButton mPop;
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

