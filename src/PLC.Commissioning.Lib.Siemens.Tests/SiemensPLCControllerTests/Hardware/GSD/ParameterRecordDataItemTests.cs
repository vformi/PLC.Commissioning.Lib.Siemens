using NUnit.Framework;
using System;
using System.Xml;
using System.Collections.Generic;
using System.IO;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models;

namespace PLC.Commissioning.Lib.Siemens.Tests.SiemensPLCControllerTests.Hardware.GSD
{
    [TestFixture]
    public class ParameterRecordDataItemTests
    {
        private string _testDataPath;
        private GSDHandler _gsdHandler;

        [SetUp]
        public void Setup()
        {
            _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "gsd");
            _gsdHandler = new GSDHandler();
            
            // Ensure namespace registration for XPath queries
            XmlDocument tempDoc = new XmlDocument();
            _gsdHandler.nsmgr = new XmlNamespaceManager(tempDoc.NameTable);
            _gsdHandler.nsmgr.AddNamespace("gsd", "http://www.profibus.com/GSDML");
        }

        [Test]
        [TestCase("GSDML-V2.3-LEUZE-BCL648i-20150128.xml")]
        [TestCase("GSDML-V2.25-LEUZE-BCL348i-20120814.xml")]
        [TestCase("GSDML-V2.31-LEUZE-BCL348i-20150923.xml")]
        [TestCase("GSDML-V2.41-LEUZE-BCL248i-20211213.xml")]
        public void ParseParameterRecordDataItem_ValidatesAcrossGSDMLFiles(string fileName)
        {
            string filePath = Path.Combine(_testDataPath, fileName);
            Assert.IsTrue(File.Exists(filePath), $"Test file not found: {filePath}");

            bool initSuccess = _gsdHandler.Initialize(filePath);
            Assert.IsTrue(initSuccess, $"Failed to initialize GSDHandler for {fileName}");

            XmlDocument gsdmlDoc = new XmlDocument();
            gsdmlDoc.Load(filePath);

            // Select first ParameterRecordDataItem
            XmlNode parameterNode = gsdmlDoc.SelectSingleNode("//gsd:ParameterRecordDataItem", _gsdHandler.nsmgr);
            Assert.IsNotNull(parameterNode, $"No ParameterRecordDataItem found in {fileName}");
            
            // Parse ParameterRecordDataItem
            var paramItem = new ParameterRecordDataItem(_gsdHandler);
            paramItem.ParseParameterRecordDataItem(parameterNode);

            Assert.IsNotNull(paramItem, "ParameterRecordDataItem should not be null.");
            Assert.IsTrue(paramItem.Refs.Count > 0, $"ParameterRecordDataItem in {fileName} should have at least one <Ref>.");
        }

        [Test]
        public void ParseParameterRecordDataItem_ThrowsOnMissingIndex()
        {
            string xml = @"
                <ParameterRecordDataItem Length='4'>
                    <Name TextId='IDT_RECORD_NAME_GeneralParameter' />
                    <Ref DataType='Unsigned8' ByteOffset='0' DefaultValue='13' AllowedValues='0..255' TextId='PARAM_REF_NAME_Test' />
                </ParameterRecordDataItem>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNode node = doc.DocumentElement;

            var paramItem = new ParameterRecordDataItem(_gsdHandler);
            var exception = Assert.Throws<InvalidOperationException>(() => paramItem.ParseParameterRecordDataItem(node));

            Assert.That(exception.Message, Does.Contain("Missing or invalid 'Index' attribute"));
        }

        [Test]
        public void ParseParameterRecordDataItem_ThrowsOnMissingLength()
        {
            string xml = @"
                <ParameterRecordDataItem Index='0'>
                    <Name TextId='IDT_RECORD_NAME_GeneralParameter' />
                    <Ref DataType='Unsigned8' ByteOffset='0' DefaultValue='13' AllowedValues='0..255' TextId='PARAM_REF_NAME_Test' />
                </ParameterRecordDataItem>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNode node = doc.DocumentElement;

            var paramItem = new ParameterRecordDataItem(_gsdHandler);
            var exception = Assert.Throws<InvalidOperationException>(() => paramItem.ParseParameterRecordDataItem(node));

            Assert.That(exception.Message, Does.Contain("Missing or invalid 'Length' attribute"));
        }

        [Test]
        public void ParseParameterRecordDataItem_ThrowsOnMissingNameTextId()
        {
            string xml = @"
                <ParameterRecordDataItem Index='0' Length='4'>
                    <Name />
                    <Ref DataType='Unsigned8' ByteOffset='0' DefaultValue='13' AllowedValues='0..255' TextId='PARAM_REF_NAME_Test' />
                </ParameterRecordDataItem>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNode node = doc.DocumentElement;

            var paramItem = new ParameterRecordDataItem(_gsdHandler);
            var exception = Assert.Throws<InvalidOperationException>(() => paramItem.ParseParameterRecordDataItem(node));

            Assert.That(exception.Message, Does.Contain("Missing required <Name> node or 'TextId' attribute in ParameterRecordDataItem."));
        }

