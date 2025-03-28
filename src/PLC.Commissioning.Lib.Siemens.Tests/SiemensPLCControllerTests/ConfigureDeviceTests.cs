using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PLC.Commissioning.Lib.Abstractions.Enums;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware;
using Serilog;
using Siemens.Engineering.HW;

namespace PLC.Commissioning.Lib.Siemens.Tests.SiemensPLCControllerTests
{
    [TestFixture]
    public class ConfigureDeviceTests
    {
        private SiemensPLCController _plc;
        private string _testDataPath;
        private Dictionary<string, object> _importedDevices;

        [SetUp]
        public void SetUp()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
            _plc = new SiemensPLCController();
            string jsonFilePath = Path.Combine(_testDataPath, "Configurations", "valid_config.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);

            // Define list of GSD file paths
            var gsdFilePaths = new List<string>
            {
                Path.Combine(_testDataPath, "gsd", "GSDML-V2.41-LEUZE-BCL248i-20211213.xml")
            };

            string validFilePath = Path.Combine(_testDataPath, "aml", "valid_multiple_devices.aml");

            // Use ImportDevices method
            var importResult = _plc.ImportDevices(validFilePath);
            _importedDevices = importResult.Value;

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
        public void ConfigureDevice_ShouldSucceed_WhenValidParametersAreProvided_WithImport()
        {
            Assert.That(_importedDevices, Is.Not.Null.And.Not.Empty, "Imported devices should not be null or empty.");
            var importedDevice = _importedDevices.Values.FirstOrDefault();

            Assert.That(importedDevice, Is.Not.Null, "The first imported device should not be null.");
            var parametersToConfigure = new Dictionary<string, object>
            {
                { "ipAddress", "192.168.60.100" },
                { "profinetName", "dut" }
            };

            var result = _plc.ConfigureDevice(importedDevice, parametersToConfigure);

            Assert.That(result.IsSuccess, Is.True, "Configuration should succeed.");
        }

        [Test]
        public void ConfigureDevice_ShouldFail_WhenDeviceIsNull()
        {
            var parametersToConfigure = new Dictionary<string, object>
            {
                { "ipAddress", "192.168.60.100" },
                { "profinetName", "dut" }
            };

            var result = _plc.ConfigureDevice(null, parametersToConfigure);

            Assert.That(result.IsSuccess, Is.False, "Configuration should fail when device is null.");
            Assert.That(result.Errors, Is.Not.Empty, "Result should contain an error message.");
            Assert.That(result.Errors.First().Metadata["ErrorCode"], Is.EqualTo(OperationErrorCode.ConfigurationFailed), "Error code should indicate configuration failure.");
        }

        [Test]
        public void ConfigureDevice_ShouldFail_WhenDeviceIsIncorrectType()
        {
            var parametersToConfigure = new Dictionary<string, object>
            {
                { "ipAddress", "192.168.60.100" },
                { "profinetName", "dut" }
            };
            object incorrectDevice = new { Name = "IncorrectDevice" };

            var result = _plc.ConfigureDevice(incorrectDevice, parametersToConfigure);

            Assert.That(result.IsSuccess, Is.False, "Configuration should fail for incorrect device type.");
            Assert.That(result.Errors, Is.Not.Empty, "Result should contain an error message.");
            Assert.That(result.Errors.First().Metadata["ErrorCode"], Is.EqualTo(OperationErrorCode.ConfigurationFailed), "Error code should indicate configuration failure.");
        }

        [Test]
        public void ConfigureDevice_ShouldFail_WhenParametersToConfigureIsEmpty()
        {
            var importedDevice = _importedDevices?.Values.FirstOrDefault();
            var parametersToConfigure = new Dictionary<string, object>();
            var result = _plc.ConfigureDevice(importedDevice, parametersToConfigure);

            Assert.That(result.IsSuccess, Is.False, "Configuration should fail when parameters dictionary is empty.");
            Assert.That(result.Errors, Is.Not.Empty, "Result should contain an error message.");
            Assert.That(result.Errors.First().Metadata["ErrorCode"], Is.EqualTo(OperationErrorCode.ConfigurationFailed), "Error code should indicate configuration failure.");
        }

        [Test]
        public void ConfigureDevice_ShouldFail_WhenRequiredParametersAreMissing()
        {
            var importedDevice = _importedDevices?.Values.FirstOrDefault();
            var parametersToConfigure = new Dictionary<string, object>();
            var result = _plc.ConfigureDevice(importedDevice, parametersToConfigure);

            Assert.That(result.IsSuccess, Is.False, "Configuration should fail when required parameters are missing.");
            Assert.That(result.Errors, Is.Not.Empty, "Result should contain an error message.");
            Assert.That(result.Errors.First().Metadata["ErrorCode"], Is.EqualTo(OperationErrorCode.ConfigurationFailed), "Error code should indicate configuration failure.");
        }
    }
}
