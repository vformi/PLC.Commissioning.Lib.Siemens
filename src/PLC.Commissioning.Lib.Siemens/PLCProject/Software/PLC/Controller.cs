using Siemens.Engineering.Connection;
using Siemens.Engineering.Download;
using Siemens.Engineering.HW;
using Siemens.Engineering.Online;
using PLC.Commissioning.Lib.Siemens.PLCProject.Software.Abstractions;
using PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions;
using Serilog;
using System;
using PLC.Commissioning.Lib.Siemens.PLCProject.Enums;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Software.PLC
{
    /// <summary>
    /// Implements the <see cref="IController"/> interface to manage PLC operations.
    /// </summary>
    public class Controller : IController
    {
        private readonly ICompilerService _compiler;
        private readonly OnlineProviderService _onlineProvider;
        private readonly NetworkConfigurationService _networkConfigurator;
        private readonly PLCOperationsService _plcOperations;

        /// <summary>
        /// Initializes a new instance of the <see cref="Controller"/> class with the specified services.
        /// </summary>
        /// <param name="compiler">The compiler service to use for compiling PLC projects.</param>
        /// <param name="onlineProvider">The online provider service to manage PLC online/offline states.</param>
        /// <param name="networkConfigurator">The network configurator service to manage network settings.</param>
        /// <param name="plcOperations">The PLC operations service to control PLC operations.</param>
        public Controller(
            ICompilerService compiler,
            OnlineProviderService onlineProvider,
            NetworkConfigurationService networkConfigurator,
            PLCOperationsService plcOperations)
        {
            _compiler = compiler;
            _onlineProvider = onlineProvider;
            _networkConfigurator = networkConfigurator;
            _plcOperations = plcOperations;
        }

        /// <summary>
        /// Displays the available PLC connection possibilities.
        /// </summary>
        public void DisplayPLCConnectionPossibilities() => _networkConfigurator.DisplayPLCConnectionPossibilities();

        /// <summary>
        /// Configures the network settings for the PLC using the specified network card and interface.
        /// </summary>
        /// <param name="networkCardName">The name of the network card to use.</param>
        /// <param="interfaceNumber">The interface number to use.</param>
        /// <param name="targetInterfaceName">The name of the target interface.</param>
        /// <returns><c>true</c> if the configuration succeeds; otherwise, <c>false</c>.</returns>
        public bool TryConfigureNetwork(string networkCardName, int interfaceNumber, string targetInterfaceName)
        {
            var result = _networkConfigurator.ConfigureNetwork(networkCardName, interfaceNumber, targetInterfaceName);

            switch (result)
            {
                case NetworkConfigurationResult.Success:
                    var targetConfiguration = _networkConfigurator.GetTargetConfiguration();
                    _plcOperations.SetTargetConfiguration(targetConfiguration);
                    Log.Information("Network configuration applied successfully with card {NetworkCard}.", networkCardName);
                    return true;

                case NetworkConfigurationResult.ModeNotFound:
                    Log.Error("Configuration mode 'PN/IE' not found.");
                    return false;

                case NetworkConfigurationResult.PcInterfaceNotFound:
                    Log.Error("PC interface '{NetworkCardName}' not found.", networkCardName);
                    return false;

                case NetworkConfigurationResult.TargetInterfaceNotFound:
                    Log.Error("Target interface '{TargetInterfaceName}' not found.", targetInterfaceName);
                    return false;

                case NetworkConfigurationResult.UnexpectedError:
                    Log.Error("Network configuration failed due to an unexpected error.");
                    return false;

                default:
                    Log.Error("Network configuration failed due to an unknown error.");
                    return false;
            }
        }

        /// <summary>
        /// Compiles the project associated with the specified CPU device item.
        /// </summary>
        /// <param name="cpu">The CPU <see cref="DeviceItem"/> to be compiled.</param>
        /// <returns><c>true</c> if the compilation was successful; otherwise, <c>false</c>.</returns>
        public bool CompileDevice(DeviceItem cpu) => _compiler.CompileProject(cpu);

        /// <summary>
        /// Downloads project to the PLC
        /// </summary>
        /// <param name="options">The Download options <see cref="DownloadOptions"/></param>
        /// <returns><c>true</c> if the PLC was started successfully; otherwise, <c>false</c>.</returns>
        public bool Download(DownloadOptions options) => _plcOperations.DownloadToPLC(options);

        /// <summary>
        /// Gets the current online state of the PLC device.
        /// </summary>
        /// <returns>The current <see cref="OnlineState"/> of the PLC device.</returns>
        public OnlineState GetOnlineState() => _onlineProvider.GetOnlineState();

        /// <summary>
        /// Stops the PLC.
        /// </summary>
        /// /// <returns><c>true</c> if the PLC was started successfully; otherwise, <c>false</c>.</returns>
        public bool Stop() => _plcOperations.StopPLC();

        /// <summary>
        /// Starts the PLC
        /// </summary>
        /// <returns><c>true</c> if the PLC was started successfully; otherwise, <c>false</c>.</returns>
        public bool Start() => _plcOperations.StartPLC();

        /// <summary>
        /// Switches the PLC to online mode.
        /// </summary>
        public void GoOnline() => _onlineProvider.GoOnline();

        /// <summary>
        /// Switches the PLC to offline mode.
        /// </summary>
        public void GoOffline() => _onlineProvider.GoOffline();

        /// <summary>
        /// Sets the target configuration for PLC operations.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfiguration"/> to set as the target configuration.</param>
        public void SetTargetConfiguration(IConfiguration configuration) => _plcOperations.SetTargetConfiguration(configuration);
    }
}
