using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Serilog;
using FluentResults;
using PLC.Commissioning.Lib.Abstractions.Enums;

namespace PLC.Commissioning.Lib.Siemens.Tests.SiemensPLCControllerTests
{
    [TestFixture]
    public class InitializeTests
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

            _plc = new SiemensPLCController();
            _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
            Log.Information("Test setup completed. Test data directory: {Path}", _testDataPath);
        }

        [Test]
        public void InitializeOffline_ShouldPass_WhenConfiguredCorrectly_WithSafety()
        {
            string jsonFilePath = Path.Combine(_testDataPath, "Configurations/valid_config.json");
            Assert.That(File.Exists(jsonFilePath), Is.True, $"File not found: {jsonFilePath}");
            _plc.Configure(jsonFilePath);

            var result = _plc.Initialize(safety: true);

            Assert.That(result.IsSuccess, Is.True, "Offline Initialize should succeed with safety enabled and valid configuration.");
        }

        [Test]
        public void InitializeOffline_ShouldPass_WhenConfiguredCorrectly_WithoutSafety()
        {
            string jsonFilePath = Path.Combine(_testDataPath, "Configurations/valid_config.json");
            Assert.That(File.Exists(jsonFilePath), Is.True, $"File not found: {jsonFilePath}");
            _plc.Configure(jsonFilePath);

            var result = _plc.Initialize(safety: false);

            Assert.That(result.IsSuccess, Is.True, "Offline Initialize should succeed without safety and valid configuration.");
        }

        [Test]
        public void InitializeOffline_ShouldFail_WhenProjectPathIsInvalid()
        {
            string jsonFilePath = Path.Combine(_testDataPath, "Configurations/invalid_project_path.json");
            Assert.That(File.Exists(jsonFilePath), Is.True, $"File not found: {jsonFilePath}");
            _plc.Configure(jsonFilePath);

            var result = _plc.Initialize(safety: false);

            Assert.That(result.IsFailed, Is.True, "Offline Initialize should fail with an invalid project path.");
            Assert.That(result.Errors.First().Metadata["ErrorCode"], Is.EqualTo(OperationErrorCode.InitializationFailed));
        }

        [Test]
        public void InitializeOnline_ShouldPass_WhenConfiguredCorrectly()
        {
            string jsonFilePath = Path.Combine(_testDataPath, "Configurations/valid_config.json");
            Assert.That(File.Exists(jsonFilePath), Is.True, $"File not found: {jsonFilePath}");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);

            var result = _plc.InitializeOnline();

            Assert.That(result.IsSuccess, Is.True, "Online Initialize should succeed with valid configuration.");
        }

        [Test]
        public void InitializeOnline_ShouldFail_WhenNetworkCardIsMissing()
        {
            string jsonFilePath = Path.Combine(_testDataPath, "Configurations/missing_keys_config.json");
            Assert.That(File.Exists(jsonFilePath), Is.True, $"File not found: {jsonFilePath}");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);

            var result = _plc.InitializeOnline();

            Assert.That(result.IsFailed, Is.True, "Online Initialize should fail when network card is missing.");
            Assert.That(result.Errors.First().Metadata["ErrorCode"], Is.EqualTo(OperationErrorCode.InitializationFailed));
        }

        [Test]
        public void InitializeOnline_ShouldFail_ToGoOnlineWhenNetworkCardIsIncorrect()
        {
            string jsonFilePath = Path.Combine(_testDataPath, "Configurations/invalid_network_card.json");
            Assert.That(File.Exists(jsonFilePath), Is.True, $"File not found: {jsonFilePath}");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);

            var result = _plc.InitializeOnline();

            Assert.That(result.IsFailed, Is.True, "Online Initialize should fail with an incorrect network card.");
            Assert.That(result.Errors.First().Metadata["ErrorCode"], Is.EqualTo(OperationErrorCode.InitializationFailed));
        }

        [TearDown]
        public void TearDown()
        {
            _plc.Dispose();
            Log.Information("SiemensPLCController disposed.");
        }
    }
}
