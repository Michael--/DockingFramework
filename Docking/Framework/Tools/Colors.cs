using System.Drawing;
using System.Xml.Serialization;
using System;

namespace Docking.Tools
{
    // This class converts between the various color classes brought in by the libraries...
    //    from C#:
    //       System.Drawing.Color
    //    from GTK#:
    //       Gdk.Color
    //       Gtk.Color
    //       Cairo.Color
    //    or even:
    //       UInt32, where the 4 bytes correspond to R, G, B, A
    // The sad thing is that each color class decides to use its own scaling:
    // some use the range 0..255, some 0..65535, some 0.0...1.0
    public static class ColorConverter
    {
        public static UInt32 RGBA_to_UInt32(byte r, byte g, byte b, byte a)
        {
           return ( ((UInt32) r) << 24 )
                | ( ((UInt32) g) << 16 )	
                | ( ((UInt32) b) <<  8 )	
                | ( ((UInt32) a) <<  0 );
        }

        public static void UInt32_to_RGBA(UInt32 rgba, out byte r, out byte g, out byte b, out byte a)
        {
           r = (byte)( (rgba>>24) & 0xFF );
           g = (byte)( (rgba>>16) & 0xFF );
           b = (byte)( (rgba>> 8) & 0xFF );
           a = (byte)( (rgba>> 0) & 0xFF );
        }

		public static string RGB_to_RGBString(byte r, byte g, byte b)
		{
			return string.Format("#{0:X2}{1:X2}{2:X2}", r, g, b);
		}

		public static string RGBA_to_RGBAString(byte r, byte g, byte b, byte a)
		{
			return a==0xFF ? string.Format("#{0:X2}{1:X2}{2:X2}",       r, g, b   )
				            : string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", r, g, b, a);
		}

      public static string Color_to_RGBAString(System.Drawing.Color color)
      {
         return RGBA_to_RGBAString(color.R, color.G, color.B, color.A);
      }

		public static string UInt32_to_RGBString(UInt32 rgba)
		{
			byte r, g, b, a;
			UInt32_to_RGBA(rgba, out r, out g, out b, out a);
			return RGB_to_RGBString(r, g, b);
		}

		public static string UInt32_to_RGBAString(UInt32 rgba)
		{
			byte r, g, b, a;
			UInt32_to_RGBA(rgba, out r, out g, out b, out a);
			return RGBA_to_RGBAString(r, g, b, a);
		}

		static int hexnibble(char c)
		{
		   if(c >= '0' && c <= '9')
				return c - '0';
		   if(c >= 'A' && c <= 'F')
				return (c - 'A')+10;
    		if(c >= 'a' && c <= 'f')
				return (c - 'a')+10;
			return -1;
        }

		public static bool RGBAString_to_RGBA(string s, out byte r, out byte g, out byte b, out byte a)
		{
			r = g = b = 0;
			a = 0xFF;

         if((s.Length!=7 && s.Length!=9) || s[0]!='#')
            return false;

         int n1 = hexnibble(s[1]);
         int n2 = hexnibble(s[2]);
         if(n1<0 || n2<0) return false;
         r = (byte)(n1*16+n2);

         n1 = hexnibble(s[3]);
         n2 = hexnibble(s[4]);
         if(n1<0 || n2<0) return false;
         g = (byte)(n1*16+n2);

         n1 = hexnibble(s[5]);
         n2 = hexnibble(s[6]);
         if(n1<0 || n2<0) return false;
         b = (byte)(n1*16+n2);

         if(s.Length==9)
         {
            n1 = hexnibble(s[7]);
            n2 = hexnibble(s[8]);
            if(n1<0 || n2<0) return false;
            a = (byte)(n1*16+n2);
         }

         return true;
		}

      public static bool RGBAString_to_Color(string s, out System.Drawing.Color color)
      {
         byte r, g, b, a;
         bool ok = RGBAString_to_RGBA(s, out r, out g, out b, out a);
         color = ok ? System.Drawing.Color.FromArgb(a, r, g, b) : System.Drawing.Color.Pink;
         return ok;
      }

