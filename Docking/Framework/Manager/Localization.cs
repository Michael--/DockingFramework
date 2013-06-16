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

        public void SearchForResources(string s)
        {
            String folder = Path.GetDirectoryName(s);
            String name = Path.GetFileName(s);

            string[] files = Directory.GetFiles (folder, name);
            foreach (string f in files)
                Read(f);

            Languages.TryGetValue("default", out mDefaultLanguage); // last alternative
            Languages.TryGetValue("de-DE", out mCurrentLanguage);// TODO: could be switched by user
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
                lang.Nodes.Add(key, n);
            }
        }

        static ComponentManager componentManager;
        Dictionary<string, Language> Languages = new Dictionary<string,Language>();
        static Language mDefaultLanguage;
        static Language mCurrentLanguage;

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

            componentManager.MessageWriteLine("Localization: Key '{0}' not existing", key);
            return null;
        }
    }

   public static class LocalizationExtensions
   {
      public static string L(this string key, Type t)
      {
          string result = Localization.GetString(t.ToString() + "." + key);
          if (result != null)
              return result;
          return key;
      }

      public static string L(this string key)
      {
          string result = Localization.GetString(key);
          if (result != null)
              return result;
          return key;
      }
   }

}
