using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Cryptography;

namespace Docking.Tools
{
   public class WebClient2 : System.Net.WebClient
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

      public WebClient2(bool withUserAgent = false)
      {
         if (withUserAgent && UserAgent != null && UserAgent.Length > 0)
            Headers.Add(System.Net.HttpRequestHeader.UserAgent, UserAgent);

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
                  Proxy = new System.Net.WebProxy(ProxyServer, ProxyPort);
                  if (ProxyUsername != null && ProxyUsername.Length > 0)
                     Proxy.Credentials = new System.Net.NetworkCredential(ProxyUsername, ProxyPassword ?? "");
               }
            }
         }
      }

      // hexdump hex dump (copied from LittleHelper, need in also in other context) 
      static String ToHexString(byte[] ar)
      {
         StringBuilder result = new StringBuilder();
         for (int i = 0; i < ar.Length; i++)
            result.Append(BitConverter.ToString(ar, i, 1));
         return result.ToString();
      }

      // Byte array from hexdump string
      static Byte[] FromHexString(String s)
      {
         if (s == null || (s.Length % 2) != 0)
            return null;
         Byte[] bytes = new Byte[s.Length / 2];
         for (int i = 0; i < s.Length / 2; i++)
            bytes[i] = Convert.ToByte(s.Substring(i * 2, 2), 16);
         return bytes;
      }

   }
}
