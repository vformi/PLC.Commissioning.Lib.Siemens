using System;
using System.IO;
using System.Reflection;
using PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions;
using Serilog;
using Siemens.Engineering;

namespace PLC.Commissioning.Lib.Siemens.PLCProject
{
    public class SiemensManagerService : ISiemensManagerService
    {
        private TiaPortal _tiaPortal;
        private bool _useUserInterface;

        // double resolve!! not needed! use pages 80/84 of the manual https://cache.industry.siemens.com/dl/files/533/109798533/att_1069908/v1/TIAPortalOpennessenUS_en-US.pdf

        public SiemensManagerService(bool useUserInterface)
        {
            Log.Information("Initializing SiemensManagerService...");

            // Register the AssemblyResolve event early
            AppDomain.CurrentDomain.AssemblyResolve += MyResolver;
            _useUserInterface = useUserInterface;

            Log.Information("AssemblyResolve event attached.");
        }

        public void StartTIA()
        {
            Log.Information("Starting TIA Portal...");

            // Delayed logic to start TIA Portal until after the resolver is registered
            RunTiaPortal();
        }

        private void RunTiaPortal()
        {
            TiaPortalMode mode = _useUserInterface ? TiaPortalMode.WithUserInterface : TiaPortalMode.WithoutUserInterface;

            using (_tiaPortal = new TiaPortal(mode))
            {
                Log.Information($"TIA Portal started with{(mode == TiaPortalMode.WithUserInterface ? "" : "out")} user interface.");

                // Further implementation of your TIA logic can be added here.
            }
        }

        private static Assembly MyResolver(object sender, ResolveEventArgs args)
        {
            int index = args.Name.IndexOf(',');
            if (index == -1)
            {
                return null;
            }
            string name = args.Name.Substring(0, index) + ".dll";
            // Edit the following path according to your installed version of TIA Portal
            string path = Path.Combine(@"C:\Program Files\Siemens\Automation\Portal V17\PublicAPI\V17\", name);
            string fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
            {
                return Assembly.LoadFrom(fullPath);
            }
            return null;
        }

        public TiaPortal GetTiaPortal()
        {
            if (_tiaPortal == null)
            {
                throw new InvalidOperationException("TIA Portal has not been started.");
            }
            return _tiaPortal;
        }

        public void Dispose()
        {
            _tiaPortal?.Dispose();
            AppDomain.CurrentDomain.AssemblyResolve -= MyResolver;
        }
    }
}
