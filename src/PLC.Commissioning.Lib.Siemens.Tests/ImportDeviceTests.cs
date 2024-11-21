using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Serilog;
using Siemens.Engineering.HW;

namespace PLC.Commissioning.Lib.Siemens.Tests
{
    [TestFixture]
    public class ImportDeviceTests : IDisposable
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

            Log.Information("Test setup completed. Test data directory: {Path}", _testDataPath);
        }
        
        [Test]
        // Tests that the ImportDevice method returns null 
        // when called before the controller is initialized.
        public void ImportDevice_ShouldReturnNull_WhenNotInitialized()
        {
            // Arrange
            string deviceConfigFilePath = Path.Combine(_testDataPath, "aml", "valid_single_device.aml");

            // Act - Without initializing _plc or setting up any handlers
            var result = _plc.ImportDevice(deviceConfigFilePath);

            // Assert
            Assert.That(result, Is.Null, "The result should be null when the PLC is not initialized.");
        }

        [Test]
        // Tests that the ImportDevice method returns null when an 
        // invalid device configuration file path is provided.
        public void ImportDevice_ShouldReturnNull_WhenFilePathIsInvalid()
        {
            // Arrange
            string invalidFilePath = "nonExistentDeviceConfigFile.xml";
            _plc.Initialize(safety: false);

            // Act
            var result = _plc.ImportDevice(invalidFilePath);

            // Assert
            Assert.That(result, Is.Null, "The result should be null when an invalid file path is provided.");
        }

        [Test]
        // Tests that the ImportDevice method successfully imports
        // a device when provided with a valid device configuration file.
        public void ImportDevice_ShouldReturnDevice_WhenFileIsCorrect()
        {
            // Arrange
            string validFilePath = Path.Combine(_testDataPath, "aml", "valid_multiple_devices.aml");
            _plc.Initialize(safety: false);

            // Act
            var importedDevicesObj = _plc.ImportDevice(validFilePath);
            var importedDevices = importedDevicesObj as Dictionary<string, Device>;
            // Asserty
            Assert.That(importedDevices, Is.Not.Null, "The returned dictionary should not be null.");
            Assert.That(importedDevices.Count, Is.GreaterThan(0),
                "The returned dictionary should have at least one device.");

            foreach (var deviceObj in importedDevices.Values)
            {
                var device = deviceObj as Device;
                Assert.That(device, Is.Not.Null, "Each device in the dictionary should be of type 'Device'.");
                Assert.That(device.Name, Is.Not.Null.And.Not.Empty, "Each device should have a valid name.");
            }
        }

        [Test]
        // Tests that the ImportDevice method returns null when an incorrect or invalid 
        // device configuration file is provided.
        public void ImportDevice_ShouldReturnNull_WhenFileIsIncorrect()
        {
            // Arrange
            string invalidFilePath = Path.Combine(_testDataPath, "aml", "invalid.aml");
            _plc.Initialize(safety: false);

            // Act
            var result = _plc.ImportDevice(invalidFilePath);

            // Assert
            Assert.That(result, Is.Null, "The result should be null when an incorrect file is provided.");
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

        ~ImportDeviceTests()
        {
            Dispose(false);
        }
    }
}