using NUnit.Framework;
using System;
using System.Xml;
using System.IO;
using System.Linq;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;

namespace PLC.Commissioning.Lib.Siemens.Tests.SiemensPLCControllerTests.Hardware.GSD
{
    [TestFixture]
    public class ModuleItemTests
    {
        private GSDHandler _gsdHandler;
        private XmlDocument _xmlDocument;
        private ModuleItem _moduleItem;
        
        private string _testDataPath;

        [SetUp]
        public void Setup()
        {
            _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "gsd");
            string testFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "gsd", "GSDML-V2.3-LEUZE-BCL648i-20150128.xml");

            _gsdHandler = new GSDHandler();
            bool initializationSucceeded = _gsdHandler.Initialize(testFilePath);
            Assert.IsTrue(initializationSucceeded, "GSDHandler should successfully initialize.");

            _xmlDocument = _gsdHandler.xmlDoc;
        }
        
        [Test]
        [TestCase("GSDML-V2.3-LEUZE-BCL648i-20150128.xml")]
        [TestCase("GSDML-V2.25-LEUZE-BCL348i-20120814.xml")]
        [TestCase("GSDML-V2.31-LEUZE-BCL348i-20150923.xml")]
        [TestCase("GSDML-V2.41-LEUZE-BCL248i-20211213.xml")]
        [TestCase("GSDML-V2.42-LEUZE-RSL400P CU 4M12-20230816.xml")]
        public void ModuleItem_ValidatesFullModuleItemAcrossVersions(string fileName)
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, fileName);
            var gsdHandler = new GSDHandler();

            // Act
            bool initializationSucceeded = gsdHandler.Initialize(filePath);
            Assert.IsTrue(initializationSucceeded, $"Expected GSDHandler.Initialize() to succeed for {fileName}.");

            // Create ModuleList
            var moduleList = new ModuleList(gsdHandler);

            // Check ModuleItems
            Assert.IsNotNull(moduleList.ModuleItems, "ModuleItems should not be null.");
            Assert.IsNotEmpty(moduleList.ModuleItems, $"Expected at least one ModuleItem in {fileName}, but got zero.");

            // Debug Output: Verify each ModuleItem
            Console.WriteLine($"Testing GSD File: {fileName}");
            bool hasParameterData = false;
            bool hasFParameterData = false;
            bool hasIOData = false;

            foreach (var module in moduleList.ModuleItems)
            {
                Console.WriteLine($"- ModuleItem: {module.Model.Name} (ID: {module.Model.ID})");

                // Check ParameterRecordDataItem
                if (module.ParameterRecordDataItem != null)
                {
                    Console.WriteLine(" - Has ParameterRecordDataItem");
                    hasParameterData = true;
                }

                // Check FParameterRecordDataItem
                if (module.FParameterRecordDataItem != null)
                {
                    Console.WriteLine(" - Has FParameterRecordDataItem");
                    hasFParameterData = true;
                }

                // Check IOData
                if (module.Model.IOData != null)
                {
                    bool hasInputs = module.Model.IOData.Inputs.Any();
                    bool hasOutputs = module.Model.IOData.Outputs.Any();

                    if (hasInputs || hasOutputs)
                    {
                        Console.WriteLine(" - Has IOData (Inputs/Outputs detected)");
                        hasIOData = true;
                    }
                }
            }

            // Ensure at least one module in the GSD file has parameter data
            Assert.IsTrue(hasParameterData || hasFParameterData, 
                $"No ParameterRecordDataItem or FParameterRecordDataItem found in {fileName}. This might indicate a parsing issue.");

            // Ensure at least one module has IOData
            Assert.IsTrue(hasIOData, 
                $"No IOData found in {fileName}. This might indicate a parsing issue.");
        }
        
        [Test]
        public void ModuleItem_ParsesBasicPropertiesCorrectly()
        {
            XmlNode moduleNode = _xmlDocument.SelectSingleNode("//gsd:ModuleList/gsd:ModuleItem", _gsdHandler.nsmgr);
            Assert.IsNotNull(moduleNode, "ModuleItem node should exist in the XML.");

            _moduleItem = new ModuleItem(_gsdHandler);
            _moduleItem.ParseModuleItem(moduleNode);

            Assert.IsNotNull(_moduleItem.Model, "Model should be initialized.");
            Assert.IsNotNull(_moduleItem.Model.ID, "Module ID should not be null.");
            Assert.IsNotNull(_moduleItem.Model.ModuleIdentNumber, "ModuleIdentNumber should not be null.");

            // Validate Name and InfoText
            Assert.IsNotNull(_moduleItem.Model.Name, "Module name should not be null.");
            Assert.IsNotNull(_moduleItem.Model.InfoText, "InfoText should not be null.");
        }

        [Test]
        public void ModuleItem_HandlesMissingAttributesGracefully()
        {
            XmlNode moduleNode = _xmlDocument.SelectSingleNode("//gsd:ModuleList/gsd:ModuleItem", _gsdHandler.nsmgr);
            Assert.IsNotNull(moduleNode, "ModuleItem node should exist in the XML.");

            // Remove ID and ModuleIdentNumber to simulate missing data
            XmlElement moduleElement = (XmlElement)moduleNode;
            moduleElement.RemoveAttribute("ID");
            moduleElement.RemoveAttribute("ModuleIdentNumber");

            _moduleItem = new ModuleItem(_gsdHandler);
            _moduleItem.ParseModuleItem(moduleNode);

            Assert.IsNull(_moduleItem.Model.ID, "Module ID should be null if missing.");
            Assert.IsNull(_moduleItem.Model.ModuleIdentNumber, "ModuleIdentNumber should be null if missing.");
        }

        [Test]
        public void ModuleItem_ParsesIODataCorrectly()
        {
            // Pick a specific module that we know has IOData (adjust the ID if needed)
            string moduleIdToTest = "ID_MOD_DecodingResult_16Byte"; 

            XmlNode moduleNode = _xmlDocument.SelectSingleNode(
                $"//gsd:ModuleList/gsd:ModuleItem[@ID='{moduleIdToTest}']", 
                _gsdHandler.nsmgr
            );

            Assert.IsNotNull(moduleNode, $"ModuleItem with ID '{moduleIdToTest}' should exist in the XML.");

            _moduleItem = new ModuleItem(_gsdHandler);
            _moduleItem.ParseModuleItem(moduleNode);

            // Ensure IOData is parsed
            Assert.IsNotNull(_moduleItem.Model.IOData, "IOData should be parsed.");

            bool hasInputs = _moduleItem.Model.IOData?.Inputs.Any() ?? false;
            bool hasOutputs = _moduleItem.Model.IOData?.Outputs.Any() ?? false;
            Assert.IsTrue(hasInputs || hasOutputs, "IOData should contain at least one input or output.");

            // Validate Inputs
            var inputData = _moduleItem.Model.IOData.Inputs;
            Assert.IsNotNull(inputData, "Input data should not be null.");
            Assert.IsTrue(inputData.Any(), "Input data should have at least one DataItem.");

            var firstInput = inputData.FirstOrDefault();
            Assert.IsNotNull(firstInput, "First input should be parsed correctly.");
            Assert.IsNotNull(firstInput.DataType, "First input should have a DataType.");
            
            // Ensure bit-level data parsing for Input
            var bitDataItems = firstInput.BitDataItems;
            Assert.IsNotNull(bitDataItems, "BitDataItems should be parsed.");
            Assert.IsTrue(bitDataItems.Any(), "BitDataItems should contain at least one entry.");
            
            var firstBit = bitDataItems.FirstOrDefault();
            Assert.IsNotNull(firstBit, "First BitDataItem should be parsed.");
            Assert.IsTrue(firstBit.BitOffset >= 0, "BitOffset should be a valid integer.");
            Assert.IsNotNull(firstBit.TextId, "BitDataItem should have a valid TextId.");

            // Validate Outputs (if available)
            if (hasOutputs)
            {
                var outputData = _moduleItem.Model.IOData.Outputs;
                Assert.IsNotNull(outputData, "Output data should not be null.");
                Assert.IsTrue(outputData.Any(), "Output data should have at least one DataItem.");

                var firstOutput = outputData.FirstOrDefault();
                Assert.IsNotNull(firstOutput, "First output should be parsed correctly.");
                Assert.IsNotNull(firstOutput.DataType, "First output should have a DataType.");
            }
        }

        
        [Test]
        public void ModuleItem_ParsesParametersCorrectly()
        {
            XmlNode moduleNode = _xmlDocument.SelectSingleNode("//gsd:ModuleList/gsd:ModuleItem", _gsdHandler.nsmgr);
            Assert.IsNotNull(moduleNode, "ModuleItem node should exist in the XML.");

            _moduleItem = new ModuleItem(_gsdHandler);
            _moduleItem.ParseModuleItem(moduleNode);

            // Normal parameters
            bool hasNormalParameters = _moduleItem.ParameterRecordDataItem != null;
            bool hasSafetyParameters = _moduleItem.FParameterRecordDataItem != null;

            Assert.IsTrue(hasNormalParameters || hasSafetyParameters, "At least one type of parameters should be parsed.");
        }

        [Test]
        public void ModuleItem_ToString_FormatsCorrectly()
        {
            XmlNode moduleNode = _xmlDocument.SelectSingleNode("//gsd:ModuleList/gsd:ModuleItem", _gsdHandler.nsmgr);
            Assert.IsNotNull(moduleNode, "ModuleItem node should exist in the XML.");

            _moduleItem = new ModuleItem(_gsdHandler);
            _moduleItem.ParseModuleItem(moduleNode);

            string output = _moduleItem.ToString();
            Assert.IsNotNull(output, "ToString should not return null.");
            Assert.IsNotEmpty(output, "ToString should return a non-empty string.");
            Assert.IsTrue(output.Contains("Name:"), "Output should contain 'Name:'");
            Assert.IsTrue(output.Contains("ID:"), "Output should contain 'ID:'");
            Assert.IsTrue(output.Contains("Info Text:"), "Output should contain 'Info Text:'");
        }

        [Test]
        public void ModuleItem_ToString_HandlesEmptyModuleGracefully()
        {
            _moduleItem = new ModuleItem(_gsdHandler);
            string output = _moduleItem.ToString();

            Assert.IsNotNull(output, "ToString should not return null.");
            Assert.IsTrue(output.Contains("No parameters available."), "Output should indicate missing parameters.");
        }
    }
}
