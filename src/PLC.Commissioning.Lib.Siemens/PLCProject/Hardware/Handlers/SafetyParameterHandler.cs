using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Siemens.Engineering.HW.Features;
using System.Linq;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Abstractions;
using Serilog;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Handlers
{
    // TODO: figure out user-input validation here
    /// <summary>
    /// Provides methods to handle safety parameters for GSD devices.
    /// </summary>
    public class SafetyParameterHandler : ISafetyParameterHandler
    {
        // TIA to Internal parameter mapping
        private static readonly Dictionary<string, string> TiaToInternalMapping = new Dictionary<string, string>
        {
            {"F_SIL", "Failsafe_FSIL"},
            {"F_Block_ID", "Failsafe_FBlockID"},
            {"F_Par_Version", "Failsafe_FParVersion"},
            {"F_Source_Add", "Failsafe_FSourceAddress"},
            {"F_Dest_Add", "Failsafe_FDestinationAddress"},
            {"F_Par_CRC_WithoutAddresses", "Failsafe_FParameterSignatureWithoutAddresses"},
            {"F_Par_CRC", "Failsafe_FParameterSignatureWithAddresses"},
            {"F_iPar_CRC", "Failsafe_FParameterSignatureIndividualParameters"},
            {"F_CRC_Length", "Failsafe_F_CRC_Length"},
            {"F_WD_Time", "Failsafe_FMonitoringtime"},
            {"F_IO_DB_number", "Failsafe_FIODBNumber"},
            {"F_IO_DB_name", "Failsafe_FIODBName"},
            {"F_Passivation", "Failsafe_FPassivation"},
            {"Manual_assignment_of_f-monitoring_time", "Failsafe_ManualAssignmentFMonitoringtime"},
            {"F_IO_DB_manual_number_assignment", "Failsafe_ManualAssignmentFIODBNumber"}
        };


        // Internal to TIA parameter mapping
        private static readonly Dictionary<string, string> InternalToTiaMapping = new Dictionary<string, string>();
        
        // Static constructor to populate InternalToTiaMapping
        static SafetyParameterHandler()
        {
            foreach (var pair in TiaToInternalMapping)
            {
                InternalToTiaMapping.Add(pair.Value, pair.Key);
            }
        }
        
        /// <summary>
        /// Retrieves the safety module data for a given GSD device item.
        /// </summary>
        /// <param name="gsdDeviceItem">
        ///   The GSD device item from which to retrieve safety parameters.
        /// </param>
        /// <param name="parameterSelections">
        ///   An optional list of specific safety parameters to retrieve. 
        ///   If <c>null</c>, all known safety parameters are retrieved.
        /// </param>
        /// <returns>
        ///   A <see cref="Dictionary{string, object}"/> of retrieved safety parameters on success;
        ///   <c>null</c> if retrieval fails for an reason.
        /// </returns>
        public Dictionary<string, object> GetSafetyModuleData(
            GsdDeviceItem gsdDeviceItem,
            List<string> tiaParameterSelections = null)
        {
            // Determine which attributes to retrieve
            List<string> attributesToRetrieve;
            if (tiaParameterSelections == null)
            {
                attributesToRetrieve = new List<string>();
                foreach (string value in TiaToInternalMapping.Values)
                {
                    attributesToRetrieve.Add(value);
                }
            }
            else
            {
                attributesToRetrieve = new List<string>();
                foreach (string tiaParam in tiaParameterSelections)
                {
                    if (TiaToInternalMapping.ContainsKey(tiaParam))
                    {
                        attributesToRetrieve.Add(TiaToInternalMapping[tiaParam]);
                    }
                }
            }

            var attributesDictionary = new Dictionary<string, object>();

            try
            {
                object[] retrievedAttributes = gsdDeviceItem.GetAttributes(attributesToRetrieve).ToArray();
                for (int i = 0; i < attributesToRetrieve.Count; i++)
                {
                    if (retrievedAttributes[i] != null)
                    {
                        string tiaName = InternalToTiaMapping[attributesToRetrieve[i]];
                        attributesDictionary.Add(tiaName, retrievedAttributes[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error retrieving attributes: " + ex.Message);
                return null;
            }

            return attributesDictionary;
        }
        
        /// <summary>
        /// Sets the safety module data for a given GSD device item.
        /// </summary>
        /// <param name="gsdDeviceItem">The GSD device item whose safety parameters are to be set.</param>
        /// <param name="parameterValues">
        /// A dictionary containing the safety parameters to set and their desired values.
        /// </param>
        /// <returns>
        /// <c>true</c> if the safety module data was successfully set; otherwise, <c>false</c>.
        /// </returns>
        public bool SetSafetyModuleData(GsdDeviceItem gsdDeviceItem, Dictionary<string, object> parameterValues)
        {
            const ulong fAddressMin = 1, fAddressMax = 65534;
            const ulong fwdTimeMin = 0, fwdTimeMax = 10000;

            bool manualMonitoringEnabled = false;
            bool manualIodbEnabled = false;

            // Check manual assignment flags
            if (parameterValues.ContainsKey("Manual_assignment_of_f-monitoring_time"))
            {
                manualMonitoringEnabled = ConvertToUInt64(parameterValues["Manual_assignment_of_f-monitoring_time"]) == 1;
            }
            if (parameterValues.ContainsKey("F_IO_DB_manual_number_assignment"))
            {
                manualIodbEnabled = ConvertToUInt64(parameterValues["F_IO_DB_manual_number_assignment"]) == 1;
            }

            foreach (var param in parameterValues)
            {
                if (!TiaToInternalMapping.ContainsKey(param.Key))
                {
                    Log.Error("Unknown parameter: " + param.Key);
                    continue;
                }

                string mappedParam = TiaToInternalMapping[param.Key];

                try
                {
                    switch (mappedParam)
                    {
                        case "Failsafe_FSourceAddress":
                        case "Failsafe_FDestinationAddress":
                            ulong address = ConvertToUInt64(param.Value);
                            if (address < fAddressMin || address > fAddressMax)
                            {
                                Log.Error($"Invalid value for {param.Key}. Allowed range: {fAddressMin}-{fAddressMax}.");
                                continue;
                            }
                            gsdDeviceItem.SetAttribute(mappedParam, address);
                            break;

                        case "Failsafe_FMonitoringtime":
                            if (!manualMonitoringEnabled)
                            {
                                Log.Error($"Cannot set {param.Key} because manual monitoring time assignment is not enabled.");
                                continue;
                            }
                            ulong wdTime = ConvertToUInt64(param.Value);
                            if (wdTime < fwdTimeMin || wdTime > fwdTimeMax)
                            {
                                Log.Error($"Invalid value for {param.Key}. Allowed range: {fwdTimeMin}-{fwdTimeMax}.");
                                continue;
                            }
                            gsdDeviceItem.SetAttribute(mappedParam, wdTime);
                            break;

                        case "Failsafe_FIODBNumber":
                            if (!manualIodbEnabled)
                            {
                                Log.Error($"Cannot set {param.Key} because manual IO DB number assignment is not enabled.");
                                continue;
                            }
                            gsdDeviceItem.SetAttribute(mappedParam, ConvertToUInt64(param.Value));
                            break;

                        case "Failsafe_FIODBName":
                            gsdDeviceItem.SetAttribute(mappedParam, param.Value.ToString());
                            break;

                        case "Failsafe_ManualAssignmentFIODBNumber":
                        case "Failsafe_ManualAssignmentFMonitoringtime":
                            ulong value = ConvertToUInt64(param.Value);
                            gsdDeviceItem.SetAttribute(mappedParam, value);
                            if (mappedParam == "Failsafe_ManualAssignmentFIODBNumber")
                                manualIodbEnabled = value == 1;
                            else if (mappedParam == "Failsafe_ManualAssignmentFMonitoringtime")
                                manualMonitoringEnabled = value == 1;
                            break;
                        
                        case "Failsafe_FParameterSignatureWithAddresses":
                        case "Failsafe_FParameterSignatureWithoutAddresses":
                        case "Failsafe_FParameterSignatureIndividualParameters":
                            gsdDeviceItem.SetAttribute(mappedParam, ConvertToUInt64(param.Value));
                            break;

                        default:
                            Log.Error($"Parameter {param.Key} is not writable.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error setting {param.Key}: {ex.Message}");
                }
            }

            return true;
        }
        
        /// <summary>
        /// Handles the safety parameters by retrieving and optionally logging the safety module data.
        /// </summary>
        /// <param name="module">The GSD device item representing the safety module.</param>
        /// <param name="parameterSelections">A list of parameter selections for the safety module.</param>
        /// <returns>
        /// A <see cref="Dictionary{string, object}"/> of retrieved safety parameters if successful; <c>null</c> otherwise.
        /// </returns>
        public Dictionary<string, object> HandleSafetyParameters(GsdDeviceItem module, List<string> parameterSelections)
        {
            // Retrieve the safety module data; returns null if something fails
            var moduleData = GetSafetyModuleData(module, parameterSelections);
            if (moduleData == null)
            {
                Log.Error("Safety parameter reading failed due to invalid or unsupported parameters. Aborting operation.");
                return null;
            }
    
            // Optionally log them
            foreach (var kvp in moduleData)
            {
                Log.Debug($"Safety Parameter: {kvp.Key}, Value: {kvp.Value}");
            }

            // Return the dictionary for further processing
            return moduleData;
        }
        
        
        /// <summary>
        /// Converts an input value of various types into a <see cref="System.UInt64"/> for consistent handling of safety parameters.
        /// This method ensures that all parameters (except <c>Failsafe_FIODBName</c>) are represented as <see cref="System.UInt64"/>,
        /// supporting user-friendly inputs such as booleans, "check"/"uncheck" strings, and numeric values.
        /// </summary>
        /// <param name="value">
        /// The input value to convert. Supported types include:
        /// <list type="bullet">
        ///   <item><c>null</c>: Returns 0.</item>
        ///   <item><see cref="bool"/>: <c>true</c> maps to 1, <c>false</c> to 0.</item>
        ///   <item><see cref="string"/>: "check" maps to 1, "uncheck" to 0, or parsed as a number.</item>
        ///   <item>Numeric types: Converted directly to <see cref="System.UInt64"/> (e.g., <see cref="int"/>, <see cref="ulong"/>).</item>
        /// </list>
        /// </param>
        /// <returns>
        /// The converted value as a <see cref="System.UInt64"/>.
        /// </returns>
        private static ulong ConvertToUInt64(object value)
        {
            if (value == null)
            {
                return 0;
            }
            else if (value is bool boolValue)
            {
                return boolValue ? 1UL : 0UL;
            }
            else if (value is string strValue)
            {
                if (string.Equals(strValue, "check", StringComparison.OrdinalIgnoreCase))
                    return 1UL;
                else if (string.Equals(strValue, "uncheck", StringComparison.OrdinalIgnoreCase))
                    return 0UL;
                else
                    return ulong.Parse(strValue);
            }
            else
            {
                return Convert.ToUInt64(value);
            }
        }
    }
}
