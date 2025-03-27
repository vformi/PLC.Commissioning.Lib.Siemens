using NUnit.Framework;
using System;
using System.IO;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;

namespace PLC.Commissioning.Lib.Siemens.Tests.SiemensPLCControllerTests.Hardware.GSD
{
    [TestFixture]
    public class ModuleInfoTests
    {
        private string _testDataPath;

        [SetUp]
        public void Setup()
        {
            // Points to "TestData/gsd" in the bin directory
            _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "gsd");
        }

        // Adjust or add more TestCase entries as needed
        [Test]
        [TestCase("GSDML-V2.3-LEUZE-BCL648i-20150128.xml")]
        [TestCase("GSDML-V2.25-LEUZE-BCL348i-20120814.xml")]
        [TestCase("GSDML-V2.31-LEUZE-BCL348i-20150923.xml")]
        [TestCase("GSDML-V2.41-LEUZE-BCL248i-20211213.xml")]
        [TestCase("GSDML-V2.42-LEUZE-RSL400P CU 4M12-20230816.xml")]
        public void ModuleInfo_RequiredAttributesArePresent(string fileName)
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, fileName);
            var gsdHandler = new GSDHandler();

            // Act
            bool initOk = gsdHandler.Initialize(filePath);

            // Assert
            Assert.IsTrue(initOk, $"Failed to initialize GSDHandler for {fileName}.");

            // Create ModuleInfo and do strict checks
            var moduleInfo = new ModuleInfo(gsdHandler);
            Assert.IsNotNull(moduleInfo.Model, "ModuleInfo.Model must not be null.");

            // Now we assert that each expected property is non-null or non-empty.
            Assert.That(moduleInfo.Model.Name, Is.Not.Null.And.Not.Empty, "ModuleInfo.Model.Name is missing.");
            Assert.That(moduleInfo.Model.InfoText, Is.Not.Null.And.Not.Empty, "ModuleInfo.Model.InfoText is missing.");
            Assert.That(moduleInfo.Model.VendorName, Is.Not.Null.And.Not.Empty, "ModuleInfo.Model.VendorName is missing.");
            Assert.That(moduleInfo.Model.OrderNumber, Is.Not.Null.And.Not.Empty, "ModuleInfo.Model.OrderNumber is missing.");
            Assert.That(moduleInfo.Model.HardwareRelease, Is.Not.Null.And.Not.Empty, "ModuleInfo.Model.HardwareRelease is missing.");
            Assert.That(moduleInfo.Model.SoftwareRelease, Is.Not.Null.And.Not.Empty, "ModuleInfo.Model.SoftwareRelease is missing.");
        }
    }
}
