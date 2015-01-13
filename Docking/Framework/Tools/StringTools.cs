using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Docking.Tools
{
   public class StringTools
   {
      public static string ShrinkPath(string path, int maxLength)
      {
         if (path.Length < maxLength)
            return path;

         var slash = Platform.IsWindows ? '\\' : '/';

         var parts = new List<string>(path.Split(slash));

         string start = parts[0];
         if (parts.Count() > 1)
         {
            start += slash + parts[1];
            parts.RemoveAt(1);
         }
         parts.RemoveAt(0);
         string end = null;
         if (parts.Count() > 0)
         {
            end = parts[parts.Count - 1];
            parts.RemoveAt(parts.Count - 1);

            parts.Insert(0, "...");
            while (parts.Count > 1 &&
              start.Length + end.Length + parts.Sum(p => p.Length) + parts.Count > maxLength)
               parts.RemoveAt(parts.Count - 1);
         }

         var mid = "" + slash;
         parts.ForEach(p => mid += p + slash);
         string result;
         if (end != null)
            result = start + mid + end;
         else
            result = start;

         if (result.Length > maxLength && maxLength > 5)
         {
            int l = maxLength / 2 - 1;
            result = result.Substring(0, l) + "~" + result.Substring(result.Length - l);
         }

         return result;
      }

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

         if(bytecount>=MEGABYTE)
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0} MB",
                                 ((float) bytecount)/MEGABYTE);
         else if(bytecount>=KILOBYTE)
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0} KB",
                                 ((float) bytecount)/KILOBYTE);
         else if(bytecount==1)
            return "1 Byte";            
         else
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} Bytes", bytecount);            
      }
   }
}
