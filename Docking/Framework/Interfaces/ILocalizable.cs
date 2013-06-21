using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Docking.Components
{
   // TODO SLohse: I think that it is a bit unlucky to have 2 so similar interfaces ILocalizable, ILocalized.
   // One has the function "tell the class that the localization has just changed",
   // the other has the function "localize yourself".
   // Probably the 2 can be merged into 1.
   //
   // Currently, ILocalizable should probably be renamed to ILocalizableComponent
   //            ILocalized   should probably be renamed to ILocalizableWidget
   //
   // However, WHY are components told AFTER the localization that it has happened,
   // and widgets are asked BEFORE to do it ON THEIR OWN?
   // Can't this be unified? To simply "ILocalizable"?
   // With a simple function .Localize(...)?


   /// <summary>
   /// Will be called for any component control after localization has been done, but just before app will be redrawn
   /// </summary>
   public interface ILocalizable
   {
      string Name { get; }
      void LocalizationChanged(DockItem item);
   }

   /// <summary>
   /// Will be called for any widget in a container. Any control/widget with localization support should implement this interface.
   /// </summary>
   public interface ILocalized
   {
      void Localize(string namespc);
   }
}
