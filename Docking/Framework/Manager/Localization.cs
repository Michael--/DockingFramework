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
         mFolder = Path.GetDirectoryName(s);
         String name = Path.GetFileName(s);

         string[] files = Directory.GetFiles(mFolder, name);
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
            object obj = node.GetValue((ITypeResolutionService)null);
            Debug.Assert(obj is string);
            string value = obj as string;
            string comment = node.Comment;

            Node n = new Node(key, value, comment, basename);
            if (!lang.Nodes.ContainsKey(key))
               lang.Nodes.Add(key, n);
            else
               componentManager.MessageWriteLine("Localization: Key '{0}' already exist", key);
         }
      }

      public void WriteChangedResourceFiles()
      {
         // 1st: find out which file contains changes and prepare writing
         Dictionary<string, ResXResourceWriter> resourceWriter = new Dictionary<string, ResXResourceWriter>();
         foreach (Language l in Languages.Values)
         {
            foreach (Node n in l.Nodes.Values)
            {
               if (!n.Changed) // no write necessary
                  continue;

               string filename = String.Format("{0}/{1}-{2}.resx", mFolder, n.Base, l.Code);
               if (resourceWriter.ContainsKey(filename)) // already considered
                  continue;

               ResXResourceWriter rw = new ResXResourceWriter(filename);
               resourceWriter.Add(filename, rw);
            }
         }

         if (resourceWriter.Count == 0) // nothing to do, no changes
            return;

         // 2nd: write any node for any open resource file
         foreach (Language l in Languages.Values)
         {
            foreach (Node n in l.Nodes.Values)
            {
               string filename = String.Format("{0}/{1}-{2}.resx", mFolder, n.Base, l.Code);
               ResXResourceWriter rw;
               if (resourceWriter.TryGetValue(filename, out rw))
               {
                  ResXDataNode rnode = new ResXDataNode(n.Key, n.Value);
                  rnode.Comment = n.Comment;
                  rw.AddResource(rnode);
                  n.Saved(); // mark as saved
               }
               else
               {
                  Debug.Assert(!n.Changed); // last check, changed nodes must not exist here
               }
            }
         }
         foreach (ResXResourceWriter w in resourceWriter.Values)
            w.Close();
      }

      public Dictionary<string, Node> GetDefaultNodes() { return mDefaultLanguage.Nodes; }
      public int GetCurrentHashcode() { return mCurrentLanguage.GetHashCode(); }
      public Node FindCurrentNode(string key)
      {
         Node node = null;
         mCurrentLanguage.Nodes.TryGetValue(key, out node);
         return node;
      }

      public void AddNewCurrentNode(Node node)
      {
         if (!mCurrentLanguage.Nodes.ContainsKey(node.Key))
            mCurrentLanguage.Nodes.Add(node.Key, node);
      }

      public int CurrentChangeCount
      {
         get
         {
            int changes = 0;
            foreach (Node n in mCurrentLanguage.Nodes.Values)
            {
               if (n.Changed)
                  changes++;
            }
            return changes;
         }
      }


      public static ComponentManager componentManager;
      Dictionary<string, Language> Languages = new Dictionary<string, Language>();
      string mFolder;
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


      public class Node
      {
         public Node(string key, string value, string comment, string bse)
         {
            Key = key;
            Value = value;
            Comment = comment;
            Base = bse;

            OldValue = Value.Clone() as String;
            OldComment = Comment.Clone() as String; 
         }

         public string Key { get; private set; }
         public string Value { get; set; }
         public string Comment { get; set; }
         public string Base { get; private set; }
         private string OldValue { get; set; }
         private string OldComment { get; set; }

         public bool Changed
         {
            get
            {
               if (Value != OldValue)
                  return true;
               return Comment != OldComment;
            }
         }

         public void Saved() // changes has been saved
         {
            OldValue = Value.Clone() as String;
            OldComment = Comment.Clone() as String;
         }
      }

      public static string GetString(string key)
      {
         Node node = null;
         if (mCurrentLanguage != null && mCurrentLanguage.Nodes.TryGetValue(key, out node) && (node.Value as String).Length > 0)
            return node.Value as string;

         if (mDefaultLanguage != null && mDefaultLanguage.Nodes.TryGetValue(key, out node) && (node.Value as String).Length > 0)
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
   }

   // [System.ComponentModel.ToolboxItem(true)] // ?? is this really necessary as a Toolbox item ??
   public class TreeViewColumnLocalized : Gtk.TreeViewColumn, ILocalized
   {
      void ILocalized.Localize(string namespc)
      {
         if (LocalizationKey == null)
            LocalizationKey = Title;
         Title = LocalizationKey.Localized(namespc);
      }
      public string LocalizationKey { get; set; }
   }

   [System.ComponentModel.ToolboxItem(true)]
   public class CheckButtonLocalized : Gtk.CheckButton, ILocalized
   {
      void ILocalized.Localize(string namespc)
      {
         if (LocalizationKey == null)
            LocalizationKey = Label;
         Label = LocalizationKey.Localized(namespc);
      }
      public string LocalizationKey { get; set; }
   }}
