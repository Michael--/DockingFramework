//
// DockFrameTopLevel.cs
//
// Author:
//   Lluis Sanchez Gual
//

//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//



using System;
using Gtk;

namespace Docking
{
	class DockFrameTopLevel: EventBox
	{
      // Currently, GTK (sometimes) throws Exceptions a la
      //    GLib.MissingIntPtrCtorException: GLib.Object subclass DockFrameTopLevel must provide a protected or public IntPtr ctor to support wrapping of native object handles.
      // We currently have no clue why it at runtime checks if such a constructor exists, and, if not, throws an exception.
      // We need to shed more light into this problem.
      // For this reason, this dummy, not implemented constructor has been added.
      // It currently has no implementation because we currently have no clue what the heck should be put into it.
      // We just want to be able to put a breakpoint here for debugging.
      // A hint might be:
      //    public class Gtk.Object
      //    {
      //       ...
      //       protected override IntPtr Raw { get; set; }
      //       ...
      public DockFrameTopLevel(IntPtr raw) : base(raw)
      {
         throw new Exception("unimplemented IntPtr constructor");
      }

      public DockFrameTopLevel() {}

		int x, y;
		
		public int X {
			get { return x; }
			set {
				x = value;
				if (Parent != null)
					Parent.QueueResize ();
			}
		}
		
		public int Y {
			get { return y; }
			set {
				y = value;
				if (Parent != null)
					Parent.QueueResize ();
			}
		}
	}

}
