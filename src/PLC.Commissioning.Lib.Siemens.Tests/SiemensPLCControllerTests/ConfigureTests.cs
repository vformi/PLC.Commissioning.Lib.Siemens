using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using PLC.Commissioning.Lib.Abstractions.Enums;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions;

namespace PLC.Commissioning.Lib.Siemens.Tests.SiemensPLCControllerTests
{
    [TestFixture]
    public class ConfigureTests
    {
        private SiemensPLCController _plc;
        private Mock<IFileSystem> _fileSystemMock;
        private string _testDataPath;

        [SetUp]
        public void SetUp()
        {
            // Set up logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            // Set up the mock filesystem
            _fileSystemMock = new Mock<IFileSystem>();

            // Initialize the SiemensPLCController with the mocked filesystem
            _plc = new TestSiemensPLCController(_fileSystemMock.Object);

            // Set up the test data path (for reference, though we'll mock file operations)
            _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Configurations");

            Log.Information("Test setup completed. Test data directory: {Path}", _testDataPath);
        }
        
        [TearDown]
        public void TearDown()
        {
            _plc.Dispose();
            _plc = null;
            Log.Information("Controller disposed at test tear-down.");
        }

        [Test]
        public void Configure_ShouldReturnOk_WhenJsonIsValid()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_testDataPath, "valid_config.json");
            var config = new Dictionary<string, object>
            {
                { "projectPath", @"C:\Projects\PLCProject" },
                { "networkCard", "Ethernet1" }
            };
            string jsonContent = JsonConvert.SerializeObject(config);
            _fileSystemMock.Setup(fs => fs.ReadAllText(jsonFilePath)).Returns(jsonContent);
            Log.Debug("Testing valid configuration file: {FilePath}", jsonFilePath);

            // Act
            var result = _plc.Configure(jsonFilePath);

            // Assert
            Assert.IsTrue(result.IsSuccess, "The Configure method should return a successful Result for a valid JSON configuration.");
            Assert.AreEqual(@"C:\Projects\PLCProject", GetPrivateField(_plc, "_projectPath"), "ProjectPath should be set correctly.");
            Assert.AreEqual("Ethernet1", GetPrivateField(_plc, "_networkCard"), "NetworkCard should be set correctly.");
            _fileSystemMock.Verify(fs => fs.ReadAllText(jsonFilePath), Times.Once(), "FileSystem.ReadAllText was not called as expected.");
        }

        [Test]
        public void Configure_ShouldReturnFail_WhenJsonIsMissingKeys()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_testDataPath, "missing_keys_config.json");
            var config = new Dictionary<string, object>
            {
                { "projectPath", @"C:\Projects\PLCProject" }
                // Missing "networkCard"
            };
            string jsonContent = JsonConvert.SerializeObject(config);
            _fileSystemMock.Setup(fs => fs.ReadAllText(jsonFilePath)).Returns(jsonContent);
            Log.Debug("Testing configuration file missing keys: {FilePath}", jsonFilePath);

            // Act
            var result = _plc.Configure(jsonFilePath);

            // Assert
            Assert.IsFalse(result.IsSuccess, "The Configure method should return a failed Result for a JSON configuration missing required keys.");
            Assert.AreEqual(OperationErrorCode.ConfigurationFailed, result.Errors[0].Metadata["ErrorCode"], "ErrorCode should indicate ConfigurationFailed.");
        }

        [Test]
        public void Configure_ShouldReturnFail_WhenFilePathIsInvalid()
        {
            // Arrange
            string invalidFilePath = Path.Combine(_testDataPath, "non_existent_config.json");
            _fileSystemMock.Setup(fs => fs.ReadAllText(invalidFilePath)).Throws(new FileNotFoundException("File not found."));
            Log.Debug("Testing invalid configuration file path: {FilePath}", invalidFilePath);

            // Act
            var result = _plc.Configure(invalidFilePath);

            // Assert
            Assert.IsFalse(result.IsSuccess, "The Configure method should return a failed Result for an invalid file path.");
            Assert.AreEqual(OperationErrorCode.ConfigurationFailed, result.Errors[0].Metadata["ErrorCode"], "ErrorCode should indicate ConfigurationFailed.");
        }

        [Test]
        public void Configure_ShouldReturnFail_WhenJsonIsMalformed()
        {
            // Arrange
            string jsonFilePath = Path.Combine(_testDataPath, "malformed_config.json");
            string malformedJson = "{ \"projectPath\": \"C:\\Projects\\PLCProject\", \"networkCard\": "; // Incomplete JSON
            _fileSystemMock.Setup(fs => fs.ReadAllText(jsonFilePath)).Returns(malformedJson);
            Log.Debug("Testing malformed configuration file: {FilePath}", jsonFilePath);

            // Act
            var result = _plc.Configure(jsonFilePath);

            // Assert
            Assert.IsFalse(result.IsSuccess, "The Configure method should return a failed Result for a malformed JSON file.");
            Assert.AreEqual(OperationErrorCode.ConfigurationFailed, result.Errors[0].Metadata["ErrorCode"], "ErrorCode should indicate ConfigurationFailed.");
        }

        // Helper class to inject the mocked IFileSystem
        private class TestSiemensPLCController : SiemensPLCController
        {
            private readonly IFileSystem _testFileSystem;

            public TestSiemensPLCController(IFileSystem fileSystem)
            {
                _testFileSystem = fileSystem;
            }

            protected override IFileSystem FileSystem => _testFileSystem;
        }

        // Helper method to access private fields for assertions
        private object GetPrivateField(object obj, string fieldName)
        {
            var field = typeof(SiemensPLCController).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null)
            {
                Assert.Fail($"Field '{fieldName}' not found in SiemensPLCController.");
            }
            return field.GetValue(obj);
        }
    }
}