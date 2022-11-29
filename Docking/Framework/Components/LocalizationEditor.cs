
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Docking.Framework.Interfaces;
using Docking.Tools;
using Docking.Widgets;
using Gtk;

namespace Docking.Components
{
   [System.ComponentModel.ToolboxItem(true)]
   public partial class LocalizationEditor : Component, ILocalizableComponent
   {
      private const int nodeIndex       = 0;
      private const int keyIndex        = 1;
      private const int usValueIndex    = 2;
      private const int localValueIndex = 3;


      private          int       displayedHashCode = 0;
      private readonly ListStore listStore;

      /// <summary>
      /// Initializes a new instance.
      /// Invoked by ComponentManager either on user action or during application startup.
      /// </summary>
      public LocalizationEditor()
      {
         Build();

         TreeViewColumn keyColumn = new TreeViewColumnLocalized() { Title        = "Key", Sizing          = TreeViewColumnSizing.Fixed, FixedWidth      = 150, SortColumnId = keyIndex };
         TreeViewColumn usValueColumn = new TreeViewColumnLocalized() { Title    = "US name", Sizing      = TreeViewColumnSizing.Fixed, FixedWidth      = 150, SortColumnId = usValueIndex };
         TreeViewColumn localValueColumn = new TreeViewColumnLocalized() { Title = "Current name", Sizing = TreeViewColumnSizing.Autosize, SortColumnId = localValueIndex };

         treeview1.AppendColumn(keyColumn);
         treeview1.AppendColumn(usValueColumn);
         treeview1.AppendColumn(localValueColumn);

         CellRendererText keyCell = new CellRendererText();
         CellRendererText usValueCell = new CellRendererText();
         CellRendererText localValueCell = new CellRendererText();

         localValueCell.Editable =  true;
         localValueCell.Edited   += new EditedHandler(localValueCell_Edited);

         keyColumn.PackStart(keyCell, true);
         usValueColumn.PackStart(usValueCell, true);
         localValueColumn.PackStart(localValueCell, true);

         keyColumn.AddAttribute(keyCell, "text", keyIndex);
         usValueColumn.AddAttribute(usValueCell, "text", usValueIndex);
         localValueColumn.AddAttribute(localValueCell, "text", localValueIndex);

         // Create a model that will hold the content, assign the model to the TreeView
         listStore       = new ListStore(typeof(Localization.Node), typeof(string), typeof(string), typeof(string));
         treeview1.Model = listStore;
         treeview1.GetColumn(0).Click(); // enable sorting 1st column ascending as default

         treeview1.SearchColumn = 0;
         treeview1.SearchEqualFunc = (TreeModel model, int column, string key, TreeIter iter) =>
         {
            Localization.Node node = (Localization.Node)model.GetValue(iter, column);
            if (node != null)
            {
               return !node.Key.ToLower().Contains(key.ToLower());
            }

            return true;
         };

         button1.Clicked += (sender, e) =>
         {
            ComponentManager.Localization.WriteChangedResourceFiles();
            UpdateChangeCount();
         };

         buttonTranslate.Clicked    += new EventHandler(buttonTranslate_Clicked);
         buttonTranslateAll.Clicked += new EventHandler(buttonTranslateAll_Clicked);
      }

      public override void Loaded()
      {
         base.Loaded();

         UpdateList();
         UpdateChangeCount();
      }

      public override bool IsCloseOK()
      {
         // TODO if the current script is unsaved, prompt the user here to save it and offer him a "Cancel" button.
         // When the user presses "Cancel", return false from this function to prevent the closing from happening.
         return true;
      }

