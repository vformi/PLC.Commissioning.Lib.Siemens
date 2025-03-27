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
        /// <inheritdoc />
        public TiaPortal TiaPortal { get; private set; }
        
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

            if (TiaPortal != null)
            {
                Log.Warning("TIA Portal is already running. Reusing the existing instance.");
                return;
            }

            // Try to connect to an existing instance
            foreach (TiaPortalProcess process in TiaPortal.GetProcesses())
            {
                try
                {
                    Log.Information("Found TIA Portal process with ID: {ProcessId} and Path: {Path}", process.Id, process.Path);
                    TiaPortal = process.Attach();
                    Log.Information("Connected to existing TIA Portal process.");
                    return;
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

            // If no existing instance is found, start a new one
            TiaPortalMode mode = _useUserInterface ? TiaPortalMode.WithUserInterface : TiaPortalMode.WithoutUserInterface;
            TiaPortal = new TiaPortal(mode);

            Log.Information(
                $"TIA Portal started with{(mode == TiaPortalMode.WithUserInterface ? "" : "out")} user interface.");
        }

        /// <summary>
        /// Cleans up and releases resources used by the <see cref="SiemensManagerService"/>.
        /// </summary>
        public void Dispose()
        {
            if (TiaPortal != null)
            {
                TiaPortal.Dispose();
                TiaPortal = null;
            }
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
