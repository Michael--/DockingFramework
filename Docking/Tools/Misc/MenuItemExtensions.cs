using Gtk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Docking.Tools
{
   public static class MenuItemExtensions
   {
      public static string GetText(this ImageMenuItem item)
      {
         return (item.Child as Label).Text;
      }
   }
}
