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
using Docking.Tools;
using Gtk;
using Docking.Framework;

namespace Docking.Components
{
   public partial class Localization
   {
       public static IMessageWriteLine mDbgOut;

       public Localization()
       {}

       public Localization(IMessageWriteLine dbgout)
       {
          mDbgOut = dbgout;
       }

      public static void LocalizeMenu(Gtk.Container container)
      {
         foreach (Gtk.Widget w in container.AllChildren)  // strange GTK artefact: method .Children does _not_ return ALL children. .AllChildren does. So what should .Children be GOOD FOR???? WTF
         {
            MenuItem item = w as MenuItem;

            if (item != null && item.Submenu != null)
               LocalizeMenu(item.Submenu as Menu);

            if (w is Gtk.Container)
               LocalizeMenu((w as Gtk.Container));

            if (w is ILocalizableWidget)
               (w as ILocalizableWidget).Localize("MENU");
         }
      }

      public static void LocalizeControls(string namespc, Gtk.Container container)
      {
         foreach (Gtk.Widget w in container.AllChildren) // strange GTK artefact: method .Children does _not_ return ALL children. .AllChildren does. So what should .Children be GOOD FOR???? WTF
         {
            if (w is Gtk.Container)
               LocalizeControls(namespc, (w as Gtk.Container));

            if (w is TreeView)
            {
               foreach(TreeViewColumn c in (w as TreeView).Columns)
               {
                  if (c is ILocalizableWidget)
                     (c as ILocalizableWidget).Localize(namespc);
               }
            }

            if (w is Gtk.FileChooserWidget)
            {
                //w.DumpWidgetsHierarchy();
                Gtk.Label lbl_CreateFolder = w.GetChild(0, 0, 0, 0, 5, 0) as Gtk.Label;
                Gtk.Label lbl_Location     = w.GetChild(0, 0, 0, 1, 0) as Gtk.Label;
                /*
                Gtk.Label lbl_Places       = w.GetChild(0, 0, 1, 0, 0, 0, 0, 0, 0, 0) as Gtk.Label;    // TODO WHY DO WE GET null HERE??
                Gtk.Label lbl_Name         = w.GetChild(0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0) as Gtk.Label; // TODO WHY DO WE GET null HERE??
                Gtk.Label lbl_Size         = w.GetChild(0, 0, 1, 1, 0, 0, 0, 1, 0, 0, 0) as Gtk.Label; // TODO WHY DO WE GET null HERE??
                Gtk.Label lbl_Modified     = w.GetChild(0, 0, 1, 1, 0, 0, 0, 2, 0, 0, 0) as Gtk.Label; // TODO WHY DO WE GET null HERE??
                */
                if(lbl_CreateFolder!=null)
                   lbl_CreateFolder.LabelProp = "Create Folder".Localized("Docking.Components");
                if(lbl_Location!=null)
                  lbl_Location.LabelProp = "Path".Localized("Docking.Components");
            }

            if (w is ILocalizableWidget)
               (w as ILocalizableWidget).Localize(namespc);
         }
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

      public string CurrentLanguageCode { get { return mCurrentLanguageCode; } }
      public string CurrentLanguageName { get { return mCurrentLanguageName; } }
      public string DefaultLanguageCode { get { return mDefaultLanguageCode; } }
      public string DefaultLanguageName { get { return mDefaultLanguageName; } }

      public void SearchForResources(string s)
      {
         mFolder = Path.GetDirectoryName(s);
         String name = Path.GetFileName(s);

         string[] files = Directory.GetFiles(mFolder, name);
         foreach (string f in files)
            Read(f);

         mDefaultLanguageCode = mDefaultLanguageName = "en-US";
         Languages.TryGetValue(mDefaultLanguageCode, out mDefaultLanguage); // last alternative
         if (mDefaultLanguage != null)
         {
            Node node;
            if (mDefaultLanguage.Nodes.TryGetValue("LANGUAGE_NAME", out node))
               mDefaultLanguageName = node.Value as string;
         }
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

            Node n = new Node(key, value, comment, basename, value, comment);
            if (!lang.Nodes.ContainsKey(key))
               lang.Nodes.Add(key, n);
            else
                if (Localization.mDbgOut != null)
                    Localization.mDbgOut.MessageWriteLine("Localization: Key '{0}' already exists", key);
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

      public Node FindDefaultNode(string key)
      {
         Node node = null;
         mDefaultLanguage.Nodes.TryGetValue(key, out node);
         return node;
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

      Dictionary<string, Language> Languages = new Dictionary<string, Language>();
      string mFolder;
      static Language mDefaultLanguage;
      static Language mCurrentLanguage;
      static string mCurrentLanguageCode;
      static string mCurrentLanguageName;
      static string mDefaultLanguageCode;
      static string mDefaultLanguageName;

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
         public Node(string key, string value, string comment, string bse, string oldValue, string oldComment)
         {
            Key = key;
            Value = value;
            Comment = comment;
            Base = bse;

            OldValue = oldValue;
            OldComment = oldComment; 
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
            OldValue = Value;
            OldComment = Comment;
         }
      }

      public static string GetString(string key)
      {
         Node node = null;
         if (mCurrentLanguage != null && mCurrentLanguage.Nodes.TryGetValue(key, out node) && (node.Value as String).Length > 0)
            return node.Value as string;

         if (mDefaultLanguage != null && mDefaultLanguage.Nodes.TryGetValue(key, out node) && (node.Value as String).Length > 0)
            return node.Value as string;

         if (Localization.mDbgOut != null)
             Localization.mDbgOut.MessageWriteLine("Missing localization key '{0}'", key);

         return null;
      }
   }

}
