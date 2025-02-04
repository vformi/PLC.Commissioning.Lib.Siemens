using System;
using System.IO;
using NUnit.Framework;
using Serilog;

namespace PLC.Commissioning.Lib.Siemens.Tests
{
    [TestFixture]
    public class CompileTests : IDisposable
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
        public void Compile_ShouldPass_WhenProjectIsCompilable()
        {
            // Arrange
            _plc.Initialize(safety: false);

            // Act 
            bool result = _plc.Compile();

            // Assert
            Assert.That(result, Is.True, "PLC should compile successfully.");
        }

        [Test]
        public void Compile_ShouldFail_WhenProjectIsNotCompilable()
        {
            // todo 
            // Arrange
            _plc.Initialize(safety: false);

            // Act 
            bool result = _plc.Compile();

            // Assert
            Assert.That(result, Is.False, "PLC should not compile successfully.");
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

        ~CompileTests()
        {
            Dispose(false);
        }
    }
}
