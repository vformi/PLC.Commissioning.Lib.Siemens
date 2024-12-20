﻿using PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions;
using Siemens.Engineering;
using Siemens.Engineering.Cax;
using System;
using System.IO;
using Serilog;
using System.Linq;

namespace PLC.Commissioning.Lib.Siemens.PLCProject
{
    /// <summary>
    /// Implements the <see cref="IProjectHandlerService"/> to manage TIA Portal projects.
    /// </summary>
    public class ProjectHandlerService : IProjectHandlerService
    {
        /// <summary>
        /// Represents the TIA Portal instance used for interacting with Siemens PLC projects.
        /// </summary>
        private readonly TiaPortal _tiaPortal;

        /// <summary>
        /// Represents the current Siemens PLC project loaded in the TIA Portal.
        /// </summary>
        private Project _project;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectHandlerService"/> class with the specified TIA Portal instance.
        /// </summary>
        /// <param name="tiaPortal">The TIA Portal instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="tiaPortal"/> is null.</exception>
        public ProjectHandlerService(TiaPortal tiaPortal)
        {
            _tiaPortal = tiaPortal ?? throw new ArgumentNullException(nameof(tiaPortal));
        }

        /// <summary>
        /// Gets the currently opened TIA Portal project.
        /// </summary>
        public Project Project => _project;

        /// <summary>
        /// Gets the current TIA Portal instance associated with the service.
        /// </summary>
        public TiaPortal TiaPortal => _tiaPortal;

        /// <summary>
        /// Attempts to open or retrieve and open a TIA Portal project based on the file extension.
        /// </summary>
        /// <param name="projectPath">The full path to the TIA Portal project file (.ap17 or .zap17).</param>
        /// <returns>
        /// <c>true</c> if the project was opened or retrieved successfully; otherwise, <c>false</c> if the project could not be opened 
        /// (e.g., due to the project being locked or in use by another process).
        /// </returns>
        /// <exception cref="FileNotFoundException">Thrown when the specified project file is not found at the given path.</exception>
        public bool HandleProject(string projectPath)
        {
            if (!File.Exists(projectPath))
            {
                throw new FileNotFoundException("Project file not found.", projectPath);
            }

            string extension = Path.GetExtension(projectPath).ToLowerInvariant();
            try
            {
                switch (extension)
                {
                    case ".ap17":
                        OpenProject(projectPath);
                        return true;
                    case ".zap17":
                        RetrieveAndOpenProject(projectPath);
                        return true;
                    default:
                        Log.Error($"File extension '{extension}' is not supported.");
                        return false;
                }
            }
            catch(EngineeringTargetInvocationException ex)
            {
                if (ex.InnerException != null && ex.InnerException.Message.Contains("cannot be accessed"))
                {
                    Log.Error($"The project '{projectPath}' is currently locked and cannot be opened. Please wait for 2 minutes before trying again.");
                    return false;
                }
                else
                {
                    Log.Error($"Failed to open project '{projectPath}': {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to open project '{projectPath}': {ex.Message}");
                return false;
            }

        }

        /// <summary>
        /// Opens a TIA Portal project from the specified file path.
        /// </summary>
        /// <param name="projectPath">The full path to the TIA Portal project file (.ap17).</param>
        private void OpenProject(string projectPath)
        {
            try
            {
                _project = _tiaPortal.Projects.Open(new FileInfo(projectPath));
                Log.Information($"Project '{projectPath}' opened successfully.");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to open project '{projectPath}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves a TIA Portal project from an archive and opens it.
        /// </summary>
        /// <param name="archivePath">The full path to the TIA Portal archive file (.zap17).</param>
        private void RetrieveAndOpenProject(string archivePath)
        {
            string retrievedProjectsDirectory = Path.GetDirectoryName(archivePath);
            string projectName = Path.GetFileNameWithoutExtension(archivePath); // Assume the project folder is named after the archive file without the extension
            string targetDirectory = Path.Combine(retrievedProjectsDirectory, projectName);

            // Ensure only the specific directory that would be created by the Retrieve operation is deleted
            if (Directory.Exists(targetDirectory))
            {
                Log.Information($"Project directory '{targetDirectory}' already exists. Deleting...");
                try
                {
                    Directory.Delete(targetDirectory, true); // Recursively delete the specific directory
                    Log.Information($"Existing project directory '{targetDirectory}' deleted successfully.");
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to delete existing project directory '{targetDirectory}': {ex.Message}");
                    throw new IOException($"Failed to delete existing project directory '{targetDirectory}'.", ex);
                }
            }

            try
            {
                DirectoryInfo retrievedDir = new DirectoryInfo(retrievedProjectsDirectory);
                _project = _tiaPortal.Projects.Retrieve(new FileInfo(archivePath), retrievedDir);
                Log.Information($"Project '{archivePath}' retrieved and opened successfully in directory '{targetDirectory}'.");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to retrieve project from '{archivePath}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Imports an AML file into the currently opened TIA Portal project.
        /// </summary>
        /// <param name="amlFilePath">The full path to the AML file.</param>
        /// <returns><c>true</c> if the import was successful; otherwise, <c>false</c>.</returns>
        public bool ImportAmlFile(string amlFilePath)
        {
            Log.Information("Attempting to import DUT from a file.");
            if (_project is null)
            {
                Log.Error("Error, project is not provided.");
                return false;
            }

            var importProvider = _project.GetService<CaxProvider>();
            var logFilePath = Path.Combine(Path.GetTempPath(), $"ImportLog_{Guid.NewGuid()}.log");

            if (importProvider is null)
            {
                Log.Error("Failed to retrieve CAX provider from project.");
                return false;
            }

            var importFileInfo = new FileInfo(amlFilePath);

            try
            {
                importProvider.Import(importFileInfo, new FileInfo(logFilePath), CaxImportOptions.RetainTiaDevice);

                // Read the log file to determine if the import was truly successful
                string[] logLines = File.ReadAllLines(logFilePath);
                bool hasErrors = logLines.Any(line => line.Contains("ERROR"));

                foreach (var line in logLines)
                {
                    Log.Debug(line);
                }

                if (hasErrors)
                {
                    Log.Error("Import encountered errors. Check the log for details.");
                    return false;
                }

                Log.Information("Import successful.");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Import failed: " + ex.Message);
                return false;
            }
            finally
            {
                if (File.Exists(logFilePath))
                {
                    File.Delete(logFilePath);
                }
            }
        }


        /// <summary>
        /// Closes the currently opened TIA Portal project.
        /// </summary>
        public void CloseProject()
        {
            _project?.Close();
            Log.Information("Project closed successfully.");
        }

        /// <summary>
        /// Saves the currently opened TIA Portal project with a new name and then closes it.
        /// </summary>
        /// <param name="projectName">The new name for the saved project.</param>
        public void SaveProjectAs(string projectName)
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string newProjectSubPath = $"Openness/Saved_Projects/{projectName}";
            string newProjectPath = Path.Combine(documentsPath, newProjectSubPath);
            DirectoryInfo directoryInfo = new DirectoryInfo(newProjectPath);

            if (_project != null)
            {
                _project.SaveAs(directoryInfo);
                Log.Information($"Project saved successfully as {projectName} at {newProjectPath}");
            }
            else
            {
                Log.Information("No project is currently loaded.");
            }
        }
    }
}
