using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Serilog;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware;

namespace PLC.Commissioning.Lib.Siemens.Tests.SiemensPLCControllerTests
{
    [TestFixture]
    public class DeleteDeviceTests
    {
        private SiemensPLCController _plc;
        private bool _disposed;
        private string _testDataPath;

        [SetUp]
        public void SetUp()
        {
            // Configure Serilog for testing
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            // Prepare test data directory
            _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");

            // Ensure directory exists
            if (!Directory.Exists(_testDataPath))
            {
                Directory.CreateDirectory(_testDataPath);
            }

            // Initialize & configure the SiemensPLCController
            _plc = new SiemensPLCController();
            string jsonFilePath = Path.Combine(_testDataPath, "Configurations", "valid_config.json");
            Assert.That(File.Exists(jsonFilePath), $"Config file should exist: {jsonFilePath}");

            _plc.Configure(jsonFilePath);

            // Open the TIA project in offline mode
            _plc.Initialize(safety: false);

            Log.Information("DeleteDeviceTests SetUp completed. Test data directory: {Path}", _testDataPath);
        }

        [Test]
        public void DeleteDevice_ShouldFail_WhenDeviceIsNull()
        {
            // Act
            var result = _plc.DeleteDevice(null);

            // Assert
            Assert.That(result.IsSuccess, Is.False, "DeleteDevice should fail when a null device is passed.");
        }

        [Test]
        public void DeleteDevice_ShouldFail_WhenDeviceIsNotImportedDevice()
        {
            // Arrange: Create some random object that is not an ImportedDevice
            var invalidDevice = new { Name = "InvalidType" };

            // Act
            var result = _plc.DeleteDevice(invalidDevice);

            // Assert
            Assert.That(result.IsSuccess, Is.False, "DeleteDevice should fail when passed an object that is not an ImportedDevice.");
        }

        [Test]
        public void DeleteDevice_ShouldSucceed_WhenDeviceIsImportedAndProjectContainsIt()
        {
            // Arrange
            string amlFilePath = Path.Combine(_testDataPath, "aml", "valid_multiple_devices.aml");
            var gsdFiles = new List<string>
            {
                Path.Combine(_testDataPath, "gsd", "GSDML-V2.41-LEUZE-BCL248i-20211213.xml"),
            };

            Assert.That(File.Exists(amlFilePath), $"AML file should exist: {amlFilePath}");
            Assert.That(gsdFiles.All(File.Exists), "GSDML files should exist for device import.");

            var importResult = _plc.ImportDevices(amlFilePath, gsdFiles);
            Assert.That(importResult, Is.Not.Null, "ImportDevices should not return null.");

            var importedDevices = importResult.Value as Dictionary<string, object>;
            Assert.That(importedDevices, Is.Not.Null, "ImportDevices should return a valid Dictionary<string, object>.");
            Assert.That(importedDevices.Count, Is.GreaterThan(0), "Expected at least one device to have been imported.");

            var deviceToDelete = importedDevices.Values.FirstOrDefault() as ImportedDevice;
            Assert.That(deviceToDelete, Is.Not.Null, "No valid ImportedDevice found in the imported devices.");

            var deviceCheck = _plc.GetDeviceByName(deviceToDelete.DeviceName);
            Assert.That(deviceCheck, Is.Not.Null, $"Device '{deviceToDelete.DeviceName}' should exist in the project before deletion.");

            // Act
            var deleteResult = _plc.DeleteDevice(deviceToDelete);

            // Assert
            Assert.That(deleteResult.IsSuccess, Is.True, $"DeleteDevice should succeed for device '{deviceToDelete.DeviceName}'.");

            // Validate device is deleted
            var deviceCheckAfterDelete = _plc.GetDeviceByName(deviceToDelete.DeviceName);
            Assert.That(deviceCheckAfterDelete.IsSuccess, Is.False, "Device should no longer exist in the project after deletion.");
            Assert.That(deviceCheckAfterDelete.Reasons.Any(r => r.Message.Contains("was not found")), 
                "Expected error message indicating the device was not found.");
        }

        [Test]
        public void DeleteDevice_ShouldFail_WhenDeviceDoesNotExistInProject()
        {
            // Arrange
            string nonExistentDeviceName = "FakeDevice_999";

            // Ensure that the device does not exist
            var deviceCheck = _plc.GetDeviceByName(nonExistentDeviceName);
            Assert.That(deviceCheck.IsSuccess, Is.False, $"Device '{nonExistentDeviceName}' should not exist in the project.");

            // Attempt to delete a non-existent device
            var deleteResult = _plc.DeleteDevice(nonExistentDeviceName);

            // Assert
            Assert.That(deleteResult.IsSuccess, Is.False);
        }


        [TearDown]
        public void TearDown()
        {
            _plc.Dispose();
        }
    }
}
