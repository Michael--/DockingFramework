using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Docking.Components
{
   // TODO SLohse: We should consider merging these 2 interfaces into just one ILocalizable.
   // Currently, the 2 exist separately because
   // - components need to be notified _after_ the localization has happened ("LocalizationChanged()")
   // - widgets to the localization on their own ("Localize()")
   // Maybe there is an elegant way to merge the 2 principles.

    /// <summary>
   /// Will be called for any component control *after* localization has been done, but just before app will be redrawn.
   /// The component can then take more steps to localize itself.
   /// </summary>
   public interface ILocalizableComponent
   {
      string Name { get; }
      void LocalizationChanged(DockItem item);
   }

   /// <summary>
   /// Will be called for any widget in a container. Any control/widget with localization support should implement this interface.
   /// </summary>
   public interface ILocalizableWidget
   {
      void Localize(string namespc);
   }
}
