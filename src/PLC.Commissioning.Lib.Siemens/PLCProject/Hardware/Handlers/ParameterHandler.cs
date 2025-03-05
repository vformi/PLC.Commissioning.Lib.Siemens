using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models;
using Siemens.Engineering.HW.Features;
using System;
using System.Collections.Generic;
using Serilog;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;
using System.Linq;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Abstractions;
using Siemens.Engineering.HW;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Abstractions;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Handlers
{
    /// <summary>
    /// Handles getting and setting module parameters for a GSD device item.
    /// </summary>
    public class ParameterHandler : IParameterHandler
    {
        /// <summary>
        /// Represents the device item in the GSD file
        /// </summary>
        private readonly IDeviceItem _deviceItem;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterHandler"/> class with the specified module item.
        /// </summary>
        /// <param name="moduleItem">The module item to handle parameter operations for.</param>
        public ParameterHandler(IDeviceItem deviceItem)
        {
            _deviceItem = deviceItem ?? throw new ArgumentNullException(nameof(deviceItem));
        }

        /// <summary>
        /// Retrieves module data from the specified GSD device item based on the selected parameters.
        /// </summary>
        /// <param name="gsdDeviceItem">The GSD device item from which to retrieve data.</param>
        /// <param name="parameterSelections">A list of parameters to retrieve. If null, all parameters are retrieved.</param>
        /// <returns>A list of parsed values representing the module data.</returns>
        public List<ParsedValueModel> GetModuleData(GsdDeviceItem gsdDeviceItem, List<string> parameterSelections = null)
        {
            try
            {
                // Check if gsdDeviceItem is null
                if (gsdDeviceItem is null)
                {
                    Log.Error("GSD device item cannot be null.");
                    return null;
                }

                // Validate the parameter selections if provided
                if (parameterSelections != null && parameterSelections.Count > 0)
                {
                    var parameterValues = parameterSelections.ToDictionary(key => key, key => (object)null); // Creating a dictionary for validation purposes
                    if (!AreParameterKeysValid(parameterValues))
                    {
                        return null;
                    }
                }

                int dsNumber = _deviceItem.ParameterRecordDataItem.DsNumber;
                int byteOffset = 0;
                int lengthInBytes = _deviceItem.ParameterRecordDataItem.LengthInBytes;

                byte[] prmDataComplete = gsdDeviceItem.GetPrmData(dsNumber, byteOffset, lengthInBytes);

                // byte array length validation
                if (prmDataComplete is null || prmDataComplete.Length < lengthInBytes)
                {
                    Log.Error("The byte array length is insufficient to retrieve the required data.");
                    return null;
                }

                Log.Debug("prmDataComplete (Hex): " + BitConverter.ToString(prmDataComplete));

                return ParseModuleData(prmDataComplete, parameterSelections);
            }
            catch (Exception ex)
            {
                Log.Error($"An unexpected error occurred while retrieving module data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sets module data on the specified GSD device item using the provided parameter values.
        /// </summary>
        /// <param name="gsdDeviceItem">The GSD device item on which to set data.</param>
        /// <param name="parameterValues">A dictionary of parameter names and values to set.</param>
        /// <returns><c>true</c> if the data was successfully written, otherwise <c>false</c>.</returns>
        public bool SetModuleData(GsdDeviceItem gsdDeviceItem, Dictionary<string, object> parameterValues)
        {
            try
            {
                // Check if gsdDeviceItem is null
                if (gsdDeviceItem is null)
                {
                    Log.Error("GSD device item cannot be null.");
                    return false;
                }

                // Validate all keys first
                if (!AreParameterKeysValid(parameterValues))
                {
                    Log.Error("Parameter validation failed.");
                    return false;
                }

                int dsNumber = _deviceItem.ParameterRecordDataItem.DsNumber;
                int byteOffset = 0;
                int lengthInBytes = _deviceItem.ParameterRecordDataItem.LengthInBytes;

                // First get the data that are already there 
                byte[] originalData = gsdDeviceItem.GetPrmData(dsNumber, byteOffset, lengthInBytes);
                
                // Validate the retrieved data
                if (originalData is null)
                {
                    Log.Error("Failed to retrieve the original data from the device.");
                    return false;
                }

                if (originalData.Length < lengthInBytes)
                {
                    Log.Error($"The retrieved data is insufficient. Expected at least {lengthInBytes} bytes, but got {originalData.Length} bytes.");
                    return false;
                }

                // Generate the data to write
                byte[] dataToWrite = WriteModuleData(originalData, parameterValues);

                // Validate the generated data before writing it to the device
                if (dataToWrite is null || dataToWrite.Length == 0)
                {
                    Log.Error("Data generation failed due to invalid parameters. Aborting operation.");
                    return false;
                }

                // Ensure the generated data is of sufficient length
                if (dataToWrite.Length < lengthInBytes)
                {
                    Log.Error($"The generated data is insufficient. Expected at least {lengthInBytes} bytes, but got {dataToWrite.Length} bytes.");
                    return false;
                }

                // Set the parameter data on the device
                gsdDeviceItem.SetPrmData(dsNumber, byteOffset, dataToWrite);

                // Log the successful operation
                Log.Debug($"Successfully written data (Hex): {BitConverter.ToString(dataToWrite)} to DSNumber: {dsNumber} at ByteOffset: {byteOffset}.");

                return true;
            }
            catch (Exception ex)
            {
                // Catch any unexpected exceptions and log them
                Log.Error($"An unexpected error occurred: {ex.Message}, {ex}");
                return false;
            }
        }
        
        /// <summary>
        /// Handles the regular parameters by retrieving and logging the module data for the provided module item.
        /// </summary>
        /// <param name="module">The GSD device item representing the module or DAP.</param>
        /// <param name="parameterSelections">A list of parameter selections to be retrieved for the module or DAP.</param>
        /// <returns><c>true</c> if the regular parameters were handled successfully; otherwise, <c>false</c>.</returns>
        public Dictionary<string, object> HandleRegularParameters(GsdDeviceItem module, List<string> parameterSelections)
        {
            var moduleData = GetModuleData(module, parameterSelections);
            if (moduleData is null)
            {
                Log.Error("Parameter reading failed due to invalid parameters. Aborting operation.");
                return null;
            }

            // Build a dictionary from the retrieved moduleData
            var resultDict = new Dictionary<string, object>();
            foreach (var parsedValue in moduleData)
            {
                resultDict[parsedValue.Parameter] = parsedValue.Value;
                Log.Debug($"Parameter: {parsedValue.Parameter}, Value: {parsedValue.Value}");
            }

            return resultDict;
        }

        #region Private methods
        /// <summary>
        /// Parses the module data from the provided byte array and extracts parameter values.
        /// </summary>
        /// <param name="data">The byte array containing the module data.</param>
        /// <param name="parameterSelections">
        /// A list of parameter names to parse. If <c>null</c>, all parameters are parsed.
        /// </param>
        /// <returns>
        /// A list of <see cref="ParsedValueModel"/> instances representing the parsed parameter values.
        /// </returns>
        /// <remarks>
        /// This method iterates over the parameter references defined in the device item,
        /// retrieves raw values using appropriate data type getters, and maps them to user-friendly values
        /// if allowed value assignments are specified.
        /// </remarks>
        internal List<ParsedValueModel> ParseModuleData(byte[] data, List<string> parameterSelections = null)
        {
            var parsedValues = new List<ParsedValueModel>();

            foreach (var refItem in _deviceItem.ParameterRecordDataItem.Refs)
            {
                // Skip parameters not in the selection list, if a selection list is provided
                if (parameterSelections != null && !parameterSelections.Contains(refItem.Text))
                {
                    continue;
                }

                object rawValue = null;
                object mappedValue = null;

                switch (refItem.DataType)
                {
                    case "Bit":
                        {
                            // Retrieve the bit value with validation
                            var bitValue = DataTypeGetter.GetBitValue(data, refItem.ByteOffset, refItem.BitOffset ?? 0);
                            if (bitValue.HasValue)
                                rawValue = bitValue.Value ? 1 : 0;
                            break;
                        }

                    case "BitArea":
                        {
                            // Retrieve the bit area value with validation
                            var bitAreaValue = DataTypeGetter.GetBitAreaValue(
                                data,
                                refItem.ByteOffset,
                                refItem.BitOffset ?? 0,
                                refItem.BitLength ?? 1);
                            if (bitAreaValue.HasValue)
                                rawValue = bitAreaValue.Value;
                            break;
                        }

                    case "Integer32":
                        {
                            // Retrieve the 32-bit integer value with validation
                            var int32Value = DataTypeGetter.GetInt32Value(data, refItem.ByteOffset);
                            if (int32Value.HasValue)
                                rawValue = int32Value.Value;
                            break;
                        }

                    case "Unsigned16":
                        {
                            // Retrieve the 16-bit unsigned integer value with validation
                            var uint16Value = DataTypeGetter.GetUInt16Value(data, refItem.ByteOffset);
                            if (uint16Value.HasValue)
                                rawValue = uint16Value.Value;
                            break;
                        }

                    case "Unsigned8":
                        {
                            // Retrieve the 8-bit unsigned integer value with validation
                            var uint8Value = DataTypeGetter.GetUInt8Value(data, refItem.ByteOffset);
                            if (uint8Value.HasValue)
                                rawValue = uint8Value.Value;
                            break;
                        }

                    case "Integer16":
                        {
                            // Retrieve the 16-bit signed integer value with validation
                            var int16Value = DataTypeGetter.GetInt16Value(data, refItem.ByteOffset);
                            if (int16Value.HasValue)
                                rawValue = int16Value.Value;
                            break;
                        }

                    case "VisibleString":
                        {
                            // Retrieve the string value with validation
                            var stringValue = DataTypeGetter.GetVisibleStringValue(data, refItem.ByteOffset, refItem.Length ?? 0);
                            if (stringValue != null)
                                rawValue = stringValue;
                            break;
                        }

                    // Add cases for other data types as needed.

                    default:
                        {
                            // Handle unsupported data types if necessary
                            break;
                        }
                }

                // Proceed only if a valid raw value was obtained
                if (rawValue != null)
                {
                    // Map the raw value to a user-friendly value if allowed assignments are specified
                    if (refItem.AllowedValueAssignments != null &&
                        refItem.AllowedValueAssignments.TryGetValue(rawValue.ToString(), out var assignment))
                    {
                        mappedValue = assignment;
                    }
                    else
                    {
                        mappedValue = rawValue;
                    }

                    // Add the parsed value to the list
                    parsedValues.Add(new ParsedValueModel
                    {
                        Parameter = refItem.Text,
                        Value = mappedValue,
                        ValueItemTarget = refItem.ValueItemTarget,
                    });
                }
            }
            return parsedValues;
        }


        /// <summary>
        /// Writes module data to a byte array based on the provided parameter values.
        /// </summary>
        /// <param name="data">The byte array to be modified with the new parameter values.</param>
        /// <param name="parameterValues">A dictionary of parameter names and values to set.</param>
        /// <returns>
        /// The modified byte array representing the data to be written to the module.
        /// Returns <c>null</c> if invalid values are provided or setting any value fails.
        /// </returns>
        internal byte[] WriteModuleData(byte[] data, Dictionary<string, object> parameterValues)
        {
            foreach (var refItem in _deviceItem.ParameterRecordDataItem.Refs)
            {
                if (parameterValues.TryGetValue(refItem.Text, out var value))
                {
                    object rawValue = value;

                    // Map string values to raw values based on allowed value assignments
                    if (value is string stringValue)
                    {
                        if (refItem.AllowedValueAssignments != null && refItem.AllowedValueAssignments.Any())
                        {
                            if (refItem.AllowedValueAssignments.TryGetValue(stringValue, out var mappedValue))
                            {
                                rawValue = Convert.ToInt32(mappedValue);
                            }
                            else
                            {
                                Log.Error("Invalid string value for parameter {Parameter}: {Value}", refItem.Text, value);
                                return null;
                            }
                        }
                    }
                    
                    // Validation for AllowedValues
                    if (refItem.AllowedValues != null)
                    {
                        var range = refItem.AllowedValues.Split(new[] { ".." }, StringSplitOptions.None);
                        if (range.Length == 2 && int.TryParse(range[0], out int min) && int.TryParse(range[1], out int max))
                        {
                            if (rawValue is int intVal && (intVal < min || intVal > max))
                            {
                                Console.WriteLine($"ERROR: Value {intVal} for {refItem.Text} is out of allowed range {min}..{max}");
                                return null;
                            }
                        }
                    }

                    bool isValid = false;

                    switch (refItem.DataType)
                    {
                        case "Bit":
                            if (rawValue is int intValueBit && (intValueBit == 0 || intValueBit == 1))
                            {
                                isValid = DataTypeSetter.SetBitValue(data, refItem.ByteOffset, refItem.BitOffset ?? 0, intValueBit == 1);
                            }
                            break;

                        case "BitArea":
                            {
                                int bitLength = refItem.BitLength ?? 1;
                                int maxValue = (1 << bitLength) - 1;
                                if (rawValue is int intValueBitArea && intValueBitArea >= 0 && intValueBitArea <= maxValue)
                                {
                                    isValid = DataTypeSetter.SetBitAreaValue(data, refItem.ByteOffset, refItem.BitOffset ?? 0, bitLength, intValueBitArea);
                                }
                            }
                            break;

                        case "Integer32":
                            if (rawValue is int intValue32)
                            {
                                isValid = DataTypeSetter.SetInt32Value(data, refItem.ByteOffset, intValue32);
                            }
                            break;

                        case "Unsigned16":
                            if (rawValue is int intValueU16 && intValueU16 >= 0 && intValueU16 <= ushort.MaxValue)
                            {
                                isValid = DataTypeSetter.SetUInt16Value(data, refItem.ByteOffset, (ushort)intValueU16);
                            }
                            break;

                        case "Unsigned8":
                            if (rawValue is int intValueU8 && intValueU8 >= 0 && intValueU8 <= byte.MaxValue)
                            {
                                isValid = DataTypeSetter.SetUInt8Value(data, refItem.ByteOffset, (byte)intValueU8);
                            }
                            break;

                        case "Integer16":
                            if (rawValue is int intValue16 && intValue16 >= short.MinValue && intValue16 <= short.MaxValue)
                            {
                                isValid = DataTypeSetter.SetInt16Value(data, refItem.ByteOffset, (short)intValue16);
                            }
                            break;

                        case "VisibleString":
                            if (rawValue is string stringValueVisible)
                            {
                                int maxLength = refItem.Length ?? 0;
                                // Check if the incoming string is longer than allowed:
                                if (stringValueVisible.Length > maxLength)
                                {
                                    isValid = false;
                                }
                                else
                                {
                                    isValid = DataTypeSetter.SetVisibleStringValue(data,
                                        refItem.ByteOffset,
                                        maxLength,
                                        stringValueVisible);
                                }
                            }
                            break;
                            // Add cases for other data types as needed.
                    }

                    if (!isValid)
                    {
                        Log.Error($"Failed to set value for {refItem.Text}: {value}");

                        // Check if there are allowed value assignments available
                        if (refItem.AllowedValueAssignments != null && refItem.AllowedValueAssignments.Any())
                        {
                            Log.Error($"Allowed values: {string.Join(", ", refItem.AllowedValueAssignments.Values)}.");
                        }
                        else if (refItem.AllowedValues != null && refItem.AllowedValues.Any())
                        {
                            // If there are no specific assignments but there are allowed values
                            Log.Error($"Allowed values: {string.Join(", ", refItem.AllowedValues)}.");
                        }

                        return null; // Stop execution and return null
                    }
                }
            }

            return data;
        }

        
        internal bool AreParameterKeysValid(Dictionary<string, object> parameterValues)
        {
            // Check if parameterValues dictionary is null or empty
            if (parameterValues is null || parameterValues.Count == 0)
            {
                Log.Error("Parameter values dictionary is null or empty.");
                return false;
            }

            // Create a list of valid keys from the model's Ref parameters
            var validKeys = _deviceItem.ParameterRecordDataItem.Refs.Select(r => r.Text).ToList();

            foreach (var key in parameterValues.Keys)
            {
                // Check if the key is null or empty
                if (string.IsNullOrWhiteSpace(key))
                {
                    Log.Error("Parameter key is null, empty, or consists only of white-space characters.");
                    return false;
                }

                // Check if the key is valid
                if (!validKeys.Contains(key))
                {
                    Log.Error($"Invalid parameter key: {key}");
                    return false;
                }
            }

            return true;
        }
        #endregion
    }
}
