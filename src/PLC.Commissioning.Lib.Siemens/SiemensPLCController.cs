using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Serilog;
using PLC.Commissioning.Lib.Siemens.PLCProject;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;
using PLC.Commissioning.Lib.Siemens.PLCProject.Software.PLC;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Handlers;
using PLC.Commissioning.Lib.Siemens.PLCProject.UI;
using PLC.Commissioning.Lib.Abstractions;
using Siemens.Engineering;
using Siemens.Engineering.Download;
using Siemens.Engineering.Online;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Abstractions;
using PLC.Commissioning.Lib.Siemens.Webserver.Controllers;
using Siemens.Simatic.S7.Webserver.API.Enums;

namespace PLC.Commissioning.Lib.Siemens
{
    /// <summary>
    /// Implements the <see cref="IPLCControllerSiemens"/> interface for managing Siemens PLCs.
    /// Provides methods for configuring, initializing, and managing Siemens PLC projects.
    /// </summary>
    public class SiemensPLCController : IPLCControllerSiemens, IDisposable
    {
        # region Private Variables
        // Internal variables for application workflow

        /// <summary>
        /// Service for managing the Siemens Manager instance.
        /// </summary>
        private SiemensManagerService _manager;

        /// <summary>
        /// Represents the TIA Portal instance.
        /// </summary>
        private TiaPortal _tiaPortalInstance;

        /// <summary>
        /// Handles project-related operations within the TIA Portal.
        /// </summary>
        private ProjectHandlerService _projectHandler;

        /// <summary>
        /// Manages hardware configurations in the project.
        /// </summary>
        private HardwareHandler _hardwareHandler;

        /// <summary>
        /// Manages IO systems within the project.
        /// </summary>
        private IOSystemHandler _ioSystemHandler;

        /// <summary>
        /// Represents the CPU device item in the hardware configuration.
        /// </summary>
        private DeviceItem _cpu;

        /// <summary>
        /// Provides download functionalities to the PLC.
        /// </summary>
        private DownloadProvider _downloadProvider;

        /// <summary>
        /// Represents the controller object for PLC interactions.
        /// </summary>
        private Controller _controller;

        /// <summary>
        /// Handles UI interactions during download operations.
        /// </summary>
        private UIDownloadHandler _uiDownloadHandler;

        /// <summary>
        /// Handles webserver interactions for start and stop procedures
        /// </summary>
        private RPCController _rpcController;

        // Internal variables to hold configuration values

        /// <summary>
        /// The path to the Siemens PLC project.
        /// </summary>
        private string _projectPath;
        /// <summary>
        /// The network card used for PLC communications.
        /// </summary>
        private string _networkCard;

        // Flag to indicate if the object has already been disposed

        /// <summary>
        /// Indicates whether the object has been disposed.
        /// </summary>
        private bool _disposed = false;

        // Flag to indicate safety workflow

        /// <summary>
        /// Indicates whether safety features are enabled.
        /// </summary>
        private bool _safety = false;
        #endregion

        /// <summary>
        /// Gets the instance of <see cref="IOSystemHandler"/> for managing IO systems within the PLC project.
        /// </summary>
        /// <returns>
        /// The <see cref="IOSystemHandler"/> instance. If the instance does not exist, it is created;
        /// otherwise, the existing instance is reused. Returns <c>null</c> if the project handler is not initialized.
        /// </returns>
        /// <remarks>
        /// This method ensures that the <see cref="IOSystemHandler"/> is created only once (singleton pattern) and
        /// reused across multiple method calls. If the <see cref="_projectHandler"/> is not initialized,
        /// the method logs an error and returns <c>null</c>.
        /// </remarks>
        private IOSystemHandler GetIoSystemHandler()
        {
            if (_ioSystemHandler is null)
            {
                if (_projectHandler is null)
                {
                    Log.Error("Project handler is not initialized. Cannot create IOSystemHandler.");
                    return null;
                }

                _ioSystemHandler = new IOSystemHandler(_projectHandler);
                Log.Information("IOSystemHandler was successfully created.");
            }
            else
            {
                Log.Information("Reusing existing IOSystemHandler instance.");
            }

            return _ioSystemHandler;
        }

