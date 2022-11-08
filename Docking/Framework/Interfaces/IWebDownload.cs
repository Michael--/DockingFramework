
using System.Net;
using System.Text;

namespace Docking.Framework.Interfaces
{
   public interface IWebDownload
   {
      string DownloadString(string address, WebRequestInfo info);
   }

   public class WebRequestInfo
   {
      public WebHeaderCollection Headers { get; set; }
      public Encoding Encoding { get; set; }
   }
}
