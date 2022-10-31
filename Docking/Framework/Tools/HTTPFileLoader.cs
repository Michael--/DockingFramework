
using System;
using System.IO;
using Docking.Components;
using Docking.Tools;

namespace Docking.Components
{
   internal class HTTPFileLoader
   {
      private const string URL_PREFIX_FILE  = "file://";
      private const string URL_PREFIX_HTTP  = "http://";
      private const string URL_PREFIX_HTTPS = "https://";

      private readonly ComponentManager mManager;

      internal HTTPFileLoader(ComponentManager manager)
      {
         mManager = manager;
      }

      public bool OpenURL(string url_)
      {
         string url = Uri.UnescapeDataString(url_);
         if (url.StartsWith(URL_PREFIX_FILE))
         {
            string filename = url.Substring(URL_PREFIX_FILE.Length);
            if (Platform.IsWindows)
            {
               // treat how local filenames are encoded on Windows. Example: file:///D:/some/folder/myfile.txt
               if (filename.Length >= 3 &&
                   filename[0] == '/' &&

                   //filename[1]=='C' && // drive letter
                   filename[2] == ':')
               {
                  filename = filename.Substring(1);
               }

               filename = filename.Replace('/', Path.DirectorySeparatorChar);
            }

            return mManager.OpenFile(filename);
         }
         else if (url.StartsWith(URL_PREFIX_HTTP) || url.StartsWith(URL_PREFIX_HTTPS))
         {
            string filename;
            if (url.StartsWith(URL_PREFIX_HTTP))
            {
               filename = url.Substring(URL_PREFIX_HTTP.Length);
            }
            else if (url.StartsWith(URL_PREFIX_HTTPS))
            {
               filename = url.Substring(URL_PREFIX_HTTPS.Length);
            }
            else
            {
               return false;
            }

            string[] portions = filename.Split('/');

            if (portions.Length < 1)
            {
               return false;
            }

            filename = portions[portions.Length - 1];

            if (!filename.Contains("."))
            {
               filename = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName) + " TempFile.tmp";
            }

            filename = Path.Combine(Path.GetTempPath(), filename);

            if (File.Exists(filename))
            {
               int i = 2;
               string newfilename = filename;
               while (File.Exists(newfilename))
               {
                  newfilename = Path.GetFileNameWithoutExtension(filename) + " (" + i + ")" + Path.GetExtension(filename);
                  newfilename = Path.Combine(Path.GetTempPath(), newfilename);
                  i++;
               }

               filename = newfilename;
            }

            FileStream file = null;
            using(var www = new WebClient2())
            {
               try
               {
                  file = File.Create(filename, 10000, FileOptions.DeleteOnClose);
                  www.OpenRead(url).CopyTo(file);
               }
               catch
               {
                  file = null;
               }
            }

            if (file != null)
            {
               bool result = mManager.OpenFile(filename);
               file.Close(); // will implicitly delete the file, see FileOptions.DeleteOnClose above
               file = null;
               return result;
            }
         }

         return false;
      }
   }
}