		public static bool RGBAString_to_UInt32(string s, out UInt32 rgba)
		{
			byte r, g, b, a;
			bool ok = RGBAString_to_RGBA(s, out r, out g, out b, out a);
         rgba = RGBA_to_UInt32(r, g, b, a);
			return ok;
		}

        // TODO maybe inject this function into existing class System.Drawing.Color?
        // Strangely, Microsoft has decided to offer System.Drawing.Color.FromArgb(...) instead of .FromRgba(...)
        public static System.Drawing.Color RGBA_to_SystemDrawingColor(UInt32 rgba)
        {
           byte r, g, b, a;
           UInt32_to_RGBA(rgba, out r, out g, out b, out a);
           return System.Drawing.Color.FromArgb(a, r, g, b); 
        }

        // TODO maybe inject this function into existing class Cairo.Color?
        public static Cairo.Color RGBA_to_CairoColor(UInt32 rgba)
        {
           byte r, g, b, a;
           UInt32_to_RGBA(rgba, out r, out g, out b, out a);
           return new Cairo.Color(r/255f, g/255f, b/255f, a/255f);
        }

        // TODO maybe inject this function into existing class Gdk.Color?
        public static Gdk.Color RGBA_to_GdkColor(UInt32 rgba)
        {
           byte r, g, b, a;
           UInt32_to_RGBA(rgba, out r, out g, out b, out a);
           return new Gdk.Color(r, g, b);
        }

        public static Cairo.Color ToCairo(this Gtk.ColorButton button)
        {
           return new Cairo.Color(button.Color.Red/65535f, button.Color.Green/65535f, button.Color.Blue/65535f, button.Alpha/65535f);
        }

        public static void ToRGBA(this Gtk.ColorButton button, out byte r, out byte g, out byte b, out byte a)
        {
           r = (byte) (button.Color.Red  >>8);
           g = (byte) (button.Color.Green>>8);
           b = (byte) (button.Color.Blue >>8);
           a = (byte) (button.Alpha      >>8);
        }

        public static System.Drawing.Color ToSystemDrawingColor(this Gtk.ColorButton button)
        {
           byte r, g, b, a;
           button.ToRGBA(out r, out g, out b, out a);
           return System.Drawing.Color.FromArgb(a, r, g, b);
        }

        public static void FromRGBA(this Gtk.ColorButton button, byte r, byte g, byte b, byte a)
        {
           button.Color = new Gdk.Color(r, g, b);
           button.Alpha = (ushort)(a<<8);
        }

        public static void FromSystemDrawingColor(this Gtk.ColorButton button, System.Drawing.Color color)
        {
           button.Color = color.ToGdk();
           button.Alpha = (ushort)(color.A<<8);
        }

		public static void FromUInt32(this Gtk.ColorButton button, UInt32 rgba)
		{
			byte r, g, b, a;
			UInt32_to_RGBA(rgba, out r, out g, out b, out a);
			button.Color = new Gdk.Color(r, g, b);
			button.Alpha = (ushort)(a<<8);
		}

        public static void FromRGB(this Gtk.ColorButton button, byte r, byte g, byte b)
        {
           button.Color = new Gdk.Color(r, g, b);
        }