      /// <summary>
      /// Translates a string into another language using Google's translate API JSON calls.
      /// <seealso>Class TranslationServices</seealso>
      /// </summary>
      /// <param name="Text">Text to translate. Should be a single word or sentence.</param>
      /// <param name="FromCulture">
      /// Two letter culture (en of en-us, fr of fr-ca, de of de-ch)
      /// </param>
      /// <param name="ToCulture">
      /// Two letter culture (as for FromCulture)
      /// </param>
      public string TranslateGoogle(string text, string fromCulture, string toCulture)
      {
         fromCulture = fromCulture.ToLower();
         toCulture   = toCulture.ToLower();

         // normalize the culture in case something like en-us was passed
         // retrieve only en since Google doesn't support sub-locales
         string[] tokens = fromCulture.Split('-');
         if (tokens.Length > 1)
         {
            fromCulture = tokens[0];
         }

         // normalize ToCulture
         tokens = toCulture.Split('-');
         if (tokens.Length > 1)
         {
            toCulture = tokens[0];
         }

         string url = string.Format(@"http://translate.google.com/translate_a/t?client=j&text={0}&hl=en&sl={1}&tl={2}",
                                    System.Web.HttpUtility.UrlEncode(text), fromCulture, toCulture);

         // MUST add a known browser user agent or else response encoding doen't return UTF-8 (WTF Google?)
         var webRequestInfo = new WebRequestInfo()
         {
            Headers =
            {
               { HttpRequestHeader.UserAgent, "Mozilla/5.0" },
               { HttpRequestHeader.AcceptCharset, "UTF-8" }
            },
            Encoding = Encoding.UTF8 // Make sure we have response encoding to UTF-8
         };

         // Retrieve Translation with HTTP GET call
         string html = ComponentManager.WebDownload.DownloadString(url, webRequestInfo);

         // Extract out trans":"...[Extracted]...","from the JSON string
         return Regex.Match(html, "trans\":(\".*?\"),\"", RegexOptions.IgnoreCase).Groups[1].Value.Trim(new char[] { '"' });
      }

      private void buttonTranslateAll_Clicked(object sender, EventArgs e)
      {
         if (ResponseType.Yes != MessageBox.Show(null, MessageType.Question,
                                                 ButtonsType.YesNo,
                                                 "Sure to translate all empty resources?".FormatLocalizedWithPrefix(this)))
         {
            return;
         }

         TreeIter iter;
         for (bool ok = listStore.GetIterFirst(out iter); ok; ok = listStore.IterNext(ref iter))
         {
            Localization.Node usNode = listStore.GetValue(iter, nodeIndex) as Localization.Node;
            Localization.Node currentNode = ComponentManager.Localization.FindCurrentNode(usNode.Key);
            if (currentNode != null && !string.IsNullOrWhiteSpace(currentNode.Value))
            {
               continue; // translate only empty fields
            }

            string translation = TranslateGoogle(usNode.Value, ComponentManager.Localization.DefaultLanguageCode, ComponentManager.Localization.CurrentLanguageCode);
            if (string.IsNullOrWhiteSpace(translation))
            {
               continue;
            }

            listStore.SetValue(iter, localValueIndex, translation);

            if (currentNode != null)
            {
               currentNode.Value = translation;
            }
            else
            {
               Localization.Node newNode = new Localization.Node(usNode.Key, translation, "", usNode.Base, "", "");
               ComponentManager.Localization.AddNewCurrentNode(newNode);
            }
         }

         ComponentManager.MenuService.UpdateLanguage(true);
         UpdateChangeCount();
      }