        /// <summary>
        /// Configures the controller using a JSON configuration file.
        /// </summary>
        /// <param name="jsonFilePath">The path to the JSON configuration file.</param>
        /// <returns><c>true</c> if the configuration was successful; otherwise, <c>false</c>.</returns>
        public bool Configure(string jsonFilePath)
        {
            try
            {
                var jsonContent = File.ReadAllText(jsonFilePath);
                var config = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);

                _projectPath = config["projectPath"].ToString();
                _networkCard = config["networkCard"].ToString();

                Log.Debug("Configuration Loaded:");
                Log.Debug($"Project Path: {_projectPath}");
                Log.Debug($"Network Card: {_networkCard}");

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Error during configuration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Initializes the Siemens PLC controller.
        /// </summary>
        /// <param name="safety">Indicates whether safety mode is enabled.</param>
        /// <returns><c>true</c> if the initialization was successful; otherwise, <c>false</c>.</returns>
        public bool Initialize(bool safety = false)
        {
            try
            {
                Log.Information("Initialization started with safety mode: {SafetyMode}", safety);

                // Set safety mode flag
                _safety = safety;

                // Step 1: Start the TIA Portal manager
                Log.Debug("Starting TIA Portal Manager...");
                _manager = new SiemensManagerService(_safety);
                _manager.StartTIA();
                _tiaPortalInstance = _manager.TiaPortal;

                // Step 2: Initialize the ProjectHandler
                Log.Debug("Initializing ProjectHandlerService...");
                _projectHandler = new ProjectHandlerService(_tiaPortalInstance);

                // Step 3: Verify and handle the project file
                if (string.IsNullOrEmpty(_projectPath) || !File.Exists(_projectPath))
                {
                    Log.Error("Project file not found at path: {ProjectPath}", _projectPath);
                    return false;
                }
                if (!_projectHandler.HandleProject(_projectPath))
                {
                    Log.Error("Failed to handle project at path: {ProjectPath}", _projectPath);
                    return false;
                }

                // Step 4: Initialize safety-related components (if applicable)
                if (_safety)
                {
                    Log.Warning("Safety mode enabled. Initializing UIDownloadHandler...");
                    _uiDownloadHandler = new UIDownloadHandler();
                }

                // Step 5: Setup HardwareHandler and find CPU
                Log.Debug("Setting up HardwareHandler...");
                _hardwareHandler = new HardwareHandler(_projectHandler);
                _cpu = _hardwareHandler.FindCPU();

                // Enumerate devices in the project
                _hardwareHandler.EnumerateProjectDevices();

                // Step 6: Initialize services for online and download operations
                Log.Debug("Initializing OnlineProvider and DownloadProvider...");
                var onlineProvider = _cpu.GetService<OnlineProvider>();
                _downloadProvider = _cpu.GetService<DownloadProvider>();

                // Create helper services for controller configuration
                var onlineProviderService = new OnlineProviderService(onlineProvider);
                var plcOperationsService = new PLCOperationsService(_downloadProvider);
                var projectCompiler = new CompilerService();
                var networkConfigurator = new NetworkConfigurationService(onlineProvider, _downloadProvider);

                // Step 7: Setup the main controller
                Log.Debug("Configuring main controller...");
                _controller = new Controller(
                    projectCompiler,
                    onlineProviderService,
                    networkConfigurator,
                    plcOperationsService
                );

                // Step 8: Initialize or reuse IOSystemHandler
                Log.Debug("Initializing IOSystemHandler...");
                var ioSystemHandler = GetIoSystemHandler();
                if (ioSystemHandler is null)
                {
                    Log.Error("Failed to initialize IOSystemHandler.");
                    return false;
                }

                // Step 9: Determine the CPU's IP address and validate network connectivity
                string cpuIP = _ioSystemHandler.GetPLCIPAddress(_cpu);
                if (!networkConfigurator.PingIpAddress(_networkCard, cpuIP))
                {
                    Log.Error("Unable to ping CPU at IP address: {CpuIP}", cpuIP);
                    return false;
                }

                // Configure the target network
                var targetConfiguration = networkConfigurator.GetTargetConfiguration();
                _controller.SetTargetConfiguration(targetConfiguration);

                // Step 10: Configure the network card
                if (string.IsNullOrEmpty(_networkCard))
                {
                    Log.Error("Network card configuration is missing.");
                    return false;
                }
                if (!_controller.TryConfigureNetwork(_networkCard, 1, "1 X1"))
                {
                    Log.Error("Failed to configure network with card: {NetworkCard}", _networkCard);
                    return false;
                }

                // Step 11: Initialize RPC Controller
                Log.Debug("Initializing RPCController...");
                _rpcController = InitializeRpcController(cpuIP);
                if (_rpcController == null)
                {
                    Log.Warning("RPCController initialization failed. Proceeding without RPC support.");
                }

                Log.Information("Initialization completed successfully.");
                return true;
            }
            catch (EngineeringSecurityException ex)
            {
                Log.Error("EngineeringSecurityException: Ensure the user '{User}' is a member of the Siemens TIA Openness group.", Environment.UserName);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error("Initialization failed: {ErrorMessage}", ex.Message);
                return false;
            }
        }


        /// <summary>
        /// Imports a device configuration into the Siemens PLC project.
        /// </summary>
        /// <param name="filePath">The path to the device configuration file.</param>
        /// <returns>A dictionary with device names as keys and corresponding <see cref="Device"/> objects as values, or <c>null</c> if the import fails.</returns>
        public object ImportDevice(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    Log.Error("The device configuration file path is null or empty.");
                    return null;
                }

                if (!File.Exists(filePath))
                {
                    Log.Error($"The device configuration file '{filePath}' does not exist.");
                    return null;
                }

                if (_projectHandler is null)
                {
                    Log.Error("Device import failed: project is not Initialized.");
                    return null;
                }

                if (!_projectHandler.ImportAmlFile(filePath))
                {
                    Log.Error("Failed to import AML file.");
                    return null;
                }

                _hardwareHandler.EnumerateProjectDevices();

                var ioSystemHandler = GetIoSystemHandler();
                if (ioSystemHandler is null)
                {
                    Log.Error("Failed to initialize IOSystemHandler.");
                    return null;
                }
                var (subnet, ioSystem) = ioSystemHandler.FindSubnetAndIoSystem();
                var devices = _hardwareHandler.GetDevices();
                var deviceDictionary = new Dictionary<string, Device>();

                foreach (var device in devices)
                {
                    // Connect the device to the IO system
                    ioSystemHandler.ConnectDeviceToIoSystem(device, subnet, ioSystem);
                    Log.Information($"Device '{device.DeviceItems[1].Name}' was successfully connected to the IO system.");
                    // Add the device to the dictionary with its name as the key
                    deviceDictionary[device.DeviceItems[1].Name] = device;
                }

                Log.Debug("Devices in the dictionary:");
                foreach (var i in deviceDictionary)
                {
                    Log.Debug($"Device {i}: {i.Value}");
                }

                Log.Information("Device import was successful.");
                return deviceDictionary;
            }
            catch (Exception ex)
            {
                Log.Error($"Device import failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Configures specific device parameters such as IP address and Profinet name.
        /// </summary>
        /// <param name="parametersToConfigure">A dictionary containing key-value pairs of parameters to configure.</param>
        /// <returns><c>true</c> if the configuration was successful; otherwise, <c>false</c>.</returns>
        /// <example>
        /// Example JSON structure:
        /// <code>
        /// {
        ///     "ipAddress": "192.168.0.1",
        ///     "profinetName": "PLC_1"
        /// }
        /// </code>
        /// </example>
        public bool ConfigureDevice(object device, Dictionary<string, object> parametersToConfigure)
        {
            try
            {
                if (device is null)
                {
                    Log.Error("Device can not be null.");
                    return false;
                }

                if (parametersToConfigure == null || parametersToConfigure.Count == 0)
                {
                    Log.Error("Parameters to configure cannot be null or empty.");
                    return false;
                }

                // Attempt to cast the device to the expected Device type
                var specificDevice = device as Device;
                if (specificDevice is null)
                {
                    Log.Error($"The provided object could not be cast to a Device. Ensure that the correct object type is being used.");
                    return false;
                }

                var ioSystemHandler = GetIoSystemHandler();
                if (ioSystemHandler is null)
                {
                    Log.Error("Failed to initialize IOSystemHandler.");
                    return false;
                }

                if (parametersToConfigure.ContainsKey("ipAddress") &&
                    !string.IsNullOrEmpty(parametersToConfigure["ipAddress"]?.ToString()))
                {
                    if (!ioSystemHandler.SetDeviceIPAddress(specificDevice, parametersToConfigure["ipAddress"].ToString()))
                    {
                        Log.Error($"Failed to set IP address for device {specificDevice.DeviceItems[1].Name}.");
                        return false;
                    }
                }

                if (parametersToConfigure.ContainsKey("profinetName") &&
                    !string.IsNullOrEmpty(parametersToConfigure["profinetName"]?.ToString()))
                {
                    if (!ioSystemHandler.SetProfinetName(specificDevice, parametersToConfigure["profinetName"].ToString()))
                    {
                        Log.Error($"Failed to set Profinet name for device {specificDevice.DeviceItems[1].Name}.");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Device configuration failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Retrieves a device object by its name.
        /// </summary>
        /// <param name="deviceName">The name of the device to retrieve.</param>
        /// <returns>
        /// An object representing the device with the specified name, 
        /// or <c>null</c> if no such device is found. 
        /// The returned object should be cast to a <see cref="Device"/>.
        /// </returns>
        /// <remarks>
        /// Ensure that the returned object is cast to the <see cref="Device"/> type by the caller.
        /// </remarks>
        public object GetDeviceByName(string deviceName)
        {
            try
            {
                Device device = _hardwareHandler.GetDeviceByName(deviceName);

                if (device is null)
                {
                    Log.Warning($"Device named '{deviceName}' was not found.");
                }
                else
                {
                    Log.Information($"Device named '{deviceName}' found successfully.");
                }

                return device;
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred while retrieving the device named '{deviceName}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Retrieves device parameters for a specified module.
        /// </summary>
        /// <param name="device">The device object to retrieve parameters for.</param>
        /// <param name="gsdFilePath">The file path to the GSD file.</param>
        /// <param name="moduleName">The name of the module to retrieve parameters for.</param>
        /// <param name="parameterSelections">Optional list of parameters to retrieve.</param>
        /// <param name="safety">Indicates whether safety parameters are required.</param>
        /// <returns><c>true</c> if the parameters were retrieved successfully; otherwise, <c>false</c>.</returns>
        public bool GetDeviceParameters(object device, string gsdFilePath, string moduleName, List<string> parameterSelections = null, bool safety = false)
        {
            try
            {
                // Validate the input device
                if (device is null)
                {
                    Log.Error("Device is null.");
                    return false;
                }

                IOSystemHandler ioSystemHandler = GetIoSystemHandler();
                if (ioSystemHandler is null)
                {
                    Log.Error("Failed to initialize IOSystemHandler.");
                    return false;
                }

                // Cast the device to the expected type
                if (!(device is Device specificDevice))
                {
                    Log.Error("Failed to cast device to the expected type.");
                    return false;
                }

                // Read and initialize GSD file
                string xmlContent = File.ReadAllText(gsdFilePath);
                var gsdHandler = new GSDHandler();
                if (!gsdHandler.Initialize(gsdFilePath))
                {
                    return false;
                }

                // Retrieve and validate module information
                ModuleInfo moduleInfo = new ModuleInfo(gsdHandler);
                var deviceAttributes = ioSystemHandler.GetDeviceIdentificationAttributes(specificDevice);
                if (deviceAttributes is null)
                {
                    Log.Error("Failed to retrieve device identification attributes.");
                    return false;
                }

                // Validate the device name, firmware version, and order number
                if (!IsDeviceInfoMatching(moduleInfo, deviceAttributes))
                {
                    return false;
                }

                // Handle the special case where the moduleName is "DAP"
                if (moduleName.Equals("DAP", StringComparison.OrdinalIgnoreCase))
                {
                    if (safety)
                    {
                        Log.Error("DAP cannot have safety parameters.");
                        return false;
                    }

                    // Use DeviceAccessPointList to retrieve the DAP item
                    var dapList = new DeviceAccessPointList(gsdHandler);
                    var (dapItem, hasChangeableParameters) = dapList.GetDeviceAccessPointItemByID(moduleName);

                    if (dapItem is null)
                    {
                        Log.Error($"DAP '{moduleName}' not found in GSD file.");
                        return false;
                    }

                    if (!hasChangeableParameters)
                    {
                        Log.Error($"DAP '{moduleName}' exists but has no changeable parameters.");
                        return false;
                    }

                    // Retrieve module from the hardware handler
                    GsdDeviceItem module = _hardwareHandler.GetDeviceDAP(specificDevice);
                    if (module is null)
                    {
                        return false;
                    }

                    // Handle regular parameters for DAP
                    return HandleRegularParameters(dapItem, module, parameterSelections);
                }
                else
                {
                    // Retrieve module item and check for changeable parameters
                    ModuleList moduleList = new ModuleList(gsdHandler);
                    var (moduleItem, hasChangeableParameters) = moduleList.GetModuleItemByName(moduleName);

                    if (moduleItem is null)
                    {
                        Log.Error($"Module '{moduleName}' not found in GSD file.");
                        return false;
                    }

                    if (!hasChangeableParameters)
                    {
                        Log.Error($"Module '{moduleName}' exists but has no changeable parameters.");
                        return false;
                    }

                    // Retrieve module from the hardware handler
                    GsdDeviceItem module = _hardwareHandler.GetDeviceModuleByName(specificDevice, moduleName);
                    if (module is null)
                    {
                        return false;
                    }

                    // Handle safety or regular parameters
                    return safety ? HandleSafetyParameters(module, parameterSelections) : HandleRegularParameters(moduleItem, module, parameterSelections);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Getting device parameters failed: {ex.Message} {ex}");
                return false;
            }
        }

        /// <summary>
        /// Sets device parameters for a specified module.
        /// </summary>
        /// <param name="device">The device object to configure.</param>
        /// <param name="gsdFilePath">The file path to the GSD file.</param>
        /// <param name="moduleName">The name of the module to configure.</param>
        /// <param name="parametersToSet">A dictionary of parameters to set.</param>
        /// <param name="safety">Indicates whether safety parameters are being set.</param>
        /// <returns><c>true</c> if the parameters were set successfully; otherwise, <c>false</c>.</returns>
        public bool SetDeviceParameters(object device, string gsdFilePath, string moduleName, Dictionary<string, object> parametersToSet, bool safety = false)
        {
            try
            {
                // Validate the input device
                if (device is null)
                {
                    Log.Error("Device is null.");
                    return false;
                }

                IOSystemHandler ioSystemHandler = GetIoSystemHandler();
                if (ioSystemHandler is null)
                {
                    Log.Error("Failed to initialize IOSystemHandler.");
                    return false;
                }

                // Cast the device to the expected type
                if (!(device is Device specificDevice))
                {
                    Log.Error("Failed to cast device to the expected type.");
                    return false;
                }

                // Read and initialize GSD file
                string xmlContent = File.ReadAllText(gsdFilePath);
                var gsdHandler = new GSDHandler();
                if (!gsdHandler.Initialize(gsdFilePath))
                {
                    return false;
                }

                // Retrieve and validate module information
                ModuleInfo moduleInfo = new ModuleInfo(gsdHandler);
                var deviceAttributes = ioSystemHandler.GetDeviceIdentificationAttributes(specificDevice);
                if (deviceAttributes is null)
                {
                    Log.Error("Failed to retrieve device identification attributes.");
                    return false;
                }

                // Validate the device name, firmware version, and order number
                if (!IsDeviceInfoMatching(moduleInfo, deviceAttributes))
                {
                    return false;
                }

                // Handle the special case where the moduleName is "DAP"
                if (moduleName.Equals("DAP", StringComparison.OrdinalIgnoreCase))
                {
                    if (safety)
                    {
                        Log.Error("DAP cannot have safety parameters.");
                        return false;
                    }

                    // Use DeviceAccessPointList to retrieve the DAP item
                    var dapList = new DeviceAccessPointList(gsdHandler);
                    var (dapItem, hasChangeableParameters) = dapList.GetDeviceAccessPointItemByID(moduleName);

                    if (dapItem is null)
                    {
                        Log.Error($"DAP '{moduleName}' not found in GSD file.");
                        return false;
                    }

                    if (!hasChangeableParameters)
                    {
                        Log.Error($"DAP '{moduleName}' exists but has no changeable parameters.");
                        return false;
                    }

                    // Retrieve module from the hardware handler
                    GsdDeviceItem module = _hardwareHandler.GetDeviceDAP(specificDevice);
                    if (module is null)
                    {
                        return false;
                    }

                    // Handle regular parameters for DAP
                    return SetRegularParameters(dapItem, module, parametersToSet);
                }
                else
                {
                    // Retrieve module item and check for changeable parameters
                    ModuleList moduleList = new ModuleList(gsdHandler);
                    var (moduleItem, hasChangeableParameters) = moduleList.GetModuleItemByName(moduleName);

                    if (moduleItem is null)
                    {
                        Log.Error($"Module '{moduleName}' not found in GSD file.");
                        return false;
                    }

                    if (!hasChangeableParameters)
                    {
                        Log.Error($"Module '{moduleName}' exists but has no changeable parameters.");
                        return false;
                    }

                    // Retrieve module from the hardware handler
                    GsdDeviceItem module = _hardwareHandler.GetDeviceModuleByName(specificDevice, moduleName);
                    if (module is null)
                    {
                        Log.Error($"Module '{moduleName}' not found in the device.");
                        return false;
                    }
                    
                    Log.Information($"Setting device parameters succeeded for module '{moduleName}'.");
                    // Handle safety or regular parameters
                    return safety ? SetSafetyParameters(module, parametersToSet) : SetRegularParameters(moduleItem, module, parametersToSet);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Setting device parameters failed: {ex.Message} {ex}");
                return false;
            }
        }

        /// <summary>
        /// Starts the PLC.
        /// </summary>
        /// <returns><c>true</c> if the PLC was started successfully; otherwise, <c>false</c>.</returns>
        public bool Start()
        {
            try
            {
                if (_rpcController != null)
                {
                    // Attempt to start the PLC using the async method synchronously
                    _rpcController.PlcController.ChangeOperatingModeAsync(ApiPlcOperatingMode.Run).GetAwaiter().GetResult();
                    Log.Information("PLC started successfully using ChangeOperatingModeAsync.");
                    return true;
                }
                else
                {
                    Log.Warning("RPCController is not initialized. Falling back to the original Start method.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Async Start operation failed: {ex.Message}");
                Log.Warning("Falling back to the original Start method.");
            }

            // Fallback to the original Start method
            try
            {
                if (_safety)
                {
                    _controller.GoOnline(); // Figure out how to handle this
                    return _uiDownloadHandler.StartPLC();
                }
                else
                {
                    return _controller.Start();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Fallback Start operation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stops the PLC.
        /// </summary>
        /// <returns><c>true</c> if the PLC was stopped successfully; otherwise, <c>false</c>.</returns>
        public bool Stop()
        {
            try
            {
                if (_rpcController != null)
                {
                    // Attempt to stop the PLC using the async method synchronously
                    _rpcController.PlcController.ChangeOperatingModeAsync(ApiPlcOperatingMode.Stop).GetAwaiter().GetResult();
                    Log.Information("PLC stopped successfully using ChangeOperatingModeAsync.");
                    return true;
                }
                else
                {
                    Log.Warning("RPCController is not initialized. Falling back to the original Stop method.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Async Stop operation failed: {ex.Message}");
                Log.Warning("Falling back to the original Stop method.");
            }

            // Fallback to the original Stop method
            try
            {
                if (_safety)
                {
                    _controller.GoOnline(); // Figure out how to handle this
                    return _uiDownloadHandler.StopPLC();
                }
                else
                {
                    return _controller.Stop();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Fallback Stop operation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Compiles the current PLC project.
        /// </summary>
        /// <returns><c>true</c> if the compilation was successful; otherwise, <c>false</c>.</returns>
        public bool Compile()
        {
            try
            {
                return _controller.CompileDevice(_cpu);
            }
            catch (Exception ex)
            {
                Log.Error($"Compilation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Downloads the PLC project to the PLC device.
        /// </summary>
        /// <param name="options">Options for the download process.</param>
        /// <returns><c>true</c> if the download was successful; otherwise, <c>false</c>.</returns>
        public bool Download(object options)
        {
            try
            {
                var downloadProvider = _cpu.GetService<DownloadProvider>();
                if (options is string optionString && optionString == "safety" && _safety)
                {
                    Log.Warning("Loading or overloading fail-safe data in Openness is not permitted.");
                    Log.Warning("Proceeding to download fail-safe data using automated UI interaction...");
                    return _uiDownloadHandler.DownloadProcedure();
                }
                else if (options is DownloadOptions downloadOptions)
                {
                    return _controller.Download(downloadOptions);
                }
                else
                {
                    Log.Error("Invalid options provided for download.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Download failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Imports additional items such as PLC tags or other helper files into the project.
        /// </summary>
        /// <param name="filesToImport">A dictionary where the key represents the type of import (e.g., "plcTags") and the value is the file path.</param>
        /// <returns><c>true</c> if the import was successful; otherwise, <c>false</c>.</returns>
        /// <example>
        /// Example JSON structure:
        /// <code>
        /// {
        ///     "plcTags": "C:\\path\\to\\tags.xml"
        /// }
        /// </code>
        /// </example>
        public bool AdditionalImport(Dictionary<string, object> filesToImport)
        {
            try
            {
                // Check if the dictionary contains the key "plcTags"
                if (filesToImport.ContainsKey("plcTags") &&
                    !string.IsNullOrEmpty(filesToImport["plcTags"]?.ToString()))
                {
                    PLCTagsService tagsService = new PLCTagsService();
                    tagsService.ImportTagTable(_cpu, filesToImport["plcTags"].ToString());
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Additional import failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Exports specified items such as all PLC tags or the project to a different directory.
        /// </summary>
        /// <param name="filesToExport">A dictionary where the key represents the type of export (e.g., "plcTags") and the value is the export path.</param>
        /// <returns><c>true</c> if the export was successful; otherwise, <c>false</c>.</returns>
        /// <example>
        /// Example JSON structure:
        /// <code>
        /// {
        ///     "plcTags": "C:\\export\\path\\tags.xml"
        /// }
        /// </code>
        /// </example>
        public bool Export(Dictionary<string, object> filesToExport)
        {
            try
            {
                PLCTagsService tagsService = new PLCTagsService();
                tagsService.ExportAllTagTables(_cpu, filesToExport["plcTags"].ToString());
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Export failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Saves the current project to a specific Documents/Openness/Saved_Projects/ directory.
        /// </summary>
        /// <param name="projectName">The name of the project.</param>
        /// <returns><c>true</c> if the project was saved successfully; otherwise, <c>false</c>.</returns>
        public bool SaveProjectAs(string projectName)
        {
            try
            {
                _projectHandler.SaveProjectAs(projectName);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Export failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Prints GSD information for debugging purposes.
        /// </summary>
        /// <param name="gsdFilePath">The path to the GSD file to be processed.</param>
        /// <param name="moduleName">Optional: The name of a specific module to print information for. If not provided, all modules will be printed.</param>
        /// <returns><c>true</c> if the GSD information was printed successfully; otherwise, <c>false</c>.</returns>
        public bool PrintGSDInformations(string gsdFilePath, string moduleName = null)
        {
            try
            {
                // Initialize the GSD handler with the provided file path
                GSDHandler gsdHandler = new GSDHandler();
                if (!gsdHandler.Initialize(gsdFilePath))
                {
                    Log.Error("Failed to initialize GSDHandler with the provided file path.");
                    return false;
                }

                // TODO: figure out how to use the ToString methods in the Serilog debug 
                // Print general module information
                ModuleInfo moduleInfo = new ModuleInfo(gsdHandler);
                Console.WriteLine(moduleInfo.ToString());

                // Print DAP information
                DeviceAccessPointList dapList = new DeviceAccessPointList(gsdHandler);
                Console.WriteLine(dapList.ToString());

                // Print the list of modules
                ModuleList moduleList = new ModuleList(gsdHandler);
                Console.WriteLine(moduleList.ToString());
                
                // If a specific module name is provided, print its information
                if (!string.IsNullOrEmpty(moduleName))
                {
                    if (moduleName == "DAP")
                    {
                        (DeviceAccessPointItem dapItem, _) = dapList.GetDeviceAccessPointItemByID(moduleName);
                        if (dapItem != null)
                        {
                            Console.WriteLine(dapItem.ToString());
                        }
                        else
                        {
                            Log.Warning($"'{moduleName}' not found in the GSD file.");
                        }
                    }
                    else
                    {
                        (ModuleItem moduleItem, _) = moduleList.GetModuleItemByName(moduleName);
                        if (moduleItem != null)
                        {
                            Console.WriteLine(moduleItem.ToString());
                        }
                        else
                        {
                            Log.Warning($"Module with name '{moduleName}' not found in the GSD file.");
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred while printing GSD information: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="SiemensPLCController"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="SiemensPLCController"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">A boolean value indicating whether to release managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose ProjectHandlerService
                    if (_projectHandler != null)
                    {
                        try
                        {
                            _projectHandler.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("Error disposing ProjectHandlerService: {Message}", ex.Message);
                        }
                    }

                    // Dispose UIDownloadHandler
                    if (_safety && _uiDownloadHandler != null)
                    {
                        try
                        {
                            _uiDownloadHandler.CloseTIA();
                            _uiDownloadHandler.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("Error disposing UIDownloadHandler: {Message}", ex.Message);
                        }
                    }

                    // Dispose HardwareHandler
                    if (_hardwareHandler is IDisposable disposableHardwareHandler)
                    {
                        try
                        {
                            disposableHardwareHandler.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("Error disposing HardwareHandler: {Message}", ex.Message);
                        }
                    }

                    // Dispose IOSystemHandler
                    if (_ioSystemHandler is IDisposable disposableIOSystemHandler)
                    {
                        try
                        {
                            disposableIOSystemHandler.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("Error disposing IOSystemHandler: {Message}", ex.Message);
                        }
                    }

                    // Dispose SiemensManagerService
                    if (_manager != null)
                    {
                        try
                        {
                            _manager.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("Error disposing SiemensManagerService: {Message}", ex.Message);
                        }
                    }

                    // Dispose TIA Portal Instance
                    if (_tiaPortalInstance != null)
                    {
                        try
                        {
                            _tiaPortalInstance.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("Error disposing TIA Portal Instance: {Message}", ex.Message);
                        }
                    }
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Destructor for <see cref="SiemensPLCController"/>.
        /// </summary>
        ~SiemensPLCController()
        {
            Dispose(false);
        }

        #region Private Functions
        /// <summary>
        /// Checks if the device information from the provided module matches the expected device attributes.
        /// </summary>
        /// <param name="moduleInfo">The module information to be compared.</param>
        /// <param name="deviceAttributes">A dictionary of device attributes to compare against.</param>
        /// <returns><c>true</c> if all device information matches; otherwise, <c>false</c>.</returns>
        private bool IsDeviceInfoMatching(ModuleInfo moduleInfo, Dictionary<string, string> deviceAttributes)
        {
            // Compare device name
            if (!string.Equals(moduleInfo.Model.Name, deviceAttributes["TypeName"]?.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                Log.Error($"Device name mismatch: GSD module name '{moduleInfo.Model.Name}' does not match the device name: '{deviceAttributes["TypeName"]}'.");
                return false;
            }

            // Compare firmware version
            if (!string.Equals(moduleInfo.Model.SoftwareRelease, deviceAttributes["FirmwareVersion"]?.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                Log.Error($"Firmware version mismatch: GSD module firmware version '{moduleInfo.Model.SoftwareRelease}' does not match the device firmware version: '{deviceAttributes["FirmwareVersion"]}'.");
                return false;
            }

            // Compare order number
            if (!string.Equals(moduleInfo.Model.OrderNumber, deviceAttributes["OrderNumber"]?.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                Log.Error($"Order number mismatch: GSD module order number '{moduleInfo.Model.OrderNumber}' does not match the device order number: '{deviceAttributes["OrderNumber"]}'.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Handles the safety parameters by displaying the safety module data for the provided module.
        /// </summary>
        /// <param name="module">The GSD device item representing the module.</param>
        /// <param name="parameterSelections">A list of parameter selections for the module.</param>
        /// <returns><c>true</c> if the safety parameters were handled successfully; otherwise, <c>false</c>.</returns>
        private bool HandleSafetyParameters(GsdDeviceItem module, List<string> parameterSelections)

        {
            var safetyHandler = new SafetyParameterHandler();
            if (!safetyHandler.DisplaySafetyModuleData(module, parameterSelections))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Handles the regular parameters by retrieving and logging the module data for the provided module item.
        /// </summary>
        /// <param name="moduleItem">The item representing the module, either a regular module or a DAP.</param>
        /// <param name="module">The GSD device item representing the module or DAP.</param>
        /// <param name="parameterSelections">A list of parameter selections to be retrieved for the module or DAP.</param>
        /// <returns><c>true</c> if the regular parameters were handled successfully; otherwise, <c>false</c>.</returns>
        private bool HandleRegularParameters(IDeviceItem moduleItem, GsdDeviceItem module, List<string> parameterSelections)
        {
            var parameterHandler = new ParameterHandler(moduleItem);
            var moduleData = parameterHandler.GetModuleData(module, parameterSelections);

            if (moduleData is null)
            {
                Log.Error("Parameter reading failed due to invalid parameters. Aborting operation.");
                return false;
            }

            foreach (var parsedValue in moduleData)
            {
                Log.Information($"Parameter: {parsedValue.Parameter}, Value: {parsedValue.Value}");
            }

            return true;
        }

        /// <summary>
        /// Sets the safety parameters for the provided module.
        /// </summary>
        /// <param name="module">The GSD device item representing the module.</param>
        /// <param name="parametersToSet">A dictionary of parameters to be set for the safety module.</param>
        /// <returns><c>true</c> if the safety parameters were set successfully; otherwise, <c>false</c>.</returns>
        private bool SetSafetyParameters(GsdDeviceItem module, Dictionary<string, object> parametersToSet)

        {
            var safetyHandler = new SafetyParameterHandler();
            if (!safetyHandler.SetSafetyModuleData(module, parametersToSet))
            {
                Log.Error("Setting safety parameters failed.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Sets the regular parameters for the provided module item.
        /// </summary>
        /// <param name="moduleItem">The item representing the module, either a regular module or a DAP.</param>
        /// <param name="module">The GSD device item representing the module.</param>
        /// <param name="parametersToSet">A dictionary of parameters to be set for the module.</param>
        /// <returns><c>true</c> if the regular parameters were set successfully; otherwise, <c>false</c>.</returns>
        private bool SetRegularParameters(IDeviceItem moduleItem, GsdDeviceItem module, Dictionary<string, object> parametersToSet)
        {
            var parameterHandler = new ParameterHandler(moduleItem);
            return parameterHandler.SetModuleData(module, parametersToSet);
        }

        /// <summary>
        /// Initializes the RPCController for PLC communication.
        /// </summary>
        /// <param name="cpuIP">The IP address of the CPU.</param>
        /// <returns>An initialized instance of <see cref="RPCController"/> or null if required methods are missing.</returns>
        private RPCController InitializeRpcController(string cpuIP)
        {
            try
            {
                var rpcController = RPCController.InitializeAsync(cpuIP, "Everybody", "")
                    .GetAwaiter().GetResult();
                Log.Information("RPCController initialized successfully.");

                // Validate the required methods within the RPCController instance
                if (!rpcController.HasRequiredMethods(new[] { "Plc.ReadOperatingMode", "Plc.RequestChangeOperatingMode" }))
                {
                    Log.Warning("Required methods for RPCController are missing. Falling back to standard use case.");
                    return null; // Perform fallback if required methods are not available
                }

                return rpcController;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initialize RPCController: {ex.Message}");
                return null;
            }
        }
        #endregion
    }
}
