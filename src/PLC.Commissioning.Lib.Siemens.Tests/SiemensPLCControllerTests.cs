using NUnit.Framework;
using System;
using System.IO;
using System.Collections.Generic;
using Serilog;
using Newtonsoft.Json;
using Siemens.Engineering.HW;
using System.Linq;
using System.Threading;
using Siemens.Engineering.Download;

namespace PLC.Commissioning.Lib.Siemens.Tests
{
    [TestFixture]
    public class SiemensPLCControllerTests : IDisposable
    {
        private SiemensPLCController _plc;
        private bool _disposed = false;
        private string _basePath = null;
        private string _projectRoot = null;

        [SetUp]
        public void SetUp()
        {
            // figure out paths 
            _basePath = AppDomain.CurrentDomain.BaseDirectory;
            _projectRoot = Directory.GetParent(_basePath).Parent.Parent.Parent.FullName;

            // Initialize logger (optional, could be mocked if needed)
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            // Initialize the SiemensPLCController object
            _plc = new SiemensPLCController();
        }

        [Test]
        // Tests that the Stop method succeeds when stopping the PLC without safety mode enabled.
        public void StopPLC_ShouldPass_WhenCalledWithoutSafety()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);
            _plc.Start();

            // Act 
            bool result = _plc.Stop();

            // Assert
            Assert.That(result, Is.True, "PLC should stop successfully without safety mode.");
        }

        [Test]
        // Tests that the Stop method succeeds when stopping the PLC with safety mode enabled.
        public void StopPLC_ShouldPass_WhenCalledWithSafety()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: true);
            _plc.Start();

            // Act 
            bool result = _plc.Stop();

            // Assert
            Assert.That(result, Is.True, "PLC should stop successfully with safety mode enabled.");
        }

        [Test]
        // Tests that the Start method succeeds when starting the PLC without safety mode enabled.
        public void StartPLC_ShouldPass_WhenCalledWithoutSafety()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);

            // Act 
            bool result = _plc.Start();

            // Assert
            Assert.That(result, Is.True, "PLC should start successfully without safety mode.");
        }

        [Test]
        // Tests that the Start method succeeds when starting the PLC with safety mode enabled.
        public void StartPLC_ShouldPass_WhenCalledWithSafety()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: true);

            // Act 
            bool result = _plc.Start();

            // Assert
            Assert.That(result, Is.True, "PLC should start successfully with safety mode enabled.");
        }

        [Test]
        // Tests that the Compile method succeeds when the project is valid and can be compiled.
        public void Compile_ShouldPass_WhenProjectIsCompilable()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_multiple_devices.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);

            // Act 
            bool result = _plc.Compile();

            // Assert
            Assert.That(result, Is.True, "PLC should compile successfully.");
        }

        [Test]
        // Tests that the Compile method fails when the project is invalid and cannot be compiled.
        public void Compile_ShouldFail_WhenProjectIsNotCompilable()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "invalid_configuration.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);

            // Act 
            bool result = _plc.Compile();

            // Assert
            Assert.That(result, Is.False, "PLC should not compile successfully.");
        }

        [Test]
        // Tests that the Download method succeeds when downloading hardware and software
        // to the PLC without safety configurations.
        public void Download_ShouldPass_WhenDownloadingNonSafety()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);

            // Act 
            bool fullDownload = _plc.Download(DownloadOptions.Hardware | DownloadOptions.Software); // download combination

            // Assert
            Assert.That(fullDownload, Is.True, "PLC should download HW & SW successfully.");
        }

        [Test]
        // Tests that the Download method succeeds when downloading 
        // safety configurations to the PLC.
        public void Download_ShouldPass_WhenDownloadingSafety()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_safety.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: true);

            // Act 
            bool download = _plc.Download("safety");

            // Assert
            Assert.That(download, Is.True, "PLC should download safety successfully.");
        }

        [Test]
        // Tests that the Download method fails when attempting to download safety configurations 
        // without proper initialization for safety.
        public void Download_ShouldFail_WhenInitializeIsWrongAndSafetyDownload()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_safety.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);

            // Act 
            bool download = _plc.Download("safety");

            // Assert
            Assert.That(download, Is.False, "PLC should not download safety.");
        }

        [Test]
        // Tests that the Export method succeeds when exporting 
        // project data with a valid project.
        public void Export_ShouldPass_WhenProjectIsValid()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration_safety.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);
            string exportPath = "C:\\Users\\vformane\\Desktop\\Export";
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
        // Tests that the SaveProjectAs method succeeds when saving the 
        // project under a new name or path.
        public void SaveProjectAs_ShouldPass_WhenPathIsCorrect()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);
            string saveName = "ProjectAAA";
            // Act 
            bool result = _plc.SaveProjectAs(saveName);

            // Assert
            Assert.That(result, Is.True, "SaveProjectAs should return true when the project is saved successfully.");
        }

        [Test]
        // Tests that the AdditionalImport method succeeds when provided 
        // with a correct file path for import.
        public void AddutionalImport_ShouldPass_WhenPathIsCorrect()
        {
            // finish this 
            // Arrange
            string jsonFilePath = Path.Combine(_projectRoot, "configuration_files", "valid_configuration.json");
            _plc.Configure(jsonFilePath);
            _plc.Initialize(safety: false);
            string importPath = "C:\\Users\\vformane\\Desktop\\Export\\Default tag table.xml";
            var filesToImport = new Dictionary<string, object>
            {
                { "plcTags", importPath }
            };
            // Act 
            bool result = _plc.AdditionalImport(filesToImport);

            // Assert
            Assert.That(result, Is.True, "Additional import should pass properly.");
        }

        [TearDown]
        public void TearDown()
        {
            if (!_disposed)
            {
                Dispose(); // Call Dispose if it hasn't been called yet.
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
                    // Dispose of the SiemensPLCController
                    Thread.Sleep(5000); // added sleep so Tia portal could close properly
                    _plc?.Dispose();
                    
                }
                _disposed = true;
            }
        }

        ~SiemensPLCControllerTests()
        {
            Dispose(false);
        }
    }
}
