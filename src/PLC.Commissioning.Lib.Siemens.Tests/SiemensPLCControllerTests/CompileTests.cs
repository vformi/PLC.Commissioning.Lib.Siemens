using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Serilog;

namespace PLC.Commissioning.Lib.Siemens.Tests.SiemensPLCControllerTests
{
    [TestFixture]
    public class CompileTests
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

            // Set up test data path
            _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");

            // Ensure directory exists
            if (!Directory.Exists(_testDataPath))
            {
                Directory.CreateDirectory(_testDataPath);
            }

            // Load valid configuration file
            string jsonFilePath = Path.Combine(_testDataPath, "Configurations", "valid_config.json");
            Assert.That(File.Exists(jsonFilePath), $"Configuration file should exist: {jsonFilePath}");

            // Initialize SiemensPLCController
            _plc = new SiemensPLCController();
            _plc.Configure(jsonFilePath);
            Log.Information("Test setup completed. Test data directory: {Path}", _testDataPath);
        }

        [Test]
        public void Compile_ShouldPass_WhenProjectIsCompilable()
        {
            // Arrange
            _plc.Initialize(safety: false);

            // Act 
            var compileResult = _plc.Compile();

            // Assert
            Assert.That(compileResult.IsSuccess, "PLC should compile successfully.");
        }

        [Test]
        public void Compile_ShouldFail_WhenProjectIsNotCompilable()
        {
            // Arrange
            _plc.Initialize(safety: false);

            // Simulate a compilation failure by importing safety device to a non-safety PLC -> compilation failed
            string validFilePath = Path.Combine(_testDataPath, "aml", "valid_multiple_devices.aml");
            
            var gsdFilePaths = new List<string>
            {
                Path.Combine(_testDataPath, "gsd", "GSDML-V2.41-LEUZE-BCL248i-20211213.xml"),
                Path.Combine(_testDataPath, "gsd", "GSDML-V2.42-LEUZE-RSL400P CU 4M12-20230816.xml"),
            };
            // Pass the list of GSD file paths instead of a single path
            var importResult = _plc.ImportDevices(validFilePath);

            // Act 
            var compileResult = _plc.Compile();

            // Assert
            Assert.That(compileResult.IsSuccess, Is.False, "PLC should not compile successfully.");
            Assert.That(compileResult.Reasons.Any(r => r.Message.Contains("Compilation failed")),
                "Expected error message indicating compilation failure.");
        }

        [TearDown]
        public void TearDown()
        {
            _plc.Dispose();
        }
    }
}
