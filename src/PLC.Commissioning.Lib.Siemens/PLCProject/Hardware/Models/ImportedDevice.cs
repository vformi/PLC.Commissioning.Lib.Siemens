using System;
using System.Collections.Generic;
using Siemens.Engineering.HW;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Models;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models.IOTags;
using Serilog;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware
{
    /// <summary>
    /// Represents an imported device in the Siemens PLC project.
    /// Holds both the hardware (IOModuleInfo) and the detailed GSDML module data.
    /// </summary>
    public class ImportedDevice
    {
        /// <summary>
        /// Gets the unique name of the device instance in the TIA project.
        /// </summary>
        public string DeviceName { get; private set; }
        
        /// <summary>
        /// Gets the TIA Portal device object.
        /// </summary>
        public Device Device { get; private set; }

        /// <summary>
        /// Gets the list of hardware modules parsed from the TIA project.
        /// </summary>
        public List<IOModuleInfoModel> Modules { get; private set; } = new List<IOModuleInfoModel>();

        /// <summary>
        /// Gets the merged device model containing GSDML definitions.
        /// </summary>
        public ImportedDeviceGSDMLModel DeviceGsdmlModel { get; private set; }

        /// <summary>
        /// Constructs an ImportedDevice with an already‑initialized GSDML model.
        /// </summary>
        /// <param name="deviceName">The unique TIA device instance name.</param>
        /// <param name="tiaDevice">The TIA device object.</param>
        /// <param name="deviceGsdmlModel">The pre‑built GSDML model (ImportedDeviceGSDMLModel).</param>
        public ImportedDevice(string deviceName, Device tiaDevice, ImportedDeviceGSDMLModel deviceGsdmlModel)
        {
            DeviceName = deviceName ?? throw new ArgumentNullException(nameof(deviceName));
            Device = tiaDevice ?? throw new ArgumentNullException(nameof(tiaDevice));
            DeviceGsdmlModel = deviceGsdmlModel ?? throw new ArgumentNullException(nameof(deviceGsdmlModel));

            Log.Information("GSDML Model assigned for device: {DeviceName}", DeviceName);
        }

        /// <summary>
        /// Adds hardware module information to the device.
        /// </summary>
        /// <param name="module">The module information to add.</param>
        public void AddModule(IOModuleInfoModel module)
        {
            Modules.Add(module);
        }

        /// <summary>
        /// Iterates over the module names and retrieves module details from the GSDML model.
        /// </summary>
        public void PrintModulesFromGSDML()
        {
            if (DeviceGsdmlModel == null || DeviceGsdmlModel.ModuleList == null)
            {
                Log.Warning("DeviceGsdmlModel is not initialized for {DeviceName}", DeviceName);
                return;
            }

            Log.Information("Listing modules for device: {DeviceName}", DeviceName);

            foreach (var module in Modules)
            {
                string moduleName = module.ModuleName;

                // Retrieve module item from GSDML
                var (gsdmlModule, hasChangeableParameters) =
                    DeviceGsdmlModel.ModuleList.GetModuleItemByName(moduleName);

                if (gsdmlModule != null)
                {
                    Log.Debug("Module Found: {ModuleName}", moduleName);
                    Log.Debug(gsdmlModule.ToString());
                }
                else
                {
                    Log.Warning("Module {ModuleName} not found in GSDML.", moduleName);
                }
            }
        }

        /// <summary>
        /// Generates tag table definitions, extracting bit-level and byte/word-based information for PLC tag creation.
        /// Both the tag table and tag names are prefixed with the device name to ensure uniqueness.
        /// </summary>
        /// <returns>A list of tag table definitions.</returns>
        public List<TagTableModel> GetTagTableDefinitions()
        {
            List<TagTableModel> tagTables = new List<TagTableModel>();

            if (Modules.Count == 0)
            {
                Log.Warning("No hardware modules found for device {DeviceName}. Skipping tag table preparation.",
                    DeviceName);
                return tagTables;
            }

            Log.Information("Extracting tag table definitions for device: {DeviceName}", DeviceName);

            foreach (var hwModule in Modules)
            {
                Log.Debug("Processing hardware module: {ModuleName} with ID: {GsdID}", hwModule.ModuleName,
                    hwModule.GsdId);

                // Retrieve module details from GSDML using GsdId
                var (gsdmlModule, _) = DeviceGsdmlModel.ModuleList.GetModuleItemByGsdId(hwModule.GsdId);
                
                // Skip logging warning for specific modules "PN-IO" and "Device parameter"
                if (gsdmlModule == null || gsdmlModule.Model.IOData == null)
                {
                    // Only log warning if the module name is not "PN-IO" or "Device parameter"
                    if (hwModule.ModuleName != "PN-IO" && hwModule.ModuleName != "Device parameter")
                    {
                        Log.Warning("No GSDML IOData found for module {ModuleName}. Skipping tag table creation.",
                            hwModule.ModuleName);
                    }
                    continue;
                }
                
                // Use GSDML-defined name (replace spaces with underscores)
                string gsdmlModuleName = gsdmlModule.Model.Name.Replace(" ", "_");

                Log.Debug("Using GSDML Module Name: {GsdmlModuleName} instead of TIA name: {ModuleName}",
                    gsdmlModuleName, hwModule.ModuleName);

                // Create a tag table using the device name plus the GSDML module name to ensure uniqueness
                var tagTable = new TagTableModel
                {
                    TableName = $"{DeviceName}_{gsdmlModuleName}",
                    Tags = new List<TagModel>()
                };

                // Process Inputs
                if (hwModule.InputStartAddress.HasValue)
                {
                    int inputAddress = hwModule.InputStartAddress.Value;
                    ProcessIOData(gsdmlModule.Model.IOData.Inputs, "%I", tagTable, ref inputAddress, gsdmlModuleName);
                }

                // Process Outputs
                if (hwModule.OutputStartAddress.HasValue)
                {
                    int outputAddress = hwModule.OutputStartAddress.Value;
                    ProcessIOData(gsdmlModule.Model.IOData.Outputs, "%Q", tagTable, ref outputAddress, gsdmlModuleName);
                }

                // Only add the table if it contains tags
                if (tagTable.Tags.Count > 0)
                {
                    tagTables.Add(tagTable);
                }
                else
                {
                    Log.Warning("Module {ModuleName} had IOData but no valid tags. Skipping tag table.",
                        gsdmlModuleName);
                }
            }

            return tagTables;
        }
    
        /// <summary>
        /// Processes IO data items to extract tag details.
        /// </summary>
        /// <param name="ioData">The IO data list.</param>
        /// <param name="addressPrefix">The prefix for memory addresses (%I or %Q).</param>
        /// <param name="tagTable">The tag table to populate.</param>
        /// <param name="startAddress">The starting address for the module.</param>
        /// <param name="moduleName">The module's name for tag identification.</param>
        private void ProcessIOData(List<DataItem> ioData, string addressPrefix, TagTableModel tagTable,
            ref int startAddress, string moduleName)
        {
            int byteOffset = 0; // Track the byte-level offset

            foreach (var dataItem in ioData)
            {
                if (dataItem.UseAsBits)
                {
                    // Process bit-level data items within the byte
                    foreach (var bitDataItem in dataItem.BitDataItems)
                    {
                        string logicalAddress = $"{addressPrefix}{startAddress + byteOffset}.{bitDataItem.BitOffset}";
                        // Include DeviceName, moduleName, and the text ID in the tag name
                        string tagName = $"{DeviceName}_{moduleName}_{bitDataItem.TextId.Replace(" ", "_")}";

                        tagTable.Tags.Add(new TagModel
                        {
                            Name = tagName,
                            DataType = "Bool",
                            Address = logicalAddress
                        });

                        Log.Debug("Added Bit Tag: {TagName}, Address: {LogicalAddress}, DataType: Bool",
                            tagName, logicalAddress);
                    }

                    // Increment byte offset after processing all bits in this byte
                    byteOffset++;
                }
                else
                {
                    // Process byte/word/dword-level data items
                    string dataType;
                    string logicalAddress;
                    int dataSize = 1; // Default to 1 byte

                    switch (dataItem.DataType)
                    {
                        case "Unsigned8":
                            dataType = "Byte";
                            logicalAddress = $"{addressPrefix}B{startAddress + byteOffset}";
                            dataSize = 1;
                            break;

                        case "Unsigned16":
                            dataType = "UInt";
                            logicalAddress = $"{addressPrefix}W{startAddress + byteOffset}";
                            dataSize = 2;
                            break;

                        case "Unsigned32":
                            dataType = "DWord";
                            logicalAddress = $"{addressPrefix}D{startAddress + byteOffset}";
                            dataSize = 4;
                            break;

                        case "Integer8":
                            dataType = "SInt";
                            logicalAddress = $"{addressPrefix}B{startAddress + byteOffset}";
                            dataSize = 1;
                            break;

                        case "Integer16":
                            dataType = "Int";
                            logicalAddress = $"{addressPrefix}W{startAddress + byteOffset}";
                            dataSize = 2;
                            break;

                        case "Integer32":
                            dataType = "DInt";
                            logicalAddress = $"{addressPrefix}D{startAddress + byteOffset}";
                            dataSize = 4;
                            break;

                        case "Float32":
                            dataType = "Real";
                            logicalAddress = $"{addressPrefix}D{startAddress + byteOffset}";
                            dataSize = 4;
                            break;

                        case "Float64":
                            dataType = "LReal";
                            logicalAddress = $"{addressPrefix}D{startAddress + byteOffset}";
                            dataSize = 8;
                            break;

                        case "OctetString":
                        {
                            // Determine the number of bytes to map.
                            int length = dataItem.Length.HasValue ? dataItem.Length.Value : 1;
                            // Loop for each byte in the OctetString.
                            for (int i = 0; i < length; i++)
                            {
                                string currentLogicalAddress = $"{addressPrefix}B{startAddress + byteOffset + i}";
                                // Build a unique tag name by adding an index for each byte
                                string currentTagName =
                                    $"{DeviceName}_{moduleName}_{dataItem.TextId.Replace(" ", "_")}_{i}";

                                tagTable.Tags.Add(new TagModel
                                {
                                    Name = currentTagName,
                                    DataType = "Byte",
                                    Address = currentLogicalAddress,
                                });

                                Log.Debug(
                                    "Added OctetString Byte Tag: {TagName}, Address: {LogicalAddress}, DataType: Byte",
                                    currentTagName, currentLogicalAddress);
                            }

                            // Advance the offset by the full length.
                            byteOffset += length;
                            // Continue with next DataItem so no further tag is added for OctetString.
                            continue;
                        }

                        case "F_MessageTrailer4Byte":
                            // Allocate 4 bytes for the PROFIsafe message trailer
                            for (int i = 0; i < 4; i++)
                            {
                                string currentLogicalAddress = $"{addressPrefix}B{startAddress + byteOffset + i}";
                                string currentTagName;
                                if (addressPrefix == "%Q")
                                {
                                    currentTagName = $"{DeviceName}_{moduleName}_F_MessageTrailer_Q_{i}";
                                }
                                else
                                {
                                    currentTagName = $"{DeviceName}_{moduleName}_F_MessageTrailer_I_{i}";
                                }
                                

                                tagTable.Tags.Add(new TagModel
                                {
                                    Name = currentTagName,
                                    DataType = "Byte",
                                    Address = currentLogicalAddress,
                                });

                                Log.Debug(
                                    "Added Message Trailer Byte Tag: {TagName}, Address: {LogicalAddress}, DataType: Byte",
                                    currentTagName, currentLogicalAddress);
                            }

                            // Advance the offset by 4 bytes
                            byteOffset += 4;
                            continue;

                        default:
                            Log.Warning("Unhandled data type {DataType} for DataItem {TextId}. Skipping.",
                                dataItem.DataType, dataItem.TextId);
                            continue; // Skip unhandled data types
                    }

                    // Build a unique tag name for non-bit items
                    string tagName = $"{DeviceName}_{moduleName}_{dataItem.TextId.Replace(" ", "_")}";

                    tagTable.Tags.Add(new TagModel
                    {
                        Name = tagName,
                        DataType = dataType,
                        Address = logicalAddress
                    });

                    Log.Debug("Added Tag: {TagName}, Address: {LogicalAddress}, DataType: {DataType}",
                        tagName, logicalAddress, dataType);

                    // Update byte offset based on data size
                    byteOffset += dataSize;
                }
            }

            // Update startAddress only after all processing is done
            startAddress += byteOffset;
        }
    }
}