        public static Cairo.Color ToCairo(this System.Drawing.Color color)
        {
            return new Cairo.Color(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        }

        public static Gdk.Color ToGdk(this System.Drawing.Color color)
        {
            return new Gdk.Color(color.R, color.G, color.B);
        }

        public static Gdk.Color ToGdk(this Cairo.Color color)
        {
            return new Gdk.Color((byte)(color.R*255), (byte)(color.G*255), (byte)(color.B*255));
        }

        public static System.Drawing.Color ToSystemDrawingColor(this Cairo.Color color)
        {
            return System.Drawing.Color.FromArgb((byte)(color.A*255), (byte)(color.R*255), (byte)(color.G*255), (byte)(color.B*255));
        }

        public static int ToABGR(this System.Drawing.Color color)
        {
           return color.A << 24 | color.B << 16 | color.G << 8 | color.R;
        }

        public static void ToRGBA(this Cairo.Color color, out byte r, out byte g, out byte b, out byte a)
        {
           r = (byte)(color.R*255f);
           g = (byte)(color.G*255f);
           b = (byte)(color.B*255f);
           a = (byte)(color.A*255f);
        }

        public static UInt32 ToUInt32(this Cairo.Color color)
        {
           byte r, g, b, a;
           color.ToRGBA(out r, out g, out b, out a);
           return RGBA_to_UInt32(r, g, b, a);
        }

        public static Cairo.Color ToCairo(this Gdk.Color color)
        {
            return new Cairo.Color(color.Red / 65535f, color.Green / 65535f, color.Blue / 65535f);
        }

        public static void From(this Cairo.Color t, System.Drawing.Color color)
        {
            t.R = color.R / 255f;
            t.G = color.G / 255f;
            t.B = color.B / 255f;
            t.A = color.A / 255f;
        }

        public static void From(this Cairo.Color t, UInt32 rgba)
        {
           byte r, g, b, a;
           UInt32_to_RGBA(rgba, out r, out g, out b, out a);
           t.R = ((double) r) / 255.0;
           t.G = ((double) g) / 255.0;
           t.B = ((double) b) / 255.0;
           t.A = ((double) a) / 255.0;
        }

        public static void From(this Cairo.Color t, Gdk.Color color)
        {
            t.R = color.Red / 65535f;
            t.G = color.Green / 65535f;
            t.B = color.Blue / 65535f;
            t.A = 0;
        }

        public static void From(this Gdk.Color t, System.Drawing.Color color)
        {
            t.Red = (ushort)(color.R * ushort.MaxValue / 255);
            t.Green = (ushort)(color.G * ushort.MaxValue / 255);
            t.Blue = (ushort)(color.B * ushort.MaxValue / 255);
        }
    }

    public class Colors
    {
        public static Color COLOR_NEUSOFT_BLUE = Color.FromArgb(24, 91, 159);

        // Will return a good contrasting background color for a given color.
        // Depending on the input, either black or white will be returned.
        public static Color GetContrastColorBlackOrWhite(Color input)
         {
            return (input.R*0.299 + input.G*0.587 + input.B*0.114) > 186.0
                 ? Color.Black
                 : Color.White;
         }
    }

    /// <summary>
    /// Helper class to serialize System.Drawing.Color
    /// </summary>
    public class XmlColor
    {
        private Color color_ = Color.Black;

        public XmlColor() { }
        public XmlColor(Color c) { color_ = c; }


        public Color ToColor()
        {
            return color_;
        }

        public void FromColor(Color c)
        {
            color_ = c;
        }

        public static implicit operator Color(XmlColor x)
        {
            return x.ToColor();
        }

        public static implicit operator XmlColor(Color c)
        {
            return new XmlColor(c);
        }

        [XmlAttribute]
        public string HtmlColor
        {
            get { return ColorTranslator.ToHtml(color_); }
            set
            {
                try
                {
                    if (Alpha == 0xFF) // preserve named color value if possible
                        color_ = ColorTranslator.FromHtml(value);
                    else
                        color_ = Color.FromArgb(Alpha, ColorTranslator.FromHtml(value));
                }
                catch
                {
                    color_ = Color.Black;
                }
            }
        }

        [XmlAttribute]
        public byte Alpha
        {
            get { return color_.A; }
            set
            {
                if (value != color_.A) // avoid hammering named color if no alpha change
                    color_ = Color.FromArgb(value, color_);
            }
        }

        public bool ShouldSerializeAlpha() { return Alpha < 0xFF; }
    }

}
