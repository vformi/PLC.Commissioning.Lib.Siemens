using NUnit.Framework;
using System;
using System.Text;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;

namespace PLC.Commissioning.Lib.Siemens.Tests.SiemensPLCControllerTests.Hardware.GSD
{
    [TestFixture]
    public class DataTypeGetterTests
    {
        private byte[] testData;

        [SetUp]
        public void Setup()
        {
            // Initialize a general-purpose byte array for bit and byte-level tests
            testData = new byte[] { 0b00000001, 0b00000010, 0x03, 0x04, 0x05, 0x06 };
            // 0	0x01	0b00000001
            // 1	0x02	0b00000010
            // 2	0x03	0b00000011
            // 3	0x04	0b00000100
            // 4	0x05	0b00000101
            // 5	0x06	0b00000110
        }

        #region GetBitValue Tests

        [Test]
        public void GetBitValue_ValidOffsets_ReturnsCorrectValue()
        {
            Assert.IsTrue(DataTypeGetter.GetBitValue(testData, 0, 0).Value, "Bit 0 of byte 0 should be true (0b00000001)");
            Assert.IsFalse(DataTypeGetter.GetBitValue(testData, 0, 1).Value, "Bit 1 of byte 0 should be false");
            Assert.IsFalse(DataTypeGetter.GetBitValue(testData, 1, 0).Value, "Bit 0 of byte 1 should be false (0b00000010)");
            Assert.IsTrue(DataTypeGetter.GetBitValue(testData, 1, 1).Value, "Bit 1 of byte 1 should be true");
        }

        [Test]
        public void GetBitValue_InvalidOffsets_ReturnsNull()
        {
            Assert.IsNull(DataTypeGetter.GetBitValue(testData, -1, 0), "Negative byte offset should return null");
            Assert.IsNull(DataTypeGetter.GetBitValue(testData, 6, 0), "Byte offset beyond array length should return null");
            Assert.IsNull(DataTypeGetter.GetBitValue(testData, 0, -1), "Negative bit offset should return null");
            Assert.IsNull(DataTypeGetter.GetBitValue(testData, 0, 8), "Bit offset > 7 should return null");
        }

        [Test]
        public void GetBitValue_NullData_ReturnsNull()
        {
            Assert.IsNull(DataTypeGetter.GetBitValue(null, 0, 0), "Null data array should return null");
        }

        #endregion

        #region GetBitAreaValue Tests

        [Test]
        public void GetBitAreaValue_ValidParameters_ReturnsCorrectValue()
        {
            Assert.AreEqual(1, DataTypeGetter.GetBitAreaValue(testData, 0, 0, 2).Value, "Bits 0-1 of byte 0 (0b01) should be 1");
            Assert.AreEqual(0, DataTypeGetter.GetBitAreaValue(testData, 0, 1, 3).Value, "Bits 1-3 of byte 0 should be 0");
            Assert.AreEqual(1, DataTypeGetter.GetBitAreaValue(testData, 1, 1, 2).Value, "Bits 1-2 of byte 1 (0b10) should be 1 (bit 1=1, bit 2=0)");
        }

        [Test]
        public void GetBitAreaValue_InvalidParameters_ReturnsNull()
        {
            Assert.IsNull(DataTypeGetter.GetBitAreaValue(testData, -1, 0, 1), "Negative byte offset should return null");
            Assert.IsNull(DataTypeGetter.GetBitAreaValue(testData, 6, 0, 1), "Byte offset beyond array length should return null");
            Assert.IsNull(DataTypeGetter.GetBitAreaValue(testData, 0, -1, 1), "Negative bit offset should return null");
            Assert.IsNull(DataTypeGetter.GetBitAreaValue(testData, 0, 0, 0), "Bit length <= 0 should return null");
            Assert.IsNull(DataTypeGetter.GetBitAreaValue(testData, 0, 7, 2), "Bit offset + length > 8 should return null");
            Assert.IsNull(DataTypeGetter.GetBitAreaValue(testData, 0, 0, 9), "Bit length > 8 should return null");
        }

        [Test]
        public void GetBitAreaValue_NullData_ReturnsNull()
        {
            Assert.IsNull(DataTypeGetter.GetBitAreaValue(null, 0, 0, 1), "Null data array should return null");
        }

        #endregion

        #region GetInt32Value Tests

        [Test]
        public void GetInt32Value_ValidOffset_ReturnsCorrectValue()
        {
            byte[] data = new byte[] { 0x12, 0x34, 0x56, 0x78 }; // Big-endian 0x12345678
            int? value = DataTypeGetter.GetInt32Value(data, 0);
            Assert.IsNotNull(value, "Valid offset should not return null");
            Assert.AreEqual(0x12345678, value.Value, "Should correctly interpret big-endian 0x12345678 as 305419896");
        }

        [Test]
        public void GetInt32Value_InvalidOffset_ReturnsNull()
        {
            byte[] data = new byte[] { 0x00, 0x00, 0x00, 0x01 };
            Assert.IsNull(DataTypeGetter.GetInt32Value(data, -1), "Negative offset should return null");
            Assert.IsNull(DataTypeGetter.GetInt32Value(data, 1), "Offset with insufficient bytes (1 + 4 > 4) should return null");
        }

