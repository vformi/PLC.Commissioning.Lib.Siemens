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
    public class SetDeviceParametersTests : IDisposable
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
        public void SetDeviceParameters_ShouldPass_WhenValidParametersAreProvided()
        {
            // Arrange
            string gsdFilePath = Path.Combine(_testDataPath, "gsd", "GSDML-V2.41-LEUZE-BCL248i-20211213.xml");
            var importedDevices = _importedDevicesObj as Dictionary<string, Device>;
            var device = importedDevices?.Values.FirstOrDefault() as Device;
            Assert.That(device, Is.Not.Null, "The device should not be null.");

            string moduleName = "[M11] Reading gate control";
            var parametersToSet = new Dictionary<string, object>
            {
                {"Automatic reading gate repeat", "yes"},
                {"Reading gate end mode / completeness mode", 3},
                {"Restart delay", 333},
                {"Max. reading gate time when scanning", 762}
            };

            // Act
            bool result = _plc.SetDeviceParameters(device, gsdFilePath, moduleName, parametersToSet);

            // Assert
            Assert.That(result, Is.True, $"SetDeviceParameters should pass for module '{moduleName}'.");
        }

        [Test]
        public void SetDeviceParameters_ShouldFail_WhenInvalidParameterValues()
        {
            // Arrange
            string gsdFilePath = Path.Combine(_testDataPath, "gsd", "GSDML-V2.41-LEUZE-BCL248i-20211213.xml");
            var importedDevices = _importedDevicesObj as Dictionary<string, Device>;
            var device = importedDevices?.Values.FirstOrDefault() as Device;
            Assert.That(device, Is.Not.Null, "The device should not be null.");

            string moduleName = "[M11] Reading gate control";
            var invalidParameters = new Dictionary<string, object>
            {
                {"Automatic reading gate repeat", "yes"},
                {"Reading gate end mode / completeness mode", 3},
                {"Restart delay", 1000000},
                {"Max. reading gate time when scanning", 655555}
            };

            // Act
            bool result = _plc.SetDeviceParameters(device, gsdFilePath, moduleName, invalidParameters);

            // Assert
            Assert.That(result, Is.False, $"SetDeviceParameters should fail for module '{moduleName}' with invalid parameters.");
        }

        [Test]
        public void SetDeviceParameters_ShouldFail_WhenInvalidModuleNameIsProvided()
        {
            // Arrange
            string gsdFilePath = Path.Combine(_testDataPath, "gsd", "GSDML-V2.41-LEUZE-BCL248i-20211213.xml");
            var importedDevices = _importedDevicesObj as Dictionary<string, Device>;
            var device = importedDevices?.Values.FirstOrDefault() as Device;
            Assert.That(device, Is.Not.Null, "The device should not be null.");

            string invalidModuleName = "[M999] Invalid module";
            var parametersToSet = new Dictionary<string, object>
            {
                {"Parameter", "someValue"}
            };

            // Act
            bool result = _plc.SetDeviceParameters(device, gsdFilePath, invalidModuleName, parametersToSet);

            // Assert
            Assert.That(result, Is.False, $"SetDeviceParameters should fail for invalid module '{invalidModuleName}'.");
        }

        [Test]
        public void SetDeviceParameters_ShouldFail_WhenParametersAreNull()
        {
            // Arrange
            string gsdFilePath = Path.Combine(_testDataPath, "gsd", "GSDML-V2.41-LEUZE-BCL248i-20211213.xml");
            var importedDevices = _importedDevicesObj as Dictionary<string, Device>;
            var device = importedDevices?.Values.FirstOrDefault() as Device;
            Assert.That(device, Is.Not.Null, "The device should not be null.");

            string moduleName = "[M11] Reading gate control";

            // Act
            bool result = _plc.SetDeviceParameters(device, gsdFilePath, moduleName, null);

            // Assert
            Assert.That(result, Is.False, $"SetDeviceParameters should fail for module '{moduleName}' with null parameters.");
        }

        [Test]
        public void SetDeviceParameters_ShouldPass_WhenSafetyParametersAreSet()
        {
            // Arrange
            string gsdFilePath = Path.Combine(_testDataPath, "gsd", "GSDML-V2.42-LEUZE-RSL400P CU 4M12-20230816.xml");
            var importedDevices = _importedDevicesObj as Dictionary<string, Device>;
            // Retrieve the RSL400P device by its name
            var device = importedDevices?.Values.Skip(1).FirstOrDefault();

            string moduleName = "[M1] Safe signal";
            var safetyParameters = new Dictionary<string, object>
            {
                {"Failsafe_FSourceAddress", 500},
                {"Failsafe_FDestinationAddress", 1000},
                {"Failsafe_FMonitoringtime", 200}
            };

            // Act
            bool result = _plc.SetDeviceParameters(device, gsdFilePath, moduleName, safetyParameters, safety: true);

            // Assert
            Assert.That(result, Is.True, $"SetDeviceParameters should pass for safety module '{moduleName}'.");
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

        ~SetDeviceParametersTests()
        {
            Dispose(false);
        }
    }
}
