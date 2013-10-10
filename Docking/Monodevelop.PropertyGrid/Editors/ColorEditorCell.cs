//
// BooleanEditorCell.cs
//
// Author:
//   Lluis Sanchez Gual
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
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
using System.ComponentModel;
using Docking.Helper;

namespace MonoDevelop.Components.PropertyGrid.PropertyEditors
{
	[PropertyEditorType (typeof (System.Drawing.Color))]
	public class ColorEditorCell: PropertyEditorCell 
	{
		const int ColorBoxSize = 16;
		const int ColorBoxSpacing = 3;
		
		public override void GetSize (int availableWidth, out int width, out int height)
		{
			base.GetSize (availableWidth - ColorBoxSize - ColorBoxSpacing, out width, out height);
			width += ColorBoxSize + ColorBoxSpacing;
			if (height < ColorBoxSize) height = ColorBoxSize;
		}
		
		protected override string GetValueText ()
		{
			System.Drawing.Color color = (System.Drawing.Color) Value;
			//TODO: dropdown known color selector so this does something
			if (color.IsKnownColor)
				return color.Name;
			else if (color.IsEmpty)
				return "";
			else
                return String.Format("RGBA {0},{1},{2},{3}", color.R, color.G, color.B, color.A);
		}
		
		public override void Render (Gdk.Drawable window, Gdk.Rectangle bounds, Gtk.StateType state)
		{
	   	int yd = (bounds.Height - ColorBoxSize) * 4 / 5;
         int width = ColorBoxSize * 2 - 1;
         int heigth = ColorBoxSize - 1;

         Cairo.Context cr = Gdk.CairoHelper.Create(window);

         // black cross to show alpha
         cr.LineWidth = 2;
         cr.SetSourceColor(new Cairo.Color(0, 0, 0));
         cr.MoveTo(bounds.X, bounds.Y + yd);
         cr.LineTo(bounds.X + width, bounds.Y + heigth);
         cr.MoveTo(bounds.X, bounds.Y + yd + heigth);
         cr.LineTo(bounds.X + width, bounds.Y);
         cr.Stroke();

         // rect around, also only visible with alpha
         cr.Rectangle(bounds.X, bounds.Y + yd, width, heigth);
         cr.Stroke();

         // fill with color
         cr.SetSourceColor(GetCairoColor());
         cr.Rectangle(bounds.X, bounds.Y + yd, width, heigth);
         cr.Fill();

         cr.Dispose();

         bounds.X += width + ColorBoxSpacing;
         bounds.Width -= heigth + ColorBoxSpacing;

			base.Render (window, bounds, state);
		}
		
		private Gdk.Color GetColor()
		{
			System.Drawing.Color color = (System.Drawing.Color) Value;
			return new Gdk.Color (color.R, color.G, color.B);
		}

        private Cairo.Color GetCairoColor()
        {
            System.Drawing.Color color = (System.Drawing.Color)Value;
            return new Cairo.Color(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        }

		protected override IPropertyEditor CreateEditor (Gdk.Rectangle cell_area, Gtk.StateType state)
		{
			return new ColorEditor ();
		}
	}
	
	public class ColorEditor : Gtk.ColorButton, IPropertyEditor
	{
		public void Initialize (EditSession session)
		{
			if (session.Property.PropertyType != typeof(System.Drawing.Color))
				throw new ApplicationException ("Color editor does not support editing values of type " + session.Property.PropertyType);
            this.UseAlpha = true;
		}
		
		public object Value { 
			get {
				int red = (int) (255 * (float) Color.Red / ushort.MaxValue);
				int green = (int) (255 * (float) Color.Green / ushort.MaxValue);
				int blue = (int) (255 * (float) Color.Blue / ushort.MaxValue);
                int alpha = (int)(255 * (float)Alpha / ushort.MaxValue);
				return System.Drawing.Color.FromArgb (alpha, red, green, blue);
			}
			set {
				System.Drawing.Color color = (System.Drawing.Color) value;
				Color = new Gdk.Color (color.R, color.G, color.B);
                Alpha = (ushort)(color.A * ushort.MaxValue / 255.0f);
			}
		}
		
		protected override void OnColorSet ()
		{
			base.OnColorSet ();
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
		}

		public event EventHandler ValueChanged;
	}
}
