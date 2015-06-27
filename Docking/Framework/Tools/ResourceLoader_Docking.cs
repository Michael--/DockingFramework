using System;

namespace Docking.Tools
{
   // A safety wrapper around Gdk's Gdk.Pixbuf.LoadFromResource() function.
   // That function has several problems: 
   // 1. It can only load resources from the assembly where THAT CALL'S CODE is located. If you specify a different one,
   //    it will throw an exception. This makes this function very problematic when it is being called in a virtual function
   //    of a class which gets inherited between assemblies. Example: our class "ComponentFactory" is inside Docking Framework,
   //    and there the call was located, but a child class in a different assembly (TG) was inherited from that class.
   //    When that child class called the virtual function containing the .LoadFromResource() call, an exception was raised.
   // 2. It forces all callers to properly catch exceptions and check for null. That's annoying and a lot of code each time.
   // Therefore this class has been added as a cure:
   // A. It catches all exceptions and returns a dummy image on error. That way, you'll see a broken image, but the program will not crash.
   // B. It checks that only resources from THIS assembly can get loaded. If you want to load ressources from a different assembly,
   //    you need to copy+paste this class to there and use THAT, not THIS.
   public class ResourceLoader_Docking
   {
      private static string RESOURCE_PREFIX = "Docking.Framework.Resources."; // this is added here explicitly to PREVENT that resources from other assemblies get loaded! (that would lead to random crashes...)

      // a pink 16x16 dummy placeholder PNG for resources that cannot be retrieved
      private static byte[] DUMMY_PLACEHOLDER_IMAGE = System.Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAABAAAAAQAQMAAAAlPW0iAAAAK3RFWHRDcmVhdGlvbiBUaW1lAFNhIDI3IEp1biAyMDE1IDE0OjQ5OjM2ICswMTAwswJnxwAAAAd0SU1FB98GGww0ExFqaikAAAAJcEhZcwAACxIAAAsSAdLdfvwAAAAEZ0FNQQAAsY8L/GEFAAAABlBMVEUAAAD/AP82/WKvAAAADklEQVR42mP4/5+BFAQA/U4f4d7IdZcAAAAASUVORK5CYII=");

      public static Gdk.Pixbuf LoadPixbuf(string resourcename)
      {
         if(!String.IsNullOrEmpty(resourcename))
         {
            string fullresourcename = RESOURCE_PREFIX+resourcename;
            try { return Gdk.Pixbuf.LoadFromResource(fullresourcename); }
            #if DEBUG
            catch(Exception e) { System.Console.Error.WriteLine(e.ToString()); }   
            #else
            catch(Exception) {}
            #endif
         }        

         // return dummy placeholder image here instead of null to avoid program crashes!
         // return a new one each time to make all instances independent!
         return new Gdk.Pixbuf(DUMMY_PLACEHOLDER_IMAGE);
         // NOTE: GtkSharp has a bug in this Gdk.Pixbuf(DUMMY_PLACEHOLDER_IMAGE) constructor:
         // after reading from that byte stream, it lacks the call to Gdk.PixbufLoader.Close(), thus, this produces lots of output:
         // "GdkPixbufLoader finalized without calling gdk_pixbuf_loader_close() - this is not allowed. You must explicitly end the data stream to the loader before dropping the last reference."
         // We already tried to workaround that by manually ensuring the close() call by this code
         //    Gdk.PixbufLoader loader = new Gdk.PixbufLoader();
         //    bool ok = loader.Write(DUMMY_PLACEHOLDER_IMAGE);
         //    Debug.Assert(ok);
         //    ok = loader.Close();
         //    Debug.Assert(ok);
         //    Gdk.Pixbuf pixbuf = loader.Pixbuf;        
         //    loader = null;
         //    return pixbuf;
         // , however, that did not help. We suspect that the loader.Close() function itself is broken inside and does not propagate its call to Gtk.
         // S.Lohse 2015-06-27
      }

      public static Gtk.Image LoadImage(string resourcename)
      {
         return new Gtk.Image(LoadPixbuf(resourcename));
      }
   }
}