        [Test]
        public void ParseParameterRecordDataItem_ThrowsOnNoRefs()
        {
            string xml = @"
                <ParameterRecordDataItem Index='0' Length='4'>
                    <Name TextId='IDT_RECORD_NAME_GeneralParameter' />
                </ParameterRecordDataItem>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNode node = doc.DocumentElement;

            var paramItem = new ParameterRecordDataItem(_gsdHandler);
            var exception = Assert.Throws<InvalidOperationException>(() => paramItem.ParseParameterRecordDataItem(node));

            Assert.That(exception.Message, Does.Contain("Missing required <Name> node or 'TextId' attribute in ParameterRecordDataItem."));
        }

        [Test]
        public void ParseParameterRecordDataItem_FromConcreteGSDML()
        {
            string fileName = "GSDML-V2.41-LEUZE-BCL248i-20211213.xml"; // Specific GSDML file
            string filePath = Path.Combine(_testDataPath, fileName);
            
            Assert.IsTrue(File.Exists(filePath), $"Test file not found: {filePath}");

            var gsdHandler = new GSDHandler();
            bool initSuccess = gsdHandler.Initialize(filePath);
            Assert.IsTrue(initSuccess, $"Failed to initialize GSDHandler for {fileName}");

            // Load the GSDML file
            XmlDocument gsdmlDoc = new XmlDocument();
            gsdmlDoc.Load(filePath);

            // Find the specific ModuleItem (ID_MOD_DataFormatting)
            XmlNode moduleItemNode = gsdmlDoc.SelectSingleNode("//gsd:ModuleItem[@ID='ID_MOD_DataFormatting']", gsdHandler.nsmgr);
            Assert.IsNotNull(moduleItemNode, $"ModuleItem 'ID_MOD_DataFormatting' not found in {fileName}");

            // Find the ParameterRecordDataItem within RecordDataList
            XmlNode parameterNode = moduleItemNode.SelectSingleNode(".//gsd:ParameterRecordDataItem", gsdHandler.nsmgr);
            Assert.IsNotNull(parameterNode, $"No ParameterRecordDataItem found in ModuleItem 'ID_MOD_DataFormatting'");

            // Parse the ParameterRecordDataItem
            var paramItem = new ParameterRecordDataItem(gsdHandler);
            paramItem.ParseParameterRecordDataItem(parameterNode);

            // Perform Assertions
            Assert.AreEqual(0, paramItem.DsNumber, "Expected Index 0 for ParameterRecordDataItem");
            Assert.AreEqual(23, paramItem.LengthInBytes, "Expected Length 23 for ParameterRecordDataItem");

            Assert.AreEqual(5, paramItem.Refs.Count, "Expected exactly 5 <Ref> nodes");
            
            // Validate First Ref
            Assert.AreEqual("VisibleString", paramItem.Refs[0].DataType);
            Assert.AreEqual(20, paramItem.Refs[0].Length);
            Assert.AreEqual(0, paramItem.Refs[0].ByteOffset);
            Assert.AreEqual("?", paramItem.Refs[0].DefaultValue);
            Assert.AreEqual("PARAM_REF_NAME_MisreadingText", paramItem.Refs[0].TextId);

            // Validate Second Ref
            Assert.AreEqual("Bit", paramItem.Refs[1].DataType);
            Assert.AreEqual(20, paramItem.Refs[1].ByteOffset);
            Assert.AreEqual(5, paramItem.Refs[1].BitOffset);
            Assert.AreEqual("0", paramItem.Refs[1].DefaultValue);
            Assert.AreEqual("0..1", paramItem.Refs[1].AllowedValues);
            Assert.AreEqual("PARAM_REF_NAME_DecodingResultRG", paramItem.Refs[1].TextId);

            // Validate Third Ref
            Assert.AreEqual("Bit", paramItem.Refs[2].DataType);
            Assert.AreEqual(21, paramItem.Refs[2].ByteOffset);
            Assert.AreEqual(0, paramItem.Refs[2].BitOffset);
            Assert.AreEqual("0", paramItem.Refs[2].DefaultValue);
            Assert.AreEqual("0..1", paramItem.Refs[2].AllowedValues);
            Assert.AreEqual("PARAM_REF_NAME_DataAlignment", paramItem.Refs[2].TextId);

            // Validate Fourth Ref (BitArea)
            Assert.AreEqual("BitArea", paramItem.Refs[3].DataType);
            Assert.AreEqual(21, paramItem.Refs[3].ByteOffset);
            Assert.AreEqual(4, paramItem.Refs[3].BitOffset);
            Assert.AreEqual(4, paramItem.Refs[3].BitLength);
            Assert.AreEqual("3", paramItem.Refs[3].DefaultValue);
            Assert.AreEqual("0 3", paramItem.Refs[3].AllowedValues);
            Assert.AreEqual("PARAM_REF_NAME_FillMode", paramItem.Refs[3].TextId);

            // Validate Fifth Ref (Unsigned8)
            Assert.AreEqual("Unsigned8", paramItem.Refs[4].DataType);
            Assert.AreEqual(22, paramItem.Refs[4].ByteOffset);
            Assert.AreEqual("0", paramItem.Refs[4].DefaultValue);
            Assert.AreEqual("0..255", paramItem.Refs[4].AllowedValues);
            Assert.AreEqual("PARAM_REF_NAME_FillCharacter", paramItem.Refs[4].TextId);
        }

    }
}
