using NUnit.Framework;
using System;
using System.IO;
using System.Collections.Generic;
using Serilog;
using System.Linq;
using System.Threading;

namespace PLC.Commissioning.Lib.Siemens.Tests
{
    [TestFixture]
    public class ProjectManagementTests : IDisposable
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

            // Set up the test data path
            _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
            string jsonFilePath = Path.Combine(_testDataPath, "Configurations", "valid_config.json");
            _plc.Configure(jsonFilePath);

            Log.Information("Test setup completed. Test data directory: {Path}", _testDataPath);
        }

        [Test]
        public void Export_ShouldPass_WhenProjectIsValid()
        {
            // Arrange
            _plc.Initialize(safety: false);

            string exportPath =
                "C:\\Users\\Legion\\Documents\\CODING\\git\\projects\\dt.PLC.Commissioning.Lib\\src\\submodules\\Siemens\\src\\PLC.Commissioning.Lib.Siemens.Tests\\TestData\\Export";

            var filesToExport = new Dictionary<string, object>
                {
                    { "plcTags", exportPath }
                };

            // Act
            bool result = _plc.Export(filesToExport);

            // Assert
            Assert.That(result, Is.True, "Export should return true when export is successful.");
        }

        [Test]
        public void SaveProjectAs_ShouldPass_WhenPathIsCorrect()
        {
            // Arrange
            _plc.Initialize(safety: false);

            string saveName = "ProjectAAA";

            // Act
            bool result = _plc.SaveProjectAs(saveName);

            // Assert
            Assert.That(result, Is.True, "SaveProjectAs should return true when the project is saved successfully.");
        }

        [Test]
        public void AdditionalImport_ShouldPass_WhenPathIsCorrect()
        {
            // Arrange
            _plc.Initialize(safety: false);

            string importPath = "C:\\Users\\vformane\\Desktop\\Export\\Default tag table.xml";
            var filesToImport = new Dictionary<string, object>
            {
                { "plcTags", importPath }
            };

            // Act
            bool result = _plc.AdditionalImport(filesToImport);

            // Assert
            Assert.That(result, Is.True, "AdditionalImport should return true when the import succeeds.");
        }

        [TearDown]
        public void TearDown()
        {
            if (!_disposed)
            {
                Dispose(); // Ensure resources are disposed
            }
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
                    // Dispose resources
                    Thread.Sleep(5000); // Allow TIA Portal to close properly
                    _plc?.Dispose();
                    Log.Information("Test resources have been disposed.");
                }
                _disposed = true;
            }
        }

        ~ProjectManagementTests()
        {
            Dispose(false);
        }
    }
}