      private void buttonTranslate_Clicked(object sender, EventArgs e)
      {
         TreeSelection selection = treeview1.Selection;
         TreeModel model;
         TreeIter iter;
         if (selection.GetSelected(out model, out iter))
         {
            Localization.Node usNode = listStore.GetValue(iter, nodeIndex) as Localization.Node;
            Localization.Node currentNode = ComponentManager.Localization.FindCurrentNode(usNode.Key);
            if (usNode == null)
            {
               return;
            }

            string translation = TranslateGoogle(usNode.Value, ComponentManager.Localization.DefaultLanguageCode, ComponentManager.Localization.CurrentLanguageCode);
            if (string.IsNullOrWhiteSpace(translation))
            {
               return;
            }

            string reverted = TranslateGoogle(translation, ComponentManager.Localization.CurrentLanguageCode, ComponentManager.Localization.DefaultLanguageCode);

            ComponentManager.MessageWriteLine("Translate {0}-{1} '{2}' -> '{3}'", ComponentManager.Localization.DefaultLanguageCode, ComponentManager.Localization.CurrentLanguageCode, usNode.Value, translation);
            ComponentManager.MessageWriteLine("Translate {0}-{1} '{2}' -> '{3}'", ComponentManager.Localization.CurrentLanguageCode, ComponentManager.Localization.DefaultLanguageCode, translation, reverted);

            if (currentNode != null && currentNode.Value.Length > 0)
            {
               if (ResponseType.Yes != MessageBox.Show(null, MessageType.Question,
                                                       ButtonsType.YesNo,
                                                       "Overwrite value with new translation ?".FormatLocalizedWithPrefix(this)))
               {
                  return;
               }
            }

            listStore.SetValue(iter, localValueIndex, translation);

            if (currentNode != null)
            {
               currentNode.Value = translation;
            }
            else
            {
               Localization.Node newNode = new Localization.Node(usNode.Key, translation, "", usNode.Base, "", "");
               ComponentManager.Localization.AddNewCurrentNode(newNode);
            }

            ComponentManager.MenuService.UpdateLanguage(true);
            UpdateChangeCount();
         }
      }

      private void localValueCell_Edited(object o, EditedArgs args)
      {
         TreeIter iter;
         if (listStore.GetIter(out iter, new TreePath(args.Path)))
         {
            listStore.SetValue(iter, localValueIndex, args.NewText);

            Localization.Node node = listStore.GetValue(iter, nodeIndex) as Localization.Node;
            Localization.Node ln = ComponentManager.Localization.FindCurrentNode(node.Key);
            if (ln != null)
            {
               ln.Value = args.NewText;
            }
            else
            {
               Localization.Node newNode = new Localization.Node(node.Key, args.NewText, "", node.Base, "", "");
               ComponentManager.Localization.AddNewCurrentNode(newNode);
            }

            ComponentManager.MenuService.UpdateLanguage(true);
            UpdateChangeCount();
         }
      }

      private void UpdateChangeCount()
      {
         labelChanges.LabelProp = Localization.Format(this, "Changes: {0}", ComponentManager.Localization.CurrentChangeCount);
      }

      private void UpdateList()
      {
         if (displayedHashCode == ComponentManager.Localization.GetCurrentHashcode())
         {
            return;
         }

         displayedHashCode = ComponentManager.Localization.GetCurrentHashcode();
         listStore.Clear();

         Dictionary<string, Localization.Node> dn = ComponentManager.Localization.GetDefaultNodes();
         foreach (Localization.Node node in dn.Values)
         {
            TreeIter iter = listStore.Append();
            listStore.SetValue(iter, nodeIndex, node);
            listStore.SetValue(iter, keyIndex, node.Key);
            listStore.SetValue(iter, usValueIndex, node.Value);

            Localization.Node ln = ComponentManager.Localization.FindCurrentNode(node.Key);
            if (ln != null)
            {
               listStore.SetValue(iter, localValueIndex, ln.Value);
            }
         }
      }

      #region ILocalizableComponent

      string ILocalizableComponent.Name
      {
         get { return "Localization Editor"; }
      }

      void ILocalizableComponent.LocalizationChanged(DockItem item)
      {
         UpdateList();
         UpdateChangeCount();
      }

      #endregion
   }

   /// <summary>
   /// Factory provides information required to create <seealso cref="LocalizationEditor"/> instances.
   /// </summary>
   public class LocalizationEditorFactory : ComponentFactory
   {
      public override Type TypeOfInstance
      {
         get { return typeof(LocalizationEditor); }
      }

      public override String Name
      {
         get { return "Localization Editor"; }
      }

      public override String MenuPath
      {
         get { return @"View\Infrastructure\Localization Editor"; }
      }

      public override String Comment
      {
         get { return "Edit possibility for all localization strings"; }
      }

      public override Gdk.Pixbuf Icon
      {
         get { return ResourceLoader_Docking.LoadPixbuf("Localization-16.png"); }
      }
   }
}
