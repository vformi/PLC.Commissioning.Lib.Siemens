using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Serilog;
using FluentResults;
using PLC.Commissioning.Lib.Abstractions.Enums;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware;

namespace PLC.Commissioning.Lib.Siemens.Tests.SiemensPLCControllerTests
{
    [TestFixture]
    public class ImportDeviceTests
    {
        private SiemensPLCController _plc;
        private string _testDataPath;

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

            Log.Information("Test setup completed. Test data directory: {Path}", _testDataPath);
        }

        [Test]
        public void ImportDevices_ShouldFail_WhenNotInitialized()
        {
            string deviceConfigFilePath = Path.Combine(_testDataPath, "aml", "valid_single_device.aml");
            var gsdmlFiles = new List<string>();

            var result = _plc.ImportDevices(deviceConfigFilePath, gsdmlFiles);

            Assert.That(result.IsFailed, Is.True, "ImportDevices should fail if PLC is not initialized.");
            Assert.That(result.Errors.First().Metadata["ErrorCode"], Is.EqualTo(OperationErrorCode.ImportFailed));
        }

        [Test]
        public void ImportDevices_ShouldFail_WhenFilePathIsInvalid()
        {
            string invalidFilePath = "nonExistentDeviceConfigFile.xml";
            var gsdmlFiles = new List<string>();
            _plc.Initialize(safety: false);

            var result = _plc.ImportDevices(invalidFilePath, gsdmlFiles);

            Assert.That(result.IsFailed, Is.True, "ImportDevices should fail if the AML file path is invalid.");
            Assert.That(result.Errors.First().Metadata["ErrorCode"], Is.EqualTo(OperationErrorCode.ImportFailed));
        }

        [Test]
        public void ImportDevices_ShouldFail_WhenFileIsIncorrect()
        {
            string invalidFilePath = Path.Combine(_testDataPath, "aml", "invalid.aml");
            var gsdmlFiles = new List<string>();
            _plc.Initialize(safety: false);

            var result = _plc.ImportDevices(invalidFilePath, gsdmlFiles);

            Assert.That(result.IsFailed, Is.True, "ImportDevices should fail when an incorrect AML file is provided.");
            Assert.That(result.Errors.First().Metadata["ErrorCode"], Is.EqualTo(OperationErrorCode.ImportFailed));
        }

        [Test]
        public void ImportDevices_ShouldSucceed_WhenFileIsValid()
        {
            string validFilePath = Path.Combine(_testDataPath, "aml", "valid_multiple_devices.aml");
            var gsdmlFiles = new List<string>
            {
                Path.Combine(_testDataPath, "gsd", "GSDML-V2.41-LEUZE-BCL248i-20211213.xml"),
                Path.Combine(_testDataPath, "gsd", "GSDML-V2.42-LEUZE-RSL400P CU 4M12-20230816.xml")
            };

            _plc.Initialize(safety: false);
            var result = _plc.ImportDevices(validFilePath, gsdmlFiles);

            Assert.That(result.IsSuccess, Is.True, "ImportDevices should return a successful result.");
            Assert.That(result.Value, Is.Not.Null, "ImportDevices should return a dictionary of imported devices.");
            Assert.That(result.Value, Is.InstanceOf<Dictionary<string, object>>(), "Result should be a dictionary.");
            Assert.That(result.Value.Count, Is.GreaterThan(0), "Expected at least one device to be imported.");
        }

