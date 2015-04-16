// 
// Platform.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.


using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Gdk;


namespace Docking.Tools
{
   public static class Platform
   {
      static Platform()
      {
         IsWindows = System.IO.Path.DirectorySeparatorChar=='\\';
         IsUNIX    = System.Environment.OSVersion.Platform==PlatformID.Unix ||
                     System.Environment.OSVersion.Platform==PlatformID.MacOSX;
         IsMac     = IsUNIX && IsRunningOnMac();

         OSIDString = ComputeOSIDString();
      }

      public static bool IsWindows    { get; private set; }
      public static bool IsUNIX       { get; private set; }
      public static bool IsMac        { get; private set; }

      public static string OSIDString { get; private set; }

      //From Managed.Windows.Forms/XplatUI
      static bool IsRunningOnMac()
      {
         IntPtr buf = IntPtr.Zero;
         try
         {
            buf = Marshal.AllocHGlobal(8192);
            // This is a hacktastic way of getting sysname from uname ()
            if(uname(buf) == 0)
            {
               string os = Marshal.PtrToStringAnsi(buf);
               if(os == "Darwin")
                  return true;
            }
         }
         catch
         {}
         finally
         {
            if(buf != IntPtr.Zero)
               Marshal.FreeHGlobal(buf);
         }
         return false;
      }

      [DllImport("libc")]
      static extern int uname(IntPtr buf);

      // Returns a unique string identifying the currently running OS.
      // For UNIXes, the ID= entry from /etc/os-release is used for this purpose.
      // For other OSes, where this file does [of course] not exist, the behaviour is made similar.
      private static string ComputeOSIDString()
      {
         PlatformID id = System.Environment.OSVersion.Platform;
         switch(id)
         {
         case System.PlatformID.Win32S:       return "windows"; // maybe in future we need to be more specific here, like "windows_7" or something, but currently just "windows" suffices
         case System.PlatformID.Win32Windows: return "windows"; // maybe in future we need to be more specific here, like "windows_7" or something, but currently just "windows" suffices
         case System.PlatformID.Win32NT:      return "windows"; // maybe in future we need to be more specific here, like "windows_7" or something, but currently just "windows" suffices

         case System.PlatformID.WinCE:        return "windows_ce";
         case System.PlatformID.Xbox:         return "xbox";

         case System.PlatformID.MacOSX:       return "osx"; // maybe in future we need to be more specific here

         case System.PlatformID.Unix:         /*NOP*/ break;
         default:                             /*NOP*/ break;
         }

         if(!System.IO.File.Exists("/etc/os-release"))
            return "unknown";

         string[] osinfo = System.IO.File.ReadAllLines("/etc/os-release");
         foreach(string s in osinfo)
         {
            string[] portions = s.Split('=');
            if(portions.Length!=2)
               continue;
            string key = portions[0];
            string val = portions[1];

            if(key.ToLowerInvariant()=="id")
               return val; // for example "ubuntu" on Ubuntu, or "arch" on ArchLinux
         }

         return "unknown";
      }

      public static string AdjustDirectorySeparators(string filename)
      {
         return Platform.IsWindows ? filename.Replace('/', '\\') : filename.Replace('\\', '/');
      }
   }
}