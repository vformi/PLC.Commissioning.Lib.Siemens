using NUnit.Framework;
using System;
using System.IO;
using Serilog;

namespace PLC.Commissioning.Lib.Siemens.Tests
{
    [TestFixture]
    public class ConfigureTests : IDisposable
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
            _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Configurations");

            Log.Information("Test setup completed. Test data directory: {Path}", _testDataPath);
        }

        [Test]
        public void Configure_ShouldReturnTrue_WhenJsonIsValid()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_testDataPath, "valid_config.json");
            Log.Debug("Testing valid configuration file: {FilePath}", jsonFilePath);

            // Act
            var result = _plc.Configure(jsonFilePath);

            // Assert
            Assert.IsTrue(result, "The Configure method should return true for a valid JSON configuration.");
        }

        [Test]
        public void Configure_ShouldReturnFalse_WhenJsonIsMissingKeys()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_testDataPath, "missing_keys_config.json");
            Log.Debug("Testing configuration file missing keys: {FilePath}", jsonFilePath);

            // Act
            var result = _plc.Configure(jsonFilePath);

            // Assert
            Assert.IsFalse(result, "The Configure method should return false for a JSON configuration missing required keys.");
        }

        [Test]
        public void Configure_ShouldReturnFalse_WhenFilePathIsInvalid()
        {
            // Arrange
            string invalidFilePath = Path.Combine(_testDataPath, "non_existent_config.json");
            Log.Debug("Testing invalid configuration file path: {FilePath}", invalidFilePath);

            // Act
            var result = _plc.Configure(invalidFilePath);

            // Assert
            Assert.IsFalse(result, "The Configure method should return false for a nonexistent file path.");
        }

        [Test]
        public void Configure_ShouldReturnFalse_WhenJsonIsMalformed()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_testDataPath, "malformed_config.json");
            Log.Debug("Testing malformed configuration file: {FilePath}", jsonFilePath);

            // Act
            var result = _plc.Configure(jsonFilePath);

            // Assert
            Assert.IsFalse(result, "The Configure method should return false for a malformed JSON file.");
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

        ~ConfigureTests()
        {
            Dispose(false);
        }
    }
}
