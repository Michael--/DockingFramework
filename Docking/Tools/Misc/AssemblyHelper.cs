using System;

namespace Docking.Tools
{
   public class AssemblyHelper
   {
      static PlatformID platformid  = Environment.OSVersion.Platform;

      public static bool PlatformIsWin32ish
      {
         get
         {
            switch(platformid)
            {
               case PlatformID.Win32S:       return true;
               case PlatformID.Win32Windows: return true;
               case PlatformID.Win32NT:      return true;
               case PlatformID.WinCE:        return false; // note this. WinCE is very different than Win32
               case PlatformID.Unix:         return false;
               case PlatformID.Xbox:         return false; // or should we return true here better???
               case PlatformID.MacOSX:       return false;
               default:                      return false;
            }
         }
      }

      // TODO check if this is a good idea for MacOS. If not, rename this to PlatformIsUnix and create a separate PlatformIsMac
      public static bool PlatformIsUnixoid
      {
         get
         {
            switch(platformid)
            {
               case PlatformID.Win32S:       return false;
               case PlatformID.Win32Windows: return false;
               case PlatformID.Win32NT:      return false;
               case PlatformID.WinCE:        return false;
               case PlatformID.Unix:         return true;
               case PlatformID.Xbox:         return false;
               case PlatformID.MacOSX:       return true;
               default:                      return false;
            }
         }
      }
   }
}
