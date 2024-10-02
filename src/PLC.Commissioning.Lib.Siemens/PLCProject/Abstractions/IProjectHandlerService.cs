using Siemens.Engineering;
using System.IO;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions
{
    /// <summary>
    /// Defines methods for handling Siemens TIA Portal projects.
    /// </summary>
    public interface IProjectHandlerService
    {
        /// <summary>
        /// Attempts to open or retrieve and open a TIA Portal project based on the file extension.
        /// </summary>
        /// <param name="projectPath">The full path to the TIA Portal project file (.ap17 or .zap17).</param>
        /// <returns>
        /// <c>true</c> if the project was opened or retrieved successfully; otherwise, <c>false</c> if the project could not be opened 
        /// (e.g., due to the project being locked or in use by another process).
        /// </returns>
        /// <exception cref="FileNotFoundException">Thrown when the specified project file is not found at the given path.</exception>
        bool HandleProject(string projectPath);

        /// <summary>
        /// Closes the currently opened TIA Portal project.
        /// </summary>
        void CloseProject();

        /// <summary>
        /// Saves the currently opened TIA Portal project with a new name and then closes it.
        /// <param name="projectName">The new name for the saved project.</param>
        void SaveProjectAs(string projectName);

        /// <summary>
        /// Imports an AML file into the currently opened TIA Portal project.
        /// </summary>
        /// <param name="amlFilePath">The full path to the AML file.</param>
        /// <returns><c>true</c> if the import was successful; otherwise, <c>false</c>.</returns>
        bool ImportAmlFile(string amlFilePath);

        /// <summary>
        /// Gets the currently opened TIA Portal project.
        /// </summary>
        Project Project { get; }

        /// <summary>
        /// Gets the current TIA Portal instance associated with the service.
        /// </summary>
        TiaPortal TiaPortal { get; }
    }
}
