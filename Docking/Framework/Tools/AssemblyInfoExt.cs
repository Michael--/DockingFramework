﻿
using System;
using System.IO;
using System.Reflection;

namespace Docking.Tools
{
   public class AssemblyInfoExt
   {
      private static readonly Assembly assembly = Assembly.GetEntryAssembly();

      public static string Location
      {
         get { return assembly.Location; }
      }

      public static string Name
      {
         get { return assembly.GetName().Name; }
      }

      public static Version Version
      {
         get { return assembly.GetName().Version; }
      }

      public static string Directory
      {
         get { return Path.GetDirectoryName(Location); }
      }

      public static string ResourcesDirectory
      {
         get { return Path.Combine(Directory, "Resources"); }
      }

      public static string LocalSettingsFolder
      {
         get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Name); }
      }
   }
}
