
using System;
using System.Globalization;
using System.IO;
using Docking.Tools;

namespace Docking.Components
{
   public class LicenseFile
   {
      private readonly string mFilename;

      public delegate bool DecodeFunc(string s, out bool b1, out bool b2, out string s1, out string s2, out string s3);

      internal LicenseFile()
      {
         mFilename = Path.Combine(AssemblyInfoExt.LocalSettingsFolder, "license.txt");
      }

      public string LicenseContent { get; set; }

      public void Decode(DecodeFunc decoder, out LicenseGroup licenseOptions, out DateTime expireDate)
      {
         licenseOptions = new LicenseGroup();
         expireDate     = DateTime.MinValue;

         bool expired;
         bool wrongUserOrMacAddress;
         string expiration;
         string options;
         string commitID;
         bool syntax_ok = decoder(LicenseContent, out expired, out wrongUserOrMacAddress, out expiration, out options, out commitID);

         if (syntax_ok && !wrongUserOrMacAddress && !expired)
         {
            foreach (string licOptionLine in options.Split(new char[] { '|', ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
               var licOption = licOptionLine.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
               if (licOption.Length == 2)
               {
                  bool flag;
                  if (bool.TryParse(licOption[1], out flag))
                  {
                     licenseOptions.SetEnabling(licOption[0], flag);
                  }
               }
            }

            expireDate = DateTime.ParseExact(expiration, "yyyy-MM", CultureInfo.InvariantCulture);
         }
      }

      public bool Save()
      {
         try
         {
            StreamWriter file = new StreamWriter(mFilename, false);
            file.Write(LicenseContent);
            file.Close();
         }
         catch
         {
            return false;
         }

         return true;
      }

      public bool Load()
      {
         try
         {
            if (!File.Exists(mFilename) || (new FileInfo(mFilename)).Length > 10000)
            {
               return false;
            }

            LicenseContent = File.ReadAllText(mFilename)
                                 .Replace(" ", "") // we tolerate any whitespace here that might result from copying and pasting a license code manually into the license.txt file
                                 .Replace("\t", "")
                                 .Replace("\n", "")
                                 .Replace("\r", "");
         }
         catch
         {
            LicenseContent = "";
            return false;
         }

         return true;
      }
   }
}
