using NUnit.Framework;
using System;
using System.IO;
using Serilog;

namespace PLC.Commissioning.Lib.Siemens.Tests
{
    [TestFixture]
    public class InitializeTests
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

            Log.Information("Test setup completed. Test data directory: {Path}", _testDataPath);
        }

        [Test]
        // Tests that the Initialize method succeeds when the controller is 
        // correctly configured with safety mode enabled.
        public void Initialize_ShouldPass_WhenConfiguredCorrectly_WithSafety()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_testDataPath, "Configurations/valid_config.json");
            Assert.IsTrue(File.Exists(jsonFilePath), $"File not found: {jsonFilePath}");
            Log.Debug("Testing Initialize with safety enabled. Configuration file: {FilePath}", jsonFilePath);
            _plc.Configure(jsonFilePath);

            // Act
            var result = _plc.Initialize(safety: true);

            // Assert
            Assert.That(result, Is.True, "The Initialize method should succeed with safety enabled and valid configuration.");
        }

        [Test]
        // Tests that the Initialize method succeeds when the controller is 
        // correctly configured without safety mode.
        public void Initialize_ShouldPass_WhenConfiguredCorrectly_WithoutSafety()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_testDataPath, "Configurations/valid_config.json");
            Assert.IsTrue(File.Exists(jsonFilePath), $"File not found: {jsonFilePath}");
            Log.Debug("Testing Initialize without safety. Configuration file: {FilePath}", jsonFilePath);
            _plc.Configure(jsonFilePath);

            // Act
            var result = _plc.Initialize(safety: false);

            // Assert
            Assert.That(result, Is.True, "The Initialize method should succeed without safety and valid configuration.");
        }

        [Test]
        // Tests that the Initialize method fails when the project path 
        // provided in the configuration is invalid.
        public void Initialize_ShouldFail_WhenProjectPathIsInvalid()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_testDataPath, "Configurations/invalid_project_path.json");
            Assert.IsTrue(File.Exists(jsonFilePath), $"File not found: {jsonFilePath}");
            Log.Debug("Testing Initialize with an invalid project path. Configuration file: {FilePath}", jsonFilePath);
            _plc.Configure(jsonFilePath);

            // Act
            var result = _plc.Initialize(safety: false);

            // Assert
            Assert.That(result, Is.False, "The Initialize method should fail with an invalid project path.");
        }

        [Test]
        //  Tests that the Initialize method fails when the network card information
        //  is missing from the configuration.
        public void Initialize_ShouldFail_WhenNetworkCardIsMissing()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_testDataPath, "Configurations/missing_keys_config.json");
            Assert.IsTrue(File.Exists(jsonFilePath), $"File not found: {jsonFilePath}");
            Log.Debug("Testing Initialize with missing network card. Configuration file: {FilePath}", jsonFilePath);
            _plc.Configure(jsonFilePath);

            // Act
            var result = _plc.Initialize(safety: false);

            // Assert
            Assert.That(result, Is.False, "The Initialize method should fail when network card is missing.");
        }

        [Test]
        // Tests that the Initialize method fails when an incorrect 
        // network card is specified in the configuration.
        public void Initialize_ShouldFail_ToGoOnlineWhenNetworkCardIsIncorrect()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_testDataPath, "Configurations/invalid_network_card.json");
            Assert.IsTrue(File.Exists(jsonFilePath), $"File not found: {jsonFilePath}");
            Log.Debug("Testing Initialize with incorrect network card. Configuration file: {FilePath}", jsonFilePath);
            _plc.Configure(jsonFilePath);

            // Act
            var result = _plc.Initialize(safety: false);

            // Assert
            Assert.That(result, Is.False, "The Initialize method should fail with an incorrect network card.");
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
                    // Dispose the SiemensPLCController
                    _plc?.Dispose();
                    Log.Information("SiemensPLCController disposed.");
                }

                _disposed = true;
            }
        }

        ~InitializeTests()
        {
            Dispose(false);
        }
    }
}