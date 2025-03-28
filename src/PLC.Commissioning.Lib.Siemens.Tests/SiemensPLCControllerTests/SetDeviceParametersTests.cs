using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PLC.Commissioning.Lib.Abstractions.Enums;
using Serilog;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware;

namespace PLC.Commissioning.Lib.Siemens.Tests.SiemensPLCControllerTests
{
    [TestFixture]
    public class SetDeviceParametersTests
    {
        private SiemensPLCController _plc;
        private string _testDataPath;

        // This will hold our imported devices dictionary from ImportDevices()
        // which now returns Dictionary<string, object>.
        private Dictionary<string, object> _importedDevices;

        [SetUp]
        public void SetUp()
        {
            // Configure Serilog for test
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            // Prepare test data directory
            _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");

            // Initialize & configure the SiemensPLCController
            _plc = new SiemensPLCController();
            string jsonFilePath = Path.Combine(_testDataPath, "Configurations", "valid_config.json");
            _plc.Configure(jsonFilePath);

            // Open the TIA project (offline)
            _plc.Initialize(safety: false);

            // Import devices from an AML file
            string validFilePath = Path.Combine(_testDataPath, "aml", "valid_multiple_devices.aml");
            var importResult = _plc.ImportDevices(validFilePath);

            // Ensure it’s actually a Dictionary<string, object>
            _importedDevices = importResult.Value;
            Assert.That(_importedDevices, Is.Not.Null,
                "Failed to import devices or returned result is not Dictionary<string, object>.");

            Log.Information("Test setup completed. Test data directory: {Path}", _testDataPath);
        }

        [TearDown]
        public void TearDown()
        {
            _plc.Dispose();
            _plc = null;
            Log.Information("Controller disposed at test tear-down.");
        }


        [Test]
        public void SetDeviceParameters_ShouldPass_WhenValidParametersAreProvided()
        {
            // Arrange
            var importedDevice = _importedDevices.Values.FirstOrDefault() as ProjectDevice;
            Assert.That(importedDevice, Is.Not.Null, "No valid ImportedDevice in dictionary.");

            string moduleName = "[M11] Reading gate control";

            var parametersToSet = new Dictionary<string, object>
            {
                { "Automatic reading gate repeat", "yes" },
                { "Reading gate end mode / completeness mode", 3 },
                { "Restart delay", 333 },
                { "Max. reading gate time when scanning", 762 }
            };

            // Act
            var result = _plc.SetDeviceParameters(importedDevice, moduleName, parametersToSet);

            // Assert
            Assert.That(result.IsSuccess, Is.True, $"SetDeviceParameters should pass for module '{moduleName}'.");
        }


        [Test]
        public void SetDeviceParameters_ShouldFail_WhenInvalidParameterValues()
        {
            // Arrange
            var importedDevice = _importedDevices.Values.FirstOrDefault() as ProjectDevice;
            Assert.That(importedDevice, Is.Not.Null);

            string moduleName = "[M11] Reading gate control";
            var invalidParameters = new Dictionary<string, object>
            {
                { "Automatic reading gate repeat", "yes" },
                { "Reading gate end mode / completeness mode", 3 },
                { "Restart delay", 1000000 }, // Out of acceptable range
                { "Max. reading gate time when scanning", 655555 } // Invalid value
            };

            // Act
            var result = _plc.SetDeviceParameters(importedDevice, moduleName, invalidParameters);

            // Assert
            Assert.That(result.IsFailed, Is.True,
                $"SetDeviceParameters should fail for module '{moduleName}' with invalid values.");
            Assert.That(result.Errors, Is.Not.Empty, "The result should contain error messages.");
            Assert.That(result.Errors.First().Metadata["ErrorCode"],
                Is.EqualTo(OperationErrorCode.SetParametersFailed));
        }


        [Test]
        public void SetDeviceParameters_ShouldFail_WhenInvalidModuleNameIsProvided()
        {
            // Arrange
            var importedDevice = _importedDevices.Values.FirstOrDefault() as ProjectDevice;
            Assert.That(importedDevice, Is.Not.Null);

            string invalidModuleName = "[M999] Invalid module";
            var parametersToSet = new Dictionary<string, object> { { "Parameter", "someValue" } };

            // Act
            var result = _plc.SetDeviceParameters(importedDevice, invalidModuleName, parametersToSet);

            // Assert
            Assert.That(result.IsFailed, Is.True,
                $"SetDeviceParameters should fail when module name '{invalidModuleName}' does not exist.");
            Assert.That(result.Errors.First().Metadata["ErrorCode"],
                Is.EqualTo(OperationErrorCode.SetParametersFailed));
        }


        [Test]
        public void SetDeviceParameters_ShouldFail_WhenParametersAreNull()
        {
            // Arrange
            var importedDevice = _importedDevices.Values.FirstOrDefault() as ProjectDevice;
            Assert.That(importedDevice, Is.Not.Null);

            string moduleName = "[M11] Reading gate control";

            // Act
            var result = _plc.SetDeviceParameters(importedDevice, moduleName, null);

            // Assert
            Assert.That(result.IsFailed, Is.True,
                "SetDeviceParameters should fail if the provided parameters dictionary is null.");
            Assert.That(result.Errors.First().Metadata["ErrorCode"],
                Is.EqualTo(OperationErrorCode.SetParametersFailed));
        }


        [Test]
        public void SetDeviceParameters_ShouldPass_WhenSafetyParametersAreSet()
        {
            // Grab second device. Adjust if your actual device name differs.
            var importedDevice = _importedDevices.Last().Value as ProjectDevice;
            Assert.That(importedDevice, Is.Not.Null, "Could not find a second ImportedDevice.");

            string moduleName = "[M1] Safe signal";
            var safetyParameters = new Dictionary<string, object>
            {
                { "Failsafe_FSourceAddress", 500 },
                { "Failsafe_FDestinationAddress", 1000 },
                { "Failsafe_FMonitoringtime", 200 }
            };

            // Act
            var result = _plc.SetDeviceParameters(importedDevice, moduleName, safetyParameters, safety: true);

            // Assert
            Assert.That(result.IsSuccess, Is.True, $"SetDeviceParameters should pass for module '{moduleName}'.");
        }
    }
}
