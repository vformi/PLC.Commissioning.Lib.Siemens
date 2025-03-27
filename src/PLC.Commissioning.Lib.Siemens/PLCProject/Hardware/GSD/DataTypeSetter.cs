using System;
using System.Text;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD
{
    /// <summary>
    /// Provides methods to set various data types in a byte array,
    /// with proper boundary checks to handle invalid inputs gracefully.
    /// Each method returns a boolean indicating success or failure.
    /// </summary>
    public static class DataTypeSetter
    {
        /// <summary>
        /// Sets a boolean value at a specific bit in a byte array.
        /// Returns <c>true</c> if the value was set successfully; <c>false</c> if invalid parameters were provided.
        /// </summary>
        public static bool SetBitValue(byte[] data, int byteOffset, int bitOffset, bool value)
        {
            // Validate parameters
            if (data is null || byteOffset < 0 || byteOffset >= data.Length)
            {
                // Invalid byte offset
                return false;
            }

            if (bitOffset < 0 || bitOffset > 7)
            {
                // Invalid bit offset
                return false;
            }

            if (value)
            {
                data[byteOffset] |= (byte)(1 << bitOffset);
            }
            else
            {
                data[byteOffset] &= (byte)~(1 << bitOffset);
            }

            return true;
        }

        /// <summary>
        /// Sets an integer value at a specific bit area in a byte array.
        /// Returns <c>true</c> if the value was set successfully; <c>false</c> if invalid parameters were provided.
        /// </summary>
        public static bool SetBitAreaValue(byte[] data, int byteOffset, int bitOffset, int bitLength, int value)
        {
            // Validate parameters
            if (data is null || byteOffset < 0 || byteOffset >= data.Length)
            {
                return false;
            }

            if (bitOffset < 0 || bitOffset > 7 || bitLength <= 0 || bitLength > 8 || bitOffset + bitLength > 8)
            {
                return false;
            }

            int maxValue = (1 << bitLength) - 1;
            if (value < 0 || value > maxValue)
            {
                return false;
            }

            // Clear the bit area
            byte mask = (byte)(((1 << bitLength) - 1) << bitOffset);
            data[byteOffset] &= (byte)~mask;

            // Set the new value
            data[byteOffset] |= (byte)((value << bitOffset) & mask);

            return true;
        }

        /// <summary>
        /// Sets a 32-bit signed integer value in a byte array.
        /// Returns <c>true</c> if the value was set successfully; <c>false</c> if invalid parameters were provided.
        /// </summary>
        public static bool SetInt32Value(byte[] data, int byteOffset, int value)
        {
            // Validate parameters
            if (data is null || byteOffset < 0 || byteOffset + 4 > data.Length)
            {
                return false;
            }

            byte[] intBytes = BitConverter.GetBytes(value);
            Array.Reverse(intBytes); // Store in Big-endian 
            Array.Copy(intBytes, 0, data, byteOffset, 4);

            return true;
        }

        /// <summary>
        /// Sets a 16-bit unsigned integer value in a byte array.
        /// Returns <c>true</c> if the value was set successfully; <c>false</c> if invalid parameters were provided.
        /// </summary>
        public static bool SetUInt16Value(byte[] data, int byteOffset, ushort value)
        {
            if (data is null || byteOffset < 0 || byteOffset + 2 > data.Length)
            {
                return false;
            }

            byte[] ushortBytes = BitConverter.GetBytes(value);
            Array.Reverse(ushortBytes); // Store in Big-endian 
            Array.Copy(ushortBytes, 0, data, byteOffset, 2);

            return true;
        }

        /// <summary>
        /// Sets an 8-bit unsigned integer value in a byte array.
        /// Returns <c>true</c> if the value was set successfully; <c>false</c> if invalid parameters were provided.
        /// </summary>
        public static bool SetUInt8Value(byte[] data, int byteOffset, byte value)
        {
            if (data is null || byteOffset < 0 || byteOffset >= data.Length)
            {
                return false;
            }

            data[byteOffset] = value;

            return true;
        }

        /// <summary>
        /// Sets a 16-bit signed integer value in a byte array.
        /// Returns <c>true</c> if the value was set successfully; <c>false</c> if invalid parameters were provided.
        /// </summary>
        public static bool SetInt16Value(byte[] data, int byteOffset, short value)
        {
            if (data is null || byteOffset < 0 || byteOffset + 2 > data.Length)
            {
                return false;
            }

            byte[] shortBytes = BitConverter.GetBytes(value);
            Array.Reverse(shortBytes); // Store in Big-endian 
            Array.Copy(shortBytes, 0, data, byteOffset, 2);

            return true;
        }

        /// <summary>
        /// Sets a visible string (ASCII) value in a byte array.
        /// Returns <c>true</c> if the value was set successfully; <c>false</c> if invalid parameters were provided.
        /// </summary>
        public static bool SetVisibleStringValue(byte[] data, int byteOffset, int length, string value)
        {
            if (data is null || byteOffset < 0 || length < 0 || byteOffset + length > data.Length)
            {
                return false;
            }

            byte[] stringBytes = Encoding.ASCII.GetBytes(value);

            // Truncate or pad the string to fit the specified length
            byte[] paddedStringBytes = new byte[length];
            Array.Copy(stringBytes, 0, paddedStringBytes, 0, Math.Min(length, stringBytes.Length));

            // The rest remains zero (null characters)
            Array.Copy(paddedStringBytes, 0, data, byteOffset, length);

            return true;
        }

        // Additional methods with proper checks and return values can be added here...
    }
}
