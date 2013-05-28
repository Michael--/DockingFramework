using System;

namespace Docking.Components
{ 
   // example usage:
   // IPersistency myPersistency;
   // double zoom = myPersistency.LoadSetting("MapViewer-1", "Zoom", 8);
   public interface IPersistency
   {
      string LoadSetting(string instance, string key, string defaultval);
      UInt32 LoadSetting(string instance, string key, UInt32 defaultval);
       Int32 LoadSetting(string instance, string key,  Int32 defaultval);
      double LoadSetting(string instance, string key, double defaultval);
        bool LoadSetting(string instance, string key,   bool defaultval);

        void SaveSetting(string instance, string key, string val);
        void SaveSetting(string instance, string key, UInt32 val);
        void SaveSetting(string instance, string key,  Int32 val);
        void SaveSetting(string instance, string key, double val);
        void SaveSetting(string instance, string key,   bool val);
   }
}
