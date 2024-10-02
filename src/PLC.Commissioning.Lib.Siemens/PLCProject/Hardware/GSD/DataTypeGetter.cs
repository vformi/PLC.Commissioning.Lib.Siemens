using System;
using System.Text;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD
{
    /// <summary>
    /// Provides methods to retrieve various data types from a byte array,
    /// with proper boundary checks and nullable return types to handle invalid accesses.
    /// </summary>
    public static class DataTypeGetter
    {
        /// <summary>
        /// Retrieves a boolean value from a specific bit in a byte array.
        /// Returns <c>null</c> if the bit cannot be accessed due to invalid offsets.
        /// </summary>
        /// <param name="data">The byte array containing the data.</param>
        /// <param name="byteOffset">The zero-based index of the byte in the array.</param>
        /// <param name="bitOffset">The zero-based index of the bit within the byte (0-7).</param>
        /// <returns>
        /// <c>true</c> if the specified bit is set; <c>false</c> if it is not set;
        /// <c>null</c> if the bit cannot be accessed.
        /// </returns>
        public static bool? GetBitValue(byte[] data, int byteOffset, int bitOffset)
        {
            // Validate parameters
            if (data == null || byteOffset < 0 || byteOffset >= data.Length)
            {
                return null;
            }

            if (bitOffset < 0 || bitOffset > 7)
            {
                return null;
            }

            return (data[byteOffset] & (1 << bitOffset)) != 0;
        }

        /// <summary>
        /// Retrieves an integer value from a specific bit area within a byte in a byte array.
        /// Returns <c>null</c> if the bits cannot be accessed due to invalid offsets.
        /// </summary>
        /// <param name="data">The byte array containing the data.</param>
        /// <param name="byteOffset">The zero-based index of the byte in the array.</param>
        /// <param name="bitOffset">The zero-based starting index of the bit area within the byte (0-7).</param>
        /// <param name="bitLength">The number of bits representing the value (1-8).</param>
        /// <returns>
        /// An integer value representing the bits in the specified area;
        /// <c>null</c> if the bits cannot be accessed.
        /// </returns>
        public static int? GetBitAreaValue(byte[] data, int byteOffset, int bitOffset, int bitLength)
        {
            // Validate parameters
            if (data == null || byteOffset < 0 || byteOffset >= data.Length)
            {
                return null;
            }

            if (bitOffset < 0 || bitOffset > 7 || bitLength <= 0 || bitLength > 8 || bitOffset + bitLength > 8)
            {
                return null;
            }

            int value = 0;
            for (int i = 0; i < bitLength; i++)
            {
                if ((data[byteOffset] & (1 << (bitOffset + i))) != 0)
                {
                    value |= 1 << i;
                }
            }
            return value;
        }

        /// <summary>
        /// Retrieves a 32-bit signed integer from a byte array.
        /// Returns <c>null</c> if the bytes cannot be accessed due to invalid offsets.
        /// </summary>
        /// <param name="data">The byte array containing the data.</param>
        /// <param name="byteOffset">The zero-based starting index of the integer in the array.</param>
        /// <returns>
        /// A 32-bit signed integer value;
        /// <c>null</c> if the bytes cannot be accessed.
        /// </returns>
        public static int? GetInt32Value(byte[] data, int byteOffset)
        {
            // Validate parameters
            if (data == null || byteOffset < 0 || byteOffset + 4 > data.Length)
            {
                return null;
            }

            byte[] intBytes = new byte[4];
            Array.Copy(data, byteOffset, intBytes, 0, 4);
            Array.Reverse(intBytes); // Adjust for endianness if necessary
            return BitConverter.ToInt32(intBytes, 0);
        }

        /// <summary>
        /// Retrieves a 16-bit unsigned integer from a byte array.
        /// Returns <c>null</c> if the bytes cannot be accessed due to invalid offsets.
        /// </summary>
        /// <param name="data">The byte array containing the data.</param>
        /// <param name="byteOffset">The zero-based starting index of the unsigned integer in the array.</param>
        /// <returns>
        /// A 16-bit unsigned integer value;
        /// <c>null</c> if the bytes cannot be accessed.
        /// </returns>
        public static ushort? GetUInt16Value(byte[] data, int byteOffset)
        {
            // Validate parameters
            if (data == null || byteOffset < 0 || byteOffset + 2 > data.Length)
            {
                return null;
            }

            byte[] ushortBytes = new byte[2];
            Array.Copy(data, byteOffset, ushortBytes, 0, 2);
            Array.Reverse(ushortBytes); // Adjust for endianness if necessary
            return BitConverter.ToUInt16(ushortBytes, 0);
        }

        /// <summary>
        /// Retrieves an 8-bit unsigned integer from a byte array.
        /// Returns <c>null</c> if the byte cannot be accessed due to invalid offsets.
        /// </summary>
        /// <param name="data">The byte array containing the data.</param>
        /// <param name="byteOffset">The zero-based index of the byte in the array.</param>
        /// <returns>
        /// An 8-bit unsigned integer value;
        /// <c>null</c> if the byte cannot be accessed.
        /// </returns>
        public static byte? GetUInt8Value(byte[] data, int byteOffset)
        {
            // Validate parameters
            if (data == null || byteOffset < 0 || byteOffset >= data.Length)
            {
                return null;
            }

            return data[byteOffset];
        }

        /// <summary>
        /// Retrieves a 16-bit signed integer from a byte array.
        /// Returns <c>null</c> if the bytes cannot be accessed due to invalid offsets.
        /// </summary>
        /// <param name="data">The byte array containing the data.</param>
        /// <param name="byteOffset">The zero-based starting index of the signed integer in the array.</param>
        /// <returns>
        /// A 16-bit signed integer value;
        /// <c>null</c> if the bytes cannot be accessed.
        /// </returns>
        public static short? GetInt16Value(byte[] data, int byteOffset)
        {
            // Validate parameters
            if (data == null || byteOffset < 0 || byteOffset + 2 > data.Length)
            {
                return null;
            }

            byte[] shortBytes = new byte[2];
            Array.Copy(data, byteOffset, shortBytes, 0, 2);
            Array.Reverse(shortBytes); // Adjust for endianness if necessary
            return BitConverter.ToInt16(shortBytes, 0);
        }

        /// <summary>
        /// Retrieves a visible string (ASCII) value from a byte array.
        /// Returns <c>null</c> if the bytes cannot be accessed due to invalid offsets or length.
        /// </summary>
        /// <param name="data">The byte array containing the string data.</param>
        /// <param name="byteOffset">The zero-based starting index of the string in the array.</param>
        /// <param name="length">The length of the string in bytes.</param>
        /// <returns>
        /// A string representing the visible ASCII characters;
        /// <c>null</c> if the bytes cannot be accessed.
        /// </returns>
        public static string GetVisibleStringValue(byte[] data, int byteOffset, int length)
        {
            // Validate parameters
            if (data == null || byteOffset < 0 || length < 0 || byteOffset + length > data.Length)
            {
                return null;
            }

            return Encoding.ASCII.GetString(data, byteOffset, length).TrimEnd('\0');
        }

        // Additional methods with proper checks and documentation can be added here...
    }
}
