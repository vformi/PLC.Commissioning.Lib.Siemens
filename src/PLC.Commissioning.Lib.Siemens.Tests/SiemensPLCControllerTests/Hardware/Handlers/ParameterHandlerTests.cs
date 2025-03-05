using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Handlers;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Abstractions;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Abstractions;

namespace PLC.Commissioning.Lib.Siemens.Tests.SiemensPLCControllerTests.Hardware.Handlers
{
    [TestFixture]
    public class ParameterHandlerTests
    {
        private Mock<IDeviceItem> _mockDeviceItem;
        private Mock<GSDHandler> _mockGsdHandler;
        private ParameterHandler _parameterHandler;

        [SetUp]
        public void Setup()
        {
            _mockDeviceItem = new Mock<IDeviceItem>();
            _mockGsdHandler = new Mock<GSDHandler>();

            XmlDocument xmlDoc = new XmlDocument();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("gsd", "http://www.profibus.com/GSDML");
            _mockGsdHandler.Object.nsmgr = nsmgr;

            var parameterRecordDataItem = new ParameterRecordDataItem(_mockGsdHandler.Object)
            {
                DsNumber = 1,
                LengthInBytes = 33,
                Refs = new List<RefModel>
                {
                    new RefModel { DataType = "Unsigned8", ByteOffset = 0, Text = "Profile", DefaultValue = "0" },
                    new RefModel
                    {
                        DataType = "BitArea", ByteOffset = 1, BitOffset = 0, BitLength = 6, Text = "Codetype1",
                        DefaultValue = "1"
                    },
                    new RefModel { DataType = "Bit", ByteOffset = 2, BitOffset = 6, Text = "Mode", DefaultValue = "0" },
                    new RefModel { DataType = "Integer16", ByteOffset = 3, Text = "Codesize1", DefaultValue = "10" },
                    new RefModel
                    {
                        DataType = "Unsigned8", ByteOffset = 5, Text = "Codesize2", DefaultValue = "0",
                        AllowedValues = "0..63"
                    },
                    new RefModel { DataType = "Unsigned16", ByteOffset = 7, Text = "Speed", DefaultValue = "0" },
                    new RefModel { DataType = "Integer32", ByteOffset = 9, Text = "Position", DefaultValue = "0" },
                    new RefModel
                    {
                        DataType = "VisibleString", ByteOffset = 13, Length = 8, Text = "Label",
                        DefaultValue = "DEFAULT"
                    }
                }
            };

            _mockDeviceItem.Setup(x => x.ParameterRecordDataItem).Returns(parameterRecordDataItem);
            _parameterHandler = new ParameterHandler(_mockDeviceItem.Object);
        }

        [Test]
        public void ParseModuleData_Should_Parse_All_DataTypes_Correctly()
        {
            // Arrange
            byte[] testData = new byte[33];
            testData[0] = 5; // Unsigned8 (Profile)
            testData[1] = 0b00110011; // BitArea (Codetype1)
            testData[2] = 0b01000000; // Bit (Mode)
    
            // Integer16 (Codesize1) = 1000 (0x03E8) → Big-Endian
            testData[3] = 0x03;
            testData[4] = 0xE8;

            testData[5] = 42; // Unsigned8 (Codesize2)
    
            // Unsigned16 (Speed) = 43981 (0xABCD) → Big-Endian
            testData[7] = 0xAB;
            testData[8] = 0xCD;
    
            // Integer32 (Position) = 305419896 (0x12345678) → Big-Endian
            testData[9] = 0x12;
            testData[10] = 0x34;
            testData[11] = 0x56;
            testData[12] = 0x78;

            Array.Copy(Encoding.ASCII.GetBytes("TEST1234"), 0, testData, 13, 8); // VisibleString

            // Act
            var result = _parameterHandler.ParseModuleData(testData);

            // Assert
            Assert.AreEqual(8, result.Count);
            Assert.AreEqual(5, result.Find(r => r.Parameter == "Profile").Value);
            Assert.AreEqual(0b00110011, result.Find(r => r.Parameter == "Codetype1").Value);
            Assert.AreEqual(1, result.Find(r => r.Parameter == "Mode").Value);
            Assert.AreEqual(1000, result.Find(r => r.Parameter == "Codesize1").Value);
            Assert.AreEqual(42, result.Find(r => r.Parameter == "Codesize2").Value);
            Assert.AreEqual(43981, result.Find(r => r.Parameter == "Speed").Value);
            Assert.AreEqual(305419896, result.Find(r => r.Parameter == "Position").Value);
            Assert.AreEqual("TEST1234", result.Find(r => r.Parameter == "Label").Value);
        }
        
