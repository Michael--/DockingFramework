
using System;
using System.Net;
using System.Security;
using System.Security.Cryptography;


namespace Docking.Tools
{
   public class WebClientSettings
   {
      public WebClientSettings()
      {
         UserAgent              = null;
         UseProxy               = false;
         UseSystemProxySettings = true;
         ProxyServer            = null;
         ProxyPort              = 0;
         ProxyUsername          = null;
         ProxyPassword          = new EncryptedPassword();
      }

      public bool UseProxy                { get; set; }
      public bool UseSystemProxySettings  { get; set; }
      public int ProxyPort                { get; set; }
      public string UserAgent             { get; set; }
      public string ProxyServer           { get; set; }
      public string ProxyUsername         { get; set; }

      public EncryptedPassword ProxyPassword { get; private set; }
   }


   /// <summary>
   /// When using this class, make sure your program has properly set up the global variable ServicePointManager.SecurityProtocol
   /// to configure which HTTPS client modes it shall support. This normally happens somewhere during program startup in main().
   /// </summary>
   public class WebClient2 : WebClient
   {
      public static readonly WebClientSettings Settings = new WebClientSettings();

      public WebClient2(bool withUserAgent = false)
      {
         if (withUserAgent)
         {
            if (!string.IsNullOrEmpty(Settings.UserAgent))
            {
               Headers.Add(HttpRequestHeader.UserAgent, Settings.UserAgent);
            }
         }

         if (Settings.UseProxy)
         {
            if (Settings.UseSystemProxySettings)
            {
               // TODO not yet implemented
               // TODO retrieve proxy settings e.g. from Windows Control Panel
               // TODO use that data here
            }
            else
            {
               if (!string.IsNullOrEmpty(Settings.ProxyServer))
               {
                  Proxy = new WebProxy(Settings.ProxyServer, Settings.ProxyPort);

                  if (!string.IsNullOrEmpty(Settings.ProxyUsername))
                  {
                     Proxy.Credentials = new NetworkCredential(Settings.ProxyUsername, Settings.ProxyPassword.SecurePassword);
                  }
               }
            }
         }
      }

      public CookieContainer CookieContainer { get; set; } // default: null - if needed, set this before making a request

      public string Referrer { get; set; }                  // default: null - if needed, set this before making a request

      /// <summary>
      /// Returns a System.Net.WebRequest object for the specified resource.
      /// See http://stackoverflow.com/questions/1777221/using-cookiecontainer-with-webclient-class
      /// </summary>
      /// <param name="url"></param>
      /// <returns></returns>
      protected override WebRequest GetWebRequest(Uri url)
      {
         HttpWebRequest req = base.GetWebRequest(url) as HttpWebRequest;
         if (req != null)
         {
            if (CookieContainer != null)
            {
               req.CookieContainer = CookieContainer;
            }

            if (!string.IsNullOrEmpty(Referrer))
            {
               req.Referer = Referrer;
            }
         }

         return req;
      }
   }

   /// <summary>
   /// Carries the secure encrypted password.
   /// </summary>
   public class EncryptedPassword
   {
      // An array of random numbers used to encrypt the password for file storage (in the config).
      // TODO This is still not an ideal solution as everybody knowing these numbers can decrypt the password.
      // However, we somewhere need to persist the proxy password and do not want to do it in plaintext.
      // Just computing the hash of the password will not be enough since we need to send it to the proxy as a NetworkCredential()
      private static readonly byte[] Entropy = { 28, 1, 17, 20, 99, 157, 14, 31, 89, 254, 88, 4, 11, 99, 20, 18, 1, 14, 74, 33 };

      private string mPasswordClearText = String.Empty;

      /// <summary>
      /// Initializes a new instance.
      /// </summary>
      public EncryptedPassword()
      {
         //nothing to do
      }

      public string Password
      {
         get { return mPasswordClearText; }
      }

      public SecureString SecurePassword
      {
         get
         {
            unsafe
            {
               fixed (char* p = mPasswordClearText)
               {
                  return new SecureString(p, mPasswordClearText.Length);
               }
            }
         }
      }

      public string PasswordEncrypted
      {
         get
         {
            if (!string.IsNullOrEmpty(mPasswordClearText))
            {
               return ToHexString(
                  ProtectedData.Protect(
                     System.Text.Encoding.UTF8.GetBytes(mPasswordClearText.ToString()), Entropy, DataProtectionScope.CurrentUser));
            }
            else
            {
               return string.Empty;
            }
         }
         set
         {
            if (!string.IsNullOrEmpty(value))
            {
               mPasswordClearText = System.Text.Encoding.UTF8.GetString(
                  ProtectedData.Unprotect(FromHexString(value), Entropy, DataProtectionScope.CurrentUser));
            }
            else
            {
               mPasswordClearText = string.Empty;
            }
         }
      }

      #region Hex helper

      private static string ToHexString(byte[] ar)
      {
         var result = new System.Text.StringBuilder(ar.Length);
         for (int i = 0; i < ar.Length; i++)
         {
            result.Append(BitConverter.ToString(ar, i, 1));
         }

         return result.ToString();
      }

      private static byte[] FromHexString(string s)
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
   }
}
