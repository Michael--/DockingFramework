using System.Text.RegularExpressions;

namespace Docking.Tools
{
   public class StringTools
   {
      public static string StripHTMLTags(string s)
      {
         return Regex.Replace(s, @"<[^>]*>", string.Empty);
      }

      public static string StripGTKMarkupTags(string s)
      {
         return Regex.Replace(s, @"<[^>]*>", string.Empty);
      }

      public static string StripSpecialCharacters(string s)
      {
          return Regex.Replace(s, "\n", " ");
      }
   }
}
