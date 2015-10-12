﻿using System;
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
using Docking.Framework;
using Gtk;


namespace Docking.Components
{
   public partial class Localization
   {
       public static bool ComplainAboutMissingLocalizations;
       public static IMessageWriteLine DbgOut;

       public Localization()
       {}

       public Localization(IMessageWriteLine dbgout)
       {
          DbgOut = dbgout;
       }

      public static void LocalizeMenu(Gtk.Container container)
      {
         foreach(Gtk.Widget w in container.AllChildren) // .AllChildren here really is necessary, otherwise e.g. Gtk.Notebook will not properly recursed
         {
            MenuItem item = w as MenuItem;

            if (item != null && item.Submenu != null)
               LocalizeMenu(item.Submenu as Menu);

            if (w is Gtk.Container)
               LocalizeMenu((w as Gtk.Container));

            if (w is ILocalizableWidget)
               (w as ILocalizableWidget).Localize("MENU");

            if(w is ImageMenuItem) 
            {
               ImageMenuItem imi =  w as ImageMenuItem;
               Image img = imi.Image as Image;
               if(img!=null)
               {
                  string stockicon = img.Stock;
                  if(stockicon=="gtk-cut")
                  {
                     imi.Image = Docking.Tools.ResourceLoader_Docking.LoadImage("Cut-16.png");
                     (imi.Child as Label).LabelProp = "Cut".Localized("MENU");
                  }
                  else if(stockicon=="gtk-copy")
                  {
                     imi.Image = Docking.Tools.ResourceLoader_Docking.LoadImage("Copy-16.png");
                     (imi.Child as Label).LabelProp = "Copy".Localized("MENU");
                  }
                  else if(stockicon=="gtk-paste")
                  {
                     imi.Image = Docking.Tools.ResourceLoader_Docking.LoadImage("Paste-16.png");
                     (imi.Child as Label).LabelProp = "Paste".Localized("MENU");
                  }
                  else if(stockicon=="gtk-delete")
                  {
                     imi.Image = null; // Docking.Tools.ResourceLoader_Docking.LoadImage("Delete-16.png");
                     (imi.Child as Label).LabelProp = "Delete".Localized("MENU");
                  }
                  else if(stockicon=="gtk-select-all")
                  {
                     imi.Image = null; // Docking.Tools.ResourceLoader_Docking.LoadImage("SelectAll-16.png");
                     (imi.Child as Label).LabelProp = "Select All".Localized("MENU");
                  }
               }
            }
         }
      }

      public static void LocalizeControls(string prefix, Gtk.Container container)
      {
         foreach(Gtk.Widget w in container.AllChildren) // .AllChildren here really is necessary, otherwise e.g. Gtk.Notebook will not properly recursed
         {
            if(w is Gtk.Container)
               LocalizeControls(prefix, (w as Gtk.Container));

            if(w is TreeView)
            {
               foreach(TreeViewColumn c in (w as TreeView).Columns)
               {
                  if(c is ILocalizableWidget)
                     (c as ILocalizableWidget).Localize(prefix);
               }
            }

            if(w is Gtk.FileChooserWidget)
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

            if(w is ILocalizableWidget)
               (w as ILocalizableWidget).Localize(prefix);
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
         Languages.TryGetValue(mDefaultLanguageCode, out mDefaultLanguage);
         if (mDefaultLanguage != null)
         {
            Node node;
            if (mDefaultLanguage.Nodes.TryGetValue("LANGUAGE_NAME", out node))
               mDefaultLanguageName = node.Value as string;
         }
         SetLanguage(mDefaultLanguageCode);
      }

      void Read(string filename)
      {
         //CultureInfo ci;
         String name = Path.GetFileNameWithoutExtension(filename);
         int i = name.LastIndexOf('-');
         Debug.Assert(i > 0);
         i = name.LastIndexOf('-', i - 1);
         Debug.Assert(i > 0);
         if (i <= 0)
            return;
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
                if (Localization.DbgOut != null)
                    Localization.DbgOut.MessageWriteLine("Localization: Key '{0}' already exists", key);
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

               ResXResourceWriter rw = new ResXResourceWriter(new FileStream(
                  filename, FileMode.Create, FileAccess.ReadWrite,
                  FileShare.None // open the file exclusively for writing, i.e., prevent other instances of us from interfering
               ));
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

      public static string GetString(string key, string prefix = null)
      {
         if(key==null || key.Length<=0)
            return "";

         string key2 = StringTools.StripSpecialCharacters((String.IsNullOrEmpty(prefix) ? "" : prefix+".")+key);

         Node node = null;
         if (mCurrentLanguage != null && mCurrentLanguage.Nodes.TryGetValue(key2, out node) && (node.Value as String).Length > 0)
            return node.Value as string;

         if (mDefaultLanguage != null && mDefaultLanguage.Nodes.TryGetValue(key2, out node) && (node.Value as String).Length > 0)
            return node.Value as string;

         if(Localization.DbgOut!=null && ComplainAboutMissingLocalizations)
             Localization.DbgOut.MessageWriteLine("Missing localization key '{0}'", key2);

         return key;
      }
   }

}
