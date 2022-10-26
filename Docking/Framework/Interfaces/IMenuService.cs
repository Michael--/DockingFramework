
using System;
using Gtk;

namespace Docking.Framework.Interfaces
{
   public interface IMenuService
   {
      Menu FindOrCreateExportSubmenu(String text);

      void AddToolItem(ToolItem item);

      uint PushStatusbar(String txt);

      /// <summary>
      /// Pop a message from the statusbar.
      /// </summary>
      void PopStatusbar(uint id);

      void RemoveToolItem(ToolItem item);

      void AddRecentFile(string filename, bool do_update_menu = true);

      void UpdateLanguage(bool triggerRedraw);
   }
}
