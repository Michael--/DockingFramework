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
            InitToolbarButtons();
        }

        void InitToolbarButtons()
        {
            mPush = new ToolButton("Push");
            mPush.Label = "Push";
            mPush.StockId = Stock.Add;
            mPush.TooltipText = "Push a new message to status bar";
            mPush.Clicked += (sender, e) => 
            {
                String text = String.Format("Hello {0} at {1}", ++mTextCounter, DateTime.Now.ToLongTimeString());
                uint id = ComponentManager.PushStatusbar(text);
                mStack.Push(id);
                UpdateMessageText();
            };
            
            mPop = new ToolButton("Pop");
            mPop.Label = "Pop";
            mPop.StockId = Stock.Remove;
            mPop.TooltipText = "Pop newest message from status bar";
            mPop.Clicked += (sender, e) => 
            {
                if (mStack.Count > 0)
                    ComponentManager.PopStatusbar(mStack.Pop ());
                UpdateMessageText();
            };
        }

        void UpdateMessageText()
        {
            label3.Text = String.Format ("Messages pushed to status bar: {0}", mStack.Count);
        }


        #region implement IComponent
        public ComponentManager ComponentManager { get; set; }

        void IComponent.Loaded(DockItem item)
        {
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
            if (visible && !mAdded)
            {
                ComponentManager.AddToolItem (mPush);
                ComponentManager.AddToolItem (mPop);
                mAdded = true;
            }
            else if (mAdded)
            {
                ComponentManager.RemoveToolItem (mPush);
                ComponentManager.RemoveToolItem (mPop);
                while (mStack.Count > 0)
                    ComponentManager.PopStatusbar(mStack.Pop ());
                mAdded = false;
            }
            UpdateMessageText();
        }
        
        #endregion

        #region varibables, properties
        ToolButton mPush;
        ToolButton mPop;
        bool mAdded = false;
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

