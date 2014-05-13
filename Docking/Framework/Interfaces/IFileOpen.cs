using System;
using System.Collections.Generic;
using Gtk;
using Docking.Tools;

namespace Docking.Components
{
    public interface IFileOpen
    {
        /// <summary>
        /// Returns a list of supported file types. The framework for example will use collect all these from the individual components
        /// and offer them in its FileOpen dialog.
        /// </summary>
        /// <returns></returns>
        List<FileFilterExt> SupportedFileTypes();

        /// <summary>
        /// Ask the component if file type supported.
        /// Get short description if file type can be loaded by component.
        /// E.g. "test.dlt" response could be "Load as DLT", return null if not supported 
        /// </summary>
        String TryOpenFile(String filename);

        /// <summary>
        /// Load the file. Returs true on success.
        /// </summary>
        bool OpenFile(String filename);
    }
}