        [Test]
        public void WriteModuleData_Should_Modify_All_DataTypes_Correctly()
        {
            byte[] originalData = new byte[33];
            var parameterValues = new Dictionary<string, object>
            {
                { "Profile", 10 },
                { "Codetype1", 0b00001111 },
                { "Mode", 1 },
                { "Codesize1", -1000 }, // 0xFC18
                { "Codesize2", 63 },
                { "Speed", 50000 }, // 0xC350
                { "Position", -123456789 }, // 0xF8A432EB
                { "Label", "NEWTEST1" }
            };
            byte[] modifiedData = _parameterHandler.WriteModuleData(originalData, parameterValues);
            
            Console.WriteLine($"DEBUG: Modified Data: {BitConverter.ToString(modifiedData)}");
            
            Assert.AreEqual(10, modifiedData[0]);
            Assert.AreEqual(0b00001111, modifiedData[1]);
            Assert.AreEqual(0b01000000, modifiedData[2]);
            Assert.AreEqual(0xFC, modifiedData[3]); // High byte -1000
            Assert.AreEqual(0x18, modifiedData[4]); // Low byte
            Assert.AreEqual(63, modifiedData[5]);
            Assert.AreEqual(0xC3, modifiedData[7]); // High byte 50000
            Assert.AreEqual(0x50, modifiedData[8]); // Low byte 
            Assert.AreEqual(0xF8, modifiedData[9]);  // High byte -123456789
            Assert.AreEqual(0xA4, modifiedData[10]);
            Assert.AreEqual(0x32, modifiedData[11]);
            Assert.AreEqual(0xEB, modifiedData[12]); // Low byte
            Assert.AreEqual("NEWTEST1", Encoding.ASCII.GetString(modifiedData, 13, 8));
        }

        [TestCase("Profile", 256, false)] // Unsigned8 out of range
        [TestCase("Codesize2", 64, false)] // Exceeds AllowedValues 0..63
        [TestCase("Mode", 2, false)] // Bit out of range
        [TestCase("Codetype1", 64, false)] // BitArea exceeds 6 bits
        [TestCase("Codesize1", 32768, false)] // Integer16 out of range
        [TestCase("Speed", 65536, false)] // Unsigned16 out of range
        [TestCase("Label", "TOO_LONG_STRING", false)] // VisibleString too long
        public void WriteModuleData_Should_Return_Null_For_Invalid_Values(string param, object value,
            bool expectedValid)
        {
            // Arrange
            byte[] originalData = new byte[33];
            var parameterValues = new Dictionary<string, object> { { param, value } };

            // Act
            byte[] modifiedData = _parameterHandler.WriteModuleData(originalData, parameterValues);

            // Assert
            Assert.IsNull(modifiedData);
        }

        [Test]
        public void ParseModuleData_With_Parameter_Selection_Should_Return_Only_Selected()
        {
            // Arrange
            byte[] testData = new byte[33];
            testData[0] = 5;
            testData[5] = 42;
            var selections = new List<string> { "Profile", "Codesize2" };

            // Act
            var result = _parameterHandler.ParseModuleData(testData, selections);

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsNotNull(result.Find(r => r.Parameter == "Profile"));
            Assert.IsNotNull(result.Find(r => r.Parameter == "Codesize2"));
            Assert.IsNull(result.Find(r => r.Parameter == "Mode"));
        }

        [Test]
        public void AreParameterKeysValid_Should_Validate_Correctly()
        {
            // Arrange
            var validKeys = new Dictionary<string, object>
            {
                { "Profile", 10 },
                { "Codesize2", 20 }
            };
            var invalidKeys = new Dictionary<string, object>
            {
                { "NonExistent", 10 }
            };
            var emptyKeys = new Dictionary<string, object>
            {
                { "", 10 }
            };

            // Act & Assert
            Assert.IsTrue(_parameterHandler.AreParameterKeysValid(validKeys));
            Assert.IsFalse(_parameterHandler.AreParameterKeysValid(invalidKeys));
            Assert.IsFalse(_parameterHandler.AreParameterKeysValid(emptyKeys));
            Assert.IsFalse(_parameterHandler.AreParameterKeysValid(null));
        }

        [Test]
        public void WriteModuleData_With_AllowedValues_Should_Validate_Range()
        {
            // Arrange
            byte[] originalData = new byte[33];
            var validValue = new Dictionary<string, object> { { "Codesize2", 50 } };
            var invalidValue = new Dictionary<string, object> { { "Codesize2", 64 } };

            // Act
            var validResult = _parameterHandler.WriteModuleData(originalData, validValue);
            var invalidResult = _parameterHandler.WriteModuleData(originalData, invalidValue);

            // Assert
            Assert.IsNotNull(validResult);
            Assert.AreEqual(50, validResult[5]);
            Assert.IsNull(invalidResult);
        }
    }
}

