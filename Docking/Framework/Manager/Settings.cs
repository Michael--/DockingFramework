
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Docking.Tools;
using Docking.Widgets;
using Gtk;
using System.Diagnostics;

namespace Docking.Components
{
   public class Settings : IPersistency
   {
      public const string CONFIG_ROOT_ELEMENT = "DockingConfiguration";
      public const string DEFAULT_LAYOUT_NAME = "Default"; // TODO can we localize this string? Careful, the name is persisted...

      private string mCONFIGFILE;
      private string mDEFAULTCONFIGFILE;

      private XmlDocument mXmlDocument;
      private XmlNode     mXmlNode;

      /// <summary>
      /// Initializes a new instance.
      /// </summary>
      public Settings()
      {
         mCONFIGFILE        = System.IO.Path.Combine(AssemblyInfoExt.LocalSettingsFolder, "config.xml");
         mDEFAULTCONFIGFILE = System.IO.Path.Combine(AssemblyInfoExt.Directory, "defaultconfig.xml");
      }


      public string NewFilename { get; set; }

      public bool IsReadonly { get; private set; }

      public void SetFilename(string filename)
      {
         EnsureConfigFileExists(filename);
      }

      public string LoadSetting(string instance, string key, string defaultval)
      {
         if (mXmlNode == null)
         {
            return defaultval;
         }

         List<string> portions = new List<string>();
         if (!string.IsNullOrEmpty(instance))
         {
            portions.AddRange(instance.Split('/'));
         }

         portions.Add(key);
         if (portions.Count <= 0)
         {
            return defaultval;
         }

         try
         {
            XmlNode N = null;
            XmlNode parent = mXmlNode;
            foreach (string p in portions)
            {
               N = parent.SelectSingleNode(p);
               if (N == null)
               {
                  return defaultval;
               }

               parent = N;
            }

            return N.InnerText;
         }
         catch
         {
            return defaultval;
         }
      }

      public List<string> LoadSetting(string instance, string key, List<string> defaultval)
      {
         int count = LoadSetting(instance, key + ".Count", -1);
         if (count < 0)
         {
            return defaultval;
         }

         List<string> result = new List<string>();
         for (int i = 0; i < count; i++)
         {
            result.Add(LoadSetting(instance, key + "." + i, ""));
         }

         return result;
      }

      public List<bool> LoadSetting(string instance, string key, List<bool> defaultval)
      {
         int count = LoadSetting(instance, key + ".Count", -1);
         if (count < 0)
         {
            return defaultval;
         }

         List<bool> result = new List<bool>();
         for (int i = 0; i < count; i++)
         {
            result.Add(LoadSetting(instance, key + "." + i, false));
         }

         return result;
      }

      public IEnumerable<float> LoadSetting(string instance, string key, IEnumerable<float> defaultval)
      {
         int count = LoadSetting(instance, key + ".Count", -1);
         if (count < 0)
         {
            return defaultval;
         }

         List<float> result = new List<float>();
         for (int i = 0; i < count; i++)
         {
            result.Add(LoadSetting(instance, key + "." + i, 0.0f));
         }

         return result;
      }

      public IEnumerable<double> LoadSetting(string instance, string key, IEnumerable<double> defaultval)
      {
         int count = LoadSetting(instance, key + ".Count", -1);
         if (count < 0)
         {
            return defaultval;
         }

         List<double> result = new List<double>();
         for (int i = 0; i < count; i++)
         {
            result.Add(LoadSetting(instance, key + "." + i, 0.0));
         }

         return result;
      }


