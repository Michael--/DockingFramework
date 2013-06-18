using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.ComponentModel.Design;
using System.Resources;
using System.IO;
using System.Globalization;
using System.Diagnostics;

namespace Docking.Components
{
   public class Localization
   {
      public Localization(ComponentManager cm)
      {
         componentManager = cm;
      }

      public string[] AvailableLanguages()
      {
         List<string> av = new List<string>();

         foreach (KeyValuePair<string, Language> kvp in Languages)
         {
            string code = kvp.Value.Code;
            string name = kvp.Value.Code;
            Node node;
            if (kvp.Value.Nodes.TryGetValue("LANGUAGE_NAME", out node))
               name = node.Value as string;
            av.Add(code + "|" + name);
         }
         return av.ToArray();
      }

      public bool SetLanguage(string code)
      {
         Language newLanguage;
         if (code != mCurrentLanguageCode && Languages.TryGetValue(code, out newLanguage))
         {
            mCurrentLanguageCode = code;
            mCurrentLanguageName = code;
            mCurrentLanguage = newLanguage;

            Node node;
            if (mCurrentLanguage.Nodes.TryGetValue("LANGUAGE_NAME", out node))
               mCurrentLanguageName = node.Value as string;

            return true;
         }
         return false;
      }

      public string CurrentLanguage { get { return mCurrentLanguageCode; } }
      public string CurrentLanguageName { get { return mCurrentLanguageName; } }

      public void SearchForResources(string s)
      {
         String folder = Path.GetDirectoryName(s);
         String name = Path.GetFileName(s);

         string[] files = Directory.GetFiles(folder, name);
         foreach (string f in files)
            Read(f);

         Languages.TryGetValue("en-US", out mDefaultLanguage); // last alternative
         SetLanguage("en-US"); // could be switched by user
      }

      void Read(string filename)
      {
         //CultureInfo ci;
         String name = Path.GetFileNameWithoutExtension(filename);
         int i = name.IndexOf('-');
         Debug.Assert(i > 0);
         string basename = name.Substring(0, i);
         string code = name.Substring(i + 1);

         Language lang;
         if (!Languages.TryGetValue(code, out lang))
         {
            lang = new Language(code);
            Languages.Add(code, lang);
         }

         ResXResourceReader rr = new ResXResourceReader(filename);
         rr.UseResXDataNodes = true;
         IDictionaryEnumerator dict = rr.GetEnumerator();
         while (dict.MoveNext())
         {
            ResXDataNode node = dict.Value as ResXDataNode;
            string key = node.Name;
            object value = node.GetValue((ITypeResolutionService)null);
            string comment = node.Comment;

            Node n = new Node(key, value, comment, basename);
            if (!lang.Nodes.ContainsKey(key))
               lang.Nodes.Add(key, n);
            else
               componentManager.MessageWriteLine("Localization: Key '{0}' already exist", key);
         }
      }

      static ComponentManager componentManager;
      Dictionary<string, Language> Languages = new Dictionary<string, Language>();
      static Language mDefaultLanguage;
      static Language mCurrentLanguage;
      static string mCurrentLanguageCode;
      static string mCurrentLanguageName;

      class Language
      {
         public Language(string code)
         {
            Code = code;
            Nodes = new Dictionary<string, Node>();
         }
         public string Code { get; private set; }
         public Dictionary<string, Node> Nodes { get; private set; }
      }


      class Node
      {
         public Node(string key, object value, string comment, string bse)
         {
            Key = key;
            Value = value;
            Comment = comment;
            Base = bse;
         }

         public string Key { get; private set; }
         public object Value { get; set; }
         public string Comment { get; set; }
         public string Base { get; private set; }
      }

      public static string GetString(string key)
      {
         Node node = null;
         if (mCurrentLanguage != null && mCurrentLanguage.Nodes.TryGetValue(key, out node))
            return node.Value as string;

         if (mDefaultLanguage != null && mDefaultLanguage.Nodes.TryGetValue(key, out node))
            return node.Value as string;

         componentManager.MessageWriteLine("Missing localization key '{0}'", key);
         return null;
      }
   }

   public static class LocalizationExtensions
   {
      public static string Localized(this string key, string header)
      {
         if (header == null)
            return Localized(key);
         string result = Localization.GetString(header + "." + key);
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
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class LabelLocalized : Gtk.Label, ILocalized
   {
      void ILocalized.Localize(string namespc)
      {
         if (LocalizationKey == null)
            LocalizationKey = LabelProp;
         LabelProp = LocalizationKey.Localized(namespc);
      }
      public string LocalizationKey { get; set; }
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class ButtonLocalized : Gtk.Button, ILocalized
   {
      void ILocalized.Localize(string namespc)
      {
         if (LocalizationKey == null)
            LocalizationKey = Label;
         Label = LocalizationKey.Localized(namespc);
      }
      public string LocalizationKey { get; set; }
   }}
