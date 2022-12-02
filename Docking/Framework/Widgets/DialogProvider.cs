
using System;
using System.Collections.Generic;
using System.IO;
using Docking.Components;
using Docking.Tools;
using Gtk;

namespace Docking.Widgets
{
   /// <summary>
   /// Basically a factory for various dialog types
   /// </summary>
   public static class DialogProvider
   {
      public class DialogParameters
      {
         public ButtonsType  btnType         = ButtonsType.None;
         public ResponseType defaultResponse = ResponseType.None;
         public DialogFlags  flags           = DialogFlags.Modal;
         public string       text            = null;
         public string       title           = null;
         public MessageType  type            = MessageType.Other;
         public bool         usemarkup       = false;
      }

      public static ResponseType ShowDialog<TDialog>(DialogParameters parameter)
      {
         Dialog dialog = null;
         if (typeof(MessageDialog) == typeof(TDialog))
         {
            dialog = new MessageDialog(MainAppWindowInstance.GtkWindow,
                                       parameter.flags,
                                       parameter.type,
                                       parameter.btnType,
                                       parameter.text ?? string.Empty);

            dialog.Title           = parameter.title ?? string.Empty;
            dialog.DefaultResponse = parameter.defaultResponse;
         }

         //show dialog and wait for user response ...
         ResponseType response = (ResponseType)dialog.Run();
         dialog.Destroy();

         return response;
      }


      public static int SelectComponentDialog(ref List<ComponentFactoryInformation> info, ref List<Component> components)
      {
         var dlg = new ComponentSelectorDialog(MainAppWindowInstance.GtkWindow,
                                               info,
                                               components);
         int result = dlg.Run();
         dlg.GetSelectedComponents(ref info, ref components);
         dlg.Hide();

         return result;
      }

      private static int RunFileChooserDialogLocalized(FileChooserDialogLocalized dlg, List<FileFilterExt> filters)
      {
         dlg.ShowHidden = true;

         if (filters != null && filters.Count > 0)
         {
            if (filters.Count > 1)
            {
               FileFilterExt combinedfilter = new FileFilterExt();
               foreach (FileFilterExt filter in filters)
               foreach (string pattern in filter.GetAdjustedPattern())
               {
                  combinedfilter.AddPattern(pattern);
               }

               combinedfilter.Name = "All Known File Types".L();
               dlg.AddFilter(combinedfilter);
            }

            foreach (FileFilterExt filter in filters)
            {
               dlg.AddFilter(filter);
            }

            dlg.AddFilter(new FileFilterExt("*", "All Files".L()) { Name = "All Files".L() });
         }

#if false
         // we sadly cannot do this here, because we need the getters inside dlg (like .Filename), and they will no longer have proper contents after .Destroy()
         int result = dlg.Run();
         dlg.Destroy();
         return result;
#endif

         return dlg.Run();
      }

      #region OpenFile/OpenFolder dialog

      public static String OpenFolderDialog(string title, string startFolder = null)
      {
         String result = null;

         var dlg = new FileChooserDialogLocalized(title,
                                                  MainAppWindowInstance.GtkWindow,
                                                  FileChooserAction.SelectFolder,
                                                  "Select".L(), ResponseType.Accept,
                                                  "Cancel".L(), ResponseType.Cancel);

         if (!String.IsNullOrEmpty(startFolder))
         {
            dlg.SetCurrentFolder(startFolder);
         }

         if (RunFileChooserDialogLocalized(dlg, null) == (int)ResponseType.Accept)
         {
            result = dlg.Filename;
         }

         dlg.Destroy();
         return result;
      }

      public static String OpenFileDialog(string prompt, FileFilterExt filter = null, string startFolder = null)
      {
         List<FileFilterExt> filters = new List<FileFilterExt>();
         if (filter != null)
         {
            filters.Add(filter);
         }

         return OpenFileDialog(prompt, filters, startFolder);
      }

