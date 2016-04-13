using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Cryptography;
using System.Net;

namespace Docking.Tools
{
   public class WebClient2 : WebClient
   {
      public static string UserAgent              = null;

      public static bool   UseProxy               = false;
      public static bool   UseSystemProxySettings = false;
      public static string ProxyServer            = null;
      public static int    ProxyPort              = 0;
      public static string ProxyUsername          = null;

      // An array of random numbers used to encrypt the password for file storage (in the config).
      // TODO This is still not an ideal solution as everybody knowing these numbers can decrypt the password.
      // However, we somewhere need to persist the proxy password and do not want to do it in plaintext.
      // Just computing the hash of the password will not be enough since we need to send it to the proxy as a NetworkCredential()
      private static byte[] Entropy = { 28, 1, 17, 20, 99, 157, 14, 31, 89, 254, 88, 4, 11, 99, 20, 18, 1, 14, 74, 33 };
      public static string ProxyPassword { set; private get; }
      public static string ProxyPasswordEncrypted
      {
         set { ProxyPassword = (value == null || value.Length <= 0) ? "" : Encoding.UTF8.GetString(ProtectedData.Unprotect(FromHexString(value), Entropy, DataProtectionScope.CurrentUser)); }
         get { return (ProxyPassword == null || ProxyPassword.Length <= 0) ? "" : ToHexString(ProtectedData.Protect(Encoding.UTF8.GetBytes(ProxyPassword), Entropy, DataProtectionScope.CurrentUser)); }
      }

      public CookieContainer CookieContainer { get; set; } // default: null - if needed, set this before making a request
      public string          Referer         { get; set; } // default: null - if needed, set this before making a request

      public WebClient2(bool withUserAgent = false)
      {
         if (withUserAgent && UserAgent != null && UserAgent.Length > 0)
            Headers.Add(HttpRequestHeader.UserAgent, UserAgent);

         if(UseProxy)
         {
            if (UseSystemProxySettings)
            {
               // TODO not yet implemented
               // TODO retrieve proxy settings e.g. from Windows Control Panel
               // TODO use that data here
            }
            else
            {
               if (ProxyServer != null && ProxyServer.Length > 0)
               {
                  Proxy = new WebProxy(ProxyServer, ProxyPort);
                  if (ProxyUsername != null && ProxyUsername.Length > 0)
                     Proxy.Credentials = new NetworkCredential(ProxyUsername, ProxyPassword ?? "");
               }
            }
         }
      }

      static String ToHexString(byte[] ar)
      {
         StringBuilder result = new StringBuilder();
         for (int i = 0; i < ar.Length; i++)
            result.Append(BitConverter.ToString(ar, i, 1));
         return result.ToString();
      }

      static Byte[] FromHexString(String s)
      {
         if (s == null || (s.Length % 2) != 0)
            return null;
         Byte[] bytes = new Byte[s.Length / 2];
         for (int i = 0; i < s.Length / 2; i++)
            bytes[i] = Convert.ToByte(s.Substring(i * 2, 2), 16);
         return bytes;
      }

      // http://stackoverflow.com/questions/1777221/using-cookiecontainer-with-webclient-class
      protected override WebRequest GetWebRequest(Uri url)
      {
         HttpWebRequest req = base.GetWebRequest(url) as HttpWebRequest;
         if(req!=null)
         {
            if(this.CookieContainer!=null)
               req.CookieContainer = this.CookieContainer;
            if(!string.IsNullOrEmpty(this.Referer))
               req.Referer = this.Referer;
         }
         return req;
      } 

   }
}
