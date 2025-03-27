using NUnit.Framework;
using System;
using System.Xml;
using System.Collections.Generic;
using System.IO;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;

namespace PLC.Commissioning.Lib.Siemens.Tests.SiemensPLCControllerTests.Hardware.GSD
{
    [TestFixture]
    public class FParameterRecordDataItemTests
    {
        private string _testDataPath;
        private GSDHandler _gsdHandler;

        [SetUp]
        public void Setup()
        {
            _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "gsd");
            _gsdHandler = new GSDHandler();
        }

        [Test]
        [TestCase("GSDML-V2.42-LEUZE-RSL400P CU 4M12-20230816.xml")] // Future test cases can be added here
        public void ParseFParameterRecordDataItem_FromConcreteGSDML(string fileName)
        {
            string filePath = Path.Combine(_testDataPath, fileName);
            
            Assert.IsTrue(File.Exists(filePath), $"Test file not found: {filePath}");

            var gsdHandler = new GSDHandler();
            bool initSuccess = gsdHandler.Initialize(filePath);
            Assert.IsTrue(initSuccess, $"Failed to initialize GSDHandler for {fileName}");

            // Load the GSDML file
            XmlDocument gsdmlDoc = new XmlDocument();
            gsdmlDoc.Load(filePath);

            // Find the first FParameterRecordDataItem in M1_SAFE_SIGNAL ModuleItem
            XmlNode fParameterNode = gsdmlDoc.SelectSingleNode("//gsd:ModuleItem[@ID='M1_SAFE_SIGNAL']//gsd:F_ParameterRecordDataItem", gsdHandler.nsmgr);
            Assert.IsNotNull(fParameterNode, $"No FParameterRecordDataItem found in {fileName} for module M1_SAFE_SIGNAL");

            // Parse the FParameterRecordDataItem
            var fParamItem = new FParameterRecordDataItem(gsdHandler);
            fParamItem.ParseFParameterRecordDataItem(fParameterNode);

            // Perform Assertions
            Assert.IsNotNull(fParamItem, "FParameterRecordDataItem should not be null.");
            
            Assert.IsNotNull(fParamItem.F_ParamDescCRC, "F_ParamDescCRC should be parsed.");
            Assert.IsNotNull(fParamItem.F_SIL, "F_SIL should be parsed.");
            Assert.IsNotNull(fParamItem.F_CRC_Length, "F_CRC_Length should be parsed.");
            Assert.IsNotNull(fParamItem.F_Block_ID, "F_Block_ID should be parsed.");
            Assert.IsNotNull(fParamItem.F_Par_Version, "F_Par_Version should be parsed.");
            Assert.IsNotNull(fParamItem.F_Source_Add, "F_Source_Add should be parsed.");
            Assert.IsNotNull(fParamItem.F_Dest_Add, "F_Dest_Add should be parsed.");
            Assert.IsNotNull(fParamItem.F_WD_Time, "F_WD_Time should be parsed.");
            Assert.IsNotNull(fParamItem.F_Par_CRC, "F_Par_CRC should be parsed.");

            // Check expected values for safety parameters (Update with real expected values)
            Assert.AreEqual("65037", fParamItem.F_ParamDescCRC);
            Assert.AreEqual("SIL2", fParamItem.F_SIL);
            Assert.AreEqual("3-Byte-CRC", fParamItem.F_CRC_Length);
            Assert.AreEqual("0", fParamItem.F_Block_ID);
            Assert.AreEqual("1..65534", fParamItem.F_Source_Add);
            Assert.AreEqual("1..65534", fParamItem.F_Dest_Add);
            Assert.AreEqual("100", fParamItem.F_WD_Time);
            Assert.AreEqual("49406", fParamItem.F_Par_CRC);

            Console.WriteLine(fParamItem.ToString());
        }
    }
}
