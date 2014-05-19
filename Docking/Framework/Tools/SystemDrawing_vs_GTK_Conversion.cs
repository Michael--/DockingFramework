using System.IO;

namespace Docking.Tools
{
   public class SystemDrawing_vs_GTK_Conversion
   {
      // TODO is there a less brute-force way?
      public static Gdk.Pixbuf Bitmap2Pixbuf(System.Drawing.Bitmap bitmap)
      {
         MemoryStream ms = new MemoryStream();
         bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
         ms.Seek(0, SeekOrigin.Begin);
         return new Gdk.Pixbuf(ms);
      }

      public static Gdk.Pixbuf Bitmap2Pixbuf(System.Drawing.Icon icon)
      {
         return Bitmap2Pixbuf(icon.ToBitmap());
      }

   }
}
