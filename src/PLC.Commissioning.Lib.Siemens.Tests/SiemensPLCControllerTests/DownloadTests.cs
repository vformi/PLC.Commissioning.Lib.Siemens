using System;
using System.IO;
using NUnit.Framework;
using Serilog;
using Siemens.Engineering.Download;
using FluentResults;

namespace PLC.Commissioning.Lib.Siemens.Tests.SiemensPLCControllerTests
{
    [TestFixture]
    public class DownloadTests
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
            Result result = _plc.Download(DownloadOptions.Hardware | DownloadOptions.Software);

            // Assert
            Assert.That(result.IsSuccess, Is.True, "PLC should download HW & SW successfully.");
        }

        [Test]
        public void Download_ShouldPass_WhenDownloadingSafety()
        {
            // Arrange
            _plc.Initialize(safety: true);

            // Act 
            Result result = _plc.Download("safety");

            // Assert
            Assert.That(result.IsSuccess, Is.True, "PLC should download safety successfully.");
        }

        [Test]
        public void Download_ShouldFail_WhenInitializeIsWrongAndSafetyDownload()
        {
            // Arrange
            _plc.Initialize(safety: false);

            // Act 
            Result result = _plc.Download("safety");

            // Assert
            Assert.That(result.IsFailed, Is.True, "PLC should not download safety.");
            Assert.That(result.Errors, Has.Count.GreaterThan(0), "Error should be returned.");
        }

        [Test]
        public void Download_ShouldFail_WhenInvalidOptionsProvided()
        {
            // Arrange
            _plc.Initialize(safety: false);
            
            // Act
            Result result = _plc.Download(123); // Invalid option

            // Assert
            Assert.That(result.IsFailed, Is.True, "Download should fail when invalid options are provided.");
            Assert.That(result.Errors, Has.Count.GreaterThan(0), "Error should be returned.");
        }

        [TearDown]
        public void TearDown()
        {
            _plc.Dispose();
            Log.Information("SiemensPLCController disposed.");
        }
    }
}