      public static String OpenFileDialog(string title, List<FileFilterExt> filters, string startFolder = null)
      {
         string result = null;

         var dlg = new FileChooserDialogLocalized(title,
                                                  MainAppWindowInstance.GtkWindow,
                                                  FileChooserAction.Open,
                                                  "Open".L(), ResponseType.Accept,
                                                  "Cancel".L(), ResponseType.Cancel);
         if (!String.IsNullOrEmpty(startFolder))
         {
            dlg.SetCurrentFolder(startFolder);
         }

         if (RunFileChooserDialogLocalized(dlg, filters) == (int)ResponseType.Accept)
         {
            result = dlg.Filename;
            MainAppWindowInstance.SingleInstance.AddRecentFile(result);
         }

         dlg.Destroy();
         return result;
      }

      public static string[] OpenFilesDialog(string prompt, FileFilterExt filter = null, string startFolder = null)
      {
         List<FileFilterExt> filters = new List<FileFilterExt>();
         if (filter != null)
         {
            filters.Add(filter);
         }

         return OpenFilesDialog(prompt, filters, startFolder);
      }

      public static string[] OpenFilesDialog(string title, List<FileFilterExt> filters, string startFolder = null)
      {
         string[] result = null;

         var dlg = new FileChooserDialogLocalized(title,
                                                  MainAppWindowInstance.GtkWindow,
                                                  FileChooserAction.Open,
                                                  "Open".L(), ResponseType.Accept,
                                                  "Cancel".L(), ResponseType.Cancel);
         if (!String.IsNullOrEmpty(startFolder))
         {
            dlg.SetCurrentFolder(startFolder);
         }

         dlg.SelectMultiple = true;

         if (RunFileChooserDialogLocalized(dlg, filters) == (int)ResponseType.Accept)
         {
            result = dlg.Filenames;
            if (result != null)
            {
               foreach (string filename in result)
               {
                  MainAppWindowInstance.SingleInstance.AddRecentFile(filename);
               }
            }
         }

         dlg.Destroy();
         return result;
      }

      #endregion

      #region SaveFileDialog

      public static String SaveFileDialog(string prompt, FileFilterExt filter = null, string currentFilename = null)
      {
         List<FileFilterExt> filters = new List<FileFilterExt>();
         if (filter != null)
         {
            filters.Add(filter);
         }

         return SaveFileDialog(prompt, filters, currentFilename);
      }

      public static String SaveFileDialog(string title, List<FileFilterExt> filters = null, string currentFilename = null)
      {
         string result = null;

         var dlg = new FileChooserDialogLocalized(title,
                                                  MainAppWindowInstance.GtkWindow,
                                                  FileChooserAction.Save,
                                                  "Save".L(), ResponseType.Accept,
                                                  "Cancel".L(), ResponseType.Cancel);
         if (currentFilename != null)
         {
            var dirname = Path.GetDirectoryName(currentFilename);
            if (dirname != null && dirname.Length > 0)
            {
               dlg.SetCurrentFolderUri(dirname);
            }

            var filename = Path.GetFileName(currentFilename);
            if (filename != null && filename.Length > 0)
            {
               dlg.CurrentName = filename;
            }
         }

         if (RunFileChooserDialogLocalized(dlg, filters) == (int)ResponseType.Accept)
         {
            result = dlg.Filename;

            FileFilter selectedFilter = dlg.Filter;
            if (selectedFilter != null)
            {
               foreach (FileFilterExt f in filters)
               {
                  if (f == selectedFilter)
                  {
                     bool correct_extension_found = false;
                     string firstext = null;
                     foreach (string pattern in f.GetPattern())
                     {
                        string ext = pattern.TrimStart('*');
                        if (firstext == null)
                        {
                           firstext = ext;
                        }

                        if (result.EndsWith(ext, true, null))
                        {
                           correct_extension_found = true;
                           break;
                        }
                     }

                     if (!correct_extension_found && firstext != null)
                     {
                        result += firstext;
                     }

                     break;
                  }
               }
            }

            if (File.Exists(result) &&
                MessageBox.Show(MessageType.Question, ButtonsType.YesNo, "File '{0}' already exists.\nDo you want to overwrite it?", result) != ResponseType.Yes)
            {
               result = null;
            }
         }

         if (result != null)
         {
            MainAppWindowInstance.SingleInstance.AddRecentFile(result);
         }

         dlg.Destroy();
         return result;
      }

      #endregion
   }
}
