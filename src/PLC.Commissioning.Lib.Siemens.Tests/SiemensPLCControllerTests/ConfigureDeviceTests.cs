using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Serilog;
using Siemens.Engineering.HW;

namespace PLC.Commissioning.Lib.Siemens.Tests
{
    [TestFixture]
    public class ConfigureDeviceTests : IDisposable
    {
        private SiemensPLCController _plc;
        private bool _disposed = false;
        private string _testDataPath;

        [SetUp]
        public void SetUp()
        {
            // Set up logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();
            
            // Set up the test data path
            _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
            
            // Initialize and configure the SiemensPLCController
            _plc = new SiemensPLCController();
            string jsonFilePath = Path.Combine(_testDataPath, "Configurations", "valid_config.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);

            Log.Information("Test setup completed. Test data directory: {Path}", _testDataPath);
        }

        [Test]
        public void ConfigureDevice_ShouldSucceed_WhenValidParametersAreProvided_WithImport()
        {
            // Arrange
            string validFilePath = Path.Combine(_testDataPath, "aml", "valid_multiple_devices.aml");
            var importedDevicesObj = _plc.ImportDevice(validFilePath);
            var importedDevices = importedDevicesObj as Dictionary<string, Device>;

            var parametersToConfigure = new Dictionary<string, object>
            {
                { "ipAddress", "192.168.60.100" },
                { "profinetName", "dut" }
            };

            var firstDeviceObj = importedDevices?.Values.FirstOrDefault();
            Assert.That(firstDeviceObj, Is.Not.Null, "The dictionary should contain at least one device.");

            var device = firstDeviceObj as Device;
            Assert.That(device, Is.Not.Null, "The first device in the dictionary should be of type 'Device'.");

            // Act
            bool configurationResult = _plc.ConfigureDevice(device, parametersToConfigure);

            // Assert
            Assert.That(configurationResult, Is.True, $"Configuration should pass for device '{device.DeviceItems[1].Name}'.");
        }

        [Test]
        public void ConfigureDevice_ShouldSucceed_WhenValidParametersAreProvidedForMultipleDevices()
        {
            // Arrange
            // Configuration for the first device
            var parametersToConfigureRSL400P = new Dictionary<string, object>
            {
                { "ipAddress", "192.168.60.101" },
                { "profinetName", "dutrsl" }
            };
            Device deviceRSL400P = _plc.GetDeviceByName("RSL400P") as Device;

            // Act and Assert for the first device
            bool configurationResultRSL400P = _plc.ConfigureDevice(deviceRSL400P, parametersToConfigureRSL400P);
            Assert.That(configurationResultRSL400P, Is.True, $"Configuration should pass for device '{deviceRSL400P?.DeviceItems[1].Name}'.");

            // Configuration for the second device
            var parametersToConfigureBCL248i = new Dictionary<string, object>
            {
                { "ipAddress", "192.168.60.100" },
                { "profinetName", "dutbcl" }
            };
            Device deviceBCL248i = _plc.GetDeviceByName("BCL248i") as Device;

            // Act and Assert for the second device
            bool configurationResultBCL248i = _plc.ConfigureDevice(deviceBCL248i, parametersToConfigureBCL248i);
            Assert.That(configurationResultBCL248i, Is.True, $"Configuration should pass for device '{deviceBCL248i?.DeviceItems[1].Name}'.");
        }

        [Test]
        public void ConfigureDevice_ShouldFail_WhenDeviceIsNull()
        {
            // Arrange
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
        public void ConfigureDevice_ShouldFail_WhenDeviceIsIncorrectType()
        {
            // Arrange
            var parametersToConfigure = new Dictionary<string, object>
            {
                { "ipAddress", "192.168.60.100" },
                { "profinetName", "dut" }
            };
            object incorrectDevice = new { Name = "IncorrectDevice" };

            // Act
            bool configurationResult = _plc.ConfigureDevice(incorrectDevice, parametersToConfigure);

            // Assert
            Assert.That(configurationResult, Is.False, "Configuration should fail when the device is of incorrect type.");
        }

        [Test]
        public void ConfigureDevice_ShouldFail_WhenParametersToConfigureIsEmpty()
        {
            // Arrange
            string validFilePath = Path.Combine(_testDataPath, "aml", "valid_multiple_devices.aml");
            var importedDevicesObj = _plc.ImportDevice(validFilePath);
            var parametersToConfigure = new Dictionary<string, object>();
            Device device = _plc.GetDeviceByName("RSL400P") as Device;

            // Act
            bool configurationResult = _plc.ConfigureDevice(device, parametersToConfigure);

            // Assert
            Assert.That(configurationResult, Is.False, "Configuration should fail when the parameters dictionary is empty.");
        }

        [Test]
        public void ConfigureDevice_ShouldFail_WhenParametersToConfigureIsNull()
        {
            // Arrange
            string validFilePath = Path.Combine(_testDataPath, "aml", "valid_multiple_devices.aml");
            var importedDevicesObj = _plc.ImportDevice(validFilePath);
            Device device = _plc.GetDeviceByName("RSL400P") as Device;

            // Act
            bool configurationResult = _plc.ConfigureDevice(device, null);

            // Assert
            Assert.That(configurationResult, Is.False, "Configuration should fail when parametersToConfigure is null.");
        }

        [Test]
        public void ConfigureDevice_ShouldFail_WhenRequiredParametersAreMissing()
        {
            // Arrange
            string validFilePath = Path.Combine(_testDataPath, "aml", "valid_multiple_devices.aml");
            var importedDevicesObj = _plc.ImportDevice(validFilePath);
            var parametersToConfigure = new Dictionary<string, object>();
            Device device = _plc.GetDeviceByName("RSL400P") as Device;

            // Act
            bool configurationResult = _plc.ConfigureDevice(device, parametersToConfigure);

            // Assert
            Assert.That(configurationResult, Is.False, "Configuration should fail when required parameters are missing.");
        }

        [TearDown]
        public void TearDown()
        {
            Dispose();
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
                    _plc?.Dispose();
                    Log.Information("Test resources disposed.");
                }

                _disposed = true;
            }
        }

        ~ConfigureDeviceTests()
        {
            Dispose(false);
        }
    }
}