        [Test]
        public void GetInt32Value_NullData_ReturnsNull()
        {
            Assert.IsNull(DataTypeGetter.GetInt32Value(null, 0), "Null data array should return null");
        }

        #endregion

        #region GetUInt16Value Tests

        [Test]
        public void GetUInt16Value_ValidOffset_ReturnsCorrectValue()
        {
            byte[] data = new byte[] { 0x12, 0x34 }; // Big-endian representation of 4660
            ushort? value = DataTypeGetter.GetUInt16Value(data, 0);
            Assert.IsNotNull(value, "Valid offset should not return null");
            Assert.AreEqual(0x1234, value.Value, "Should correctly interpret big-endian 0x1234 as 4660");

            data = new byte[] { 0x11, 0xD0 }; // Big-endian representation of 4560
            value = DataTypeGetter.GetUInt16Value(data, 0);
            Assert.AreEqual(4560, value.Value, "Should correctly interpret big-endian 0x11D0 as 4560");
        }

        [Test]
        public void GetUInt16Value_InvalidOffset_ReturnsNull()
        {
            byte[] data = new byte[] { 0x00, 0x01 };
            Assert.IsNull(DataTypeGetter.GetUInt16Value(data, -1), "Negative offset should return null");
            Assert.IsNull(DataTypeGetter.GetUInt16Value(data, 1), "Offset with insufficient bytes (1 + 2 > 2) should return null");
        }

        [Test]
        public void GetUInt16Value_NullData_ReturnsNull()
        {
            Assert.IsNull(DataTypeGetter.GetUInt16Value(null, 0), "Null data array should return null");
        }

        #endregion

        #region GetUInt8Value Tests

        [Test]
        public void GetUInt8Value_ValidOffset_ReturnsCorrectValue()
        {
            byte[] data = new byte[] { 0x01 };
            byte? value = DataTypeGetter.GetUInt8Value(data, 0);
            Assert.IsNotNull(value, "Valid offset should not return null");
            Assert.AreEqual(1, value.Value, "Should return byte value 1");
        }

        [Test]
        public void GetUInt8Value_InvalidOffset_ReturnsNull()
        {
            byte[] data = new byte[] { 0x01 };
            Assert.IsNull(DataTypeGetter.GetUInt8Value(data, -1), "Negative offset should return null");
            Assert.IsNull(DataTypeGetter.GetUInt8Value(data, 1), "Offset beyond array length should return null");
        }

        [Test]
        public void GetUInt8Value_NullData_ReturnsNull()
        {
            Assert.IsNull(DataTypeGetter.GetUInt8Value(null, 0), "Null data array should return null");
        }

        #endregion

        #region GetInt16Value Tests

        [Test]
        public void GetInt16Value_ValidOffset_ReturnsCorrectValue()
        {
            byte[] data = new byte[] { 0xFF, 0xFF }; // Big-endian representation of -1
            short? value = DataTypeGetter.GetInt16Value(data, 0);
            Assert.IsNotNull(value, "Valid offset should not return null");
            Assert.AreEqual(-1, value.Value, "Should correctly interpret big-endian 0xFFFF as -1");

            data = new byte[] { 0x12, 0x34 }; // Big-endian representation of 4660
            value = DataTypeGetter.GetInt16Value(data, 0);
            Assert.AreEqual(0x1234, value.Value, "Should correctly interpret big-endian 0x1234 as 4660");
        }

        [Test]
        public void GetInt16Value_InvalidOffset_ReturnsNull()
        {
            byte[] data = new byte[] { 0xFF, 0xFF };
            Assert.IsNull(DataTypeGetter.GetInt16Value(data, -1), "Negative offset should return null");
            Assert.IsNull(DataTypeGetter.GetInt16Value(data, 1), "Offset with insufficient bytes (1 + 2 > 2) should return null");
        }

        [Test]
        public void GetInt16Value_NullData_ReturnsNull()
        {
            Assert.IsNull(DataTypeGetter.GetInt16Value(null, 0), "Null data array should return null");
        }

        #endregion

        #region GetVisibleStringValue Tests

        [Test]
        public void GetVisibleStringValue_ValidParameters_ReturnsCorrectString()
        {
            byte[] data = Encoding.ASCII.GetBytes("Hello\0World");
            string value = DataTypeGetter.GetVisibleStringValue(data, 0, 5);
            Assert.AreEqual("Hello", value, "Should return 'Hello' and trim null terminator");
        }

        [Test]
        public void GetVisibleStringValue_InvalidParameters_ReturnsNull()
        {
            byte[] data = Encoding.ASCII.GetBytes("Hello");
            Assert.IsNull(DataTypeGetter.GetVisibleStringValue(data, -1, 5), "Negative offset should return null");
            Assert.IsNull(DataTypeGetter.GetVisibleStringValue(data, 0, 6), "Length exceeding array should return null");
            Assert.IsNull(DataTypeGetter.GetVisibleStringValue(data, 0, -1), "Negative length should return null");
        }

        [Test]
        public void GetVisibleStringValue_NullData_ReturnsNull()
        {
            Assert.IsNull(DataTypeGetter.GetVisibleStringValue(null, 0, 5), "Null data array should return null");
        }

        #endregion
    }
}