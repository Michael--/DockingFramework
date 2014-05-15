using System;
using System.Collections.Generic;
using Docking.Tools;

namespace Docking.Components
{
    public interface IFileOpen
    {
        /// <summary>
        /// Returns a list of supported file types this component can open.
        /// </summary>
        /// <returns></returns>
        List<FileFilterExt> SupportedFileTypes();

        /// <summary>
        /// Ask the component if it can open a specific given file.
        /// If yes, this function will return the string how this components names such files.
        /// If no, it will return null.
        /// </summary>
        String TryOpenFile(String filename);

        /// <summary>
        /// Load the file. Returns true on success.
        /// </summary>
        bool OpenFile(String filename);
    }
}

