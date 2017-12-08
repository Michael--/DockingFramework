//
// MouseTracker.cs
//
// Author:
//       Jason Smith <jason.smith@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using Gtk;

namespace Docking.Helper
{
   public class MouseTracker
   {
      public bool Hovered { get; private set; }
      public Gdk.Point MousePosition { get; private set; }

      /// <summary>
      /// Enable MouseMoved events
      /// </summary>
      public bool MotionEvents
      {
         get { return mTrackMotion; }
         set
         {
            if (value != mTrackMotion)
            {
               mTrackMotion = value;
               UpdateEventFlags();
            }
         }
      }

      /// <summary>
      /// Enable EnterNotify and LeaveNotify events
      /// </summary>
      public bool EnterLeaveEvents
      {
         get { return mEnterLeave; }
         set
         {
            if (value != mEnterLeave)
            {
               mEnterLeave = value;
               UpdateEventFlags();
            }
         }
      }

      /// <summary>
      /// Enable ButtonPressed and ButtonReleased events
      /// </summary>
      public bool ButtonPressedEvents
      {
         get { return mButtonPressed; }
         set
         {
            if (value != mButtonPressed)
            {
               mButtonPressed = value;
               UpdateEventFlags();
            }
         }
      }

      public event MotionNotifyEventHandler MouseMoved;
      public event EnterNotifyEventHandler EnterNotify;
      public event LeaveNotifyEventHandler LeaveNotify;
      public event ButtonPressEventHandler ButtonPressed;
      public event ButtonReleaseEventHandler ButtonReleased;

      bool mEnterLeave = false;
      bool mTrackMotion = false;
      bool mButtonPressed = false;
      Gtk.Widget mOwner;

      public MouseTracker(Gtk.Widget owner)
      {
         this.mOwner = owner;
         Hovered = false;
         MousePosition = new Gdk.Point(0, 0);
         UpdateEventFlags();

         owner.MotionNotifyEvent += (o, args) =>
         {
            MousePosition = new Gdk.Point((int)args.Event.X, (int)args.Event.Y);
            if (MouseMoved != null)
               MouseMoved(this, args);
         };

         owner.EnterNotifyEvent += (o, args) =>
         {
            Hovered = true;
            if (EnterNotify != null)
               EnterNotify(this, args);
         };

         owner.LeaveNotifyEvent += (o, args) =>
         {
            Hovered = false;
            if (LeaveNotify != null)
               LeaveNotify(this, args);
         };

         owner.ButtonPressEvent += (o, args) =>
         {
            if (ButtonPressed != null)
               ButtonPressed(this, args);
         };

         owner.ButtonReleaseEvent += (o, args) =>
         {
            if (ButtonPressed != null)
               ButtonReleased(this, args);
         };
      }

      void UpdateEventFlags()
      {
         if (MotionEvents)
            mOwner.Events |= Gdk.EventMask.PointerMotionMask;
         else
            mOwner.Events &= ~Gdk.EventMask.PointerMotionMask;

         if (mEnterLeave)
            mOwner.Events |= Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask;
         else
            mOwner.Events &= ~(Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask);

         if (mButtonPressed)
            mOwner.Events |= Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask;
         else
            mOwner.Events &= ~(Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask);
      }
   }
}

