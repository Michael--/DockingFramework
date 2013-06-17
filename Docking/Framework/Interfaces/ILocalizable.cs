using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Docking.Components
{
   /// <summary>
   /// Will be called for any component control after localization has been done, but just before app will be redraw
   /// </summary>
   public interface ILocalizable
   {
      string Name { get; }
      void LocalizationChanged(DockItem item);
   }

   /// <summary>
   /// Will be call for any widget in a container. Any control/widget with localization support should implement this interface.
   /// </summary>
   public interface ILocalized
   {
      void Localize(string namespc);
   }
}
