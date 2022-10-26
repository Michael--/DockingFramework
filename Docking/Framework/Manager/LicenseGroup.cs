
using System;
using System.Collections.Generic;

namespace Docking.Components
{
   public class LicenseGroup
   {
      public enum State
      {
         UNKNOWN,  // group is unknown and state is unknown
         DISABLED, // group is registered and disabled
         ENABLED,  // group is registered and enabled
      }

      /// <summary>
      /// All existing groups and their current enabling state
      /// </summary>
      private readonly Dictionary<string, State> m_Groups = new Dictionary<string, State>();

      static LicenseGroup()
      {
         DefaultState = State.DISABLED;
      }

      internal LicenseGroup()
      {
         //nothing to do
      }

      /// <summary>
      /// The default state for not registered groups
      /// </summary>
      public static State DefaultState { get; set; }

      /// <summary>
      /// return true if any group in given multiple-groups string is enabled
      /// </summary>
      public bool IsEnabled(string groups)
      {
         if (!string.IsNullOrEmpty(groups))
         {
            foreach (var group in groups.Split(new char[] { '|', ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
               var st = GetState(group);
               if (st == State.ENABLED)
               {
                  return true;
               }

               if (st == State.DISABLED)
               {
                  return false;
               }
            }
         }

         return DefaultState == State.ENABLED;
      }

      /// <summary>
      /// Enable/Disable a group
      /// </summary>
      public void SetEnabling(string group, bool enabled)
      {
         lock(m_Groups)
         {
            State result;
            if (m_Groups.TryGetValue(group.ToLowerInvariant(), out result))
            {
               result = enabled ? State.ENABLED : State.DISABLED;
            }
            else
            {
               m_Groups.Add(group.ToLowerInvariant(), enabled ? State.ENABLED : State.DISABLED);
            }
         }
      }

      private State GetState(string group)
      {
         if (!string.IsNullOrEmpty(group))
         {
            lock(m_Groups)
            {
               State result;
               if (m_Groups.TryGetValue(group.ToLowerInvariant(), out result))
               {
                  return result;
               }
            }
         }

         return State.UNKNOWN;
      }
   }
}
