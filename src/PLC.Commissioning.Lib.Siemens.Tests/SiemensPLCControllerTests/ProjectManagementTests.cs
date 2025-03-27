using NUnit.Framework;
using System;
using System.IO;
using System.Collections.Generic;
using Serilog;

namespace PLC.Commissioning.Lib.Siemens.Tests.SiemensPLCControllerTests
{
    [TestFixture]
    public class ProjectManagementTests
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

            // Initialize the SiemensPLCController
            _plc = new SiemensPLCController();

            // Set up the test data path, ensuring all file paths are relative to the project directory
            _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");

            // Ensure the directory exists
            if (!Directory.Exists(_testDataPath))
            {
                Directory.CreateDirectory(_testDataPath);
            }

            // Load configuration for PLC
            string jsonFilePath = Path.Combine(_testDataPath, "Configurations", "valid_config.json");
            Assert.That(File.Exists(jsonFilePath), "Configuration file should exist.");

            _plc.Configure(jsonFilePath);
            Log.Information("Test setup completed. Test data directory: {Path}", _testDataPath);
        }

        [Test]
        public void Export_ShouldPass_WhenProjectIsValid()
        {
            // Arrange
            _plc.Initialize(safety: false);

            // Define export path inside test data directory
            string exportPath = Path.Combine(_testDataPath, "Export");

            // Ensure export directory exists
            if (!Directory.Exists(exportPath))
            {
                Directory.CreateDirectory(exportPath);
            }

            var filesToExport = new Dictionary<string, object>
            {
                { "plcTags", exportPath }
            };

            // Act
            var result = _plc.Export(filesToExport);

            // Assert
            Assert.That(result.IsSuccess, "Export should return true when successful.");

            // Validate that the export directory contains files
            Assert.That(Directory.GetFiles(exportPath).Length > 0, "Export directory should contain files.");
        }

        [Test]
        public void SaveProjectAs_ShouldPass_WhenPathIsCorrect()
        {
            // Arrange
            _plc.Initialize(safety: false);

            string saveName = "TestProject";

            // Act
            var result = _plc.SaveProjectAs(saveName);

            // Assert
            Assert.That(result.IsSuccess, "SaveProjectAs should return true when successful.");
        }

        [Test]
        public void AdditionalImport_ShouldPass_WhenPathIsCorrect()
        {
            // Arrange
            _plc.Initialize(safety: false);

            // Define import path inside test data directory
            string importPath = Path.Combine(_testDataPath, "Import", "DefaultTagTable.xml");

            // Ensure file exists before attempting import
            Assert.That(File.Exists(importPath), "Import file should exist: " + importPath);

            var filesToImport = new Dictionary<string, object>
            {
                { "plcTags", importPath }
            };

            // Act
            var result = _plc.AdditionalImport(filesToImport);

            // Assert
            Assert.That(result.IsSuccess, "AdditionalImport should return true when successful.");
        }

        [TearDown]
        public void TearDown()
        {
            if (!_disposed)
            {
                _plc.Dispose(); // Ensure resources are disposed
            }
        }
    }
}
