using NUnit.Framework;
using System;
using System.Linq;
using System.Text;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;

namespace PLC.Commissioning.Lib.Siemens.Tests.SiemensPLCControllerTests.Hardware.GSD
{
    [TestFixture]
    public class DataTypeSetterTests
    {
        private byte[] testData;

        [SetUp]
        public void Setup()
        {
            // Initialize a byte array with enough space for testing multi-byte operations
            testData = new byte[8]; // 8 bytes to accommodate all data types
            Array.Clear(testData, 0, testData.Length); // Start with all zeros
        }

        #region SetBitValue Tests

        [Test]
        public void SetBitValue_ValidParameters_SetsBitCorrectly()
        {
            Assert.IsTrue(DataTypeSetter.SetBitValue(testData, 0, 0, true), "Should set bit 0 successfully");
            Assert.AreEqual(0b00000001, testData[0], "Bit 0 should be set to 1");

            Assert.IsTrue(DataTypeSetter.SetBitValue(testData, 0, 1, true), "Should set bit 1 successfully");
            Assert.AreEqual(0b00000011, testData[0], "Bits 0 and 1 should be set");

            Assert.IsTrue(DataTypeSetter.SetBitValue(testData, 0, 0, false), "Should clear bit 0 successfully");
            Assert.AreEqual(0b00000010, testData[0], "Bit 0 should be cleared, bit 1 remains set");
        }

        [Test]
        public void SetBitValue_InvalidParameters_ReturnsFalse()
        {
            Assert.IsFalse(DataTypeSetter.SetBitValue(null, 0, 0, true), "Null array should return false");
            Assert.IsFalse(DataTypeSetter.SetBitValue(testData, -1, 0, true), "Negative byte offset should return false");
            Assert.IsFalse(DataTypeSetter.SetBitValue(testData, 8, 0, true), "Byte offset beyond length should return false");
            Assert.IsFalse(DataTypeSetter.SetBitValue(testData, 0, -1, true), "Negative bit offset should return false");
            Assert.IsFalse(DataTypeSetter.SetBitValue(testData, 0, 8, true), "Bit offset > 7 should return false");
        }

        #endregion

        #region SetBitAreaValue Tests

        [Test]
        public void SetBitAreaValue_ValidParameters_SetsBitsCorrectly()
        {
            Assert.IsTrue(DataTypeSetter.SetBitAreaValue(testData, 0, 0, 2, 3), "Should set bits 0-1 to 3 (0b11)");
            Assert.AreEqual(0b00000011, testData[0], "Bits 0-1 should be 11");

            Assert.IsTrue(DataTypeSetter.SetBitAreaValue(testData, 0, 2, 3, 5), "Should set bits 2-4 to 5 (0b101)");
            Assert.AreEqual(0b00010111, testData[0], "Bits 0-1 = 11, bits 2-4 = 101");

            Assert.IsTrue(DataTypeSetter.SetBitAreaValue(testData, 0, 2, 3, 0), "Should clear bits 2-4");
            Assert.AreEqual(0b00000011, testData[0], "Bits 2-4 should be 000");
        }

        [Test]
        public void SetBitAreaValue_InvalidParameters_ReturnsFalse()
        {
            Assert.IsFalse(DataTypeSetter.SetBitAreaValue(null, 0, 0, 2, 1), "Null array should return false");
            Assert.IsFalse(DataTypeSetter.SetBitAreaValue(testData, -1, 0, 2, 1), "Negative byte offset should return false");
            Assert.IsFalse(DataTypeSetter.SetBitAreaValue(testData, 8, 0, 2, 1), "Byte offset beyond length should return false");
            Assert.IsFalse(DataTypeSetter.SetBitAreaValue(testData, 0, -1, 2, 1), "Negative bit offset should return false");
            Assert.IsFalse(DataTypeSetter.SetBitAreaValue(testData, 0, 0, 0, 1), "Bit length <= 0 should return false");
            Assert.IsFalse(DataTypeSetter.SetBitAreaValue(testData, 0, 7, 2, 1), "Bit offset + length > 8 should return false");
            Assert.IsFalse(DataTypeSetter.SetBitAreaValue(testData, 0, 0, 2, -1), "Negative value should return false");
            Assert.IsFalse(DataTypeSetter.SetBitAreaValue(testData, 0, 0, 2, 4), "Value > max (3 for 2 bits) should return false");
        }

        #endregion

        #region SetInt32Value Tests

        [Test]
        public void SetInt32Value_ValidOffset_SetsValueCorrectly()
        {
            Assert.IsTrue(DataTypeSetter.SetInt32Value(testData, 0, 0x12345678));
            byte[] expected = { 0x12, 0x34, 0x56, 0x78 }; // Big-Endian
            CollectionAssert.AreEqual(expected, testData.Take(4).ToArray());
        }

