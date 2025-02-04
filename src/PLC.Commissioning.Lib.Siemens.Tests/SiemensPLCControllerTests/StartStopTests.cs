using System;
using System.IO;
using NUnit.Framework;
using Serilog;

namespace PLC.Commissioning.Lib.Siemens.Tests
{
    [TestFixture]
    public class StartStopTests : IDisposable
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

            // Initialize SiemensPLCController
            _plc = new SiemensPLCController();
            string jsonFilePath = Path.Combine(_testDataPath, "Configurations", "valid_config.json");
            _plc.Configure(jsonFilePath);
            
            Log.Information("Test setup completed. Test data directory: {Path}", _testDataPath);
        }
    
        [Test]
        public void StopPLC_ShouldPass_WhenCalledWithoutSafety()
        {
            // Arrange
            _plc.Initialize(safety: false);

            // Act
            bool result = _plc.Stop();

            // Assert
            Assert.That(result, Is.True, "PLC should stop successfully without safety mode.");
        }

        [Test]
        public void StopPLC_ShouldPass_WhenCalledWithSafety()
        {
            // Arrange
            _plc.Initialize(safety: true);

            // Act
            bool result = _plc.Stop();

            // Assert
            Assert.That(result, Is.True, "PLC should stop successfully with safety mode enabled.");
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                // Start the PLC to reset its state before cleanup
                if (!_plc.Start())
                {
                    Log.Warning("Failed to start the PLC during teardown.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while starting the PLC during teardown.");
            }
            finally
            {
                Dispose();
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
                    _plc?.Dispose();
                    Log.Information("Test resources disposed.");
                }

                _disposed = true;
            }
        }

        ~StartStopTests()
        {
            Dispose(false);
        }
    }
}
