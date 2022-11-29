
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Resources;
using Docking.Framework;
using Docking.Tools;
using Gtk;

namespace Docking.Components
{
   public class Localization
   {
      private static string   sCurrentLanguageCode;
      private static string   sCurrentLanguageName;
      private static string   sDefaultLanguageCode;
      private static string   sDefaultLanguageName;
      private static Language sDefaultLanguage;
      private static Language sCurrentLanguage;

      public static bool sComplainAboutMissingLocalizations;

      public static IMessageWriteLine sLogger;

      private static readonly string sPreferredUserCultureThreeletterISOLanguageName
         = System.Threading.Thread.CurrentThread.CurrentCulture.ThreeLetterISOLanguageName.ToLowerInvariant();

      private          string                       mResxFolder;
      private readonly Dictionary<string, Language> mLanguages = new Dictionary<string, Language>();

      /// <summary>
      /// Initializes a new instance.
      /// </summary>
      /// <param name="default_language">The language code</param>
      /// <param name="logger">The message logger</param>
      internal Localization(string default_language, IMessageWriteLine logger)
      {
         sDefaultLanguageCode = sDefaultLanguageName = default_language;
         sLogger               = logger;
      }

      public string CurrentLanguageCode
      {
         get { return sCurrentLanguageCode; }
      }

      public string CurrentLanguageName
      {
         get { return sCurrentLanguageName; }
      }

      public string DefaultLanguageCode
      {
         get { return sDefaultLanguageCode; }
      }

      public string DefaultLanguageName
      {
         get { return sDefaultLanguageName; }
      }

      public int CurrentChangeCount
      {
         get
         {
            int changes = 0;
            foreach (Node n in sCurrentLanguage.Nodes.Values)
            {
               if (n.Changed)
               {
                  changes++;
               }
            }

            return changes;
         }
      }

      // https://en.wikipedia.org/wiki/List_of_ISO_639-2_codes
      public static string PreferredUserCultureThreeLetterISOLanguageName()
      {
         return sPreferredUserCultureThreeletterISOLanguageName;
      }

      public static string Format(Bin o, string fmt, params object[] args)
      {
         return fmt.FormatLocalizedWithPrefix(o, args);
      }

      // special case: no fmt string arguments
      public static string Format(Bin o, string fmt)
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

      public static void LocalizeMenu(Container container)
      {
         if (container != null)
         {
            foreach (Widget w in container.AllChildren) // .AllChildren here really is necessary, otherwise e.g. Gtk.Notebook will not properly recursed
            {
               MenuItem item = w as MenuItem;

               if (item != null && item.Submenu != null)
               {
                  LocalizeMenu(item.Submenu as Menu);
               }

               if (w is Container)
               {
                  LocalizeMenu((w as Container));
               }

               if (w is ILocalizableWidget)
               {
                  (w as ILocalizableWidget).Localize("MENU");
               }

               if (w is ImageMenuItem)
               {
                  ImageMenuItem imi = w as ImageMenuItem;
                  Image img = imi.Image as Image;
                  if (img != null)
                  {
                     string stockicon = img.Stock;
                     if (stockicon == "gtk-cut")
                     {
                        imi.Image                      = ResourceLoader_Docking.LoadImage("Cut-16.png");
                        (imi.Child as Label).LabelProp = "Cut".Localized("MENU");
                     }
                     else if (stockicon == "gtk-copy")
                     {
                        imi.Image                      = ResourceLoader_Docking.LoadImage("Copy-16.png");
                        (imi.Child as Label).LabelProp = "Copy".Localized("MENU");
                     }
                     else if (stockicon == "gtk-paste")
                     {
                        imi.Image                      = ResourceLoader_Docking.LoadImage("Paste-16.png");
                        (imi.Child as Label).LabelProp = "Paste".Localized("MENU");
                     }
                     else if (stockicon == "gtk-delete")
                     {
                        imi.Image                      = null; // Docking.Tools.ResourceLoader_Docking.LoadImage("Delete-16.png");
                        (imi.Child as Label).LabelProp = "Delete".Localized("MENU");
                     }
                     else if (stockicon == "gtk-select-all")
                     {
                        imi.Image                      = null; // Docking.Tools.ResourceLoader_Docking.LoadImage("SelectAll-16.png");
                        (imi.Child as Label).LabelProp = "Select All".Localized("MENU");
                     }
                  }
               }
            }
         }
      }