        [Test]
        public void ImportDevices_ShouldCreatePLCTagTables_WhenDevicesImported()
        {
            string validFilePath = Path.Combine(_testDataPath, "aml", "valid_multiple_devices.aml");
            var gsdmlFiles = new List<string>
            {
                Path.Combine(_testDataPath, "gsd", "GSDML-V2.41-LEUZE-BCL248i-20211213.xml"),
                Path.Combine(_testDataPath, "gsd", "GSDML-V2.42-LEUZE-RSL400P CU 4M12-20230816.xml")
            };

            _plc.Initialize(safety: false);
            var importResult = _plc.ImportDevices(validFilePath, gsdmlFiles);

            Assert.That(importResult.IsSuccess, Is.True, "ImportDevices should succeed.");
            Assert.That(importResult.Value, Is.Not.Null, "Imported devices should not be null.");
            Assert.That(importResult.Value.Count, Is.GreaterThan(0), "At least one device should be imported.");

            var tagTablesResult = _plc.ReadPLCTagTables();
            Assert.That(tagTablesResult.IsSuccess, Is.True, "Reading PLC tag tables should succeed.");
            Assert.That(tagTablesResult.Value, Is.Not.Null.And.Not.Empty, "PLC tag tables should be created after importing devices.");

            var tagTables = tagTablesResult.Value;
            Console.WriteLine("Retrieved PLC Tag Tables:");
            
            foreach (var group in tagTables)
            {
                Console.WriteLine($"Group: {group.Key}");
                foreach (var table in group.Value)
                {
                    Console.WriteLine($"  TagTable: {table}");
                }
            }
            
            // Validate BCL248i group
            Assert.That(tagTables.ContainsKey("BCL248i"), Is.True, "BCL248i group should exist.");
            var bclTagTables = tagTables["BCL248i"];
            Assert.That(bclTagTables, Does.Contain("BCL248i_[M10]_Activation"));
            Assert.That(bclTagTables, Does.Contain("BCL248i_[M60]_Device_status"));
            Assert.That(bclTagTables, Does.Contain("BCL248i_[M20]_Decoder_state"));
            Assert.That(bclTagTables, Does.Contain("BCL248i_[M22]_Decoding_result_2_(8_bytes)"));
            
            // Validate RSL400P group
            Assert.That(tagTables.ContainsKey("RSL400P"), Is.True, "RSL400P group should exist.");
            var rslTagTables = tagTables["RSL400P"];
            Assert.That(rslTagTables, Does.Contain("RSL400P_[M1]_Safe_signal"));
            Assert.That(rslTagTables, Does.Contain("RSL400P_[M2]_System_status"));
            Assert.That(rslTagTables, Does.Contain("RSL400P_[M3]_Scan_Number"));
            Assert.That(rslTagTables, Does.Contain("RSL400P_[M4]_Reflector_Status"));
            Assert.That(rslTagTables, Does.Contain("RSL400P_[M5]_Protective_function_A_status"));
            Assert.That(rslTagTables, Does.Contain("RSL400P_[M6]_Protective_function_B_status"));
            Assert.That(rslTagTables, Does.Contain("RSL400P_[M7]_Protective_function_A_violation"));
            Assert.That(rslTagTables, Does.Contain("RSL400P_[M8]_Protective_function_B_violation"));
        }
        
        [Test]
        public void ImportDevices_ShouldFail_WhenOneOfMultipleDevicesIsMissingGSD()
        {
            // Arrange
            string validFilePath = Path.Combine(_testDataPath, "aml", "valid_multiple_devices.aml");
            var gsdFilePaths = new List<string>
            {
                // Only one GSD file even though the AML has two devices
                Path.Combine(_testDataPath, "gsd", "GSDML-V2.41-LEUZE-BCL248i-20211213.xml"),
            };

            _plc.Initialize(safety: false);

            // Act
            var result = _plc.ImportDevices(validFilePath, gsdFilePaths);

            // Assert
            Assert.That(result.IsFailed, Is.True, 
                "ImportDevices should fail if not all GSD files are provided for multiple devices.");

            // You can further check the error message to ensure it mentions the missing device
            var error = result.Errors.First();
            StringAssert.Contains("could not be imported", error.Message, 
                "Error should mention that at least one device is missing its GSD file.");
            StringAssert.Contains("RSL400P", error.Message, 
                "Error message should indicate the missing RSL400P GSD if that is the device in the AML.");
        }


        [TearDown]
        public void TearDown()
        {
            _plc.Dispose();
            Log.Information("SiemensPLCController disposed.");
        }
    }
}
