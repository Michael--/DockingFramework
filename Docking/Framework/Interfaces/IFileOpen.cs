using System;

namespace Docking.Components
{
    public interface IFileOpen
    {
        /// <summary>
        /// Ask the component if file type supported.
        /// Get short description if file type can be loaded by component.
        /// E.g. "test.dlt" response could be "Load as DLT", return null if not supported 
        /// </summary>
        String TryOpenFile(String filename);

        /// <summary>
        /// Load the file.
        /// </summary>
        void OpenFile(String filename);
    }
}

