using PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions;
using Siemens.Engineering;
using Siemens.Engineering.Cax;
using System;
using System.Collections.Generic;
using System.IO;
using Serilog;
using System.Linq;
using System.Text.RegularExpressions;

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
        
        private string _currentProjectPath;
        public string CurrentProjectPath => _currentProjectPath;
        
        private readonly IFileSystem _fileSystem; // Added IFileSystem
       
        private bool _disposed;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectHandlerService"/> class.
        /// </summary>
        /// <param name="tiaPortal">The TIA Portal instance.</param>
        /// <param name="fileSystem">The filesystem abstraction.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tiaPortal"/> or <paramref name="fileSystem"/> is null.</exception>
        public ProjectHandlerService(TiaPortal tiaPortal, IFileSystem fileSystem)
        {
            _tiaPortal = tiaPortal ?? throw new ArgumentNullException(nameof(tiaPortal));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            
        }

        /// <inheritdoc />
        public Project Project => _project;

        /// <inheritdoc />
        public TiaPortal TiaPortal => _tiaPortal;

        /// <inheritdoc />
        public bool HandleProject(string projectPath)
        {
            if (!_fileSystem.FileExists(projectPath))
            {
                Log.Error("Project file not found: {ProjectPath}", projectPath);
                return false;
            }

            string extension = Path.GetExtension(projectPath).ToLowerInvariant();
            var apZapRegex = new Regex(@"^\.(ap|zap)\d+$");

            try
            {
                switch (extension)
                {
                    case var ext when apZapRegex.IsMatch(ext) && ext.StartsWith(".ap"):
                        OpenProject(projectPath);
                        return true;

                    case var ext when apZapRegex.IsMatch(ext) && ext.StartsWith(".zap"):
                        RetrieveAndOpenProject(projectPath);
                        return true;

                    default:
                        Log.Error($"File extension '{extension}' is not supported.");
                        return false;
                }
            }
            catch (EngineeringTargetInvocationException ex)
            {
                if (ex.InnerException?.Message.Contains("cannot be accessed") == true)
                {
                    Log.Error($"The project '{projectPath}' is currently locked and cannot be accessed.");
                }
                return false; 
            }
            catch (Exception ex)
            {
                Log.Error($"An unexpected error occurred while handling project '{projectPath}': {ex.Message}");
                return false; 
            }
        }

        /// <inheritdoc />
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
                string[] logLines = _fileSystem.ReadAllLines(logFilePath); // Use IFileSystem
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
                if (_fileSystem.FileExists(logFilePath)) // Use IFileSystem
                {
                    _fileSystem.DeleteFile(logFilePath); // Add DeleteFile to IFileSystem interface
                }
            }
        }

        /// <inheritdoc />
        public void SaveProjectAs(string projectName)
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string newProjectSubPath = $"PLCCommissioningLib/Saved_Projects/{projectName}";
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
        
        /// <inheritdoc />
        public List<string> GetGsdmlFiles()
        {
            // If no project has been loaded, we can’t locate GSDML files.
            if (string.IsNullOrEmpty(_currentProjectPath))
            {
                Log.Warning("Cannot retrieve GSDML files because no project is loaded or path is not set.");
                return new List<string>();
            }

            // The .apNN file is in the project directory. Let's get that directory:
            string projectDirectory = Path.GetDirectoryName(_currentProjectPath);
            if (string.IsNullOrEmpty(projectDirectory))
            {
                Log.Warning("Unable to determine the project directory from the current project path.");
                return new List<string>();
            }

            // Construct the path to "AdditionalFiles\GSD"
            string gsdFolder = Path.Combine(projectDirectory, "AdditionalFiles", "GSD");

            // Check if this folder exists
            if (!_fileSystem.DirectoryExists(gsdFolder))
            {
                Log.Warning("The GSD folder does not exist at: {FolderPath}", gsdFolder);
                return new List<string>();
            }

            // Retrieve the files
            string[] foundFiles = _fileSystem.GetDirectoryFiles(gsdFolder, "*.xml");

            // Convert to a List<string> for convenience
            var gsdmlFiles = foundFiles.ToList();

            if (!gsdmlFiles.Any())
            {
                Log.Information("No .gsdml files were found in folder: {FolderPath}", gsdFolder);
            }
            else
            {
                Log.Information("Found {Count} GSDML file(s) in folder: {FolderPath}", gsdmlFiles.Count, gsdFolder);
            }

            return gsdmlFiles;
        }


        #region IDisposable Implementation

        /// <summary>
        /// Disposes the resources used by the <see cref="ProjectHandlerService"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="ProjectHandlerService"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">A boolean value indicating whether to release managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    try
                    {
                        CloseProject();
                    }
                    catch (EngineeringObjectDisposedException ex)
                    {
                        Log.Warning("Attempted to close an already disposed project: {Message}", ex.Message);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error while closing project during disposal: {Message}", ex.Message);
                    }

                    if (_tiaPortal != null)
                    {
                        try
                        {
                            _tiaPortal.Dispose();
                            Log.Information("TIA Portal instance disposed.");
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("TIA Portal instance disposal error: {Message}", ex.Message);
                        }
                    }
                }

                _disposed = true;
            }
        }
        
        /// <inheritdoc />
        public void CloseProject()
        {
            if (_project != null)
            {
                try
                {
                    _project.Close();
                    Log.Information("Project closed successfully.");
                }
                catch (EngineeringObjectDisposedException ex)
                {
                    Log.Warning("Project was already disposed: {Message}", ex.Message);
                }
                finally
                {
                    _project = null; // Avoid repeated disposal
                }
            }
            else
            {
                Log.Debug("No project to close.");
            }
        }
        
        #endregion
        
        # region Private methods 
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

                // Store the actual path from the TIA Project object, if available
                if (_project.Path != null)
                {
                    _currentProjectPath = _project.Path.FullName;
                    Log.Debug($"TIA Portal reported project path as '{_currentProjectPath}'.");
                }
                else
                {
                    // If TIA doesn't give a Path, fallback to the provided file path
                    _currentProjectPath = projectPath;
                    Log.Debug($"TIA Portal did not provide a Path. Using '{_currentProjectPath}'.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to open project '{projectPath}': {ex.Message}");
                throw;
            }
        }

        
        private void RetrieveAndOpenProject(string archivePath)
        {
            string absoluteArchivePath = Path.GetFullPath(archivePath);
            if (!_fileSystem.FileExists(absoluteArchivePath))
            {
                Log.Error($"Project archive file not found at '{absoluteArchivePath}'.");
                throw new FileNotFoundException("Project archive file not found.", absoluteArchivePath);
            }

            string retrievedProjectsDirectory = Path.GetDirectoryName(absoluteArchivePath);
            string projectName = Path.GetFileNameWithoutExtension(absoluteArchivePath);
            string targetDirectory = Path.Combine(retrievedProjectsDirectory, projectName);

            if (_fileSystem.DirectoryExists(targetDirectory))
            {
                Log.Information($"Project directory '{targetDirectory}' already exists. Deleting...");
                try
                {
                    _fileSystem.DeleteDirectory(targetDirectory, true);
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
                // TIA retrieves the archive into the 'retrievedDir'
                DirectoryInfo retrievedDir = new DirectoryInfo(retrievedProjectsDirectory);
                _project = _tiaPortal.Projects.Retrieve(new FileInfo(absoluteArchivePath), retrievedDir);

                Log.Information($"Project '{absoluteArchivePath}' retrieved and opened successfully in directory '{targetDirectory}'.");

                // Now store the *actual* TIA Portal project path
                if (_project.Path != null)
                {
                    _currentProjectPath = _project.Path.FullName;
                    Log.Debug($"TIA Portal reported project path as '{_currentProjectPath}'.");
                }
                else
                {
                    // If TIA doesn't provide a path, fallback to what we know
                    _currentProjectPath = targetDirectory;
                    Log.Debug($"TIA Portal did not provide a Path. Using '{_currentProjectPath}'.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to retrieve project from '{absoluteArchivePath}': {ex.Message}");
                throw;
            }
        }
        #endregion
    }
}
