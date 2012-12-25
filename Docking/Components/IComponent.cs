using System;

namespace Docking.Components
{
    public interface IComponent
    {
        ComponentManager ComponentManager { get; set; }
        void ComponentsRegistered(DockItem item);
    }
}

