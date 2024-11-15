using System;
using System.IO;
using System.Reflection;
using PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions;
using Serilog;
using Siemens.Engineering;

namespace PLC.Commissioning.Lib.Siemens.PLCProject
{
    /// <summary>
    /// Provides services to manage TIA Portal instances, including starting a new instance or connecting to an existing one.
    /// </summary>
    public class SiemensManagerService : ISiemensManagerService
    {
        public TiaPortal tiaPortal;
        private bool _useUserInterface;

        // The AssemblyResolve method could be implemented here if dynamic loading is required.
        // However, app.config or copying the DLL directly is the preferred approach.
        // See pages 80–84 of the TIA Portal Openness manual for more details:
        // https://cache.industry.siemens.com/dl/files/533/109798533/att_1069908/v1/TIAPortalOpennessenUS_en-US.pdf
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SiemensManagerService"/> class.
        /// </summary>
        /// <param name="useUserInterface">Indicates whether the TIA Portal should be started with a user interface.</param>
        public SiemensManagerService(bool useUserInterface)
        {
            _useUserInterface = useUserInterface;
            // Optionally register the AssemblyResolve event if needed (commented out by default).
            //AppDomain.CurrentDomain.AssemblyResolve += MyResolver;
        }

        /// <summary>
        /// Starts a new instance of TIA Portal.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if TIA Portal cannot be started.</exception>
        public void StartTIA()
        {
            Log.Information("Starting TIA Portal...");

            // Additional logic before starting TIA Portal can be added here.
            RunTiaPortal();
        }
        
        /// <summary>
        /// Retrieves and connects to a running instance of TIA Portal.
        /// </summary>
        /// <param name="processId">Optional: The process ID of the desired TIA Portal instance.</param>
        /// <returns>A connected <see cref="TiaPortal"/> instance or null if no running instance is found.</returns>
        public TiaPortal GetRunningTiaPortalInstance(int? processId = null)
        {
            foreach (TiaPortalProcess process in TiaPortal.GetProcesses())
            {
                try
                {
                    Log.Information("Found TIA Portal process with ID: {ProcessId} and Path: {Path}", process.Id, process.Path);

                    // If a process ID is specified, only connect to that specific process
                    if (processId.HasValue && process.Id != processId.Value)
                        continue;

                    Log.Information("Connecting to TIA Portal process ID: {ProcessId}", process.Id);

                    tiaPortal = process.Attach();

                    // Log additional attributes about the process
                    Log.Information("Connected to TIA Portal:");

                    return tiaPortal;
                }
                catch (EngineeringSecurityException ex)
                {
                    Log.Error("Failed to connect to TIA Portal process due to a security exception: {Error}", ex.Message);
                }
                catch (Exception ex)
                {
                    Log.Error("Error connecting to TIA Portal process: {Error}", ex.Message);
                }
            }

            Log.Warning("No running TIA Portal instance found.");
            return null;
        }
        
        /// <summary>
        /// Starts new TIA Portal instance in the specified mode (with or without user interface).
        /// </summary>
        private void RunTiaPortal()
        {
            TiaPortalMode mode = _useUserInterface ? TiaPortalMode.WithUserInterface : TiaPortalMode.WithoutUserInterface;
            tiaPortal = new TiaPortal(mode);
            Log.Information(
                $"TIA Portal started with{(mode == TiaPortalMode.WithUserInterface ? "" : "out")} user interface.");
        }

        /// <summary>
        /// Gets the current <see cref="TiaPortal"/> instance.
        /// </summary>
        /// <returns>The current <see cref="TiaPortal"/> instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the TIA Portal instance has not been started or connected.</exception>
        public TiaPortal GetTiaPortal()
        {
            if (tiaPortal == null)
            {
                throw new InvalidOperationException("TIA Portal has not been started.");
            }
            return tiaPortal;
        }

        /// <summary>
        /// Cleans up and releases resources used by the <see cref="SiemensManagerService"/>.
        /// </summary>
        public void Dispose()
        {
            tiaPortal?.Dispose();
            // Unregister the AssemblyResolve event if previously registered.
            AppDomain.CurrentDomain.AssemblyResolve -= MyResolver;
        }

        /// <summary>
        /// ------------- Example of AssemblyResolve method if dynamic dependency resolution is needed
        /// Resolves assembly references dynamically if required.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="ResolveEventArgs"/> that contains the event data.</param>
        /// <returns>The resolved assembly or null if the assembly could not be found.</returns>
        private static Assembly MyResolver(object sender, ResolveEventArgs args)
        {
            int index = args.Name.IndexOf(',');
            if (index == -1)
            {
                return null;
            }
            string name = args.Name.Substring(0, index) + ".dll";
            Log.Information("Assembly resolve called for: {Name}", name);

            // Adjust the path to match the installed TIA Portal version
            string path = Path.Combine(@"C:\Program Files\Siemens\Automation\Portal V17\PublicAPI\V17\", name);
            if (File.Exists(path))
            {
                Log.Information("Assembly found at: {Path}", path);
                return Assembly.LoadFrom(path);
            }

            Log.Error("Assembly not found for: {Name}", args.Name);
            return null;
        }
    }
}
