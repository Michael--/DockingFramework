using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Docking.Components
{
   public class LicenseGroup
   {
      /// <summary>
      /// The default state for not registered groups
      /// </summary>
      public static State DefaultState { get; set; }

      static LicenseGroup()
      {
         DefaultState = State.DISABLED;
      }

      public LicenseGroup()
      {
      }

      public enum State
      {
         NONE,       // group is not registered and state is unknown
         DISABLED,   // group is registered as disabled
         ENABLED,    // group is registered as enabled
      }

      private State GetState(string group)
      {
         State result;
         lock (m_Groups)
         {
            if (group != null && m_Groups.TryGetValue(group.ToLowerInvariant(), out result))
               return result;
         }
         return State.NONE;
      }

      /// <summary>
      /// return true if any group in given string is enabled
      /// </summary>
      public bool IsEnabled(string groups)
      {
         if (groups != null)
         {
            foreach (var s in groups.Split(new char[] { '|', ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
               var st = GetState(s);
               if (st == State.ENABLED)
                  return true;
               if (st == State.DISABLED)
                  return false;
            }
         }
         return DefaultState == State.ENABLED;
      }

      /// <summary>
      /// Enable/Disable a group
      /// </summary>
      public void SetGroup(string group, bool enabled)
      {
         lock (m_Groups)
         {
            // change existing or add new
            State result;
            if (m_Groups.TryGetValue(group.ToLowerInvariant(), out result))
               result = enabled ? State.ENABLED : State.DISABLED;
            else
               m_Groups.Add(group.ToLowerInvariant(), enabled ? State.ENABLED : State.DISABLED);
         }
      }

      /// <summary>
      /// All existing group names and its current enabling state
      /// </summary>
      Dictionary<string, State> m_Groups = new Dictionary<string, State>();
   }
  

}
