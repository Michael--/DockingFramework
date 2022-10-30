
using System;
using System.Collections.Generic;
using System.IO;
using Docking.Components;
using Docking.Tools;
using Docking.Widgets;
using Gtk;

namespace Docking.Widgets
{
   /// <summary>
   /// Basically a factory for various dialog types
   /// </summary>
   public class DialogProvider
   {
      private readonly MainWindowBase mMainWindowBase;

      internal DialogProvider(MainWindowBase mainWindowBase)
      {
         mMainWindowBase = mainWindowBase;
      }

      #region Dialogs
      public String OpenFolderDialog(string title, string startFolder = null)
      {
         String result = null;

         var dlg = new FileChooserDialogLocalized(title, mMainWindowBase, FileChooserAction.SelectFolder,
                                              "Select".L(), ResponseType.Accept,
                                              "Cancel".L(), ResponseType.Cancel);

         if (!String.IsNullOrEmpty(startFolder))
            dlg.SetCurrentFolder(startFolder);

         if (RunFileChooserDialogLocalized(dlg, null) == (int)ResponseType.Accept)
            result = dlg.Filename;

         dlg.Destroy();
         return result;
      }

      public String OpenFileDialog(string prompt, FileFilterExt filter = null, string startFolder = null)
      {
         List<FileFilterExt> filters = new List<FileFilterExt>();
         if (filter != null)
         {
            filters.Add(filter);
         }

         return OpenFileDialog(prompt, filters, startFolder);
      }

      public String OpenFileDialog(string title, List<FileFilterExt> filters, string startFolder = null)
      {
         string result = null;

         var dlg = new FileChooserDialogLocalized(title, mMainWindowBase, FileChooserAction.Open,
                                              "Open".L(), ResponseType.Accept,
                                              "Cancel".L(), ResponseType.Cancel);
         if (!String.IsNullOrEmpty(startFolder))
         {
            dlg.SetCurrentFolder(startFolder);
         }

         if (RunFileChooserDialogLocalized(dlg, filters) == (int)ResponseType.Accept)
         {
            result = dlg.Filename;
            mMainWindowBase.AddRecentFile(result);
         }

         dlg.Destroy();
         return result;
      }

      public string[] OpenFilesDialog(string prompt, FileFilterExt filter = null, string startFolder = null)
      {
         List<FileFilterExt> filters = new List<FileFilterExt>();
         if (filter != null)
         {
            filters.Add(filter);
         }

         return OpenFilesDialog(prompt, filters, startFolder);
      }

      public string[] OpenFilesDialog(string title, List<FileFilterExt> filters, string startFolder = null)
      {
         string[] result = null;

         var dlg = new FileChooserDialogLocalized(title, mMainWindowBase, FileChooserAction.Open,
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
                  mMainWindowBase.AddRecentFile(filename);
               }
            }
         }

         dlg.Destroy();
         return result;
      }

      public String SaveFileDialog(string prompt, FileFilterExt filter = null, string currentFilename = null)
      {
         List<FileFilterExt> filters = new List<FileFilterExt>();
         if (filter != null)
         {
            filters.Add(filter);
         }
         return SaveFileDialog(prompt, filters, currentFilename);
      }

      public String SaveFileDialog(string title, List<FileFilterExt> filters = null, string currentFilename = null)
      {
         string result = null;

         var dlg = new FileChooserDialogLocalized(title, mMainWindowBase, FileChooserAction.Save,
                                              "Save".L(), ResponseType.Accept,
                                              "Cancel".L(), ResponseType.Cancel);
         if (currentFilename != null)
         {
            var dirname = System.IO.Path.GetDirectoryName(currentFilename);
            if (dirname != null && dirname.Length > 0)
            {
               dlg.SetCurrentFolderUri(dirname);
            }

            var filename = System.IO.Path.GetFileName(currentFilename);
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
                           firstext = ext;
                        if (result.EndsWith(ext, true, null))
                        {
                           correct_extension_found = true;
                           break;
                        }
                     }
                     if (!correct_extension_found && firstext != null)
                        result += firstext;
                     break;
                  }
               }
            }

            if (File.Exists(result) &&
               MessageBox.Show(MessageType.Question, ButtonsType.YesNo, "File '{0}' already exists.\nDo you want to overwrite it?", result) != ResponseType.Yes)
               result = null;
         }

         if (result != null)
         {
            mMainWindowBase.AddRecentFile(result);
         }
         dlg.Destroy();
         return result;
      }

      public int SelectComponentDialog(ref List<ComponentFactoryInformation> info, ref List<Component> components)
      {
         var dlg = new ComponentSelectorDialog(mMainWindowBase, info, components);
         int result = dlg.Run();
         dlg.GetSelectedComponents(ref info, ref components);
         dlg.Hide();

         return result;
      }

      private int RunFileChooserDialogLocalized(FileChooserDialogLocalized dlg, List<FileFilterExt> filters)
      {
         dlg.ShowHidden = true;

         if (filters != null && filters.Count > 0)
         {
            if (filters.Count > 1)
            {
               FileFilterExt combinedfilter = new FileFilterExt();
               foreach (FileFilterExt filter in filters)
                  foreach (string pattern in filter.GetAdjustedPattern())
                     combinedfilter.AddPattern(pattern);
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

      #endregion
   }
}