      public static void LocalizeControls(string prefix, Container container)
      {
         foreach (Widget w in container.AllChildren) // .AllChildren here really is necessary, otherwise e.g. Gtk.Notebook will not properly recursed
         {
            if (w is Container)
            {
               LocalizeControls(prefix, (w as Container));
            }

            if (w is TreeView)
            {
               foreach (TreeViewColumn c in (w as TreeView).Columns)
               {
                  if (c is ILocalizableWidget)
                  {
                     (c as ILocalizableWidget).Localize(prefix);
                  }
               }
            }

            if (w is FileChooserWidget)
            {
               //w.DumpWidgetsHierarchy();
               Label lbl_CreateFolder = w.GetChild(0, 0, 0, 0, 5, 0) as Label;
               Label lbl_Location = w.GetChild(0, 0, 0, 1, 0) as Label;
               /*
               Gtk.Label lbl_Places       = w.GetChild(0, 0, 1, 0, 0, 0, 0, 0, 0, 0) as Gtk.Label;    // TODO WHY DO WE GET null HERE??
               Gtk.Label lbl_Name         = w.GetChild(0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0) as Gtk.Label; // TODO WHY DO WE GET null HERE??
               Gtk.Label lbl_Size         = w.GetChild(0, 0, 1, 1, 0, 0, 0, 1, 0, 0, 0) as Gtk.Label; // TODO WHY DO WE GET null HERE??
               Gtk.Label lbl_Modified     = w.GetChild(0, 0, 1, 1, 0, 0, 0, 2, 0, 0, 0) as Gtk.Label; // TODO WHY DO WE GET null HERE??
               */
               if (lbl_CreateFolder != null)
               {
                  lbl_CreateFolder.LabelProp = "Create Folder".Localized("Docking.Components");
               }

               if (lbl_Location != null)
               {
                  lbl_Location.LabelProp = "Path".Localized("Docking.Components");
               }
            }

            if (w is ILocalizableWidget)
            {
               (w as ILocalizableWidget).Localize(prefix);
            }
         }
      }


      public string[] AvailableLanguages()
      {
         List<string> av = new List<string>();

         foreach (KeyValuePair<string, Language> kvp in mLanguages)
         {
            string code = kvp.Value.Code;
            string name = kvp.Value.Code;
            Node node;
            if (kvp.Value.Nodes.TryGetValue("LANGUAGE_NAME", out node))
            {
               name = node.Value as string;
            }

            av.Add(code + "|" + name);
         }

         return av.ToArray();
      }

      public bool SetLanguage(string code)
      {
         if (code == "default")
         {
            code = DefaultLanguageCode;
         }

         Language newLanguage;
         if (code != sCurrentLanguageCode && mLanguages.TryGetValue(code, out newLanguage))
         {
            sCurrentLanguageCode = code;
            sCurrentLanguageName = code;
            sCurrentLanguage     = newLanguage;

            Node node;
            if (sCurrentLanguage.Nodes.TryGetValue("LANGUAGE_NAME", out node))
            {
               sCurrentLanguageName = node.Value as string;
            }

            return true;
         }

         return false;
      }

      public void SearchForResources(string fullFilepath)
      {
         mResxFolder = Path.GetDirectoryName(fullFilepath);
         String name = Path.GetFileName(fullFilepath);

         string[] fnames = Directory.GetFiles(mResxFolder, name);
         foreach (string fname in fnames)
         {
            Read(fname);
         }

         mLanguages.TryGetValue(sDefaultLanguageCode, out sDefaultLanguage);
         if (sDefaultLanguage != null)
         {
            Node node;
            if (sDefaultLanguage.Nodes.TryGetValue("LANGUAGE_NAME", out node))
            {
               sDefaultLanguageName = node.Value as string;
            }
         }

         SetLanguage(sDefaultLanguageCode);
      }

