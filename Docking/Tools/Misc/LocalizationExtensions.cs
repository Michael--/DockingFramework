using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Docking.Components;

namespace Docking.Tools
{
   public static class LocalizationExtensions
   {
      public static string Localized(this string key, object o)
      {
         return Localized(key, o.GetType().Namespace);
      }

      public static string Localized(this string key, string prefix)
      {
         if (prefix == null)
            return Localized(key);
         string result = Localization.GetString(prefix + "." + key);
         if (result != null)
            return result;
         return key;
      }

      public static string Localized(this string key)
      {
         string result = Localization.GetString(key);
         if (result != null)
            return result;
         return key;
      }

      public static string FormatLocalized(this string key, string header, params object[] args)
      {
         try
         {
            return String.Format(Localized(key, header), args);
         }
         catch (FormatException)
         {
            Localization.componentManager.MessageWriteLine("FormatLocalized Exception: key='{0}.{1}' fmt='{2}'", header, key, Localized(key, header));
            return Localized(key, header);
         }
      }

      public static string FormatLocalized(this string key, params object[] args)
      {
         try
         {
            return String.Format(Localized(key), args);
         }
         catch (FormatException)
         {
            Localization.componentManager.MessageWriteLine("FormatLocalized Exception: key='{0}' fmt='{1}'", key, Localized(key));
            return Localized(key);
         }
      }
   }
}
