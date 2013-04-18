using System;

namespace Docking.Components
{
    public interface IMessage
    {
       void WriteLine(String str, params object[] args);
    }
}

