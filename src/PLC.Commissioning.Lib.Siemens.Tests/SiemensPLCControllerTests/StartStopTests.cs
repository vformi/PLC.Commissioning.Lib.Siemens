using System;
using System.IO;
using NUnit.Framework;
using Serilog;

namespace PLC.Commissioning.Lib.Siemens.Tests.SiemensPLCControllerTests
{
    [TestFixture]
    public class StartStopTests
    {
        private SiemensPLCController _plc;
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

            // Initialize SiemensPLCController
            _plc = new SiemensPLCController();
            string jsonFilePath = Path.Combine(_testDataPath, "Configurations", "valid_config.json");
            _plc.Configure(jsonFilePath);
            
            Log.Information("Test setup completed. Test data directory: {Path}", _testDataPath);
        }
    
        [Test]
        public void StartPLC_ShouldPass_WhenCalledWithoutSafety()
        {
            // Arrange
            _plc.Initialize(safety: false);

            // Act
            var result = _plc.Start();

            // Assert
            Assert.That(result.IsSuccess, Is.True, "PLC should start successfully without safety mode.");
        }

        [Test]
        public void StartPLC_ShouldPass_WhenCalledWithSafety()
        {
            // Arrange
            _plc.Initialize(safety: true);

            // Act
            var result = _plc.Start();

            // Assert
            Assert.That(result.IsSuccess, Is.True, "PLC should start successfully with safety mode enabled.");
        }

        [Test]
        public void StopPLC_ShouldPass_WhenCalledWithoutSafety()
        {
            // Arrange
            _plc.Initialize(safety: false);
            _plc.Start(); // Ensure PLC is running before stopping

            // Act
            var result = _plc.Stop();

            // Assert
            Assert.That(result.IsSuccess, Is.True, "PLC should stop successfully without safety mode.");
        }

        [Test]
        public void StopPLC_ShouldPass_WhenCalledWithSafety()
        {
            // Arrange
            _plc.Initialize(safety: true);
            _plc.Start(); // Ensure PLC is running before stopping

            // Act
            var result = _plc.Stop();

            // Assert
            Assert.That(result.IsSuccess, Is.True, "PLC should stop successfully with safety mode enabled.");
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                _plc.Dispose();
                Log.Information("Test resources disposed.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred during teardown.");
            }
        }
    }
}