      public UInt32 LoadSetting(string instance, string key, UInt32 defaultval)
      {
         string s = LoadSetting(instance, key, "");
         UInt32 result;
         return (s != "" && UInt32.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
                   ? result
                   : defaultval;
      }

      public Int32 LoadSetting(string instance, string key, Int32 defaultval)
      {
         string s = LoadSetting(instance, key, "");
         Int32 result;
         return (s != "" && Int32.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
                   ? result
                   : defaultval;
      }

      public float LoadSetting(string instance, string key, float defaultval)
      {
         string s = LoadSetting(instance, key, "");
         float result;
         return (s != "" && float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                   ? result
                   : defaultval;
      }

      public double LoadSetting(string instance, string key, double defaultval)
      {
         string s = LoadSetting(instance, key, "");
         double result;
         return (s != "" && Double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                   ? result
                   : defaultval;
      }

      public bool LoadSetting(string instance, string key, bool defaultval)
      {
         string s = LoadSetting(instance, key, "").ToLowerInvariant();
         if (s == "true")
         {
            return true;
         }
         else if (s == "false")
         {
            return false;
         }
         else
         {
            return defaultval;
         }
      }

      public System.Drawing.Color LoadSetting(string instance, string key, System.Drawing.Color defaultval)
      {
         string s = LoadSetting(instance, key, "");
         System.Drawing.Color result;
         return (s != "" && ColorConverter.RGBAString_to_Color(s, out result))
                   ? result
                   : defaultval;
      }

      public object LoadSetting(string instance, string key, Type value)
      {
         if (mXmlNode == null)
         {
            try
            {
               //create default object type
               return Activator.CreateInstance(value);
            }
            catch
            {
               return null;
            }
         }

         try
         {
            XmlNode node = null;
            XmlNode parent = mXmlNode;

            node = parent.SelectSingleNode(instance);

            var elem = XElement.Parse(node.OuterXml);
            XElement objectSerial;

            if (key == String.Empty)
            {
               objectSerial = elem.Elements().Where(itr => itr.Name == value.Name).Single();
            }
            else
            {
               objectSerial = elem.Elements().Where(itr => itr.Name == key).Single();
            }

            XmlSerializer reader = new XmlSerializer(value);
            var obj = reader.Deserialize(objectSerial.CreateReader());
            if (obj != null)
            {
               return obj;
            }

            return null;
         }
         catch(InvalidOperationException)
         {
            try
            {
               //create default object type
               return Activator.CreateInstance(value);
            }
            catch
            {
               return null;
            }
         }
         catch
         {
            return null;
         }
      }

      public void SaveSetting(string instance, string key, string val)
      {
         if (mXmlNode == null)
         {
            return;
         }

         List<string> portions = new List<string>();
         if (!string.IsNullOrEmpty(instance))
         {
            portions.AddRange(instance.Split('/'));
         }

         portions.Add(key);
         if (portions.Count <= 0)
         {
            return;
         }

         XmlNode N = null;
         XmlNode parent = mXmlNode;
         foreach (string p in portions)
         {
            try
            {
               N = parent.SelectSingleNode(p);
            }
            catch
            {
               N = null;
            }

            if (N == null)
            {
               N = mXmlDocument.CreateElement(p);
               parent.AppendChild(N);
            }

            parent = N;
         }

         N.InnerText = String.IsNullOrEmpty(val) ? "" : val; // note that this does XML-Escaping, for example > becomes &gt; , so you do not have to care what is inside 'val'; anything can be stored
      }

      public void SaveSetting(string instance, string key, List<string> val)
      {
         int count = val == null ? 0 : val.Count;
         SaveSetting(instance, key + ".Count", count);
         for (int i = 0; i < count; i++)
         {
            SaveSetting(instance, key + "." + i, val[i]);
         }
      }

      public void SaveSetting(string instance, string key, List<bool> val)
      {
         int count = val == null ? 0 : val.Count;
         SaveSetting(instance, key + ".Count", count);
         for (int i = 0; i < count; i++)
         {
            SaveSetting(instance, key + "." + i, val[i]);
         }
      }

      public void SaveSetting(string instance, string key, IEnumerable<float> val)
      {
         int count = val == null ? 0 : val.Count();
         SaveSetting(instance, key + ".Count", count);
         for (int i = 0; i < count; i++)
         {
            SaveSetting(instance, key + "." + i, val.ElementAt(i));
         }
      }

      public void SaveSetting(string instance, string key, IEnumerable<double> val)
      {
         int count = val == null ? 0 : val.Count();
         SaveSetting(instance, key + ".Count", count);
         for (int i = 0; i < count; i++)
         {
            SaveSetting(instance, key + "." + i, val.ElementAt(i));
         }
      }

      public void SaveSetting(string instance, string key, UInt32 val)
      {
         SaveSetting(instance, key, val.ToString(CultureInfo.InvariantCulture));
      }

      public void SaveSetting(string instance, string key, Int32 val)
      {
         SaveSetting(instance, key, val.ToString(CultureInfo.InvariantCulture));
      }

      public void SaveSetting(string instance, string key, float val)
      {
         SaveSetting(instance, key, val.ToString(CultureInfo.InvariantCulture));
      }

      public void SaveSetting(string instance, string key, double val)
      {
         SaveSetting(instance, key, val.ToString(CultureInfo.InvariantCulture));
      }

      public void SaveSetting(string instance, string key, bool val)
      {
         SaveSetting(instance, key, val.ToString(CultureInfo.InvariantCulture));
      }

      public void SaveSetting(string instance, string key, System.Drawing.Color val)
      {
         SaveSetting(instance, key, ColorConverter.Color_to_RGBAString(val));
      }

      public void SaveSetting(string instance, string key, object value)
      {
         if (mXmlNode == null)
         {
            return;
         }

         if (!value.GetType().IsSerializable)
         {
            return;
         }

         // Serialise to the XML document
         XmlNode parent = mXmlNode;
         XmlNode node;

         if (parent != null)
         {
            try
            {
               node = parent.SelectSingleNode(instance);
            }
            catch(System.Xml.XPath.XPathException) //only catch path exception
            {
               //create new node for this instance
               node = mXmlDocument.CreateElement(instance);
               parent.AppendChild(node);
            }

            //create new subKey
            XDocument xdoc = new XDocument();

            using(XmlWriter writer = xdoc.CreateWriter())
            {
               var ser = new XmlSerializer(value.GetType());
               ser.Serialize(writer, value);
            }

            //Remove namespace info
            xdoc.Descendants()
                .Attributes()
                .Where(x => x.IsNamespaceDeclaration)
                .Remove();

            //remove old entry and add new
            try
            {
               var elem = XElement.Parse(node.OuterXml);
               elem.Elements().Where(itr => itr.Name == xdoc.Elements().First().Name).Remove();
               if (key != String.Empty)
               {
                  xdoc.Elements().First().Name = key;
               }

               elem.Add(xdoc.Elements().First());
            }
            catch(System.Xml.XPath.XPathException) //only catch path exception
            {
               //create new node for this instance
               //node = mXmlDocument.CreateElement(instance);
               //parent.AppendChild(node);
            }

            return;
         }
      }

      public void SaveColumnWidths(string instance, TreeView treeview)
      {
         int okcount = 0;
         StringBuilder widths = new StringBuilder();
         foreach (TreeViewColumn col in treeview.Columns)
         {
            int w = col.Width;
            if (w != 0)
            {
               okcount++;
            }

            if (widths.Length > 0)
            {
               widths.Append(";");
            }

            string title = (col is TreeViewColumnLocalized) ? (col as TreeViewColumnLocalized).LocalizationKey : col.Title;
            title = Regex.Replace(title, "[=;]", "");
            if (!string.IsNullOrEmpty(title))
            {
               widths.Append(title).Append("=").Append(col.Width);
            }
         }

         if (okcount == treeview.Columns.Count())
         {
            SaveSetting(instance, treeview.Name + ".ColumnWidths", widths.ToString());
         }
      }


      public void LoadColumnWidths(string instance, TreeView treeview)
      {
         string columnwidths = LoadSetting(instance, treeview.Name + ".ColumnWidths", "");
         string[] all = columnwidths.Split(';');
         foreach (string s in all)
         {
            string[] one = s.Split('=');
            if (one.Length == 2)
            {
               int width;
               if (Int32.TryParse(one[1], out width))
               {
                  if (width == 0) // quickfix: make sure no columns become invisible
                  {
                     continue;
                  }

                  foreach (TreeViewColumn col in treeview.Columns)
                  {
                     string title = (col is TreeViewColumnLocalized) ? (col as TreeViewColumnLocalized).LocalizationKey : col.Title;
                     title = Regex.Replace(title, "[=;]", "");
                     if (!string.IsNullOrEmpty(title) && title.ToLowerInvariant() == one[0].ToLowerInvariant())
                     {
                        col.SetWidth(width);
                     }
                  }
               }
            }
         }
      }

      public void LoadConfigurationFile(String filename = null)
      {
         if (string.IsNullOrEmpty(filename))
         {
            filename = mCONFIGFILE;
         }

         NewFilename  = filename;
         mXmlDocument = new XmlDocument();

         if (!File.Exists(filename))
         {
            mXmlNode = mXmlDocument.AppendChild(mXmlDocument.CreateElement(CONFIG_ROOT_ELEMENT));
            return;
         }

         try
         {
            mXmlDocument.Load(filename);
         }
         catch
         {
            mXmlDocument = new XmlDocument();
         }

         mXmlNode = mXmlDocument.SelectSingleNode(CONFIG_ROOT_ELEMENT);
         if (mXmlNode == null)
         {
            mXmlNode = mXmlDocument.AppendChild(mXmlDocument.CreateElement(CONFIG_ROOT_ELEMENT));
         }

         ApplyDownwardsCompatibilityConfigSettingMappings();

         IsReadonly = LoadSetting("", "readonly", false);
      }

      public void SaveConfigurationFile()
      {
         if (!string.IsNullOrEmpty(NewFilename))
         {
            try
            {
               string dir = Path.GetDirectoryName(NewFilename);
               if (!Directory.Exists(dir))
               {
                  Directory.CreateDirectory(dir);
               }

               // open the file exclusively for writing, i.e., prevent other instances of us from interfering!
               using (FileStream f = new FileStream(NewFilename, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
               {
                  mXmlDocument.Save(f);
               }
            }
            catch(Exception)
            {
               //this.MessageWriteLine("Failed to save configuration file '{0}': {1}", NewFilename, e.ToString());
            }
         }
      }

      public void DeleteFile()
      {
         if (File.Exists(mCONFIGFILE))
         {
            File.Delete(mCONFIGFILE);
         }
      }

      public void RemapComponent(string from, string to)
      {
         XmlNode layouts = mXmlNode.SelectSingleNode("layouts");
         if (layouts != null)
         {
            XmlNodeList nodes = layouts.SelectNodes(@"//item[@id]"); // selects all nodes named "item" with attribute "id"
            foreach (XmlNode node in nodes)
            {
               foreach (XmlAttribute attr in node.Attributes)
               {
                  if (attr.Name == "id")
                  {
                     if (attr.Value == from)
                     {
                        attr.Value = to;
                     }
                     else if (attr.Value.StartsWith(from + "-"))
                     {
                        attr.Value = to + "-" + attr.Value.Substring(from.Length + 1);
                     }
                  }
               }
            }
         }

         List<XmlNode> todo = new List<XmlNode>();
         foreach (XmlNode node in mXmlNode.ChildNodes)
         {
            if (node.Name == from || node.Name.StartsWith(from + "-"))
            {
               todo.Add(node);
            }
         }


         foreach (XmlNode oldnode in todo)
         {
            string newname;
            if (oldnode.Name == from)
            {
               newname = to;
            }
            else if (oldnode.Name.StartsWith(from + "-"))
            {
               newname = to + "-" + oldnode.Name.Substring(from.Length + 1);
            }
            else
            {
               throw new InvalidDataException(); // we should never get here
            }

            XmlNode newnode = mXmlDocument.CreateNode(XmlNodeType.Element, newname, "");
            newnode.InnerXml = oldnode.InnerXml;
            mXmlNode.InsertAfter(newnode, oldnode);
            mXmlNode.RemoveChild(oldnode);
         }
      }

      public void SaveLayout(DockFrame DockFrame)
      {
         if (DockFrame == null)
         {
            return;
         }

         // step 1: save DockFrame layouts in memory file
         MemoryStream ms = new MemoryStream();
         XmlTextWriter xmlWriter = new XmlTextWriter(ms, Encoding.UTF8);
         DockFrame.SaveLayouts(xmlWriter);
         xmlWriter.Flush();

         // step 2: re-load as XmlDocument:
         XmlDocument doc = new XmlDocument();
         doc.Load(new XmlTextReader(new MemoryStream(ms.ToArray())));

         // select layouts and replace in XmlConfiguration
         // note that a node from other document must imported before use for add/replace
         XmlNode layouts = doc.SelectSingleNode("layouts");
         XmlNode newLayouts = mXmlDocument.ImportNode(layouts, true);
         XmlNode oldLayouts = mXmlNode.SelectSingleNode("layouts");

         if (oldLayouts != null)
         {
            mXmlNode.ReplaceChild(newLayouts, oldLayouts);
         }
         else
         {
            mXmlNode.AppendChild(newLayouts);
         }
      }

      public void LoadLayout(DockFrame dockFrame)
      {
         // load XML node "layouts" in a memory file
         // we should leave the implementation of the Mono Develop Docking as it is
         // to make it easier to update with newest version
         XmlNode layouts = mXmlNode.SelectSingleNode("layouts");
         if (layouts != null)
         {
            using(var ms = new MemoryStream())
            using(var xmlWriter = new XmlTextWriter(ms, Encoding.UTF8))
            {
               layouts.WriteTo(xmlWriter);
               xmlWriter.Flush();

               using(var xmlReader = new XmlTextReader(new MemoryStream(ms.ToArray())))
               {
                  dockFrame.LoadLayouts(xmlReader);
               }
            }
         }
      }

      #region Binary Persistency

      // TODO It does not really make sense to but binary blobs into XML... this way the file is not really editable/parsable anymore. Suggestion: Prefer using IPersistency.
      /// <summary>
      /// Load an object from persistence.
      /// The optional parameter 'item' can be used to identify the proper DockItem instance.
      /// </summary>

      //[Obsolete("Method is deprecated and will be removed soon")]
      public object LoadObject(String elementName, Type t, DockItem item)
      {
         String pimpedElementName = elementName;
         if (item != null)
         {
            pimpedElementName += "_" + item.Id.ToString();
         }

         if (mXmlNode == null || pimpedElementName == null)
         {
            return null;
         }

         XmlNode element = mXmlNode.SelectSingleNode(pimpedElementName);
         if (element == null)
         {
            return null;
         }

         // deserialize new method
         XmlNode node = element.SelectSingleNode(t.Name + "_FMT");
         if (node != null)
         {
            MemoryStream formattedStream = new MemoryStream();

            byte[] data = FromHexString(node.InnerText);
            formattedStream.Write(data, 0, data.Length);
            formattedStream.Flush();
            formattedStream.Seek(0, SeekOrigin.Begin);

            try
            {
               System.Runtime.Serialization.IFormatter formatter = new BinaryFormatter();
               object result = (object)formatter.Deserialize(formattedStream);
               return result;
            }
            catch
            {
               return null;
            }
         }

         // deserialize old method, only necessary to read old persistence, can be removed in some weeks
         node = element.SelectSingleNode(t.Name);
         if (node == null)
         {
            return null;
         }

         MemoryStream ms = new MemoryStream();
         XmlTextWriter xmlWriter = new XmlTextWriter(ms, Encoding.UTF8);

         node.WriteTo(xmlWriter);
         xmlWriter.Flush();
         XmlReader xmlReader = new XmlTextReader(new MemoryStream(ms.ToArray()));

         try
         {
            // TODO: construction of XmlSerializer(type) needs a lot of time totally unexpected, an optimization rergarding persistence could be necessary ...
            XmlSerializer serializer = new XmlSerializer(t);
            return serializer.Deserialize(xmlReader);
         }
         catch
         {
            return null;
         }
      }

      // Byte array from hexdump string
      // TODO this code is misplaced in this class, move it to somewhere else, e.g. "Tools"
      private static Byte[] FromHexString(String s)
      {
         if (s == null || (s.Length % 2) != 0)
         {
            return null;
         }

         Byte[] bytes = new Byte[s.Length / 2];
         for (int i = 0; i < s.Length / 2; i++)
         {
            bytes[i] = Convert.ToByte(s.Substring(i * 2, 2), 16);
         }

         return bytes;
      }

      #endregion

      /// <summary>
      /// Validates that configfile refers to an existing file on file system -or-
      /// when configfile is null ensures that config.xml does exist or will be created with the content from defaultconfig.xml.
      /// </summary>
      /// <param name="configfile"></param>
      private void EnsureConfigFileExists(string configfile)
      {
         if (!string.IsNullOrEmpty(configfile))
         {
            if (!File.Exists(configfile))
            {
               throw new Exception(string.Format("config file '{0}' not found", configfile));
            }

            mCONFIGFILE = configfile;
         }
         else if (!File.Exists(mCONFIGFILE))
         {
            string dir = System.IO.Path.GetDirectoryName(mCONFIGFILE);

            //create dir if necessary
            if (!string.IsNullOrEmpty(dir))
            {
               if (!Directory.Exists(dir))
               {
                  Directory.CreateDirectory(dir);
               }

               if (!Directory.Exists(dir))
               {
                  throw new Exception("cannot create local user data directory");
               }
            }

            //copy default to actual file
            if (File.Exists(mDEFAULTCONFIGFILE))
            {
               File.Copy(mDEFAULTCONFIGFILE, mCONFIGFILE);
            }
         }
      }

      private void ApplyDownwardsCompatibilityConfigSettingMappings()
      {
         Version versionConfig = new Version(LoadSetting("", "ConfigSavedByVersion", "0.0.0.0"));
         Version versionAsm = AssemblyInfoExt.Version;
         if (versionConfig.Major != 0 && versionConfig.Minor != 0)
         {
            if ((versionConfig.Major < 1) ||
                (versionConfig.Major == 1 && versionConfig.Minor < 1))
            {
               RemapComponent("TempoGiusto.MapViewer.MapViewer", "TempoGiusto.MapExplorer.MapExplorer");
               RemapComponent("TempoGiusto.MapViewer.MapLayers", "TempoGiusto.MapExplorer.MapExplorerLayers");
               RemapComponent("TempoGiusto.MapViewer.GeoFinder", "TempoGiusto.MapExplorer.GeoFinder");
               RemapComponent("TempoGiusto.MapViewerFUnit.MapViewerFUnit", "TempoGiusto.MapViewer.MapViewer");
            }

            if ((versionConfig.Major < 1) ||
                (versionConfig.Major == 1 && versionConfig.Minor < 3))
            {
               RemapComponent("TempoGiusto.Routing.Routes", "TempoGiusto.Routing.RoutesFromLog");
            }
         }
      }
   }
}
