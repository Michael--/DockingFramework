using System;
using System.Collections.Generic;
using Docking.Tools;

namespace Docking.Components
{
    public interface IFileOpen
    {
        /// <summary>
        /// Returns a list of supported file types this component can open.
        /// TODO DEPRECATED! This function has been relocated to class ComponentFactory now. REMOVE THIS FUNCTION FROM HERE!
        /// </summary>
        /// <returns></returns>
        List<FileFilterExt> SupportedFileTypes();

        /// <summary>
        /// Ask the component if it can open a specific given file.
        /// </summary>
        bool CanOpenFile(String filename);

        /// <summary>
        /// Load the file. Returns true on success.
        /// </summary>
        bool OpenFile(String filename);
    }
}

