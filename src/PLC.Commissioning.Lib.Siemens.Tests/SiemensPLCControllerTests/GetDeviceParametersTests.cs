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
    public class GetDeviceParametersTests : IDisposable
    {
        private SiemensPLCController _plc;
        private bool _disposed = false;
        private string _testDataPath;
        private object _importedDevicesObj;
            
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
            string validFilePath = Path.Combine(_testDataPath, "aml", "valid_multiple_devices.aml");
            _importedDevicesObj = _plc.ImportDevice(validFilePath);

            Log.Information("Test setup completed. Test data directory: {Path}", _testDataPath);
        }

        [Test]
        public void GetDeviceParameters_ShouldPass_WhenValidModuleNameIsProvided()
        {
            // Arrange
            string gsdFilePath = Path.Combine(_testDataPath, "gsd", "GSDML-V2.41-LEUZE-BCL248i-20211213.xml");
            var importedDevices = _importedDevicesObj as Dictionary<string, Device>;

            var firstDeviceObj = importedDevices?.Values.FirstOrDefault();
            Assert.That(firstDeviceObj, Is.Not.Null, "The dictionary should contain at least one device.");

            var device = firstDeviceObj as Device;
            Assert.That(device, Is.Not.Null, "The first device in the dictionary should be of type 'Device'.");

            // Act
            bool parametersResult = _plc.GetDeviceParameters(device, gsdFilePath, "[M11] Reading gate control");

            // Assert
            Assert.That(parametersResult, Is.True, $"GetDeviceParameters should pass for device '{device.DeviceItems[1].Name}'.");
        }

        [Test]
        public void GetDeviceParameters_ShouldPass_WhenSpecificValidParametersAreProvided()
        {
            // Arrange
            string gsdFilePath = Path.Combine(_testDataPath, "gsd", "GSDML-V2.41-LEUZE-BCL248i-20211213.xml");
            var importedDevices = _importedDevicesObj as Dictionary<string, Device>;
            List<string> parametersToRead = new List<string>
            {
                "Automatic reading gate repeat",
                "Reading gate end mode / completeness mode",
                "Restart delay"
            };

            var firstDeviceObj = importedDevices?.Values.FirstOrDefault();
            Assert.That(firstDeviceObj, Is.Not.Null, "The dictionary should contain at least one device.");

            var device = firstDeviceObj as Device;
            Assert.That(device, Is.Not.Null, "The first device in the dictionary should be of type 'Device'.");

            // Act
            bool parametersResult = _plc.GetDeviceParameters(device, gsdFilePath, "[M11] Reading gate control", parametersToRead);

            // Assert
            Assert.That(parametersResult, Is.True, $"GetDeviceParameters should pass for device '{device.DeviceItems[1].Name}'.");
        }

        [Test]
        public void GetDeviceParameters_ShouldFail_WhenSpecificInvalidParametersAreProvided()
        {
            // Arrange
            string gsdFilePath = Path.Combine(_testDataPath, "gsd", "GSDML-V2.41-LEUZE-BCL248i-20211213.xml");
            var importedDevices = _importedDevicesObj as Dictionary<string, Device>;
            List<string> invalidParameters = new List<string>
            {
                "Invalid parameter 1",
                "Invalid parameter 2"
            };

            var firstDeviceObj = importedDevices?.Values.FirstOrDefault();
            Assert.That(firstDeviceObj, Is.Not.Null, "The dictionary should contain at least one device.");

            var device = firstDeviceObj as Device;
            Assert.That(device, Is.Not.Null, "The first device in the dictionary should be of type 'Device'.");

            // Act
            bool parametersResult = _plc.GetDeviceParameters(device, gsdFilePath, "[M11] Reading gate control", invalidParameters);

            // Assert
            Assert.That(parametersResult, Is.False, $"GetDeviceParameters should fail for device '{device.DeviceItems[1].Name}'.");
        }

        [Test]
        public void GetDeviceParameters_ShouldFail_WhenInvalidModuleNameIsProvided()
        {
            // Arrange
            string gsdFilePath = Path.Combine(_testDataPath, "gsd", "GSDML-V2.41-LEUZE-BCL248i-20211213.xml");
            var importedDevices = _importedDevicesObj as Dictionary<string, Device>;

            var firstDeviceObj = importedDevices?.Values.FirstOrDefault();
            Assert.That(firstDeviceObj, Is.Not.Null, "The dictionary should contain at least one device.");

            var device = firstDeviceObj as Device;
            Assert.That(device, Is.Not.Null, "The first device in the dictionary should be of type 'Device'.");

            // Act
            bool parametersResult = _plc.GetDeviceParameters(device, gsdFilePath, "[M999] Non-existent module");

            // Assert
            Assert.That(parametersResult, Is.False, $"GetDeviceParameters should fail for device '{device.DeviceItems[1].Name}'.");
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

        ~GetDeviceParametersTests()
        {
            Dispose(false);
        }
    }
}
