using System;
using System.Text.RegularExpressions;

namespace Docking.Tools
{
   public class StringTools
   {
      public static string StripHTMLTags(string s)
      {
          if(s==null)
             return null;
          if(s.Length<=0)
             return "";
         return Regex.Replace(s, @"<[^>]*>", string.Empty);
      }

      public static string StripGTKMarkupTags(string s)
      {
          if(s==null)
             return null;
          if(s.Length<=0)
             return "";
         return Regex.Replace(s, @"<[^>]*>", string.Empty);
      }

      public static string StripSpecialCharacters(string s)
      {
          if(s==null)
             return null;
          if(s.Length<=0)
             return "";
          return Regex.Replace(s, "[\n\r]+", " ");
      }

      public static string ByteCountToString(ulong bytecount)
      {
         const ulong KILOBYTE = 1024;
         const ulong MEGABYTE = 1024*1024;

         if(bytecount>=1000000)
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0} MB",
                                 ((float) bytecount)/MEGABYTE);
         else if(bytecount>=1000)
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0} KB",
                                 ((float) bytecount)/KILOBYTE);
         else if(bytecount!=1)
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} Bytes", bytecount);
         else
            return "1 Byte";
      }
   }
}
