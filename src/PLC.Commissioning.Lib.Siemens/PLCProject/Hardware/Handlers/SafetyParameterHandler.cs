using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Siemens.Engineering.HW.Features;
using System.Linq;
using Serilog;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Handlers
{
    /// <summary>
    /// Provides methods to handle safety parameters for GSD devices.
    /// </summary>
    public class SafetyParameterHandler
    {
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
            List<string> parameterSelections = null)
        {
            // List of all possible safety-related attributes
            var allSafetyAttributes = new List<string>
            {
                "Failsafe_FBlockID",
                "Failsafe_FDestinationAddress",
                "Failsafe_FIODBName",
                "Failsafe_FIODBNumber",
                "Failsafe_FMonitoringtime",
                "Failsafe_FParVersion",
                "Failsafe_FParameterSignatureWithAddresses",
                "Failsafe_FParameterSignatureWithoutAddresses",
                "Failsafe_FSIL",
                "Failsafe_FSourceAddress",
                "Failsafe_ManualAssignmentFIODBNumber",
                "Failsafe_ManualAssignmentFMonitoringtime",
            };

            // If no specific attributes are provided by the user, use all safety attributes
            var attributesToRetrieve = parameterSelections ?? allSafetyAttributes;
            
            var attributesDictionary = new Dictionary<string, object>();

            try
            {
                // Retrieve all specified attributes in one call
                var retrievedAttributes = gsdDeviceItem.GetAttributes(attributesToRetrieve);

                // Add non-null attributes to the dictionary
                for (int i = 0; i < attributesToRetrieve.Count; i++)
                {
                    if (retrievedAttributes[i] != null)
                    {
                        attributesDictionary[attributesToRetrieve[i]] = retrievedAttributes[i];
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Unexpected error retrieving attributes: {ex.Message}");
                return null;
            }

            // If we get here, retrieval was successful
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
            // Define allowed ranges for specific parameters
            const ulong FAddressMin = 1;
            const ulong FAddressMax = 65534;
            const ulong FWDTimeMin = 0; // Adjust according to specific sensor requirements
            const ulong FWDTimeMax = 10000;

            foreach (var param in parameterValues)
            {
                try
                {
                    switch (param.Key)
                    {
                        case "Failsafe_FSourceAddress":
                            ulong sourceAddress = Convert.ToUInt64(param.Value);
                            if (sourceAddress < FAddressMin || sourceAddress > FAddressMax)
                            {
                                Log.Error($"Invalid value for {param.Key}. Allowed range is {FAddressMin}-{FAddressMax}.");
                                return false;
                            }
                            gsdDeviceItem.SetAttribute(param.Key, sourceAddress);
                            break;

                        case "Failsafe_FDestinationAddress":
                            ulong destinationAddress = Convert.ToUInt64(param.Value);
                            if (destinationAddress < FAddressMin || destinationAddress > FAddressMax)
                            {
                                Log.Error($"Invalid value for {param.Key}. Allowed range is {FAddressMin}-{FAddressMax}.");
                                return false;
                            }
                            gsdDeviceItem.SetAttribute(param.Key, destinationAddress);
                            break;

                        case "Failsafe_FMonitoringtime": // Corresponds to <F_WD_Time>
                            ulong wdTime = Convert.ToUInt64(param.Value);
                            if (wdTime < FWDTimeMin || wdTime > FWDTimeMax)
                            {
                                Log.Error($"Invalid value for {param.Key}. Allowed range is {FWDTimeMin}-{FWDTimeMax}.");
                                return false;
                            }
                            gsdDeviceItem.SetAttribute(param.Key, wdTime);
                            break;

                        case "Failsafe_FIODBName":
                        case "Failsafe_ManualAssignmentFIODBNumber":
                            gsdDeviceItem.SetAttribute(param.Key, param.Value);
                            break;

                        default:
                            Log.Error($"Parameter {param.Key} is not writable or not recognized.");
                            return false;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error setting attribute {param.Key}: {ex.Message}");
                    return false;
                }
            }

            return true;
        }
    }
}
