using System.IO;
using System.Text;

namespace Docking.Tools
{
   public class EncodingStringWriter : StringWriter
   {
       private readonly Encoding _encoding;

       public EncodingStringWriter(Encoding encoding)
       : base(new StringBuilder())
       {
           _encoding = encoding;
       }

       public override Encoding Encoding
       {
           get { return _encoding;  }                
       }
   }
}
