using Gtk;
using System.Collections.Generic;

namespace Docking.Tools
{
   // this class works around the problem that GTK's class FileFilter does not allow read acces to a pattern which has been set via .AddPattern()
   public class FileFilterExt : Gtk.FileFilter
   {
      List<string> mPatterns = new List<string>();

      public FileFilterExt()
      {}

      public FileFilterExt(string pattern, string name)
      {
         Name = name+" ("+pattern+")";
         AddPattern(pattern);         
      }

      new public void AddPattern(string pattern)
      { 
         mPatterns.Add(pattern);
         base.AddPattern(pattern);
      }
 
      public IEnumerable<string> GetPatterns()
      {
         return mPatterns;
      }

      // returns true if a given filename matches one of the patterns of this filter
      public bool Matches(string filename)
      {
         return Filter(new FileFilterInfo { Contains = FileFilterFlags.Filename, Filename = filename });
      }
   }
}
