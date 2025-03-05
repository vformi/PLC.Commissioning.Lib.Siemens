using NUnit.Framework;
using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;

namespace PLC.Commissioning.Lib.Siemens.Tests.SiemensPLCControllerTests.Hardware.GSD
{
    [TestFixture]
    public class DeviceAccessPointListTests
    {
        private string _testDataPath;

        [SetUp]
        public void Setup()
        {
            _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "gsd");
        }

        [Test]
        [TestCase("GSDML-V2.3-LEUZE-BCL648i-20150128.xml")]
        [TestCase("GSDML-V2.25-LEUZE-BCL348i-20120814.xml")]
        [TestCase("GSDML-V2.31-LEUZE-BCL348i-20150923.xml")]
        [TestCase("GSDML-V2.41-LEUZE-BCL248i-20211213.xml")]
        [TestCase("GSDML-V2.42-LEUZE-RSL400P CU 4M12-20230816.xml")]
        public void DeviceAccessPointList_ParsesDAPItemsCorrectly(string fileName)
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, fileName);
            Assert.IsTrue(File.Exists(filePath), $"Test file not found: {filePath}");

            var gsdHandler = new GSDHandler();
            bool initSuccess = gsdHandler.Initialize(filePath);
            Assert.IsTrue(initSuccess, $"Failed to initialize GSDHandler for {fileName}");

            // Act
            var dapList = new DeviceAccessPointList(gsdHandler);

            // Assert
            Assert.IsNotNull(dapList.DeviceAccessPointItems, "DeviceAccessPointItems list should not be null.");
            Assert.IsNotEmpty(dapList.DeviceAccessPointItems, $"Expected at least one DeviceAccessPointItem in {fileName}.");

            foreach (var dapItem in dapList.DeviceAccessPointItems)
            {
                Assert.IsNotNull(dapItem.Model.ID, "DAP Item ID should be parsed.");
                Assert.IsNotNull(dapItem.Model.ModuleIdentNumber, "ModuleIdentNumber should be parsed.");

                Console.WriteLine($"Parsed Device Access Point: {dapItem.Model.ID}");
            }
        }

        // TODO: Find a GSDML that has FParams inside DAP 
        // [Test]
        // [TestCase("GSDML-V2.42-LEUZE-RSL400P CU 4M12-20230816.xml")]
        // public void DeviceAccessPointList_ParsesFParameterRecordDataItemsCorrectly(string fileName)
        // {
        //     // Arrange
        //     string filePath = Path.Combine(_testDataPath, fileName);
        //     Assert.IsTrue(File.Exists(filePath), $"Test file not found: {filePath}");
        //
        //     var gsdHandler = new GSDHandler();
        //     bool initSuccess = gsdHandler.Initialize(filePath);
        //     Assert.IsTrue(initSuccess, $"Failed to initialize GSDHandler for {fileName}");
        //
        //     // Act
        //     var dapList = new DeviceAccessPointList(gsdHandler);
        //
        //     // Assert
        //     Assert.IsNotNull(dapList.DeviceAccessPointItems, "DeviceAccessPointItems list should not be null.");
        //     Assert.IsNotEmpty(dapList.DeviceAccessPointItems, $"Expected at least one DeviceAccessPointItem in {fileName}.");
        //
        //     bool foundFParamRecordDataItem = false;
        //     foreach (var dapItem in dapList.DeviceAccessPointItems)
        //     {
        //         if (dapItem.Model.FParameterRecordDataItem != null)
        //         {
        //             foundFParamRecordDataItem = true;
        //             Assert.IsNotNull(dapItem.Model.FParameterRecordDataItem.F_ParamDescCRC, "F_ParamDescCRC should be parsed.");
        //             Assert.IsNotNull(dapItem.Model.FParameterRecordDataItem.F_SIL, "F_SIL should be parsed.");
        //             Assert.IsNotNull(dapItem.Model.FParameterRecordDataItem.F_CRC_Length, "F_CRC_Length should be parsed.");
        //             Assert.IsNotNull(dapItem.Model.FParameterRecordDataItem.F_Block_ID, "F_Block_ID should be parsed.");
        //             Assert.IsNotNull(dapItem.Model.FParameterRecordDataItem.F_Par_Version, "F_Par_Version should be parsed.");
        //             Assert.IsNotNull(dapItem.Model.FParameterRecordDataItem.F_Source_Add, "F_Source_Add should be parsed.");
        //             Assert.IsNotNull(dapItem.Model.FParameterRecordDataItem.F_Dest_Add, "F_Dest_Add should be parsed.");
        //             Assert.IsNotNull(dapItem.Model.FParameterRecordDataItem.F_WD_Time, "F_WD_Time should be parsed.");
        //             Assert.IsNotNull(dapItem.Model.FParameterRecordDataItem.F_Par_CRC, "F_Par_CRC should be parsed.");
        //
        //             Console.WriteLine($"Parsed F-Parameter Record DataItem for DAP: {dapItem.Model.ID}");
        //         }
        //     }
        //
        //     Assert.IsTrue(foundFParamRecordDataItem, "Expected at least one DAP to contain an F-ParameterRecordDataItem.");
        // }
        
        [Test]
        [TestCase("GSDML-V2.3-LEUZE-BCL648i-20150128.xml")]
        public void DeviceAccessPointList_ParsesParameterRecordDataItemsCorrectly(string fileName)
        {
            // Arrange
            string filePath = Path.Combine(_testDataPath, fileName);
            Assert.IsTrue(File.Exists(filePath), $"Test file not found: {filePath}");
        
            var gsdHandler = new GSDHandler();
            bool initSuccess = gsdHandler.Initialize(filePath);
            Assert.IsTrue(initSuccess, $"Failed to initialize GSDHandler for {fileName}");
        
            // Act
            var dapList = new DeviceAccessPointList(gsdHandler);
        
            // Assert
            Assert.IsNotNull(dapList.DeviceAccessPointItems, "DeviceAccessPointItems list should not be null.");
            Assert.IsNotEmpty(dapList.DeviceAccessPointItems, $"Expected at least one DeviceAccessPointItem in {fileName}.");
        
            bool foundParamRecordDataItem = false;
            foreach (var dapItem in dapList.DeviceAccessPointItems)
            {
                if (dapItem.Model.ParameterRecordDataItem != null)
                {
                    foundParamRecordDataItem = true;
                    Assert.IsNotNull(dapItem.Model.ParameterRecordDataItem.DsNumber, "DsNumber should be parsed.");
                    Assert.IsNotNull(dapItem.Model.ParameterRecordDataItem.LengthInBytes, "LengthInBytes should be parsed.");
                    Assert.IsNotNull(dapItem.Model.ParameterRecordDataItem.Refs, "Refs should be parsed.");
                    Assert.IsNotEmpty(dapItem.Model.ParameterRecordDataItem.Refs, "Refs should contain at least one item.");
        
                    Console.WriteLine($"Parsed Parameter Record DataItem for DAP: {dapItem.Model.ID}");
                }
            }
        
            Assert.IsTrue(foundParamRecordDataItem, "Expected at least one DAP to contain a ParameterRecordDataItem.");
        }
    }
}
