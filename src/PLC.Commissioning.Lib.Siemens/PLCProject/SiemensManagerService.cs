using System;
using System.IO;
using System.Reflection;
using PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions;
using Serilog;
using Siemens.Engineering;

namespace PLC.Commissioning.Lib.Siemens.PLCProject
{
    /// <summary>
    /// Implements the <see cref="ISiemensManagerService"/> to manage interactions with the Siemens TIA Portal.
    /// </summary>
    public class SiemensManagerService : ISiemensManagerService
    {
        /// <summary>
        /// Represents the TIA Portal instance used for PLC interactions.
        /// </summary>
        private TiaPortal _tiaPortal;

        /// <summary>
        /// Indicates whether the TIA Portal should be started with a user interface.
        /// </summary>
        private bool _useUserInterface;

        /// <summary>
        /// Initializes a new instance of the <see cref="SiemensManagerService"/> class.
        /// </summary>
        /// <param name="useUserInterface">Specifies whether to use the TIA Portal with a user interface.</param>
        public SiemensManagerService(bool useUserInterface)
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
            _useUserInterface = useUserInterface;
        }

        /// <summary>
        /// Starts the TIA Portal with the specified mode.
        /// </summary>
        public void StartTIA()
        {
            TiaPortalMode mode = _useUserInterface ? TiaPortalMode.WithUserInterface : TiaPortalMode.WithoutUserInterface;
            _tiaPortal = new TiaPortal(mode);
            Log.Information($"TIA Portal started with{(mode == TiaPortalMode.WithUserInterface ? "" : "out")} user interface.");
        }

        /// <summary>
        /// Gets the current instance of the TIA Portal.
        /// </summary>
        /// <returns>The <see cref="TiaPortal"/> instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the TIA Portal is not started.</exception>
        public TiaPortal GetTiaPortal()
        {
            if (_tiaPortal is null)
            {
                throw new InvalidOperationException("TIA Portal is not started.");
            }

            return _tiaPortal;
        }

        /// <summary>
        /// Releases all resources used by the <see cref="SiemensManagerService"/> instance.
        /// </summary>
        public void Dispose()
        {
            _tiaPortal?.Dispose();
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
        }

        /// <summary>
        /// Resolves assembly references for the TIA Portal.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="ResolveEventArgs"/> that contains the event data.</param>
        /// <returns>The loaded assembly, or null if the assembly could not be found.</returns>
        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            int index = args.Name.IndexOf(',');
            if (index == -1)
            {
                return null;
            }
            string name = args.Name.Substring(0, index) + ".dll";
            string tiaPortalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TIA_Portal_Path"); // Adjust path as needed
            string path = Path.Combine(tiaPortalPath, name);
            string fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
            {
                return Assembly.LoadFrom(fullPath);
            }
            return null;
        }
    }
}
