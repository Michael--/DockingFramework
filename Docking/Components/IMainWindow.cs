using System;

namespace Docking.Components
{
    public interface IMainWindow
    {
        DockFrame DockFrame { get; }
        ComponentFactoryInformation[] ComponentInfos { get; }
    }
}

