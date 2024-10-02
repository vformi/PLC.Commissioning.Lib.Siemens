using NUnit.Framework;
using System;
using System.IO;
using System.Collections.Generic;
using Serilog;
using Newtonsoft.Json;
using Siemens.Engineering.HW;
using System.Linq;
using System.Threading;
using Siemens.Engineering.Download;

namespace PLC.Commissioning.Lib.Siemens.Tests
{
    [TestFixture]
    public class SiemensPLCControllerTests : IDisposable
    {
        private SiemensPLCController _plc;
        private bool _disposed = false;
        private string _basePath = null;
        private string _projectRoot = null;

        [SetUp]
        public void SetUp()
        {
            // figure out paths 
            _basePath = AppDomain.CurrentDomain.BaseDirectory;
            _projectRoot = Directory.GetParent(_basePath).Parent.Parent.Parent.FullName;

            // Initialize logger (optional, could be mocked if needed)
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            // Initialize the SiemensPLCController object
            _plc = new SiemensPLCController();
        }

        [Test]
        /// Tests that the Configure method returns true when provided with a valid JSON configuration file.
        public void Configure_ShouldReturnTrue_WhenJsonIsValid()
        {
            // Arrange
            string jsonFilePath = "validConfig.json";
            var jsonContent = new Dictionary<string, object>
            {
                {"projectPath", "C:\\Projects\\MyProject" },
                {"networkCard", "eth0" },
                {"gsdFilePath", "C:\\GSD\\file.gsd" }
            };
            File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(jsonContent));

            // Act
            var result = _plc.Configure(jsonFilePath);

            // Assert
            Assert.IsTrue(result);
        }


        [Test]
        /// Tests that the Configure method returns false when the JSON configuration file is missing required keys.
        public void Configure_ShouldReturnFalse_WhenJsonIsMissingKeys()
        {
            // Arrange
            string jsonFilePath = "invalidConfig.json";
            var jsonContent = new Dictionary<string, object>
            {
                {"projectPath", "C:\\Projects\\MyProject" }
                // Missing networkCard and gsdFilePath
            };
            File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(jsonContent));

            // Act
            var result = _plc.Configure(jsonFilePath);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        /// Tests that the Configure method returns false when the provided JSON file 
        /// path is invalid or the file does not exist.
        public void Configure_ShouldReturnFalse_WhenFilePathIsInvalid()
        {
            // Arrange
            string invalidFilePath = "nonExistentConfig.json";

            // Act
            var result = _plc.Configure(invalidFilePath);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        /// Tests that the Configure method returns false when the JSON configuration 
        /// file is malformed or contains invalid JSON.
        public void Configure_ShouldReturnFalse_WhenJsonIsMalformed()
        {
            // Arrange
            string jsonFilePath = "malformedConfig.json";
            string malformedJson = "{ invalidJson: true"; // Malformed JSON
            File.WriteAllText(jsonFilePath, malformedJson);

            // Act
            var result = _plc.Configure(jsonFilePath);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        /// Tests that the Initialize method succeeds when the controller is 
        /// correctly configured with safety mode enabled.
        public void Initialize_ShouldPass_WhenConfiguredCorrectly_WithSafety()
        {
            // Arrange
            string jsonFilePath = "validConfigSafety.json";
            var jsonContent = new Dictionary<string, object>
            {
                { "projectPath", "C:\\Users\\vformane\\OneDrive - Leuze electronic GmbH + Co. KG\\Osobní\\Diplomka\\Siemens\\Blank_project_RSL400_DAP2.zap17" },
                { "networkCard", "Realtek USB GbE Family Controller" },
            };
            File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(jsonContent));
            _plc.Configure(jsonFilePath);

            // Act
            var result = _plc.Initialize(safety: true);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        /// Tests that the Initialize method succeeds when the controller is 
        /// correctly configured without safety mode.
        public void Initialize_ShouldPass_WhenConfiguredCorrectly_WithoutSafety()
        {
            // Arrange
            string jsonFilePath = "validConfigNonSafety.json";
            var jsonContent = new Dictionary<string, object>
            {
                { "projectPath", "C:\\Users\\vformane\\OneDrive - Leuze electronic GmbH + Co. KG\\Osobní\\Diplomka\\Siemens\\Blank_project_RSL400_DAP2.zap17" },
                { "networkCard", "Realtek USB GbE Family Controller" }, // Description: , Realtek USB GbE Family Controller
            };

            File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(jsonContent));
            _plc.Configure(jsonFilePath);

            // Act
            var result = _plc.Initialize(safety: false);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        /// Tests that the Initialize method fails when the project path 
        /// provided in the configuration is invalid.
        public void Initialize_ShouldFail_WhenProjectPathIsInvalid()
        {
            // Arrange
            string jsonFilePath = "invalidProjectConfig.json";
            var jsonContent = new Dictionary<string, object>
            {
                { "projectPath", "C:\\InvalidPath\\InvalidProject.zap17" },
                { "networkCard", "Realtek USB GbE Family Controller" },
            };
            File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(jsonContent));
            _plc.Configure(jsonFilePath);

            // Act
            var result = _plc.Initialize(safety: false);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        ///  Tests that the Initialize method fails when the network card information
        ///  is missing from the configuration.
        public void Initialize_ShouldFail_WhenNetworkCardIsMissing()
        {
            // Arrange
            string jsonFilePath = "missingNetworkCardConfig.json";
            var jsonContent = new Dictionary<string, object>
            {
                { "projectPath", "C:\\Users\\vformane\\OneDrive - Leuze electronic GmbH + Co. KG\\Osobn�\\Diplomka\\Siemens\\Blank_project_RSL400_DAP2.zap17" },
            };
            File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(jsonContent));
            _plc.Configure(jsonFilePath);

            // Act
            var result = _plc.Initialize(safety: false);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        /// Tests that the Initialize method fails when an incorrect 
        /// network card is specified in the configuration.
        public void Initialize_ShouldFail_ToGoOnlineWhenNetworkCardIsIncorrect()
        {
            // Arrange
            string jsonFilePath = "incorrectNetworkCardConfig.json";
            var jsonContent = new Dictionary<string, object>
            {
                { "projectPath", "C:\\Users\\vformane\\OneDrive - Leuze electronic GmbH + Co. KG\\Osobn�\\Diplomka\\Siemens\\Blank_project_RSL400_DAP2.zap17" },
                { "networkCard", "IncorrectNetworkCard" },
            };
            File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(jsonContent));
            _plc.Configure(jsonFilePath);

            // Act
            var result = _plc.Initialize(safety: false);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        /// Tests that the ImportDevice method returns null 
        /// when called before the controller is initialized.
        public void ImportDevice_ShouldReturnNull_WhenNotInitialized()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            _plc.Configure(jsonFilePath);
            string deviceConfigFilePath = Path.Combine(_projectRoot, "configuration_files", "aml", "valid.aml"); 

            // Act - Without initializing _plc or setting up any handlers
            var result = _plc.ImportDevice(deviceConfigFilePath);

            // Assert
            Assert.That(result, Is.Null, "The result should be null when the PLC is not initialized.");
        }

        [Test]
        /// Tests that the ImportDevice method returns null when an 
        /// invalid device configuration file path is provided.
        public void ImportDevice_ShouldReturnNull_WhenFilePathIsInvalid()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            _plc.Configure(jsonFilePath);
            string invalidFilePath = "nonExistentDeviceConfigFile.xml";
            _plc.Initialize(safety: false);

            // Act
            var result = _plc.ImportDevice(invalidFilePath);

            // Assert
            Assert.That(result, Is.Null, "The result should be null when an invalid file path is provided.");
        }

