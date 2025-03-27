using NUnit.Framework;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;
using System;
using System.IO;

namespace PLC.Commissioning.Lib.Siemens.Tests.SiemensPLCControllerTests.Hardware.GSD
{
    [TestFixture]
    public class GSDHandlerTests
    {
        private string _testDataPath;

        [SetUp]
        public void Setup()
        {
            // This assumes the TestData/gsd folder is in your bin output directory
            // If you're structuring differently, adjust as needed
            _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "gsd");
        }

        [Test]
        [TestCase("GSDML-V2.3-LEUZE-BCL648i-20150128.xml")]
        [TestCase("GSDML-V2.25-LEUZE-BCL348i-20120814.xml")]
        [TestCase("GSDML-V2.31-LEUZE-BCL348i-20150923.xml")]
        [TestCase("GSDML-V2.41-LEUZE-BCL248i-20211213.xml")]
        [TestCase("GSDML-V2.42-LEUZE-RSL400P CU 4M12-20230816.xml")]
        public void Initialize_ReturnsTrue_ForValidGsdmlFiles(string fileName)
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, fileName);
            var gsdHandler = new GSDHandler();

            // Act
            bool result = gsdHandler.Initialize(filePath);

            // Assert
            Assert.IsTrue(result, $"Expected initialization to succeed for {fileName}, but got false.");
            Assert.IsNotNull(gsdHandler.xmlDoc, "XmlDocument should not be null after valid initialization.");
            Assert.IsNotNull(gsdHandler.nsmgr, "XmlNamespaceManager should not be null after valid initialization.");
        }

        [Test]
        public void Initialize_ReturnsFalse_WhenFileNotFound()
        {
            // Arrange
            // purposely pass a file that doesn't exist
            string invalidPath = Path.Combine(_testDataPath, "NoSuchFile.xml");
            var gsdHandler = new GSDHandler();

            // Act
            bool result = gsdHandler.Initialize(invalidPath);

            // Assert
            Assert.IsFalse(result, "Expected initialization to fail for a non-existent file path, but returned true.");
        }

        [Test]
        public void Initialize_ReturnsFalse_WhenFileIsMalformed()
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, "MalformedGSDML.xml");
            var gsdHandler = new GSDHandler();

            // Act
            bool result = gsdHandler.Initialize(filePath);

            // Assert
            Assert.IsFalse(result, "Expected initialization to fail for a malformed XML, but returned true.");
        }

        [Test]
        public void GetExternalText_ReturnsNull_WhenTextIdIsEmptyOrNull()
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, "GSDML-V2.3-LEUZE-BCL648i-20150128.xml");
            var gsdHandler = new GSDHandler();
            Assert.IsTrue(gsdHandler.Initialize(filePath), "Initialization should succeed for a valid file.");

            // Act
            var resultEmpty = gsdHandler.GetExternalText(String.Empty);
            var resultNull = gsdHandler.GetExternalText(null);

            // Assert
            Assert.IsNull(resultEmpty, "Empty text ID should return null.");
            Assert.IsNull(resultNull, "Null text ID should return null.");
        }

        [Test]
        public void GetExternalText_ReturnsCorrectValue_ForKnownTextId()
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, "GSDML-V2.3-LEUZE-BCL648i-20150128.xml");
            var gsdHandler = new GSDHandler();
            Assert.IsTrue(gsdHandler.Initialize(filePath));
            
            string knownTextId = "DeviceDescription";
            string expectedValue = "PROFINET IO Identification Device";

            // Act
            var actualValue = gsdHandler.GetExternalText(knownTextId);

            // Assert
            Assert.AreEqual(expectedValue, actualValue, "The external text should match expected device name.");
        }

        [Test]
        public void GetValueItem_ReturnsExpectedValueItem()
        {
            // Arrange
            // Suppose in this file we know there's a ValueItem with ID="SomeUniqueID"
            // and it has certain assignments.
            string filePath = Path.Combine(_testDataPath, "GSDML-V2.3-LEUZE-BCL648i-20150128.xml");
            var gsdHandler = new GSDHandler();
            Assert.IsTrue(gsdHandler.Initialize(filePath));

            string expectedId = "DAP_PARAM_REF_VALUE_Profile";

            // Act
            var valueItem = gsdHandler.GetValueItem(expectedId);

            // Assert
            Assert.IsNotNull(valueItem, $"ValueItem with ID={expectedId} should not be null.");
            Assert.AreEqual(expectedId, valueItem.ID, "ValueItem ID should match.");
            Assert.IsNotEmpty(valueItem.Assignments, "Expected one or more Assign objects in ValueItem.");
        }
    }
}
