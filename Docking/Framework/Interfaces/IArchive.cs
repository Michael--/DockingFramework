using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Docking.Components
{
   public interface IArchive
   {
      bool IsArchive(string filename);
      string[] GetFileNames(object handle);
      object Open(string filename);
      void Close(object handle);
      string Extract(object handle, string filename);
   }
}
