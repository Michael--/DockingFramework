using System;
using System.Collections.Generic;

namespace Docking.Components
{ 
   // example usage:
   // IPersistency myPersistency;
   // double zoom = myPersistency.LoadSetting("MapViewer-1", "Zoom", 8);
   public interface IPersistency
   {
                     void  SaveSetting(string instance, string key,              UInt32  val       );
                     void  SaveSetting(string instance, string key,              Int32   val       );
                     void  SaveSetting(string instance, string key,              double  val       );
                     void  SaveSetting(string instance, string key,              bool    val       );
                     void  SaveSetting(string instance, string key,         List<bool>   val       );       
                     void  SaveSetting(string instance, string key,              string  val       );      
                     void  SaveSetting(string instance, string key,         List<string> val       );       
                     void  SaveSetting(string instance, string key, System.Drawing.Color val       ); // if you want to persist something else (e.g. Gdk.Color), first convert it to System.Drawing.Color

                   UInt32  LoadSetting(string instance, string key,              UInt32  defaultval);
                   Int32   LoadSetting(string instance, string key,              Int32   defaultval);
                   double  LoadSetting(string instance, string key,              double  defaultval);
                   bool    LoadSetting(string instance, string key,              bool    defaultval);
              List<bool>   LoadSetting(string instance, string key,         List<bool>   defaultval);
                   string  LoadSetting(string instance, string key,              string  defaultval);      
              List<string> LoadSetting(string instance, string key,         List<string> defaultval);
      System.Drawing.Color LoadSetting(string instance, string key, System.Drawing.Color defaultval); // if you want to persist something else (e.g. Gdk.Color), first convert it to System.Drawing.Color

              void SaveColumnWidths(string instance, Gtk.TreeView treeview); // key is automatically computed from treeview.Name
              void LoadColumnWidths(string instance, Gtk.TreeView treeview); // key is automatically computed from treeview.Name
   }
}