        [Test]
        /// Tests that the ImportDevice method successfully imports
        /// a device when provided with a valid device configuration file.
        public void ImportDevice_ShouldReturnDevice_WhenFileIsCorrect()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            _plc.Configure(jsonFilePath);

            string validFilePath = Path.Combine(_projectRoot, "configuration_files", "aml", "valid.aml");
            _plc.Initialize(safety: false);

            // Act
            var importedDevicesObj = _plc.ImportDevice(validFilePath);
            var importedDevices = importedDevicesObj as Dictionary<string, Device>;
            // Asserty
            Assert.That(importedDevices, Is.Not.Null, "The returned dictionary should not be null.");
            Assert.That(importedDevices.Count, Is.GreaterThan(0), "The returned dictionary should have at least one device.");

            foreach (var deviceObj in importedDevices.Values)
            {
                var device = deviceObj as Device;
                Assert.That(device, Is.Not.Null, "Each device in the dictionary should be of type 'Device'.");
                Assert.That(device.Name, Is.Not.Null.And.Not.Empty, "Each device should have a valid name.");
            }
        }

        [Test]
        /// Tests that the ImportDevice method successfully imports devices 
        /// from two valid device configuration files.
        public void ImportDevice_ShouldReturnDevices_WhenTwoFilesAreCorrect()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            _plc.Configure(jsonFilePath);

            string validFilePath1 = Path.Combine(_projectRoot, "configuration_files", "aml", "valid.aml");
            string validFilePath2 = Path.Combine(_projectRoot, "configuration_files", "aml", "valid2.aml");

            _plc.Initialize(safety: false);

            // Act
            var importedDevices1 = _plc.ImportDevice(validFilePath1) as Dictionary<string, Device>;
            var importedDevices2 = _plc.ImportDevice(validFilePath2) as Dictionary<string, Device>;

            // Assert
            Assert.That(importedDevices1, Is.Not.Null, "The returned dictionary for the first file should not be null.");
            Assert.That(importedDevices1.Count, Is.GreaterThan(0), "The first returned dictionary should have at least one device.");

            foreach (var device in importedDevices1.Values)
            {
                Assert.That(device, Is.Not.Null, "Each device in the first dictionary should be of type 'Device'.");
                Assert.That(device.Name, Is.Not.Null.And.Not.Empty, "Each device should have a valid name.");
            }

            Assert.That(importedDevices2, Is.Not.Null, "The returned dictionary for the second file should not be null.");
            Assert.That(importedDevices2.Count, Is.GreaterThan(0), "The second returned dictionary should have at least one device.");

            foreach (var device in importedDevices2.Values)
            {
                Assert.That(device, Is.Not.Null, "Each device in the second dictionary should be of type 'Device'.");
                Assert.That(device.Name, Is.Not.Null.And.Not.Empty, "Each device should have a valid name.");
            }
        }

        [Test]
        /// Tests that the ImportDevice method successfully imports multiple devices from a single valid 
        /// configuration file containing multiple devices.
        public void ImportDevice_ShouldReturnDevices_WhenOneFileWithMultipleDevicesIsCorrect()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            _plc.Configure(jsonFilePath);

            string validFilePath = Path.Combine(_projectRoot, "configuration_files", "aml", "multiple_devices.aml");
            _plc.Initialize(safety: false);

            // Act
            var importedDevices = _plc.ImportDevice(validFilePath) as Dictionary<string, Device>;

            // Assert
            Assert.That(importedDevices, Is.Not.Null, "The returned dictionary should not be null.");
            Assert.That(importedDevices.Count, Is.GreaterThan(0), "The returned dictionary should have at least one device.");

            foreach (var device in importedDevices.Values)
            {
                Assert.That(device, Is.Not.Null, "Each device in the dictionary should be of type 'Device'.");
                Assert.That(device.Name, Is.Not.Null.And.Not.Empty, "Each device should have a valid name.");
            }
        }

        [Test]
        /// Tests that the ImportDevice method returns null when an incorrect or invalid 
        /// device configuration file is provided.
        public void ImportDevice_ShouldReturnNull_WhenFileIsIncorrect()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            _plc.Configure(jsonFilePath);
            string invalidFilePath = Path.Combine(_projectRoot, "configuration_files", "aml", "invalid.aml");
            _plc.Initialize(safety: false);

            // Act
            var result = _plc.ImportDevice(invalidFilePath);

            // Assert
            Assert.That(result, Is.Null, "The result should be null when an incorrect file is provided.");
        }

        [Test]
        /// Tests that the ConfigureDevice method succeeds when valid parameters
        /// are provided for an imported device.
        public void ConfigureDevice_ShouldSucceed_WhenValidParametersAreProvided_WithImport()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            _plc.Configure(jsonFilePath);

            string validFilePath = Path.Combine(_projectRoot, "configuration_files", "aml", "valid.aml");
            _plc.Initialize(safety: false);
            var importedDevicesObj = _plc.ImportDevice(validFilePath);
            var importedDevices = importedDevicesObj as Dictionary<string, Device>;

            var parametersToConfigure = new Dictionary<string, object>
            {
                { "ipAddress", "192.168.60.100" },
                { "profinetName", "dut" } // Assuming you want to set the Profinet name to the device's name
            };

            // Check (Assert for the import method to be sure)  
            var firstDeviceObj = importedDevices.Values.FirstOrDefault();

            Assert.That(firstDeviceObj, Is.Not.Null, "The dictionary should contain at least one device.");

            var device = firstDeviceObj as Device;
            Assert.That(device, Is.Not.Null, "The first device in the dictionary should be of type 'Device'.");
            Assert.That(device.DeviceItems[1].Name, Is.Not.Null.And.Not.Empty, "The first device should have a valid name.");

            // Act
            bool configurationResult = _plc.ConfigureDevice(device, parametersToConfigure);

            // Assert
            Assert.That(configurationResult, Is.True, $"Configuration should pass for device '{device.DeviceItems[1].Name}'.");
        }

        [Test]
        /// Tests that the ConfigureDevice method succeeds when configuring
        /// multiple devices with valid parameters.
        public void ConfigureDevice_ShouldSucceed_WhenValidParametersAreProvidedForMultipleDevices()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);

            // Configuration for the first device
            var parametersToConfigureBCL348i = new Dictionary<string, object>
            {
                { "ipAddress", "192.168.60.100" },
                { "profinetName", "dut" }
            };
            Device deviceBCL348i = _plc.GetDeviceByName("BCL348i") as Device;

            // Act and Assert for the first device
            bool configurationResultBCL348i = _plc.ConfigureDevice(deviceBCL348i, parametersToConfigureBCL348i);
            Assert.That(configurationResultBCL348i, Is.True, $"Configuration should pass for device '{deviceBCL348i?.DeviceItems[1].Name}'.");

            // Configuration for the second device
            var parametersToConfigureBCL248i = new Dictionary<string, object>
            {
                { "ipAddress", "192.168.60.101" },
                { "profinetName", "dut248" }
            };
            Device deviceBCL248i = _plc.GetDeviceByName("BCL248i") as Device;

            // Act and Assert for the second device
            bool configurationResultBCL248i = _plc.ConfigureDevice(deviceBCL248i, parametersToConfigureBCL248i);
            Assert.That(configurationResultBCL248i, Is.True, $"Configuration should pass for device '{deviceBCL248i?.DeviceItems[1].Name}'.");
        }

        [Test]
        /// Tests that the ConfigureDevice method fails when a null device is provided.
        public void ConfigureDevice_ShouldFail_WhenDeviceIsNull()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);
            var parametersToConfigure = new Dictionary<string, object>
            {
                { "ipAddress", "192.168.60.100" },
                { "profinetName", "dut" }
            };

            // Act
            bool configurationResult = _plc.ConfigureDevice(null, parametersToConfigure);

            // Assert
            Assert.That(configurationResult, Is.False, "Configuration should fail when the device is null.");
        }

        [Test]
        /// Tests that the ConfigureDevice method fails when the 
        /// provided device is of an incorrect type.
        public void ConfigureDevice_ShouldFail_WhenDeviceIsIncorrectType()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);
            var parametersToConfigure = new Dictionary<string, object>
            {
                { "ipAddress", "192.168.60.100" },
                { "profinetName", "dut" }
            };

            // Simulate passing an object that is not of type `Device`
            object incorrectDevice = new { Name = "IncorrectDevice" };

            // Act
            bool configurationResult = _plc.ConfigureDevice(incorrectDevice, parametersToConfigure);

            // Assert
            Assert.That(configurationResult, Is.False, "Configuration should fail when the device is of incorrect type.");
        }

        [Test]
        /// Tests that the ConfigureDevice method fails when the parameters dictionary is empty
        public void ConfigureDevice_ShouldFail_WhenParametersToConfigureIsEmpty()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);
            var parametersToConfigure = new Dictionary<string, object>();
            Device device = _plc.GetDeviceByName("BCL348i") as Device;

            // Act
            bool configurationResult = _plc.ConfigureDevice(device, parametersToConfigure);

            // Assert
            Assert.That(configurationResult, Is.False, "Configuration should fail when the parameters dictionary is empty.");
        }

        [Test]
        /// Tests that the ConfigureDevice method fails when the parameters to configure are null.
        public void ConfigureDevice_ShouldFail_WhenParametersToConfigureIsNull()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);
            Device device = _plc.GetDeviceByName("BCL348i") as Device;

            // Act
            bool configurationResult = _plc.ConfigureDevice(device, null);

            // Assert
            Assert.That(configurationResult, Is.False, "Configuration should fail when parametersToConfigure is null.");
        }

        [Test]
        /// Tests that the ConfigureDevice method fails when required parameters
        /// are missing in the parameters dictionary.
        public void ConfigureDevice_ShouldFail_WhenRequiredParametersAreMissing()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);
            var parametersToConfigure = new Dictionary<string, object>
            {
                // Missing "ipAddress" and "profinetName"
            };
            Device device = _plc.GetDeviceByName("BCL348i") as Device;

            // Act
            bool configurationResult = _plc.ConfigureDevice(device, parametersToConfigure);

            // Assert
            Assert.That(configurationResult, Is.False, "Configuration should fail when required parameters are missing.");
        }

        [Test]
        /// Tests that the GetDeviceParameters method succeeds
        /// when provided with a valid module name.
        public void GetDeviceParameters_ShouldPass_WhenValidModuleNameIsProvided()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            _plc.Configure(jsonFilePath);

            string validFilePath = Path.Combine(_projectRoot, "configuration_files", "aml", "valid.aml");
            string gsdFilePath = Path.Combine(_projectRoot, "configuration_files", "gsd", "GSDML-V2.41-LEUZE-BCL348i-20211213.xml");
            _plc.Initialize(safety: false);
            var importedDevicesObj = _plc.ImportDevice(validFilePath);
            var importedDevices = importedDevicesObj as Dictionary<string, Device>;

            // Check (Assert for the import method to be sure)
            var firstDeviceObj = importedDevices.Values.FirstOrDefault();

            Assert.That(firstDeviceObj, Is.Not.Null, "The dictionary should contain at least one device.");

            var device = firstDeviceObj as Device;
            Assert.That(device, Is.Not.Null, "The first device in the dictionary should be of type 'Device'.");
            Assert.That(device.DeviceItems[1].Name, Is.Not.Null.And.Not.Empty, "The first device should have a valid name.");

            // Act
            bool parametersResult = _plc.GetDeviceParameters(device, gsdFilePath, "[M11] Reading gate control");

            // Assert
            Assert.That(parametersResult, Is.True, $"GetDeviceParameters should pass for device '{device.DeviceItems[1].Name}'.");
        }

        [Test]
        /// Tests that the GetDeviceParameters method succeeds when
        /// specific valid parameters are requested.
        public void GetDeviceParameters_ShouldPass_WhenSpecificValidParametersAreProvided()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            Console.WriteLine("JSON File Path: " + jsonFilePath);
            _plc.Configure(jsonFilePath);

            string validFilePath = Path.Combine(_projectRoot, "configuration_files", "aml", "valid.aml");
            string gsdFilePath = Path.Combine(_projectRoot, "configuration_files", "gsd", "GSDML-V2.41-LEUZE-BCL348i-20211213.xml");
            _plc.Initialize(safety: false);
            var importedDevicesObj = _plc.ImportDevice(validFilePath);
            var importedDevices = importedDevicesObj as Dictionary<string, Device>;
            List<string> parametersToRead = new List<string> { "Automatic reading gate repeat", "Reading gate end mode / completeness mode",
                        "Restart delay" };

            // Check (Assert for the import method to be sure)
            var firstDeviceObj = importedDevices.Values.FirstOrDefault();

            Assert.That(firstDeviceObj, Is.Not.Null, "The dictionary should contain at least one device.");

            var device = firstDeviceObj as Device;
            Assert.That(device, Is.Not.Null, "The first device in the dictionary should be of type 'Device'.");
            Assert.That(device.DeviceItems[1].Name, Is.Not.Null.And.Not.Empty, "The first device should have a valid name.");

            // Act
            bool parametersResult = _plc.GetDeviceParameters(device, gsdFilePath, moduleName: "[M11] Reading gate control", parameterSelections: parametersToRead);

            // Assert
            Assert.That(parametersResult, Is.True, $"GetDeviceParameters should pass for device '{device.DeviceItems[1].Name}'.");
        }

        [Test]
        /// Tests that the GetDeviceParameters method fails when specific 
        /// invalid parameters are requested.
        public void GetDeviceParameters_ShouldFail_WhenSpecificInvalidParametersAreProvided()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            Console.WriteLine("JSON File Path: " + jsonFilePath);
            _plc.Configure(jsonFilePath);

            string validFilePath = Path.Combine(_projectRoot, "configuration_files", "aml", "valid.aml");
            string gsdFilePath = Path.Combine(_projectRoot, "configuration_files", "gsd", "GSDML-V2.41-LEUZE-BCL348i-20211213.xml");
            _plc.Initialize(safety: false);
            var importedDevicesObj = _plc.ImportDevice(validFilePath);
            var importedDevices = importedDevicesObj as Dictionary<string, Device>;
            List<string> parametersToRead = new List<string> { "Automatic reading gate repeatsdsadad", "Reading gate end mode / completeness mode",
                        "Restart delay" };

            // Check (Assert for the import method to be sure)
            var firstDeviceObj = importedDevices.Values.FirstOrDefault();

            Assert.That(firstDeviceObj, Is.Not.Null, "The dictionary should contain at least one device.");

            var device = firstDeviceObj as Device;
            Assert.That(device, Is.Not.Null, "The first device in the dictionary should be of type 'Device'.");
            Assert.That(device.DeviceItems[1].Name, Is.Not.Null.And.Not.Empty, "The first device should have a valid name.");

            // Act
            bool parametersResult = _plc.GetDeviceParameters(device, gsdFilePath, moduleName: "[M11] Reading gate control", parameterSelections: parametersToRead);

            // Assert
            Assert.That(parametersResult, Is.False, $"GetDeviceParameters should pass for device '{device.DeviceItems[1].Name}'.");
        }

        [Test]
        /// Tests that the GetDeviceParameters method fails 
        /// when an invalid module name is provided.
        public void GetDeviceParameters_ShouldFail_WhenInvalidModuleNameIsProvided()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            Console.WriteLine("JSON File Path: " + jsonFilePath);
            _plc.Configure(jsonFilePath);

            string validFilePath = Path.Combine(_projectRoot, "configuration_files", "aml", "valid.aml");
            string gsdFilePath = Path.Combine(_projectRoot, "configuration_files", "gsd", "GSDML-V2.41-LEUZE-BCL348i-20211213.xml");
            _plc.Initialize(safety: false);
            var importedDevicesObj = _plc.ImportDevice(validFilePath);
            var importedDevices = importedDevicesObj as Dictionary<string, Device>;

            // Check (Assert for the import method to be sure)
            var firstDeviceObj = importedDevices.Values.FirstOrDefault();

            Assert.That(firstDeviceObj, Is.Not.Null, "The dictionary should contain at least one device.");

            var device = firstDeviceObj as Device;
            Assert.That(device, Is.Not.Null, "The first device in the dictionary should be of type 'Device'.");
            Assert.That(device.DeviceItems[1].Name, Is.Not.Null.And.Not.Empty, "The first device should have a valid name.");

            // Act
            bool parametersResult = _plc.GetDeviceParameters(device, gsdFilePath, "[M111] Reading gate");

            // Assert
            Assert.That(parametersResult, Is.False, $"GetDeviceParameters should fail for device '{device.DeviceItems[1].Name}'.");
        }

        [Test]
        /// Tests that the GetDeviceParameters method fails when a 
        /// GSD file for a different device is provided.
        public void GetDeviceParameters_ShouldFail_WhenDifferentGsdIsProvided()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            Console.WriteLine("JSON File Path: " + jsonFilePath);
            _plc.Configure(jsonFilePath);

            string validFilePath = Path.Combine(_projectRoot, "configuration_files", "aml", "valid.aml");
            string gsdFilePath = Path.Combine(_projectRoot, "configuration_files", "gsd", "gsdml-v2.42-leuze-rsl400p cu 4m12-20230816.xml");
            _plc.Initialize(safety: false);
            var importedDevicesObj = _plc.ImportDevice(validFilePath);
            var importedDevices = importedDevicesObj as Dictionary<string, Device>;

            // Check (Assert for the import method to be sure)
            var firstDeviceObj = importedDevices.Values.FirstOrDefault();

            Assert.That(firstDeviceObj, Is.Not.Null, "The dictionary should contain at least one device.");

            var device = firstDeviceObj as Device;
            Assert.That(device, Is.Not.Null, "The first device in the dictionary should be of type 'Device'.");
            Assert.That(device.DeviceItems[1].Name, Is.Not.Null.And.Not.Empty, "The first device should have a valid name.");

            // Act
            bool parametersResult = _plc.GetDeviceParameters(device, gsdFilePath, "[M111] Reading gate");

            // Assert
            Assert.That(parametersResult, Is.False, $"GetDeviceParameters should fail for device '{device.DeviceItems[1].Name}'.");
        }

        [Test]
        /// Tests that the GetDeviceParameters method fails when the GSD file 
        /// has different module information than expected.
        public void GetDeviceParameters_ShouldFail_WhenGsdHasDifferentModuleInfo()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            Console.WriteLine("JSON File Path: " + jsonFilePath);
            _plc.Configure(jsonFilePath);

            string validFilePath = Path.Combine(_projectRoot, "configuration_files", "aml", "valid.aml");
            string gsdFilePath = Path.Combine(_projectRoot, "configuration_files", "gsd", "invalidModuleInfo.xml");
            _plc.Initialize(safety: false);
            var importedDevicesObj = _plc.ImportDevice(validFilePath);
            var importedDevices = importedDevicesObj as Dictionary<string, Device>;

            // Check (Assert for the import method to be sure)
            var firstDeviceObj = importedDevices.Values.FirstOrDefault();

            Assert.That(firstDeviceObj, Is.Not.Null, "The dictionary should contain at least one device.");

            var device = firstDeviceObj as Device;
            Assert.That(device, Is.Not.Null, "The first device in the dictionary should be of type 'Device'.");
            Assert.That(device.DeviceItems[1].Name, Is.Not.Null.And.Not.Empty, "The first device should have a valid name.");

            // Act
            bool parametersResult = _plc.GetDeviceParameters(device, gsdFilePath, "[M11] Reading gate control");

            // Assert
            Assert.That(parametersResult, Is.False, $"GetDeviceParameters should fail for device '{device.DeviceItems[1].Name}'.");
        }

        [Test]
        /// Tests that the GetDeviceParameters method fails when the specified module
        /// is not present in the device.
        public void GetDeviceParameters_ShouldFail_WhenSpecifiedModuleIsNotPresentInTheDevice()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            Console.WriteLine("JSON File Path: " + jsonFilePath);
            _plc.Configure(jsonFilePath);

            string validFilePath = Path.Combine(_projectRoot, "configuration_files", "aml", "valid.aml");
            string gsdFilePath = Path.Combine(_projectRoot, "configuration_files", "gsd", "GSDML-V2.41-LEUZE-BCL348i-20211213.xml");
            _plc.Initialize(safety: false);
            var importedDevicesObj = _plc.ImportDevice(validFilePath);
            var importedDevices = importedDevicesObj as Dictionary<string, Device>;

            // Check (Assert for the import method to be sure)
            var firstDeviceObj = importedDevices.Values.FirstOrDefault();

            Assert.That(firstDeviceObj, Is.Not.Null, "The dictionary should contain at least one device.");

            var device = firstDeviceObj as Device;
            Assert.That(device, Is.Not.Null, "The first device in the dictionary should be of type 'Device'.");
            Assert.That(device.DeviceItems[1].Name, Is.Not.Null.And.Not.Empty, "The first device should have a valid name.");

            // Act
            bool parametersResult = _plc.GetDeviceParameters(device, gsdFilePath, "[M20] Decoder state");

            // Assert
            Assert.That(parametersResult, Is.False, $"GetDeviceParameters should fail for device '{device.DeviceItems[1].Name}'.");
        }

        [Test]
        /// Tests that the GetDeviceParameters method fails when a safety module is specified
        /// but the safety flag is not set correctly.
        public void GetDeviceParameters_ShouldFail_WhenSpecifiedModuleIsNotSafety()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            Console.WriteLine("JSON File Path: " + jsonFilePath);
            _plc.Configure(jsonFilePath);

            string validFilePath = Path.Combine(_projectRoot, "configuration_files", "aml", "valid.aml");
            string gsdFilePath = Path.Combine(_projectRoot, "configuration_files", "gsd", "GSDML-V2.41-LEUZE-BCL348i-20211213.xml");
            _plc.Initialize(safety: false);
            var importedDevicesObj = _plc.ImportDevice(validFilePath);
            var importedDevices = importedDevicesObj as Dictionary<string, Device>;

            // Check (Assert for the import method to be sure)
            var firstDeviceObj = importedDevices.Values.FirstOrDefault();

            Assert.That(firstDeviceObj, Is.Not.Null, "The dictionary should contain at least one device.");

            var device = firstDeviceObj as Device;
            Assert.That(device, Is.Not.Null, "The first device in the dictionary should be of type 'Device'.");
            Assert.That(device.DeviceItems[1].Name, Is.Not.Null.And.Not.Empty, "The first device should have a valid name.");

            // Act
            bool parametersResult = _plc.GetDeviceParameters(device, gsdFilePath, "[M11] Reading gate control", safety: true);

            // Assert
            Assert.That(parametersResult, Is.False, $"GetDeviceParameters should fail for device '{device.DeviceItems[1].Name}'.");
        }

        [Test]
        /// Tests that the GetDeviceParameters method succeeds when a safety module is 
        /// specified and the safety flag is set.
        public void GetDeviceParameters_ShouldPass_WhenSafetyModuleIsSpecified()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            Console.WriteLine("JSON File Path: " + jsonFilePath);
            _plc.Configure(jsonFilePath);

            string gsdFilePath = Path.Combine(_projectRoot, "configuration_files", "gsd", "GSDML-V2.43-LEUZE-FBPS648i-20240514.xml");
            _plc.Initialize(safety: false);
            Device device = _plc.GetDeviceByName("FBPS648i") as Device;

            // Act
            bool parametersResult = _plc.GetDeviceParameters(device, gsdFilePath, "[M50] Safe Position 32 Bit", safety: true);

            // Assert
            Assert.That(parametersResult, Is.True, $"GetDeviceParameters should pass for device '{device.DeviceItems[1].Name}'.");
        }

        [Test]
        ///  Tests that the GetDeviceParameters method succeeds when accessing normal parameters 
        ///  of a safety module without the safety flag.
        public void GetDeviceParameters_ShouldPass_WhenSafetyModuleHasNormalParameters()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            Console.WriteLine("JSON File Path: " + jsonFilePath);
            _plc.Configure(jsonFilePath);

            string gsdFilePath = Path.Combine(_projectRoot, "configuration_files", "gsd", "GSDML-V2.43-LEUZE-FBPS648i-20240514.xml");
            _plc.Initialize(safety: false);
            Device device = _plc.GetDeviceByName("FBPS648i") as Device;

            // Act
            bool parametersResult = _plc.GetDeviceParameters(device, gsdFilePath, "[M50] Safe Position 32 Bit", safety: false);

            // Assert
            Assert.That(parametersResult, Is.True, $"GetDeviceParameters should pass for device '{device.DeviceItems[1].Name}'.");
        }

        [Test]
        ///  Tests that the GetDeviceParameters method fails when the specified
        ///  module has no parameters to retrieve.
        public void GetDeviceParameters_ShouldFail_WhenModuleHasNoParameters()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            Console.WriteLine("JSON File Path: " + jsonFilePath);
            _plc.Configure(jsonFilePath);

            string gsdFilePath = Path.Combine(_projectRoot, "configuration_files", "gsd", "GSDML-V2.43-LEUZE-FBPS648i-20240514.xml");
            _plc.Initialize(safety: false);
            Device device = _plc.GetDeviceByName("FBPS648i") as Device;

            // Act
            bool parametersResult = _plc.GetDeviceParameters(device, gsdFilePath, "[M07] Device Status", safety: false);

            // Assert
            Assert.That(parametersResult, Is.False, $"GetDeviceParameters should fail for device '{device.DeviceItems[1].Name}'.");
        }

        [Test]
        /// Tests that the SetDeviceParameters method succeeds when valid
        /// parameters are provided for a module.
        public void SetDeviceParameters_ShouldPass_WhenValidParametersAreProvided()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            _plc.Configure(jsonFilePath);

            string validFilePath = Path.Combine(_projectRoot, "configuration_files", "aml", "valid.aml");
            string gsdFilePath = Path.Combine(_projectRoot, "configuration_files", "gsd", "GSDML-V2.41-LEUZE-BCL348i-20211213.xml");
            _plc.Initialize(safety: false);
            var importedDevicesObj = _plc.ImportDevice(validFilePath);
            var importedDevices = importedDevicesObj as Dictionary<string, Device>;

            var firstDeviceObj = importedDevices.Values.FirstOrDefault();
            Assert.That(firstDeviceObj, Is.Not.Null, "The dictionary should contain at least one device.");

            var device = firstDeviceObj as Device;
            Assert.That(device, Is.Not.Null, "The first device in the dictionary should be of type 'Device'.");
            Assert.That(device.DeviceItems[1].Name, Is.Not.Null.And.Not.Empty, "The first device should have a valid name.");

            string moduleName = "[M11] Reading gate control";
            Dictionary<string, object> parametersToSet = new Dictionary<string, object>
            {
                {"Automatic reading gate repeat", "yes"},
                {"Reading gate end mode / completeness mode", 3},
                {"Restart delay", 333},
                {"Max. reading gate time when scanning", 762},
            };

            // Act
            bool setParametersResult = _plc.SetDeviceParameters(device, gsdFilePath, moduleName, parametersToSet);

            // Assert
            Assert.That(setParametersResult, Is.True, $"SetDeviceParameters should pass for module '{moduleName}' in device '{device.DeviceItems[1].Name}'.");
            _plc.GetDeviceParameters(device, gsdFilePath, "[M11] Reading gate control", safety: false); // tester check
        }

        [Test]
        /// Tests that the SetDeviceParameters method fails when invalid 
        /// parameter values are provided.
        public void SetDeviceParameters_ShouldFail_WhenInvalidParameterValues()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            _plc.Configure(jsonFilePath);

            string validFilePath = Path.Combine(_projectRoot, "configuration_files", "aml", "valid.aml");
            string gsdFilePath = Path.Combine(_projectRoot, "configuration_files", "gsd", "GSDML-V2.41-LEUZE-BCL348i-20211213.xml");
            _plc.Initialize(safety: false);
            var importedDevicesObj = _plc.ImportDevice(validFilePath);
            var importedDevices = importedDevicesObj as Dictionary<string, Device>;

            var firstDeviceObj = importedDevices.Values.FirstOrDefault();
            Assert.That(firstDeviceObj, Is.Not.Null, "The dictionary should contain at least one device.");

            var device = firstDeviceObj as Device;
            Assert.That(device, Is.Not.Null, "The first device in the dictionary should be of type 'Device'.");
            Assert.That(device.DeviceItems[1].Name, Is.Not.Null.And.Not.Empty, "The first device should have a valid name.");

            string moduleName = "[M11] Reading gate control";
            Dictionary<string, object> parametersToSet = new Dictionary<string, object>
            {
                {"Automatic reading gate repeat", "yes"},
                {"Reading gate end mode / completeness mode", 3},
                {"Restart delay", 1000000},
                {"Max. reading gate time when scanning", 655555},
            };

            // Act
            bool setParametersResult = _plc.SetDeviceParameters(device, gsdFilePath, moduleName, parametersToSet);

            // Assert
            Assert.That(setParametersResult, Is.False, $"SetDeviceParameters should fail for invalid parameters in module '{moduleName}''.");
        }

        [Test]
        ///  Tests that the SetDeviceParameters method fails when an 
        ///  invalid module name is provided.
        public void SetDeviceParameters_ShouldFail_WhenInvalidModuleNameIsProvided()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            _plc.Configure(jsonFilePath);

            string validFilePath = Path.Combine(_projectRoot, "configuration_files", "aml", "valid.aml");
            string gsdFilePath = Path.Combine(_projectRoot, "configuration_files", "gsd", "GSDML-V2.41-LEUZE-BCL348i-20211213.xml");
            _plc.Initialize(safety: false);
            var importedDevicesObj = _plc.ImportDevice(validFilePath);
            var importedDevices = importedDevicesObj as Dictionary<string, Device>;

            var firstDeviceObj = importedDevices.Values.FirstOrDefault();
            Assert.That(firstDeviceObj, Is.Not.Null, "The dictionary should contain at least one device.");

            var device = firstDeviceObj as Device;
            Assert.That(device, Is.Not.Null, "The first device in the dictionary should be of type 'Device'.");
            Assert.That(device.DeviceItems[1].Name, Is.Not.Null.And.Not.Empty, "The first device should have a valid name.");

            string invalidModuleName = "[M999] Invalid module";
            Dictionary<string, object> parametersToSet = new Dictionary<string, object>
            {
                {"Parameter", "someValue"}
            };

            // Act
            bool setParametersResult = _plc.SetDeviceParameters(device, gsdFilePath, invalidModuleName, parametersToSet);

            // Assert
            Assert.That(setParametersResult, Is.False, $"SetDeviceParameters should fail for invalid module '{invalidModuleName}'.");
        }

        [Test]
        /// Tests that the SetDeviceParameters method fails when invalid parameters are provided.
        public void SetDeviceParameters_ShouldFail_WhenInvalidParametersAreProvided()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            _plc.Configure(jsonFilePath);

            string validFilePath = Path.Combine(_projectRoot, "configuration_files", "aml", "valid.aml");
            string gsdFilePath = Path.Combine(_projectRoot, "configuration_files", "gsd", "GSDML-V2.41-LEUZE-BCL348i-20211213.xml");
            _plc.Initialize(safety: false);
            var importedDevicesObj = _plc.ImportDevice(validFilePath);
            var importedDevices = importedDevicesObj as Dictionary<string, Device>;

            var firstDeviceObj = importedDevices.Values.FirstOrDefault();
            Assert.That(firstDeviceObj, Is.Not.Null, "The dictionary should contain at least one device.");

            var device = firstDeviceObj as Device;
            Assert.That(device, Is.Not.Null, "The first device in the dictionary should be of type 'Device'.");
            Assert.That(device.DeviceItems[1].Name, Is.Not.Null.And.Not.Empty, "The first device should have a valid name.");

            string moduleName = "[M11] Reading gate control";
            Dictionary<string, object> parametersToSet = new Dictionary<string, object>
            {
                {"InvalidParameter", "someValue"}
            };

            // Act
            bool setParametersResult = _plc.SetDeviceParameters(device, gsdFilePath, moduleName, parametersToSet);

            // Assert
            Assert.That(setParametersResult, Is.False, $"SetDeviceParameters should fail for parameters provided '{parametersToSet}'.");
        }

        [Test]
        /// Tests that the SetDeviceParameters method fails when no parameters are provided to set.
        public void SetDeviceParameters_ShouldFail_WhenNoParametersAreProvided()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            Console.WriteLine("JSON File Path: " + jsonFilePath);
            _plc.Configure(jsonFilePath);

            string gsdFilePath = Path.Combine(_projectRoot, "configuration_files", "gsd", "GSDML-V2.43-LEUZE-FBPS648i-20240514.xml");
            _plc.Initialize(safety: false);
            Device device = _plc.GetDeviceByName("FBPS648i") as Device;

            string validModuleName = "[M50] Safe Position 32 Bit";
            Dictionary<string, object> parametersToSet = new Dictionary<string, object>();

            // Act
            bool setParametersResult = _plc.SetDeviceParameters(device, gsdFilePath, validModuleName, parametersToSet);

            // Assert
            Assert.That(setParametersResult, Is.False, $"SetDeviceParameters should fail when no parameters are provided '{validModuleName}'.");
        }

        [Test]
        /// Tests that the SetDeviceParameters method fails when the parameters dictionary is null.
        public void SetDeviceParameters_ShouldFail_WhenParametersAreNull()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            _plc.Configure(jsonFilePath);

            string validFilePath = Path.Combine(_projectRoot, "configuration_files", "aml", "valid.aml");
            string gsdFilePath = Path.Combine(_projectRoot, "configuration_files", "gsd", "GSDML-V2.41-LEUZE-BCL348i-20211213.xml");
            _plc.Initialize(safety: false);
            var importedDevicesObj = _plc.ImportDevice(validFilePath);
            var importedDevices = importedDevicesObj as Dictionary<string, Device>;

            var firstDeviceObj = importedDevices.Values.FirstOrDefault();
            Assert.That(firstDeviceObj, Is.Not.Null, "The dictionary should contain at least one device.");

            var device = firstDeviceObj as Device;
            Assert.That(device, Is.Not.Null, "The first device in the dictionary should be of type 'Device'.");
            Assert.That(device.DeviceItems[1].Name, Is.Not.Null.And.Not.Empty, "The first device should have a valid name.");

            string moduleName = "[M11] Reading gate control";

            // Act
            bool setParametersResult = _plc.SetDeviceParameters(device, gsdFilePath, moduleName, null);

            // Assert
            Assert.That(setParametersResult, Is.False, $"SetDeviceParameters should fail for invalid module '{moduleName}'.");
        }

        [Test]
        /// Tests that the SetDeviceParameters method fails when attempting to set parameters on a 
        /// module that has no changeable parameters.
        public void SetDeviceParameters_ShouldFail_WhenModuleHasNoChangeableParameters()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            Console.WriteLine("JSON File Path: " + jsonFilePath);
            _plc.Configure(jsonFilePath);

            string gsdFilePath = Path.Combine(_projectRoot, "configuration_files", "gsd", "GSDML-V2.43-LEUZE-FBPS648i-20240514.xml");
            _plc.Initialize(safety: false);
            Device device = _plc.GetDeviceByName("FBPS648i") as Device;

            string moduleName = "[M07] Device Status";
            Dictionary<string, object> parametersToSet = new Dictionary<string, object>(); // should not matter since the check for module happens before 

            // Act
            bool setParametersResult = _plc.SetDeviceParameters(device, gsdFilePath, moduleName, parametersToSet);

            // Assert
            Assert.That(setParametersResult, Is.False, $"SetDeviceParameters should fail for module '{moduleName}' with no changeable parameters.");
        }

        [Test]
        /// Tests that the SetDeviceParameters method succeeds when setting valid safety 
        /// parameters on a safety module.
        public void SetDeviceParameters_ShouldPass_WhenSafetyParametersAreSet()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            _plc.Configure(jsonFilePath);

            string gsdFilePath = Path.Combine(_projectRoot, "configuration_files", "gsd", "GSDML-V2.43-LEUZE-FBPS648i-20240514.xml");
            _plc.Initialize(safety: false);
            Device device = _plc.GetDeviceByName("FBPS648i") as Device;

            string moduleName = "[M50] Safe Position 32 Bit";
            Dictionary<string, object> safetyParametersToSet = new Dictionary<string, object>
            {
                { "Failsafe_FSourceAddress", 500 },
                { "Failsafe_FDestinationAddress", 1000 },
                { "Failsafe_FMonitoringtime", 200 },
            };

            // Act
            bool setParametersResult = _plc.SetDeviceParameters(device, gsdFilePath, moduleName, safetyParametersToSet, safety: true);

            // Assert
            Assert.That(setParametersResult, Is.True, $"SetDeviceParameters should pass for safety module '{moduleName}' in device '{device.DeviceItems[1].Name}'.");
        }

        [Test]
        /// Tests that the SetDeviceParameters method fails when 
        /// invalid safety parameters are provided.
        public void SetDeviceParameters_ShouldFail_WhenInvalidSafetyParametersAreProvided()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            _plc.Configure(jsonFilePath);

            string gsdFilePath = Path.Combine(_projectRoot, "configuration_files", "gsd", "GSDML-V2.43-LEUZE-FBPS648i-20240514.xml");
            _plc.Initialize(safety: false);
            Device device = _plc.GetDeviceByName("FBPS648i") as Device;

            string moduleName = "[M50] Safe Position 32 Bit";
            Dictionary<string, object> invalidSafetyParametersToSet = new Dictionary<string, object>
            {
                { "InvalidSafetyParameter", 9999 }
            };

            // Act
            bool setParametersResult = _plc.SetDeviceParameters(device, gsdFilePath, moduleName, invalidSafetyParametersToSet, safety: true);

            // Assert
            Assert.That(setParametersResult, Is.False, $"SetDeviceParameters should fail for invalid safety parameters in module '{moduleName}'.");
        }

        [Test]
        /// Tests that the Stop method succeeds when stopping the PLC without safety mode enabled.
        public void StopPLC_ShouldPass_WhenCalledWithoutSafety()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);
            _plc.Start();

            // Act 
            bool result = _plc.Stop();

            // Assert
            Assert.That(result, Is.True, "PLC should stop successfully without safety mode.");
        }

        [Test]
        /// Tests that the Stop method succeeds when stopping the PLC with safety mode enabled.
        public void StopPLC_ShouldPass_WhenCalledWithSafety()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: true);
            _plc.Start();

            // Act 
            bool result = _plc.Stop();

            // Assert
            Assert.That(result, Is.True, "PLC should stop successfully with safety mode enabled.");
        }

        [Test]
        /// Tests that the Start method succeeds when starting the PLC without safety mode enabled.
        public void StartPLC_ShouldPass_WhenCalledWithoutSafety()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);

            // Act 
            bool result = _plc.Start();

            // Assert
            Assert.That(result, Is.True, "PLC should start successfully without safety mode.");
        }

        [Test]
        /// Tests that the Start method succeeds when starting the PLC with safety mode enabled.
        public void StartPLC_ShouldPass_WhenCalledWithSafety()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: true);

            // Act 
            bool result = _plc.Start();

            // Assert
            Assert.That(result, Is.True, "PLC should start successfully with safety mode enabled.");
        }

        [Test]
        /// Tests that the Compile method succeeds when the project is valid and can be compiled.
        public void Compile_ShouldPass_WhenProjectIsCompilable()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);

            // Act 
            bool result = _plc.Compile();

            // Assert
            Assert.That(result, Is.True, "PLC should compile successfully.");
        }

        [Test]
        /// Tests that the Compile method fails when the project is invalid and cannot be compiled.
        public void Compile_ShouldFail_WhenProjectIsNotCompilable()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "invalid_configuration.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);

            // Act 
            bool result = _plc.Compile();

            // Assert
            Assert.That(result, Is.False, "PLC should not compile successfully.");
        }

        [Test]
        /// Tests that the Download method succeeds when downloading hardware and software
        /// to the PLC without safety configurations.
        public void Download_ShouldPass_WhenDownloadingNonSafety()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);

            // Act 
            bool fullDownload = _plc.Download(DownloadOptions.Hardware | DownloadOptions.Software); // download combination

            // Assert
            Assert.That(fullDownload, Is.True, "PLC should download HW & SW successfully.");
        }

        [Test]
        /// Tests that the Download method succeeds when downloading 
        /// safety configurations to the PLC.
        public void Download_ShouldPass_WhenDownloadingSafety()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_safety.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: true);

            // Act 
            bool download = _plc.Download("safety");

            // Assert
            Assert.That(download, Is.True, "PLC should download safety successfully.");
        }

        [Test]
        /// Tests that the Download method fails when attempting to download safety configurations 
        /// without proper initialization for safety.
        public void Download_ShouldFail_WhenInitializeIsWrongAndSafetyDownload()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_safety.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);

            // Act 
            bool download = _plc.Download("safety");

            // Assert
            Assert.That(download, Is.False, "PLC should not download safety.");
        }

        [Test]
        /// Tests that the Export method succeeds when exporting 
        /// project data with a valid project.
        public void Export_ShouldPass_WhenProjectIsValid()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_safety.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);
            string exportPath = "C:\\Users\\vformane\\Desktop\\Export";
            var filesToExport = new Dictionary<string, object>
            {
                { "plcTags", exportPath }
            };

            // Act 
            bool result = _plc.Export(filesToExport);

            // Assert
            Assert.That(result, Is.True, "Export should return true when export is successful.");
        }

        [Test]
        /// Tests that the SaveProjectAs method succeeds when saving the 
        /// project under a new name or path.
        public void SaveProjectAs_ShouldPass_WhenPathIsCorrect()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);
            string saveName = "ProjectAAA";
            // Act 
            bool result = _plc.SaveProjectAs(saveName);

            // Assert
            Assert.That(result, Is.True, "SaveProjectAs should return true when the project is saved successfully.");
        }

        [Test]
        /// Tests that the AdditionalImport method succeeds when provided 
        /// with a correct file path for import.
        public void AddutionalImport_ShouldPass_WhenPathIsCorrect()
        {
            // finish this 
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);
            string importPath = "C:\\Users\\vformane\\Desktop\\Export\\Default tag table.xml";
            var filesToImport = new Dictionary<string, object>
            {
                { "plcTags", importPath }
            };
            // Act 
            bool result = _plc.AdditionalImport(filesToImport);

            // Assert
            Assert.That(result, Is.True, "Additional import should pass properly.");
        }

        [TearDown]
        public void TearDown()
        {
            if (!_disposed)
            {
                Dispose(); // Call Dispose if it hasn't been called yet.
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose of the SiemensPLCController
                    Thread.Sleep(5000); // added sleep so Tia portal could close properly
                    _plc?.Dispose();
                    
                }
                _disposed = true;
            }
        }

        ~SiemensPLCControllerTests()
        {
            Dispose(false);
        }
    }
}
