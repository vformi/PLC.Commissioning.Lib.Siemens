using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;

namespace PLC.Commissioning.Lib.Siemens.Tests.SiemensPLCControllerTests.Hardware.GSD
{
    [TestFixture]
    public class ModuleListTests
    {
        private string _testDataPath;
        private ModuleList _moduleList;

        [SetUp]
        public void Setup()
        {
            _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "gsd");
            string filePath = Path.Combine(_testDataPath, "GSDML-V2.3-LEUZE-BCL648i-20150128.xml");

            var gsdHandler = new GSDHandler();
            bool initializationSucceeded = gsdHandler.Initialize(filePath);
            Assert.IsTrue(initializationSucceeded, $"GSDHandler should successfully initialize with {filePath}");

            _moduleList = new ModuleList(gsdHandler);
        }

        [Test]
        [TestCase("GSDML-V2.3-LEUZE-BCL648i-20150128.xml")]
        [TestCase("GSDML-V2.25-LEUZE-BCL348i-20120814.xml")]
        [TestCase("GSDML-V2.31-LEUZE-BCL348i-20150923.xml")]
        [TestCase("GSDML-V2.41-LEUZE-BCL248i-20211213.xml")]
        [TestCase("GSDML-V2.42-LEUZE-RSL400P CU 4M12-20230816.xml")]
        public void ModuleList_CreatesModuleItemsProperly(string fileName)
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, fileName);
            var gsdHandler = new GSDHandler();

            // Act
            bool initializationSucceeded = gsdHandler.Initialize(filePath);

            // Assert
            Assert.IsTrue(initializationSucceeded,
                $"Expected GSDHandler.Initialize() to succeed for {fileName}.");

            // Create ModuleList
            var moduleList = new ModuleList(gsdHandler);

            // Basic checks
            Assert.IsNotNull(moduleList.ModuleItems, "ModuleItems should not be null.");
            Assert.IsNotEmpty(moduleList.ModuleItems, 
                $"Expected at least one ModuleItem in {fileName}, but got zero.");
        }

        [Test]
        public void ModuleList_HasExpectedModules()
        {
            Assert.IsNotNull(_moduleList.ModuleItems, "ModuleItems should not be null.");
            Assert.IsNotEmpty(_moduleList.ModuleItems, "ModuleItems should contain at least one item.");

            // Check if a specific known module exists (replace with a real module name from the file)
            string expectedModuleName = "[M13] Fragmented read result"; // Adjust this name as per the actual data
            bool moduleExists = _moduleList.ModuleItems.Any(m => m.Model.Name == expectedModuleName);
            Assert.IsTrue(moduleExists, $"Expected module '{expectedModuleName}' should exist.");
        }

        [Test]
        public void GetModuleItemByName_ReturnsCorrectModule()
        {
            string knownModuleName = "[M01] Code table extension 1"; // Adjust based on actual module name in XML

            var (moduleItem, hasChangeableParams) = _moduleList.GetModuleItemByName(knownModuleName);

            Assert.IsNotNull(moduleItem, $"Module '{knownModuleName}' should be found.");
            Assert.AreEqual(knownModuleName, moduleItem.Model.Name, "Returned module should match the searched name.");
            
            // Verify if changeable parameters exist
            bool expectedHasChangeableParams = moduleItem.Model.ParameterRecordDataItem != null ||
                                               moduleItem.Model.FParameterRecordDataItem != null;
            Assert.AreEqual(expectedHasChangeableParams, hasChangeableParams, "Changeable parameters flag should match expected.");
        }

        [Test]
        public void GetModuleItemByGsdId_ReturnsCorrectModule()
        {
            string knownGsdId = "ID_MOD_CodeTableExtension_1"; 

            var (moduleItem, hasChangeableParams) = _moduleList.GetModuleItemByGsdId(knownGsdId);

            Assert.IsNotNull(moduleItem, $"Module with GSD ID '{knownGsdId}' should be found.");
            Assert.AreEqual(knownGsdId, moduleItem.Model.ID, "Returned module should match the searched GSD ID.");

            // Verify if changeable parameters exist
            bool expectedHasChangeableParams = moduleItem.Model.ParameterRecordDataItem != null ||
                                               moduleItem.Model.FParameterRecordDataItem != null;
            Assert.AreEqual(expectedHasChangeableParams, hasChangeableParams, "Changeable parameters flag should match expected.");
        }

        [Test]
        public void GetModuleItemByGsdId_ReturnsNullForInvalidGsdId()
        {
            string nonExistentGsdId = "NonExistentID";

            var (moduleItem, hasChangeableParams) = _moduleList.GetModuleItemByGsdId(nonExistentGsdId);

            Assert.IsNull(moduleItem, "Expected no module to be found for an invalid GSD ID.");
            Assert.IsFalse(hasChangeableParams, "hasChangeableParameters should be false when no module is found.");
        }
    }
}
