using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Docking.Components
{
   public class LicenseGroup
   {
      public LicenseGroup()
      {
         DefaultState = State.DISABLED;
      }

      public enum State
      {
         NONE,       // group is not registered and state is unknown
         DISABLED,   // group is registered as disabled
         ENABLED,    // group is registered as enabled
      }

      public State GetState(string group)
      {
         State result;
         lock (m_Groups)
         {
            if (group != null && m_Groups.TryGetValue(group, out result))
               return result;
         }
         return DefaultState;
      }

      public bool IsEnabled(string group)
      {
         return GetState(group) == State.ENABLED;
      }

      public bool IsDisabled(string group)
      {
         return !IsEnabled(group);
      }

      public void SetGroup(string group, bool enabled)
      {
         State result;
         lock (m_Groups)
         {
            // change existing or add new
            if (m_Groups.TryGetValue(group, out result))
               m_Groups[group] = enabled ? State.ENABLED : State.DISABLED;
            else
               m_Groups.Add(group, enabled ? State.ENABLED : State.DISABLED);
         }
      }

      /// <summary>
      /// All existing group names and its current enabling state
      /// </summary>
      Dictionary<string, State> m_Groups = new Dictionary<string, State>();

      /// <summary>
      /// The default state for not registered groups
      /// </summary>
      public State DefaultState  { get; set; }
   }
  

}
