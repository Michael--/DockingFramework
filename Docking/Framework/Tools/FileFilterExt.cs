using System.Collections.Generic;
using System.Text;
using Gtk;

namespace Docking.Tools
{
   // this class works around the problem that GTK's class FileFilter does not allow read acces to a pattern which has been set via .AddPattern()
   public class FileFilterExt : Gtk.FileFilter
   {
      List<string> m_AdjustedPattern = new List<string>();
      List<string> m_Pattern = new List<string>();

      public FileFilterExt()
      {}

      public FileFilterExt(string pattern, string name)
      {
         string adjusted_pattern, pattern_to_show_to_user;
         MakePatternCaseInsensitive(pattern, out adjusted_pattern, out pattern_to_show_to_user);

         Name = name;
         if(!string.IsNullOrEmpty(pattern_to_show_to_user))
            Name += " ("+pattern_to_show_to_user+")";

         m_Pattern.Add(pattern);
         AddPattern(adjusted_pattern);         
      }

      new public void AddPattern(string pattern)
      { 
         string adjusted_pattern, pattern_to_show_to_user;
         MakePatternCaseInsensitive(pattern, out adjusted_pattern, out pattern_to_show_to_user);

         m_AdjustedPattern.Add(adjusted_pattern);
         base.AddPattern(adjusted_pattern);
      }

      private void MakePatternCaseInsensitive(string pattern, out string adjusted_pattern, out string pattern_to_show_to_user)
      {
         StringBuilder adjusted_pattern_BUILDER        = new StringBuilder();
         StringBuilder pattern_to_show_to_user_BUILDER = new StringBuilder();

         uint brackets = 0;
         foreach(char c in pattern)
         {
            if(c=='[')
            {
               brackets++;
               adjusted_pattern_BUILDER.Append(c);
               pattern_to_show_to_user_BUILDER.Append(c);
            }
            else if(c==']')
            {
               if(brackets>0)
                  brackets--;
               adjusted_pattern_BUILDER.Append(c);
               pattern_to_show_to_user_BUILDER.Append(c);
            }
            else if(char.IsLetter(c) && brackets==0)
            {
               char L = char.ToLower(c);
               char U = char.ToUpper(c);

               adjusted_pattern_BUILDER.Append('[')
                                       .Append( L )
                                       .Append( U )
                                       .Append(']');
               pattern_to_show_to_user_BUILDER.Append(L);
            }
            else
            {
               adjusted_pattern_BUILDER.Append(c);
               pattern_to_show_to_user_BUILDER.Append(c);
            }
         }

         adjusted_pattern        = adjusted_pattern_BUILDER       .ToString();
         pattern_to_show_to_user = pattern_to_show_to_user_BUILDER.ToString();
      }

      public IEnumerable<string> GetAdjustedPattern()
      {
         return m_AdjustedPattern;
      }

      public IEnumerable<string> GetPattern()
      {
         return m_Pattern;
      }

      // returns true if a given filename matches one of the patterns of this filter
      public bool Matches(string filename)
      {
         FileFilterInfo info = new FileFilterInfo
         {
            Contains    = FileFilterFlags.Filename | FileFilterFlags.DisplayName,
            Filename    = filename,
            DisplayName = filename
         };
 
         return this.Filter(info);
      }
   }
}
