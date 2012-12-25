using System;

namespace Docking.Components
{
    public interface IComponent
    {
        IMainWindow MainWindow { get; set; }
        void ComponentsRegistered(DockItem item);
    }
}

