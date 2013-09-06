//
// ImageButton.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
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
using MonoDevelop.Ide;

namespace Docking.Helper
{
	public class ImageButton: Gtk.EventBox
	{
		Gdk.Pixbuf image;
		Gdk.Pixbuf inactiveImage;
		Gtk.Image imageWidget;
		bool hasInactiveImage;
		bool hover;
		bool pressed;

      // Currently, GTK (sometimes) throws Exceptions a la
      //    GLib.MissingIntPtrCtorException: GLib.Object subclass ImageButton must provide a protected or public IntPtr ctor to support wrapping of native object handles.
      // We currently have no clue why it at runtime checks if such a constructor exists, and, if not, throws an exception.
      // We need to shed more light into this problem.
      // For this reason, this dummy, not implemented constructor has been added.
      // It currently has no implementation because we currently have no clue what the heck should be put into it.
      // We just want to be able to put a breakpoint here for debugging.
      public ImageButton(IntPtr raw)
      {
         throw new Exception("unimplemented IntPtr constructor");
      }

		public ImageButton ()
		{
			Events |= Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask | Gdk.EventMask.ButtonReleaseMask;
			VisibleWindow = false;
			imageWidget = new Gtk.Image ();
			imageWidget.Show ();
			Add (imageWidget);
		}

		public Gdk.Pixbuf Image {
			get { return image; }
			set {
				image = value;
				Gdk.Pixbuf oldInactive = null;
				if (!hasInactiveImage) {
					oldInactive = inactiveImage;
					inactiveImage = image != null ? ImageService.MakeTransparent (image, 0.5) : null;
				}
				LoadImage ();
				if (oldInactive != null)
					oldInactive.Dispose ();
			}
		}

		public Gdk.Pixbuf InactiveImage {
			get { return hasInactiveImage ? inactiveImage : null; }
			set {
				if (!hasInactiveImage && inactiveImage != null)
					inactiveImage.Dispose ();
				hasInactiveImage = true;
				inactiveImage = value;
				LoadImage ();
			}
		}

		protected override void OnDestroyed ()
		{
			if (!hasInactiveImage && inactiveImage != null)
				inactiveImage.Dispose ();
			base.OnDestroyed ();
		}

		void LoadImage ()
		{
			if (image != null) {
				if (hover)
					imageWidget.Pixbuf = image;
				else
					imageWidget.Pixbuf = inactiveImage;
			} else {
				imageWidget.Pixbuf = null;
			}
		}

		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			hover = true;
			LoadImage ();
			return base.OnEnterNotifyEvent (evnt);
		}

		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			hover = false;
			LoadImage ();
			return base.OnLeaveNotifyEvent (evnt);
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			pressed = image != null;
			return base.OnButtonPressEvent (evnt);
		}

		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			if (pressed && evnt.Button == 1 && new Gdk.Rectangle (0, 0, Allocation.Width, Allocation.Height).Contains ((int)evnt.X, (int)evnt.Y)) {
				hover = false;
				LoadImage ();
				if (Clicked != null)
					Clicked (this, EventArgs.Empty);
				return true;
			}
			return base.OnButtonReleaseEvent (evnt);
		}

		public event EventHandler Clicked;
	}
}

