using System;
using System.Collections.Generic;
using System.Text;
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

      static WebClient2()
      {
         // Enable TLS 1.2 (which is NOT enabled by default in .NET 4.5 on Windows 10!).
         // The default value there is:
         //    ServicePointManager.SecurityProtocol = Ssl3 | Tls;
         // Without this enabling, you will get this exception from some https sites which require TLS 1.2:
         //    System.Net.WebException: The request was aborted: Could not create SSL/TLS secure channel
         // https://www.itsicherheit-online.com/news/ende-fuer-tls-1.0-und-tls-1.1-naht
         // https://www.zdnet.de/88378482/microsoft-verschiebt-support-ende-fuer-tls-1-0-und-1-1/       
         // Note that there is lots of wrong information in the internet regarding this topic...
         ServicePointManager.SecurityProtocol |= (
                 // https://en.wikipedia.org/wiki/Transport_Layer_Security#SSL_1.0,_2.0,_and_3.0
                 SecurityProtocolType.Ssl3   // SSL 3   - DEPRECATED, UNSAFE. Was superceded by TLS 1.0. We only enable it here for compatibility with ancient sites which have not upgraded to TLS 1.2 yet.
               | SecurityProtocolType.Tls    // TLS 1.0 - DEPRECATED, UNSAFE. We only enable it here for compatibility with ancient sites which have not upgraded to TLS 1.2 yet.
               | SecurityProtocolType.Tls11  // TLS 1.1 - DEPRECATED, UNSAFE. We only enable it here for compatibility with ancient sites which have not upgraded to TLS 1.2 yet.
               | SecurityProtocolType.Tls12  // TLS 1.2 - The current standard. Soon to be superceded by TLS 1.3.
            // | SecurityProtocolType.Tls13  // TLS 1.3 - The future standard (2018). Not available in .NET 4.5. https://docs.microsoft.com/en-us/dotnet/framework/network-programming/tls
         );
      }

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