        [Test]
        public void SetInt32Value_InvalidOffset_ReturnsFalse()
        {
            Assert.IsFalse(DataTypeSetter.SetInt32Value(null, 0, 1), "Null array should return false");
            Assert.IsFalse(DataTypeSetter.SetInt32Value(testData, -1, 1), "Negative offset should return false");
            Assert.IsFalse(DataTypeSetter.SetInt32Value(testData, 5, 1), "Offset + 4 > length should return false");
        }

        #endregion

        #region SetUInt16Value Tests

        [Test]
        public void SetUInt16Value_ValidOffset_SetsValueCorrectly()
        {
            Assert.IsTrue(DataTypeSetter.SetUInt16Value(testData, 0, 0x1234), "Should set 16-bit value successfully");

            byte[] expected = { 0x12, 0x34 }; // Big-Endian expected result
            CollectionAssert.AreEqual(expected, testData.Take(2).ToArray(), "Should set bytes 0-1 to 0x12, 0x34 in Big-Endian");
        }

        [Test]
        public void SetUInt16Value_InvalidOffset_ReturnsFalse()
        {
            Assert.IsFalse(DataTypeSetter.SetUInt16Value(null, 0, 1), "Null array should return false");
            Assert.IsFalse(DataTypeSetter.SetUInt16Value(testData, -1, 1), "Negative offset should return false");
            Assert.IsFalse(DataTypeSetter.SetUInt16Value(testData, 7, 1), "Offset + 2 > length should return false");
        }

        #endregion

        #region SetUInt8Value Tests

        [Test]
        public void SetUInt8Value_ValidOffset_SetsValueCorrectly()
        {
            Assert.IsTrue(DataTypeSetter.SetUInt8Value(testData, 0, 0xAB), "Should set 8-bit value successfully");
            Assert.AreEqual(0xAB, testData[0], "Byte 0 should be 0xAB");
        }

        [Test]
        public void SetUInt8Value_InvalidOffset_ReturnsFalse()
        {
            Assert.IsFalse(DataTypeSetter.SetUInt8Value(null, 0, 1), "Null array should return false");
            Assert.IsFalse(DataTypeSetter.SetUInt8Value(testData, -1, 1), "Negative offset should return false");
            Assert.IsFalse(DataTypeSetter.SetUInt8Value(testData, 8, 1), "Offset >= length should return false");
        }

        #endregion

        #region SetInt16Value Tests

        [Test]
        public void SetInt16Value_ValidOffset_SetsValueCorrectly()
        {
            Assert.IsTrue(DataTypeSetter.SetInt16Value(testData, 0, -1234));
            byte[] expected = BitConverter.GetBytes((short)-1234); // BitConverter returns in Little-Endian 
            Array.Reverse(expected); // Big-Endian to match output
            CollectionAssert.AreEqual(expected, testData.Take(2).ToArray());
        }

        [Test]
        public void SetInt16Value_InvalidOffset_ReturnsFalse()
        {
            Assert.IsFalse(DataTypeSetter.SetInt16Value(null, 0, 1), "Null array should return false");
            Assert.IsFalse(DataTypeSetter.SetInt16Value(testData, -1, 1), "Negative offset should return false");
            Assert.IsFalse(DataTypeSetter.SetInt16Value(testData, 7, 1), "Offset + 2 > length should return false");
        }

        #endregion

        #region SetVisibleStringValue Tests

        [Test]
        public void SetVisibleStringValue_ValidParameters_SetsStringCorrectly()
        {
            Assert.IsTrue(DataTypeSetter.SetVisibleStringValue(testData, 0, 5, "Hello"), "Should set string successfully");
            byte[] expected = Encoding.ASCII.GetBytes("Hello");
            CollectionAssert.AreEqual(expected, testData.Take(5).ToArray(), "Should set bytes 0-4 to 'Hello'");

            Assert.IsTrue(DataTypeSetter.SetVisibleStringValue(testData, 0, 3, "Hi"), "Should set shorter string with padding");
            CollectionAssert.AreEqual(Encoding.ASCII.GetBytes("Hi\0"), testData.Take(3).ToArray(), "Should pad with null");

            Assert.IsTrue(DataTypeSetter.SetVisibleStringValue(testData, 0, 3, "Hello"), "Should truncate longer string");
            CollectionAssert.AreEqual(Encoding.ASCII.GetBytes("Hel"), testData.Take(3).ToArray(), "Should truncate to 3 bytes");
        }

        [Test]
        public void SetVisibleStringValue_InvalidParameters_ReturnsFalse()
        {
            Assert.IsFalse(DataTypeSetter.SetVisibleStringValue(null, 0, 5, "Hello"), "Null array should return false");
            Assert.IsFalse(DataTypeSetter.SetVisibleStringValue(testData, -1, 5, "Hello"), "Negative offset should return false");
            Assert.IsFalse(DataTypeSetter.SetVisibleStringValue(testData, 0, -1, "Hello"), "Negative length should return false");
            Assert.IsFalse(DataTypeSetter.SetVisibleStringValue(testData, 4, 5, "Hello"), "Offset + length > array length should return false");
        }

        #endregion
    }
}