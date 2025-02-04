using System;
using System.IO;
using NUnit.Framework;
using Serilog;
using Siemens.Engineering.Download;

namespace PLC.Commissioning.Lib.Siemens.Tests
{
    [TestFixture]
    public class DownloadTests : IDisposable
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
            string jsonFilePath = Path.Combine(_testDataPath, "Configurations", "valid_config.json");

            // Initialize SiemensPLCController
            _plc = new SiemensPLCController();
            _plc.Configure(jsonFilePath);
            Log.Information("Test setup completed. Test data directory: {Path}", _testDataPath);
        }

        [Test]
        public void Download_ShouldPass_WhenDownloadingNonSafety()
        {
            // Arrange
            _plc.Initialize(safety: false);

            // Act 
            bool fullDownload = _plc.Download(DownloadOptions.Hardware | DownloadOptions.Software);

            // Assert
            Assert.That(fullDownload, Is.True, "PLC should download HW & SW successfully.");
        }

        [Test]
        public void Download_ShouldPass_WhenDownloadingSafety()
        {
            // Arrange
            _plc.Initialize(safety: true);

            // Act 
            bool download = _plc.Download("safety");

            // Assert
            Assert.That(download, Is.True, "PLC should download safety successfully.");
        }

        [Test]
        public void Download_ShouldFail_WhenInitializeIsWrongAndSafetyDownload()
        {
            // Arrange
            _plc.Initialize(safety: false);

            // Act 
            bool download = _plc.Download("safety");

            // Assert
            Assert.That(download, Is.False, "PLC should not download safety.");
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

        ~DownloadTests()
        {
            Dispose(false);
        }
    }
}
