using System;

namespace Docking.Framework
{
    public interface IMessageWriteLine
    {
        void MessageWriteLine(String format, params object[] args);
    }
}
