using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentResults;
using Newtonsoft.Json;
using Serilog;
using PLC.Commissioning.Lib.Siemens.PLCProject;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;
using PLC.Commissioning.Lib.Siemens.PLCProject.Software.PLC;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Handlers;
using PLC.Commissioning.Lib.Siemens.PLCProject.UI;
using PLC.Commissioning.Lib.Abstractions;
using PLC.Commissioning.Lib.Abstractions.Enums;
using PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Abstractions;
using Siemens.Engineering;
using Siemens.Engineering.Download;
using Siemens.Engineering.Online;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Abstractions;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Models;
using PLC.Commissioning.Lib.Siemens.Webserver.Controllers;
using Siemens.Engineering.SW;
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
        private readonly IFileSystem _fileSystem = new FileSystemWrapper();
        protected virtual IFileSystem FileSystem => _fileSystem;

        /// <summary>
        /// Service for managing the Siemens Manager instance.
        /// </summary>
        private ISiemensManagerService _manager;

        /// <summary>
        /// Represents the TIA Portal instance.
        /// </summary>
        private TiaPortal _tiaPortalInstance;

        /// <summary>
        /// Handles project-related operations within the TIA Portal.
        /// </summary>
        private IProjectHandlerService _projectHandler;

        /// <summary>
        /// Manages hardware configurations in the project.
        /// </summary>
        private IHardwareHandler _hardwareHandler;

        /// <summary>
        /// Manages IO systems within the project.
        /// </summary>
        private IIOSystemHandler _ioSystemHandler;
        
        /// <summary>
        /// Represents the CPU device item in the hardware configuration.
        /// </summary>
        private DeviceItem _cpu;
        
        /// <summary>
        /// Represents the plcSoftware for working with the software configuration.
        /// </summary>
        protected PlcSoftware _plcSoftware;

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
        
        private bool _onlineInitialized = false;
        #endregion

        #region Lazy Handler Getters

        /// <summary>
        /// Gets a valid IOSystemHandler. Uses lazy initialization.
        /// </summary>
        protected IIOSystemHandler IoSystemHandler
        {
            get
            {
                if (_ioSystemHandler == null)
                {
                    if (_projectHandler == null)
                    {
                        Log.Error("Project handler is not initialized. Cannot create IOSystemHandler.");
                        return null;
                    }
                    _ioSystemHandler = new IOSystemHandler(_projectHandler);
                    Log.Debug("IOSystemHandler created.");
                }
                return _ioSystemHandler;
            }
        }

        /// <summary>
        /// Gets a valid HardwareHandler. If it is null or disposed, it is reinitialized.
        /// </summary>
        protected IHardwareHandler HardwareHandler
        {
            get
            {
                if (_hardwareHandler == null)
                {
                    _hardwareHandler = new HardwareHandler(_projectHandler);
                    Log.Debug("HardwareHandler created.");
                }
                return _hardwareHandler;
            }
        }

        /// <summary>
        /// Gets an IOTagsHandler instance based on the current PlcSoftware.
        /// </summary>
        private IOTagsHandler IOTagsHandler => new IOTagsHandler(_plcSoftware);

        #endregion

        /// <inheritdoc />
        public Result Configure(string jsonFilePath)
        {
            try
            {
                var jsonContent = FileSystem.ReadAllText(jsonFilePath);
                var config = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);

                _projectPath = config["projectPath"].ToString();
                _networkCard = config["networkCard"].ToString();

                Log.Debug("Configuration Loaded:");
                Log.Debug($"Project Path: {_projectPath}");
                Log.Debug($"Network Card: {_networkCard}");

                return Result.Ok();
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error during configuration: {ex.Message}";
                Log.Error(errorMessage);

                return Result.Fail(new Error(errorMessage)
                {
                    Metadata = { ["ErrorCode"] = OperationErrorCode.ConfigurationFailed }
                });
            }
        }

        /// <inheritdoc />
        public Result Initialize(bool safety = false)
        {
            try
            {
                Log.Information("Offline Initialization started with safety mode: {SafetyMode}", safety);

                // Set safety mode flag
                _safety = safety;

                // Step 1: Start the TIA Portal manager
                Log.Debug("Starting TIA Portal Manager...");
                _manager = new SiemensManagerService(_safety);
                _manager.StartTIA();
                _tiaPortalInstance = _manager.TiaPortal;

                // Step 2: Initialize the ProjectHandler
                Log.Debug("Initializing ProjectHandlerService...");
                _projectHandler = new ProjectHandlerService(_tiaPortalInstance, _fileSystem);

                // Step 3: Verify and handle the project file
                if (string.IsNullOrEmpty(_projectPath) || !File.Exists(_projectPath))
                {
                    var errorMessage = $"Project file not found at path: {_projectPath}";
                    Log.Error(errorMessage);
                    return Result.Fail(new Error(errorMessage)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.InitializationFailed }
                    });
                }

                if (!_projectHandler.HandleProject(_projectPath))
                {
                    var errorMessage = $"Failed to handle project at path: {_projectPath}";
                    Log.Error(errorMessage);
                    return Result.Fail(new Error(errorMessage)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.InitializationFailed }
                    });
                }

                // Step 4: Initialize safety-related components (if applicable)
                if (_safety)
                {
                    Log.Warning("Safety mode enabled. Initializing UIDownloadHandler...");
                    _uiDownloadHandler = new UIDownloadHandler();
                }

                // Step 5: Setup HardwareHandler and find CPU
                Log.Debug("Setting up HardwareHandler...");
                (_cpu, _plcSoftware) = HardwareHandler.FindCPU();

                // Enumerate devices in the project
                HardwareHandler.EnumerateProjectDevices();

                // Step 6: Setup the main controller in offline mode
                Log.Debug("Configuring main controller in offline mode...");
                var projectCompiler = new CompilerService();
                _controller = new Controller(
                    projectCompiler,
                    null, // No OnlineProviderService for offline mode
                    null, // No NetworkConfigurationService for offline mode
                    null // No PLCOperationsService for offline mode
                );

                Log.Information("Offline initialization completed successfully.");
                return Result.Ok();
            }
            catch (EngineeringSecurityException ex)
            {
                var errorMessage =
                    $"EngineeringSecurityException: Ensure the user '{Environment.UserName}' is a member of the Siemens TIA Openness group.";
                Log.Error(errorMessage);
                return Result.Fail(new Error(errorMessage)
                {
                    Metadata = { ["ErrorCode"] = OperationErrorCode.InitializationFailed }
                });
            }
            catch (Exception ex)
            {
                Log.Error("Offline initialization failed:\n" +
                          "Exception Type: {ExceptionType}\n" +
                          "Message: {ErrorMessage}\n" +
                          "Stack Trace:\n{StackTrace}", 
                    ex.GetType().Name, 
                    ex.Message, 
                    ex.StackTrace);
                return Result.Fail(new Error("Offline initialization failed.")
                {
                    Metadata = { ["ErrorCode"] = OperationErrorCode.InitializationFailed }
                });
            }
        }
        
        /// <inheritdoc />
        public Result<Dictionary<string, object>> ImportDevices(string filePath, List<string> gsdmlFiles)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    var errorMessage = "The device configuration file path is null or empty.";
                    Log.Error(errorMessage);
                    var error = new Error(errorMessage)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.ImportFailed }
                    };
                    return Result.Fail<Dictionary<string, object>>(error);
                }

                if (!File.Exists(filePath))
                {
                    var errorMessage = $"The device configuration file '{filePath}' does not exist.";
                    Log.Error(errorMessage);
                    var error = new Error(errorMessage)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.ImportFailed }
                    };
                    return Result.Fail<Dictionary<string, object>>(error);
                }

                if (_projectHandler == null)
                {
                    var errorMessage = "Device import failed: project is not Initialized.";
                    Log.Error(errorMessage);
                    var error = new Error(errorMessage)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.ImportFailed }
                    };
                    return Result.Fail<Dictionary<string, object>>(error);
                }

                if (!_projectHandler.ImportAmlFile(filePath))
                {
                    var errorMessage = "Failed to import AML file.";
                    Log.Error(errorMessage);
                    var error = new Error(errorMessage)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.ImportFailed }
                    };
                    return Result.Fail<Dictionary<string, object>>(error);
                }

                // Enumerate devices
                HardwareHandler.EnumerateProjectDevices();

                var ioSystemHandler = IoSystemHandler;
                if (ioSystemHandler == null)
                {
                    var errorMessage = "Failed to initialize IOSystemHandler.";
                    Log.Error(errorMessage);
                    var error = new Error(errorMessage)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.ImportFailed }
                    };
                    return Result.Fail<Dictionary<string, object>>(error);
                }

                var (subnet, ioSystem) = ioSystemHandler.FindSubnetAndIoSystem();
                var devices = HardwareHandler.GetDevices();

                // Check PLC software
                if (_plcSoftware == null)
                {
                    var errorMessage = "PLC software is not initialized. Cannot create tag tables.";
                    Log.Error(errorMessage);
                    var error = new Error(errorMessage)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.ImportFailed }
                    };
                    return Result.Fail<Dictionary<string, object>>(error);
                }

                // Initialize IOTagsHandler
                IOTagsHandler ioTagsHandler = IOTagsHandler;

                // Build a map of GSDML models keyed by device model (TypeName)
                var gsdModelMap = BuildGsdModelMap(gsdmlFiles);
                var deviceDictionary = new Dictionary<string, object>();

                foreach (var device in devices)
                {
                    // Use TIA device’s TypeName for matching (e.g. "BCL248i")
                    string typeName = device.DeviceItems[1].GetAttribute("TypeName").ToString();
                    // Use TIA device’s unique Name for dictionary indexing (e.g. "BCL248i" or "BCL248i_1")
                    string deviceName = device.DeviceItems[1].Name;

                    if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(deviceName))
                    {
                        Log.Warning("Device has an invalid TypeName or Name. Skipping device.");
                        continue;
                    }

                    if (!gsdModelMap.TryGetValue(typeName, out var importedDeviceGsdmlModel))
                    {
                        Log.Warning("No GSD model found for device type {TypeName}", typeName);
                        continue;
                    }

                    // Create the ImportedDevice using the pre-built GSDML model.
                    var importedDevice = new ImportedDevice(deviceName, device, importedDeviceGsdmlModel);

                    // Connect the device to the IO system
                    ioSystemHandler.ConnectDeviceToIoSystem(device, subnet, ioSystem);
                    Log.Information("Device '{DeviceName}' (Type: {TypeName}) connected to IO system.", deviceName, typeName);

                    // Enumerate hardware modules.
                    List<IOModuleInfoModel> modules = HardwareHandler.EnumerateDeviceModules(device);
                    foreach (var module in modules)
                    {
                        importedDevice.AddModule(module);
                    }

                    // Create tag tables in TIA Portal
                    ioTagsHandler.CreateTagTables(importedDevice);

                    // Add the device to the dictionary
                    deviceDictionary[deviceName] = importedDevice;
                }

                Log.Information("Device import was successful.");
                return Result.Ok(deviceDictionary);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Device import failed: {ex.Message}";
                Log.Error(errorMessage);
                var error = new Error(errorMessage)
                {
                    Metadata = { ["ErrorCode"] = OperationErrorCode.ImportFailed }
                };
                return Result.Fail<Dictionary<string, object>>(error);
            }
        }
        
        /// <inheritdoc />
        public Result ConfigureDevice(object device, Dictionary<string, object> parametersToConfigure)
        {
            try
            {
                if (device == null)
                {
                    var errorMessage = "Device cannot be null.";
                    Log.Error(errorMessage);
                    var error = new Error(errorMessage)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.ConfigurationFailed }
                    };
                    return Result.Fail(error);
                }

                if (parametersToConfigure == null || parametersToConfigure.Count == 0)
                {
                    var errorMessage = "Parameters to configure cannot be null or empty.";
                    Log.Error(errorMessage);
                    var error = new Error(errorMessage)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.ConfigurationFailed }
                    };
                    return Result.Fail(error);
                }

                // Check that device is an ImportedDevice
                if (!(device is ImportedDevice importedDevice))
                {
                    var errorMessage = "Invalid device type. Expected an ImportedDevice.";
                    Log.Error(errorMessage);
                    var error = new Error(errorMessage)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.ConfigurationFailed }
                    };
                    return Result.Fail(error);
                }

                // Check that the ImportedDevice actually has a valid Device
                Device specificDevice = importedDevice.Device;
                if (specificDevice == null)
                {
                    var errorMessage = "The provided object could not be cast to a Device. Ensure the correct type is used.";
                    Log.Error(errorMessage);
                    var error = new Error(errorMessage)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.ConfigurationFailed }
                    };
                    return Result.Fail(error);
                }

                // Attempt to initialize IoSystemHandler
                var ioSystemHandler = IoSystemHandler;
                if (ioSystemHandler == null)
                {
                    var errorMessage = "Failed to initialize IOSystemHandler.";
                    Log.Error(errorMessage);
                    var error = new Error(errorMessage)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.ConfigurationFailed }
                    };
                    return Result.Fail(error);
                }

                // If "ipAddress" is specified, set it
                if (parametersToConfigure.ContainsKey("ipAddress"))
                {
                    string ipAddress = parametersToConfigure["ipAddress"]?.ToString();
                    if (!string.IsNullOrEmpty(ipAddress))
                    {
                        if (!ioSystemHandler.SetDeviceIPAddress(specificDevice, ipAddress))
                        {
                            var errorMessage = $"Failed to set IP address for device {specificDevice.DeviceItems[1].Name}.";
                            Log.Error(errorMessage);
                            var error = new Error(errorMessage)
                            {
                                Metadata = { ["ErrorCode"] = OperationErrorCode.ConfigurationFailed }
                            };
                            return Result.Fail(error);
                        }
                    }
                }

                // If "profinetName" is specified, set it
                if (parametersToConfigure.ContainsKey("profinetName"))
                {
                    string profinetName = parametersToConfigure["profinetName"]?.ToString();
                    if (!string.IsNullOrEmpty(profinetName))
                    {
                        if (!ioSystemHandler.SetProfinetName(specificDevice, profinetName))
                        {
                            var errorMessage = $"Failed to set Profinet name for device {specificDevice.DeviceItems[1].Name}.";
                            Log.Error(errorMessage);
                            var error = new Error(errorMessage)
                            {
                                Metadata = { ["ErrorCode"] = OperationErrorCode.ConfigurationFailed }
                            };
                            return Result.Fail(error);
                        }
                    }
                }

                // If we get this far, configuration is successful
                return Result.Ok();
            }
            catch (Exception ex)
            {
                var errorMessage = $"Device configuration failed: {ex.Message}";
                Log.Error(errorMessage);
                var error = new Error(errorMessage)
                {
                    Metadata = { ["ErrorCode"] = OperationErrorCode.ConfigurationFailed }
                };
                return Result.Fail(error);
            }
        }

        /// <inheritdoc />
        public Result<object> GetDeviceByName(string deviceName)
        {
            try
            {
                var device = HardwareHandler.GetDeviceByName(deviceName);
                if (device == null)
                {
                    var errorMessage = $"Device named '{deviceName}' was not found.";
                    Log.Warning(errorMessage);
                    var error = new Error(errorMessage)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.GetParametersFailed }
                    };
                    return Result.Fail<object>(error);
                }

                Log.Information($"Device named '{deviceName}' found successfully.");
                return Result.Ok<object>(device);
            }
            catch (Exception ex)
            {
                var errorMessage = $"An error occurred while retrieving the device named '{deviceName}': {ex.Message}";
                Log.Error(errorMessage);
                var error = new Error(errorMessage)
                {
                    Metadata = { ["ErrorCode"] = OperationErrorCode.GetParametersFailed }
                };
                return Result.Fail<object>(error);
            }
        }
        
        /// <inheritdoc/>
        public Result<Dictionary<string, object>> GetDeviceParameters(
            object device,
            string moduleName,
            List<string> parameterSelections = null,
            bool safety = false)
        {
            try
            {
                if (device == null)
                {
                    const string errorMsg = "Device is null.";
                    Log.Error(errorMsg);
                    var error = new Error(errorMsg)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.GetParametersFailed }
                    };
                    return Result.Fail(error);
                }

                var ioSystemHandler = IoSystemHandler;
                if (ioSystemHandler == null)
                {
                    const string errorMsg = "Failed to initialize IOSystemHandler.";
                    Log.Error(errorMsg);
                    var error = new Error(errorMsg)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.GetParametersFailed }
                    };
                    return Result.Fail(error);
                }

                // Ensure the object is an ImportedDevice
                if (!(device is ImportedDevice importedDevice))
                {
                    const string errorMsg = "Invalid device type. Expected an ImportedDevice.";
                    Log.Error(errorMsg);
                    var error = new Error(errorMsg)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.GetParametersFailed }
                    };
                    return Result.Fail(error);
                }

                Device specificDevice = importedDevice.Device;

                // *** Use the pre-built GSDML model from the imported device ***
                var gsdModel = importedDevice.DeviceGsdmlModel;
                if (gsdModel == null)
                {
                    var errorMsg = $"DeviceGsdmlModel is not initialized for device '{importedDevice.DeviceName}'.";
                    Log.Error(errorMsg);
                    var error = new Error(errorMsg)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.GetParametersFailed }
                    };
                    return Result.Fail(error);
                }

                // Retrieve device attributes from the IO system
                var deviceAttributes = ioSystemHandler.GetDeviceIdentificationAttributes(specificDevice);
                if (deviceAttributes == null)
                {
                    const string errorMsg = "Failed to retrieve device identification attributes.";
                    Log.Error(errorMsg);
                    var error = new Error(errorMsg)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.GetParametersFailed }
                    };
                    return Result.Fail(error);
                }

                // Validate the device info
                if (!IsDeviceInfoMatching(gsdModel.ModuleInfo, deviceAttributes))
                {
                    const string errorMsg = "Device information does not match the expected GSD data.";
                    Log.Error(errorMsg);
                    var error = new Error(errorMsg)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.GetParametersFailed }
                    };
                    return Result.Fail(error);
                }

                // DAP special case
                if (moduleName.Equals("DAP", StringComparison.OrdinalIgnoreCase))
                {
                    if (safety)
                    {
                        const string errorMsg = "DAP cannot have safety parameters.";
                        Log.Error(errorMsg);
                        var error = new Error(errorMsg)
                        {
                            Metadata = { ["ErrorCode"] = OperationErrorCode.GetParametersFailed }
                        };
                        return Result.Fail(error);
                    }

                    var dapList = gsdModel.Dap;
                    var (dapItem, hasChangeableParameters) = dapList.GetDeviceAccessPointItemByID(moduleName);

                    if (dapItem == null)
                    {
                        var errorMsg = $"DAP '{moduleName}' not found in GSD file.";
                        Log.Error(errorMsg);
                        var error = new Error(errorMsg)
                        {
                            Metadata = { ["ErrorCode"] = OperationErrorCode.GetParametersFailed }
                        };
                        return Result.Fail(error);
                    }

                    if (!hasChangeableParameters)
                    {
                        var errorMsg = $"DAP '{moduleName}' exists but has no changeable parameters.";
                        Log.Error(errorMsg);
                        var error = new Error(errorMsg)
                        {
                            Metadata = { ["ErrorCode"] = OperationErrorCode.GetParametersFailed }
                        };
                        return Result.Fail(error);
                    }

                    var module = HardwareHandler.GetDeviceDAP(specificDevice);
                    if (module == null)
                    {
                        const string errorMsg = "Failed to retrieve the DAP from the device.";
                        Log.Error(errorMsg);
                        var error = new Error(errorMsg)
                        {
                            Metadata = { ["ErrorCode"] = OperationErrorCode.GetParametersFailed }
                        };
                        return Result.Fail(error);
                    }

                    // Attempt to handle "regular" parameters for DAP
                    var parameterHandler = new ParameterHandler(dapItem);
                    var paramDict = parameterHandler.HandleRegularParameters(module, parameterSelections);
                    if (paramDict == null)
                    if (paramDict == null)
                    {
                        return Result.Fail<Dictionary<string, object>>(
                            new Error("Failed to handle DAP parameters.")
                            {
                                Metadata = { ["ErrorCode"] = OperationErrorCode.GetParametersFailed }
                            }
                        );
                    }
                    return Result.Ok(paramDict);
                }
                else
                {
                    // For non-DAP modules, use the ModuleList from the pre-built model
                    var moduleList = gsdModel.ModuleList;
                    var (moduleItem, hasChangeableParameters) = moduleList.GetModuleItemByName(moduleName);

                    if (moduleItem == null)
                    {
                        var errorMsg = $"Module '{moduleName}' not found in GSD file.";
                        Log.Error(errorMsg);
                        var error = new Error(errorMsg)
                        {
                            Metadata = { ["ErrorCode"] = OperationErrorCode.GetParametersFailed }
                        };
                        return Result.Fail(error);
                    }

                    if (!hasChangeableParameters)
                    {
                        var errorMsg = $"Module '{moduleName}' exists but has no changeable parameters.";
                        Log.Error(errorMsg);
                        var error = new Error(errorMsg)
                        {
                            Metadata = { ["ErrorCode"] = OperationErrorCode.GetParametersFailed }
                        };
                        return Result.Fail(error);
                    }

                    var module = HardwareHandler.GetDeviceModuleByName(specificDevice, moduleName);
                    if (module == null)
                    {
                        var errorMsg = $"Module '{moduleName}' not found in the device.";
                        Log.Error(errorMsg);
                        var error = new Error(errorMsg)
                        {
                            Metadata = { ["ErrorCode"] = OperationErrorCode.GetParametersFailed }
                        };
                        return Result.Fail(error);
                    }
                    
                    Dictionary<string, object> paramDict;
                    // Handle safety or regular parameters
                    if (safety)
                    {
                        var paramHandler = new SafetyParameterHandler();
                        paramDict = paramHandler.HandleSafetyParameters(module, parameterSelections);
                    }
                    else
                    {
                        var paramHandler = new ParameterHandler(moduleItem);
                        paramDict = paramHandler.HandleRegularParameters(module, parameterSelections);
                    }

                    // If the returned dictionary is null, it means retrieval failed.
                    if (paramDict == null)
                    {
                        return Result.Fail<Dictionary<string, object>>(
                            new Error($"Failed to handle parameters for module '{moduleName}'.")
                            {
                                Metadata = { ["ErrorCode"] = OperationErrorCode.GetParametersFailed }
                            });
                    }
                    return Result.Ok(paramDict);
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"Getting device parameters failed: {ex.Message}";
                Log.Error(errorMsg);
                var error = new Error(errorMsg)
                {
                    Metadata = { ["ErrorCode"] = OperationErrorCode.GetParametersFailed }
                };
                return Result.Fail(error);
            }
        }
        
        /// <inheritdoc />
        public Result SetDeviceParameters(
            object device, 
            string moduleName, 
            Dictionary<string, object> parametersToSet, 
            bool safety = false)
        {
            try
            {
                if (device == null)
                {
                    const string errorMsg = "Device is null.";
                    Log.Error(errorMsg);
                    var error = new Error(errorMsg)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.SetParametersFailed }
                    };
                    return Result.Fail(error);
                }

                var ioSystemHandler = IoSystemHandler;
                if (ioSystemHandler == null)
                {
                    const string errorMsg = "Failed to initialize IOSystemHandler.";
                    Log.Error(errorMsg);
                    var error = new Error(errorMsg)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.SetParametersFailed }
                    };
                    return Result.Fail(error);
                }

                if (!(device is ImportedDevice importedDevice))
                {
                    const string errorMsg = "Invalid device type. Expected an ImportedDevice.";
                    Log.Error(errorMsg);
                    var error = new Error(errorMsg)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.SetParametersFailed }
                    };
                    return Result.Fail(error);
                }

                Device specificDevice = importedDevice.Device;
                var gsdModel = importedDevice.DeviceGsdmlModel;
                if (gsdModel == null)
                {
                    var errorMsg = $"DeviceGsdmlModel is not initialized for device '{importedDevice.DeviceName}'.";
                    Log.Error(errorMsg);
                    var error = new Error(errorMsg)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.SetParametersFailed }
                    };
                    return Result.Fail(error);
                }

                // Retrieve device attributes
                var deviceAttributes = ioSystemHandler.GetDeviceIdentificationAttributes(specificDevice);
                if (deviceAttributes == null)
                {
                    const string errorMsg = "Failed to retrieve device identification attributes.";
                    Log.Error(errorMsg);
                    var error = new Error(errorMsg)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.SetParametersFailed }
                    };
                    return Result.Fail(error);
                }

                // Validate the device info
                if (!IsDeviceInfoMatching(gsdModel.ModuleInfo, deviceAttributes))
                {
                    const string errorMsg = "Device info does not match GSD data.";
                    Log.Error(errorMsg);
                    var error = new Error(errorMsg)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.SetParametersFailed }
                    };
                    return Result.Fail(error);
                }

                // Special case: DAP
                if (moduleName.Equals("DAP", StringComparison.OrdinalIgnoreCase))
                {
                    if (safety)
                    {
                        const string errorMsg = "DAP cannot have safety parameters.";
                        Log.Error(errorMsg);
                        var error = new Error(errorMsg)
                        {
                            Metadata = { ["ErrorCode"] = OperationErrorCode.SetParametersFailed }
                        };
                        return Result.Fail(error);
                    }

                    var dapList = gsdModel.Dap;
                    var (dapItem, hasChangeableParameters) = dapList.GetDeviceAccessPointItemByID(moduleName);
                    if (dapItem == null)
                    {
                        var errorMsg = $"DAP '{moduleName}' not found in GSD file.";
                        Log.Error(errorMsg);
                        var error = new Error(errorMsg)
                        {
                            Metadata = { ["ErrorCode"] = OperationErrorCode.SetParametersFailed }
                        };
                        return Result.Fail(error);
                    }

                    if (!hasChangeableParameters)
                    {
                        var errorMsg = $"DAP '{moduleName}' exists but has no changeable parameters.";
                        Log.Error(errorMsg);
                        var error = new Error(errorMsg)
                        {
                            Metadata = { ["ErrorCode"] = OperationErrorCode.SetParametersFailed }
                        };
                        return Result.Fail(error);
                    }

                    var dapModule = HardwareHandler.GetDeviceDAP(specificDevice);
                    if (dapModule == null)
                    {
                        const string errorMsg = "Failed to retrieve the DAP module from the device.";
                        Log.Error(errorMsg);
                        var error = new Error(errorMsg)
                        {
                            Metadata = { ["ErrorCode"] = OperationErrorCode.SetParametersFailed }
                        };
                        return Result.Fail(error);
                    }

                    bool handled = SetRegularParameters(dapItem, dapModule, parametersToSet);
                    return handled
                        ? Result.Ok()
                        : Result.Fail(new Error($"Failed to set parameters for DAP '{moduleName}'.")
                            {
                                Metadata = { ["ErrorCode"] = OperationErrorCode.SetParametersFailed }
                            });
                }
                else
                {
                    // Non-DAP modules
                    var moduleList = gsdModel.ModuleList;
                    var (moduleItem, hasChangeableParameters) = moduleList.GetModuleItemByName(moduleName);
                    if (moduleItem == null)
                    {
                        var errorMsg = $"Module '{moduleName}' not found in GSD file.";
                        Log.Error(errorMsg);
                        var error = new Error(errorMsg)
                        {
                            Metadata = { ["ErrorCode"] = OperationErrorCode.SetParametersFailed }
                        };
                        return Result.Fail(error);
                    }
                    if (!hasChangeableParameters)
                    {
                        var errorMsg = $"Module '{moduleName}' exists but has no changeable parameters.";
                        Log.Error(errorMsg);
                        var error = new Error(errorMsg)
                        {
                            Metadata = { ["ErrorCode"] = OperationErrorCode.SetParametersFailed }
                        };
                        return Result.Fail(error);
                    }

                    var module = HardwareHandler.GetDeviceModuleByName(specificDevice, moduleName);
                    if (module == null)
                    {
                        var errorMsg = $"Module '{moduleName}' not found in the device.";
                        Log.Error(errorMsg);
                        var error = new Error(errorMsg)
                        {
                            Metadata = { ["ErrorCode"] = OperationErrorCode.SetParametersFailed }
                        };
                        return Result.Fail(error);
                    }

                    Log.Information($"Setting device parameters for module '{moduleName}'...");
                    bool handled = safety
                        ? SetSafetyParameters(module, parametersToSet)
                        : SetRegularParameters(moduleItem, module, parametersToSet);

                    return handled
                        ? Result.Ok()
                        : Result.Fail(new Error($"Failed to set parameters for module '{moduleName}'.")
                            {
                                Metadata = { ["ErrorCode"] = OperationErrorCode.SetParametersFailed }
                            });
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"Setting device parameters failed: {ex.Message}";
                Log.Error(errorMsg);
                var error = new Error(errorMsg)
                {
                    Metadata = { ["ErrorCode"] = OperationErrorCode.SetParametersFailed }
                };
                return Result.Fail(error);
            }
        }
        
        /// <inheritdoc />
        public Result Start()
        {
            // Attempt to initialize online
            var initOnlineResult = InitializeOnline();
            if (initOnlineResult.IsFailed)
            {
                // If we can't go online, we fail Start too
                var errorMessage = "Online initialization failed during Start().";
                Log.Error(errorMessage);
                var error = new Error(errorMessage)
                {
                    Metadata = { ["ErrorCode"] = OperationErrorCode.StartFailed }
                };
                error.Reasons.AddRange(initOnlineResult.Errors);
                return Result.Fail(error);
            }

            try
            {
                if (_rpcController != null)
                {
                    // Attempt to start the PLC using async method synchronously
                    _rpcController.PlcController.ChangeOperatingModeAsync(ApiPlcOperatingMode.Run)
                        .GetAwaiter().GetResult();

                    Log.Information("PLC started successfully using ChangeOperatingModeAsync.");
                    return Result.Ok();
                }
                else
                {
                    const string warningMsg = "RPCController is not initialized. Falling back to the original Start method.";
                    Log.Warning(warningMsg);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Async Start operation failed: {ex.Message}";
                Log.Error(errorMessage);
                Log.Warning("Falling back to the original Start method.");

                // We won't return yet; we attempt the fallback below.
            }

            // Fallback to the original Start method
            try
            {
                if (_safety)
                {
                    _controller.GoOnline(); // Possibly handle exceptions here too
                    bool started = _uiDownloadHandler.StartPLC();
                    if (started)
                    {
                        return Result.Ok();
                    }
                    else
                    {
                        var errorMessage =
                            "Failed to start PLC using UIDownloadHandler in safety mode.";
                        Log.Error(errorMessage);

                        var error = new Error(errorMessage)
                        {
                            Metadata = { ["ErrorCode"] = OperationErrorCode.StartFailed }
                        };
                        return Result.Fail(error);
                    }
                }
                else
                {
                    bool started = _controller.Start();
                    if (started)
                    {
                        return Result.Ok();
                    }
                    else
                    {
                        var errorMessage =
                            "Failed to start PLC using the standard method.";
                        Log.Error(errorMessage);

                        var error = new Error(errorMessage)
                        {
                            Metadata = { ["ErrorCode"] = OperationErrorCode.StartFailed }
                        };
                        return Result.Fail(error);
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Fallback Start operation failed: {ex.Message}";
                Log.Error(errorMessage);
                var error = new Error(errorMessage)
                {
                    Metadata = { ["ErrorCode"] = OperationErrorCode.StartFailed }
                };
                return Result.Fail(error);
            }
        }
        
        /// <inheritdoc />
        public Result Stop()
        {
            // Attempt to initialize online
            var initOnlineResult = InitializeOnline();
            if (initOnlineResult.IsFailed)
            {
                var errorMessage = "Online initialization failed during Stop().";
                Log.Error(errorMessage);
                var error = new Error(errorMessage)
                {
                    Metadata = { ["ErrorCode"] = OperationErrorCode.StopFailed }
                };
                error.Reasons.AddRange(initOnlineResult.Errors);
                return Result.Fail(error);
            }

            try
            {
                if (_rpcController != null)
                {
                    // Attempt to stop the PLC using the async method synchronously
                    _rpcController.PlcController.ChangeOperatingModeAsync(ApiPlcOperatingMode.Stop)
                        .GetAwaiter().GetResult();

                    Log.Information("PLC stopped successfully using ChangeOperatingModeAsync.");
                    return Result.Ok();
                }
                else
                {
                    const string warningMsg = "RPCController is not initialized. Falling back to the original Stop method.";
                    Log.Warning(warningMsg);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Async Stop operation failed: {ex.Message}";
                Log.Error(errorMessage);
                Log.Warning("Falling back to the original Stop method.");
            }

            // Fallback to the original Stop method
            try
            {
                if (_safety)
                {
                    _controller.GoOnline();
                    bool stopped = _uiDownloadHandler.StopPLC();
                    if (stopped)
                    {
                        return Result.Ok();
                    }
                    else
                    {
                        var errorMessage =
                            "Failed to stop PLC using UIDownloadHandler in safety mode.";
                        Log.Error(errorMessage);

                        var error = new Error(errorMessage)
                        {
                            Metadata = { ["ErrorCode"] = OperationErrorCode.StopFailed }
                        };
                        return Result.Fail(error);
                    }
                }
                else
                {
                    bool stopped = _controller.Stop();
                    if (stopped)
                    {
                        return Result.Ok();
                    }
                    else
                    {
                        var errorMessage =
                            "Failed to stop PLC using the standard method.";
                        Log.Error(errorMessage);

                        var error = new Error(errorMessage)
                        {
                            Metadata = { ["ErrorCode"] = OperationErrorCode.StopFailed }
                        };
                        return Result.Fail(error);
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Fallback Stop operation failed: {ex.Message}";
                Log.Error(errorMessage);

                var error = new Error(errorMessage)
                {
                    Metadata = { ["ErrorCode"] = OperationErrorCode.StopFailed }
                };
                return Result.Fail(error);
            }
        }


        /// <inheritdoc />
        public Result Compile()
        {
            try
            {
                bool compiled = _controller.CompileDevice(_cpu);
                if (compiled)
                {
                    return Result.Ok();
                }
                else
                {
                    const string errorMessage = "Compilation failed for unknown reasons.";
                    Log.Error(errorMessage);

                    var error = new Error(errorMessage)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.CompileFailed }
                    };
                    return Result.Fail(error);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Compilation failed: {ex.Message}";
                Log.Error(errorMessage);

                var error = new Error(errorMessage)
                {
                    Metadata = { ["ErrorCode"] = OperationErrorCode.CompileFailed }
                };
                return Result.Fail(error);
            }
        }
        
        /// <inheritdoc />
        public Result Download(object options)
        {
            var initOnlineResult = InitializeOnline();
            if (initOnlineResult.IsFailed)
            {
                var errorMessage = "Online initialization failed during Download().";
                Log.Error(errorMessage);

                var error = new Error(errorMessage)
                {
                    Metadata = { ["ErrorCode"] = OperationErrorCode.DownloadFailed }
                };
                error.Reasons.AddRange(initOnlineResult.Errors);
                return Result.Fail(error);
            }

            try
            {
                switch (options)
                {
                    case string optionString when optionString == "safety" && _safety:
                    {
                        Log.Warning("Loading or overloading fail-safe data in Openness is not permitted.");
                        Log.Warning("Proceeding to download fail-safe data using automated UI interaction...");

                        bool downloaded = _uiDownloadHandler.DownloadProcedure();
                        if (downloaded)
                        {
                            return Result.Ok();
                        }
                        else
                        {
                            const string errorMsg = "UI-based download of fail-safe data failed.";
                            Log.Error(errorMsg);

                            var error = new Error(errorMsg)
                            {
                                Metadata = { ["ErrorCode"] = OperationErrorCode.DownloadFailed }
                            };
                            return Result.Fail(error);
                        }
                    }
                    case DownloadOptions downloadOptions:
                    {
                        bool downloaded = _controller.Download(downloadOptions);
                        if (downloaded)
                        {
                            return Result.Ok();
                        }
                        else
                        {
                            const string errorMsg = "Download failed using provided download options.";
                            Log.Error(errorMsg);

                            var error = new Error(errorMsg)
                            {
                                Metadata = { ["ErrorCode"] = OperationErrorCode.DownloadFailed }
                            };
                            return Result.Fail(error);
                        }
                    }
                    default:
                    {
                        const string errorMsg = "Invalid options provided for download.";
                        Log.Error(errorMsg);

                        var error = new Error(errorMsg)
                        {
                            Metadata = { ["ErrorCode"] = OperationErrorCode.DownloadFailed }
                        };
                        return Result.Fail(error);
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Download failed: {ex.Message}";
                Log.Error(errorMessage);

                var error = new Error(errorMessage)
                {
                    Metadata = { ["ErrorCode"] = OperationErrorCode.DownloadFailed }
                };
                return Result.Fail(error);
            }
        }


        /// <inheritdoc />
        public Result AdditionalImport(Dictionary<string, object> filesToImport)
        {
            try
            {
                if (filesToImport.ContainsKey("plcTags") && 
                    !string.IsNullOrEmpty(filesToImport["plcTags"]?.ToString()))
                {
                    var tagsService = new PLCTagsService();
                    tagsService.ImportTagTable(_cpu, filesToImport["plcTags"].ToString());
                }

                return Result.Ok();
            }
            catch (Exception ex)
            {
                var errorMessage = $"Additional import failed: {ex.Message}";
                Log.Error(errorMessage);

                var error = new Error(errorMessage)
                {
                    Metadata = { ["ErrorCode"] = OperationErrorCode.ImportFailed }
                };
                return Result.Fail(error);
            }
        }
        
        /// <inheritdoc />
        public Result Export(Dictionary<string, object> filesToExport)
        {
            try
            {
                var tagsService = new PLCTagsService();
                tagsService.ExportAllTagTables(_cpu, filesToExport["plcTags"].ToString());
                return Result.Ok();
            }
            catch (Exception ex)
            {
                var errorMessage = $"Export failed: {ex.Message}";
                Log.Error(errorMessage);

                var error = new Error(errorMessage)
                {
                    Metadata = { ["ErrorCode"] = OperationErrorCode.SaveProjectFailed }
                };
                return Result.Fail(error);
            }
        }

        /// <inheritdoc />
        public Result SaveProjectAs(string projectName)
        {
            try
            {
                _projectHandler.SaveProjectAs(projectName);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                var errorMessage = $"Save project failed: {ex.Message}";
                Log.Error(errorMessage);

                var error = new Error(errorMessage)
                {
                    Metadata = { ["ErrorCode"] = OperationErrorCode.SaveProjectFailed }
                };
                return Result.Fail(error);
            }
        }

        /// <inheritdoc />
        public Result PrintGSDInformations(string gsdFilePath, string moduleName = null)
        {
            try
            {
                var gsdHandler = new GSDHandler();
                if (!gsdHandler.Initialize(gsdFilePath))
                {
                    const string errorMessage = "Failed to initialize GSDHandler with the provided file path.";
                    Log.Error(errorMessage);

                    var error = new Error(errorMessage)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.GetParametersFailed }
                    };
                    return Result.Fail(error);
                }

                var moduleInfo = new ModuleInfo(gsdHandler);
                Log.Information(moduleInfo.ToString());

                var dapList = new DeviceAccessPointList(gsdHandler);
                Log.Information(dapList.ToString());

                var moduleList = new ModuleList(gsdHandler);
                Log.Information(moduleList.ToString());

                // If a specific module name is provided, print its information
                if (!string.IsNullOrEmpty(moduleName))
                {
                    if (moduleName.Equals("DAP", StringComparison.OrdinalIgnoreCase))
                    {
                        (DeviceAccessPointItem dapItem, _) = dapList.GetDeviceAccessPointItemByID(moduleName);
                        if (dapItem != null)
                        {
                            Log.Information(dapItem.ToString());
                        }
                        else
                        {
                            Log.Warning("'{ModuleName}' not found in the GSD file.", moduleName);
                        }
                    }
                    else
                    {
                        (ModuleItem moduleItem, _) = moduleList.GetModuleItemByName(moduleName);
                        if (moduleItem != null)
                        {
                            Log.Information(moduleItem.ToString());
                        }
                        else
                        {
                            Log.Warning("Module with name '{ModuleName}' not found in the GSD file.", moduleName);
                        }
                    }
                }

                return Result.Ok();
            }
            catch (Exception ex)
            {
                var errorMessage = $"An error occurred while printing GSD information: {ex.Message}";
                Log.Error(errorMessage);

                var error = new Error(errorMessage)
                {
                    Metadata = { ["ErrorCode"] = OperationErrorCode.GetParametersFailed }
                };
                return Result.Fail(error);
            }
        }
        
        /// <inheritdoc />
        public Result DeleteDevice(object device)
        {
            try
            {
                if (device == null)
                {
                    const string errorMsg = "DeleteDevice: Device is null.";
                    Log.Error(errorMsg);
                    var error = new Error(errorMsg)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.DeleteDeviceFailed }
                    };
                    return Result.Fail(error);
                }

                if (!(device is ImportedDevice importedDevice))
                {
                    const string errorMsg = "DeleteDevice: Invalid device type. Expected an ImportedDevice.";
                    Log.Error(errorMsg);
                    var error = new Error(errorMsg)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.DeleteDeviceFailed }
                    };
                    return Result.Fail(error);
                }

                // Delete the tag tables using the IOTagsHandler.
                var tagsHandler = IOTagsHandler;
                tagsHandler.DeleteDeviceTagTables(importedDevice);

                // Delete the hardware device from the project.
                bool hardwareDeletionSuccess = HardwareHandler.DeleteDevice(importedDevice.Device);
                if (!hardwareDeletionSuccess)
                {
                    const string errorMsg = "DeleteDevice: Failed to delete hardware device from project.";
                    Log.Error(errorMsg);
                    var error = new Error(errorMsg)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.DeleteDeviceFailed }
                    };
                    return Result.Fail(error);
                }

                Log.Information("Device '{DeviceName}' and its associated tags were deleted successfully.", importedDevice.DeviceName);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                var errorMessage = $"DeleteDevice: Error deleting device: {ex.Message}";
                Log.Error(errorMessage);

                var error = new Error(errorMessage)
                {
                    Metadata = { ["ErrorCode"] = OperationErrorCode.DeleteDeviceFailed }
                };
                return Result.Fail(error);
            }
        }
        
        /// <summary>
        /// Reads and returns all PLC tag tables.
        /// </summary>
        /// <returns>A dictionary mapping group names to lists of tag table names.</returns>
        public Result<Dictionary<string, List<string>>> ReadPLCTagTables()
        {
            try
            {
                if (_plcSoftware == null)
                {
                    var errorMessage = "PLC software is not initialized. Cannot read tag tables.";
                    Log.Error(errorMessage);

                    var error = new Error(errorMessage)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.ReadTagTablesFailed }
                    };
                    return Result.Fail<Dictionary<string, List<string>>>(error);
                }

                var ioTagsHandler = IOTagsHandler;
                if (ioTagsHandler == null)
                {
                    var errorMessage = "Failed to initialize IOTagsHandler.";
                    Log.Error(errorMessage);

                    var error = new Error(errorMessage)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.ReadTagTablesFailed }
                    };
                    return Result.Fail<Dictionary<string, List<string>>>(error);
                }

                var tagTables = ioTagsHandler.ReadPLCTagTables();
                
                // Count total tag tables across all groups
                int totalTagTables = tagTables.Values.Sum(group => group.Count);
                if (totalTagTables > 0)
                {
                    Log.Information($"Successfully retrieved {totalTagTables} PLC tag tables across {tagTables.Count} groups.");
                }
                else
                {
                    Log.Warning("No PLC tag tables found.");
                }

                return Result.Ok(tagTables);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to read PLC tag tables: {ex.Message}";
                Log.Error(errorMessage);

                var error = new Error(errorMessage)
                {
                    Metadata = { ["ErrorCode"] = OperationErrorCode.ReadTagTablesFailed }
                };
                return Result.Fail<Dictionary<string, List<string>>>(error);
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="SiemensPLCController"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            Log.CloseAndFlush();
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
                    // Dispose handlers in proper order.
                    try { _projectHandler?.Dispose(); } catch (Exception ex) { Log.Warning("Error disposing ProjectHandler: {Message}", ex.Message); }
                    try { _manager?.Dispose(); } catch (Exception ex) { Log.Warning("Error disposing SiemensManagerService: {Message}", ex.Message); }
                    try { _uiDownloadHandler?.Dispose(); } catch (Exception ex) { Log.Warning("Error disposing UIDownloadHandler: {Message}", ex.Message); }
                    try { (_hardwareHandler as IDisposable)?.Dispose(); } catch (Exception ex) { Log.Warning("Error disposing HardwareHandler: {Message}", ex.Message); }
                    try { (_ioSystemHandler as IDisposable)?.Dispose(); } catch (Exception ex) { Log.Warning("Error disposing IOSystemHandler: {Message}", ex.Message); }
                    try { _tiaPortalInstance?.Dispose(); } catch (Exception ex) { Log.Warning("Error disposing TiaPortal instance: {Message}", ex.Message); }
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
        /// Initializes online mode for the Siemens PLC controller.
        /// </summary>
        /// <returns>
        /// A <see cref="Result"/> indicating success or failure.
        /// On failure, metadata "ErrorCode" is set to <see cref="OperationErrorCode.InitializationFailed"/>.
        /// </returns>
        internal Result InitializeOnline()
        {
            // If we already initialized online mode, log a warning but return success
            // (or optionally, you can treat this as a "no-op" success).
            if (_onlineInitialized)
            {
                const string warningMessage = "Online mode has already been initialized.";
                Log.Warning(warningMessage);
                // For consistency, let's return a successful result, 
                // because we are effectively already online.
                return Result.Ok();
            }

            try
            {
                Log.Information("Initializing online mode...");

                // Initialize services for online and download operations
                Log.Debug("Initializing OnlineProvider and DownloadProvider...");
                var onlineProvider = _cpu.GetService<OnlineProvider>();
                _downloadProvider = _cpu.GetService<DownloadProvider>();

                // Create helper services for controller configuration
                var onlineProviderService = new OnlineProviderService(onlineProvider);
                var networkConfigurator = new NetworkConfigurationService(onlineProvider, _downloadProvider);
                var plcOperationsService = new PLCOperationsService(_downloadProvider);

                // Inject online services into the controller
                _controller.SetOnlineServices(onlineProviderService, networkConfigurator, plcOperationsService);

                // Determine the CPU's IP address and validate network connectivity
                var ioSystemHandler = IoSystemHandler;
                string cpuIP = ioSystemHandler.GetPLCIPAddress(_cpu);
                if (!networkConfigurator.PingIpAddress(_networkCard, cpuIP))
                {
                    var errorMessage = $"Unable to ping CPU at IP address: {cpuIP}";
                    Log.Error(errorMessage);
                    var error = new Error(errorMessage)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.InitializationFailed }
                    };
                    return Result.Fail(error);
                }

                // Configure the target network
                var targetConfiguration = networkConfigurator.GetTargetConfiguration();
                _controller.SetTargetConfiguration(targetConfiguration);

                // Configure the network card
                if (string.IsNullOrEmpty(_networkCard))
                {
                    var errorMessage = "Network card configuration is missing.";
                    Log.Error(errorMessage);
                    var error = new Error(errorMessage)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.InitializationFailed }
                    };
                    return Result.Fail(error);
                }
                if (!_controller.TryConfigureNetwork(_networkCard, 1, "1 X1"))
                {
                    var errorMessage = $"Failed to configure network with card: {_networkCard}";
                    Log.Error(errorMessage);
                    var error = new Error(errorMessage)
                    {
                        Metadata = { ["ErrorCode"] = OperationErrorCode.InitializationFailed }
                    };
                    return Result.Fail(error);
                }

                // Initialize RPC Controller
                Log.Debug("Initializing RPCController...");
                _rpcController = InitializeRpcController(cpuIP);
                if (_rpcController == null)
                {
                    Log.Warning("RPCController initialization failed. Proceeding without RPC support.");
                }

                // Mark that we are now online
                _onlineInitialized = true;
                Log.Information("Online initialization completed successfully.");
                return Result.Ok();
            }
            catch (Exception ex)
            {
                Log.Error("Online initialization failed:\n" +
                          "Exception Type: {ExceptionType}\n" +
                          "Message: {ErrorMessage}\n" +
                          "Stack Trace:\n{StackTrace}",
                    ex.GetType().Name,
                    ex.Message,
                    ex.StackTrace);

                var error = new Error("Online initialization failed.")
                {
                    Metadata = { ["ErrorCode"] = OperationErrorCode.InitializationFailed }
                };
                return Result.Fail(error);
            }
        }
        
        /// <summary>
        /// Checks if the device information from the provided module matches the expected device attributes.
        /// </summary>
        /// <param name="moduleInfo">The module information to be compared.</param>
        /// <param name="deviceAttributes">A dictionary of device attributes to compare against.</param>
        /// <returns><c>true</c> if all device information matches; otherwise, <c>false</c>.</returns>
        internal bool IsDeviceInfoMatching(ModuleInfo moduleInfo, Dictionary<string, string> deviceAttributes)
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
        
        /// <summary>
        /// Builds a dictionary of pre‐initialized GSDML models keyed by device model (TypeName).
        /// Each model is built by initializing a single GSDHandler per file and then extracting the
        /// relevant data into an ImportedDeviceGSDMLModel.
        /// </summary>
        /// <param name="gsdmlFiles">List of GSDML file paths.</param>
        /// <returns>A dictionary mapping device model to the corresponding ImportedDeviceGSDMLModel.</returns>
        private Dictionary<string, ImportedDeviceGSDMLModel> BuildGsdModelMap(IEnumerable<string> gsdmlFiles)
        {
            var map = new Dictionary<string, ImportedDeviceGSDMLModel>(StringComparer.OrdinalIgnoreCase);

            foreach (var filePath in gsdmlFiles)
            {
                var gsdHandler = new GSDHandler();
                if (!gsdHandler.Initialize(filePath))
                {
                    Log.Warning("Failed to initialize GSDHandler for file {FilePath}", filePath);
                    continue;
                }

                // Use the existing ModuleInfo class to extract the device model (TypeName)
                var moduleInfo = new ModuleInfo(gsdHandler);
                string deviceModel = moduleInfo.Model.Name;
                if (string.IsNullOrEmpty(deviceModel))
                {
                    Log.Warning("No valid device model found in GSD file {FilePath}", filePath);
                    continue;
                }

                if (map.ContainsKey(deviceModel))
                {
                    Log.Warning("Duplicate device model {DeviceModel} found in file {FilePath}. Skipping duplicate.", deviceModel, filePath);
                    continue;
                }

                // Build the merged GSDML model once.
                var importedDeviceGSDMLModel = new ImportedDeviceGSDMLModel
                {
                    ModuleInfo = moduleInfo,
                    Dap = new DeviceAccessPointList(gsdHandler),
                    ModuleList = new ModuleList(gsdHandler)
                };

                map[deviceModel] = importedDeviceGSDMLModel;
            }

            return map;
        }
        #endregion
    }
}