      public void WriteChangedResourceFiles()
      {
         // 1st: find out which file contains changes and prepare writing
         Dictionary<string, ResXResourceWriter> resourceWriter = new Dictionary<string, ResXResourceWriter>();
         foreach (Language l in mLanguages.Values)
         {
            foreach (Node n in l.Nodes.Values)
            {
               if (!n.Changed) // no write necessary
               {
                  continue;
               }

               string filename = String.Format("{0}/{1}-{2}.resx", mResxFolder, n.Base, l.Code);
               if (resourceWriter.ContainsKey(filename)) // already considered
               {
                  continue;
               }

               ResXResourceWriter rw = new ResXResourceWriter(new FileStream(
                                                                 filename, FileMode.Create, FileAccess.ReadWrite,
                                                                 FileShare.None // open the file exclusively for writing, i.e., prevent other instances of us from interfering
                                                              ));
               resourceWriter.Add(filename, rw);
            }
         }

         if (resourceWriter.Count == 0) // nothing to do, no changes
         {
            return;
         }

         // 2nd: write any node for any open resource file
         foreach (Language l in mLanguages.Values)
         {
            foreach (Node n in l.Nodes.Values)
            {
               string filename = String.Format("{0}/{1}-{2}.resx", mResxFolder, n.Base, l.Code);
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
         {
            w.Close();
         }
      }

      public Dictionary<string, Node> GetDefaultNodes()
      {
         return sDefaultLanguage.Nodes;
      }

      public int GetCurrentHashcode()
      {
         return sCurrentLanguage.GetHashCode();
      }

      public Node FindCurrentNode(string key)
      {
         Node node = null;
         sCurrentLanguage.Nodes.TryGetValue(key, out node);
         return node;
      }

      public void AddNewCurrentNode(Node node)
      {
         if (!sCurrentLanguage.Nodes.ContainsKey(node.Key))
         {
            sCurrentLanguage.Nodes.Add(node.Key, node);
         }
      }

      public Node FindDefaultNode(string key)
      {
         Node node = null;
         sDefaultLanguage.Nodes.TryGetValue(key, out node);
         return node;
      }

      public static string GetString(string key, string prefix = null)
      {
         if (key == null || key.Length <= 0)
         {
            return "";
         }

         string key2 = StringTools.StripSpecialCharacters((String.IsNullOrEmpty(prefix) ? "" : prefix + ".") + key);

         Node node = null;
         if (sCurrentLanguage != null && sCurrentLanguage.Nodes.TryGetValue(key2, out node) && (node.Value as String).Length > 0)
         {
            return node.Value as string;
         }

         if (sDefaultLanguage != null && sDefaultLanguage.Nodes.TryGetValue(key2, out node) && (node.Value as String).Length > 0)
         {
            return node.Value as string;
         }

         if (sComplainAboutMissingLocalizations)
         {
            sLogger.MessageWriteLine("Missing localization key '{0}'", key2);
         }

         return key;
      }

      private void Read(string filename)
      {
         //CultureInfo ci;
         String name = Path.GetFileNameWithoutExtension(filename);
         int i = name.LastIndexOf('-');
         Debug.Assert(i > 0);

         i = name.LastIndexOf('-', i - 1);
         Debug.Assert(i > 0);

         if (i <= 0) {
            return;
         }

         string basename = name.Substring(0, i);
         string code = name.Substring(i + 1);

         Language lang;
         if (!mLanguages.TryGetValue(code, out lang)) {
            lang = new Language(code);
            mLanguages.Add(code, lang);
         }

         try
         {
            using(var reader = new ResXResourceReader(filename))
            {
               reader.UseResXDataNodes = true;
               IDictionaryEnumerator dict = reader.GetEnumerator();

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
                  {
                     lang.Nodes.Add(key, n);
                  }
                  else
                  {
                     sLogger.MessageWriteLine("Localization: Key '{0}' already exists", key);
                  }
               }
            }
         }
         catch(Exception)
         {
            Console.Error.WriteLine("invalid localization file {0}", filename);
         }
      }

      private class Language
      {
         public Language(string code)
         {
            Code  = code;
            Nodes = new Dictionary<string, Node>();
         }

         public string Code { get; private set; }
         public Dictionary<string, Node> Nodes { get; private set; }
      }


      public class Node
      {
         public Node(string key, string value, string comment, string bse, string oldValue, string oldComment)
         {
            Key     = key;
            Value   = value;
            Comment = comment;
            Base    = bse;

            OldValue   = oldValue;
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
               {
                  return true;
               }

               return Comment != OldComment;
            }
         }

         public void Saved() // changes has been saved
         {
            OldValue   = Value;
            OldComment = Comment;
         }
      }
   }
}
