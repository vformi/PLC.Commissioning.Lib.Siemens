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
        [TestCase("GSDML-V2.42-LEUZE-RSL400P CU 4M12-20230816.xml")]
        public void ParseFParameterRecordDataItem_ParsesAllExpectedAttributes(string fileName)
        {
            string filePath = Path.Combine(_testDataPath, fileName);
            Assert.IsTrue(File.Exists(filePath), $"Test file not found: {filePath}");

            var gsdHandler = new GSDHandler();
            Assert.IsTrue(gsdHandler.Initialize(filePath), "GSDHandler failed to initialize.");

            XmlDocument gsdmlDoc = new XmlDocument();
            gsdmlDoc.Load(filePath);

            XmlNode fParameterNode = gsdmlDoc.SelectSingleNode("//gsd:ModuleItem[@ID='M1_SAFE_SIGNAL']//gsd:F_ParameterRecordDataItem", gsdHandler.nsmgr);
            Assert.IsNotNull(fParameterNode, "F_ParameterRecordDataItem node not found.");

            var fParamItem = new FParameterRecordDataItem(gsdHandler);
            fParamItem.ParseFParameterRecordDataItem(fParameterNode);

            // General integrity checks
            Assert.IsNotNull(fParamItem.F_ParamDescCRC, "F_ParamDescCRC must not be null.");
            Assert.IsNotEmpty(fParamItem.Parameters, "Parameters dictionary should not be empty.");

            // List of expected parameter keys to validate
            string[] expectedParams = new[]
            {
                "F_SIL",
                "F_CRC_Length",
                "F_Block_ID",
                "F_Par_Version",
                "F_Source_Add",
                "F_Dest_Add",
                "F_WD_Time",
                "F_Par_CRC",
            };

            foreach (string paramName in expectedParams)
            {
                Assert.IsTrue(fParamItem.Parameters.ContainsKey(paramName), $"Missing expected F-Parameter: {paramName}");
                Assert.IsNotNull(fParamItem.Parameters[paramName], $"{paramName} should not be null.");
            }

            // Specific value checks (you should adjust these based on actual GSD content)
            Assert.AreEqual("65037", fParamItem.F_ParamDescCRC);
            Assert.AreEqual("SIL2", fParamItem.Parameters["F_SIL"].DefaultValue);
            Assert.AreEqual("3-Byte-CRC", fParamItem.Parameters["F_CRC_Length"].DefaultValue);
            Assert.AreEqual("0", fParamItem.Parameters["F_Block_ID"].DefaultValue);
            Assert.AreEqual("1..65534", fParamItem.Parameters["F_Source_Add"].AllowedValues);
            Assert.AreEqual("1..65534", fParamItem.Parameters["F_Dest_Add"].AllowedValues);
            Assert.AreEqual("100", fParamItem.Parameters["F_WD_Time"].DefaultValue);
            Assert.AreEqual("49406", fParamItem.Parameters["F_Par_CRC"].DefaultValue);

            TestContext.Out.WriteLine(fParamItem.ToString());
        }
    }
}
