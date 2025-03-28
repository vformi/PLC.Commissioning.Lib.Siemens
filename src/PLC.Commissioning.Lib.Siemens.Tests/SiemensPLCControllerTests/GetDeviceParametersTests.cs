using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Serilog;
using Siemens.Engineering.HW;
using FluentResults;
using PLC.Commissioning.Lib.Abstractions.Enums;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware;

namespace PLC.Commissioning.Lib.Siemens.Tests.SiemensPLCControllerTests
{
    [TestFixture]
    public class GetDeviceParametersTests
    {
        private SiemensPLCController _plc;
        private string _testDataPath;
        private Dictionary<string, object> _devices;
            
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

            // Define list of GSD file paths
            var gsdFilePaths = new List<string>
            {
                Path.Combine(_testDataPath, "gsd", "GSDML-V2.41-LEUZE-BCL248i-20211213.xml")
            };

            string validFilePath = Path.Combine(_testDataPath, "aml", "valid_multiple_devices.aml");

            // Pass the list of GSD file paths instead of a single path
            var importResult = _plc.ImportDevices(validFilePath);
            _devices = importResult.Value;

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
        public void GetDeviceParameters_ShouldPass_WhenValidModuleNameIsProvided()
        {
            // Arrange
            var device = _devices.First().Value as ProjectDevice;
            Assert.That(device, Is.Not.Null);

            // Act
            var result = _plc.GetDeviceParameters(device, "[M11] Reading gate control");
            Log.Information("GetDeviceParameters returned: {Result}", result);

            // Assert
            Assert.That(result.IsSuccess, Is.True, $"GetDeviceParameters should pass for device '{device}'.");
            Assert.That(result.Value, Is.Not.Null, "The returned dictionary should not be null.");
        }

        [Test]
        public void GetDeviceParameters_ShouldPass_WhenSpecificValidParametersAreProvided()
        {
            // Arrange
            var device = _devices.First().Value as ProjectDevice;
            Assert.That(device, Is.Not.Null);
            List<string> parametersToRead = new List<string>
            {
                "Automatic reading gate repeat",
                "Reading gate end mode / completeness mode",
                "Restart delay"
            };

            // Act
            var result = _plc.GetDeviceParameters(device, "[M11] Reading gate control", parametersToRead);

            // Assert
            Assert.That(result.IsSuccess, Is.True, $"GetDeviceParameters should pass for device '{device}'.");
            Assert.That(result.Value, Is.Not.Null, "The returned dictionary should not be null.");
            Assert.That(result.Value.Keys, Is.SupersetOf(parametersToRead), "Returned dictionary should contain all requested parameters.");
        }

        [Test]
        public void GetDeviceParameters_ShouldFail_WhenSpecificInvalidParametersAreProvided()
        {
            // Arrange
            var device = _devices.First().Value as ProjectDevice;
            Assert.That(device, Is.Not.Null);
            List<string> invalidParameters = new List<string>
            {
                "Invalid parameter 1",
                "Invalid parameter 2"
            };
            
            // Act
            var result = _plc.GetDeviceParameters(device, "[M11] Reading gate control", invalidParameters);

            // Assert
            Assert.That(result.IsFailed, Is.True, $"GetDeviceParameters should fail for device '{device}'.");
            var errorCode = result.Errors.FirstOrDefault()?.Metadata["ErrorCode"];
            Assert.That(errorCode, Is.EqualTo(OperationErrorCode.GetParametersFailed));
            Assert.That(result.Errors, Is.Not.Empty, "The result should contain error messages.");
            
        }

        [Test]
        public void GetDeviceParameters_ShouldFail_WhenInvalidModuleNameIsProvided()
        {
            // Arrange
            var device = _devices.First().Value as ProjectDevice;
            Assert.That(device, Is.Not.Null);
            // Act
            var result = _plc.GetDeviceParameters(device, "[M999] Non-existent module");

            // Assert
            Assert.That(result.IsFailed, Is.True, $"GetDeviceParameters should fail for non-existent module '[M999]'.");
            var errorCode = result.Errors.FirstOrDefault()?.Metadata["ErrorCode"];
            Assert.That(errorCode, Is.EqualTo(OperationErrorCode.GetParametersFailed));
            Assert.That(result.Errors, Is.Not.Empty, "The result should contain error messages.");
        }
    }
}
