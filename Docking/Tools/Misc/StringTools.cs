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
          return Regex.Replace(s, "\n", " ");
      }
   }
}
