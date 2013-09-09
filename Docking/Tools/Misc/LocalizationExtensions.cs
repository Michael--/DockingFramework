using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Docking.Components;
using Docking.Tools;

namespace Docking.Components
{
   public partial class Localization
   {
      public static string Format(Gtk.Bin o, string fmt, params object[] args)
      {
         return fmt.FormatLocalizedWithPrefix(o, args);
      }

      // special case: no fmt string arguments
      public static string Format(Gtk.Bin o, string fmt)
      {
         return fmt.FormatLocalizedWithPrefix(o);
      }

      public static string Format(string fmt, params object[] args)
      {
         return fmt.FormatLocalized(args);
      }

      // special case: no fmt string arguments
      public static string Format(string fmt)
      {
         return fmt.FormatLocalized();
      }
   }
}


namespace Docking.Tools
{
   public static class LocalizationExtensions
   {
      public static string Localized(this string key)
      {
         return Localization.GetString(key);
      }

      public static string Localized(this string key, object o)
      {
         return Localization.GetString(key, o.GetType().Namespace);
      }

      public static string Localized(this string key, string prefix)
      {
         return Localization.GetString(key, prefix);
      }

      public static string FormatLocalized(this string key, params object[] args)
      {
         try
         {
            string localized = Localized(key) ?? key;
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, localized, args);
         }
         catch (FormatException)
         {
             if (Localization.mDbgOut != null)
                 Localization.mDbgOut.MessageWriteLine("FormatLocalized Exception: key='{0}' fmt='{1}'", key, Localized(key));
            return Localized(key);
         }
      }

      // special case: no fmt string arguments
      public static string FormatLocalized(this string key)
      {
         return Localized(key);
      }

      public static string FormatLocalizedWithPrefix(this string key, Gtk.Bin o, params object[] args)
      {
         return (o.GetType().Namespace+"."+key).FormatLocalized(args);
      }

      // special case: no fmt string arguments
      public static string FormatLocalizedWithPrefix(this string key, Gtk.Bin o)
      {
         return (o.GetType().Namespace+"."+key).FormatLocalized();
      }

      public static string FormatLocalizedWithPrefix(this string key, string prefix, params object[] args)
      {
         return (prefix+"."+key).FormatLocalized(args);
      }

      // special case: no fmt string arguments
      public static string FormatLocalizedWithPrefix(this string key, string prefix)
      {
         return (prefix+"."+key).FormatLocalized();
      }
   }
}
