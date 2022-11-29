
using System;
using Gtk;

namespace Docking.Framework.Interfaces
{
   /// <summary>
   /// Provides methods to access/manipulate application's menu/status bars.
   /// </summary>
   public interface IMenuService
   {
      /// <summary>
      /// Gets the submenu entry with given name.
      /// </summary>
      /// <param name="menuText"></param>
      /// <returns></returns>
      Menu ExportSubmenu(String menuText);

      /// <summary>
      /// Adds an item to tool bar
      /// </summary>
      /// <param name="item">Toolitem to be added</param>
      void AddToolItem(ToolItem item);

      /// <summary>
      /// Removes an item from tool bar
      /// </summary>
      /// <param name="item">Item to be removed</param>
      void RemoveToolItem(ToolItem item);

      /// <summary>
      /// Pushes a status text statusbar.
      /// </summary>
      /// <param name="txt">The text displayed on status bar</param>
      /// <returns>id of pushed status bar item for lated reference</returns>
      uint StatusbarPushText(String txt);

      /// <summary>
      /// Pops a message from statusbar.
      /// </summary>
      void StatusbarPopText(uint id);

      /// <summary>
      /// Adds an entry to "Recent file" menu
      /// </summary>
      /// <param name="filename">NAme of file displayed on menu item</param>
      /// <param name="do_update_menu"></param>
      void AddRecentFile(string filename, bool do_update_menu = true);

      /// <summary>
      /// Update text content to reflect currenttly activated language.
      /// </summary>
      /// <param name="forceRedraw">True, forces widgets to redraw/relayout its content/child widgets</param>
      void UpdateLanguage(bool forceRedraw);
   }
}
