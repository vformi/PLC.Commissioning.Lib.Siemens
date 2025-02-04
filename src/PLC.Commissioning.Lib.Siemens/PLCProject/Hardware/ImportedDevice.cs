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
        public string DeviceName { get; private set; }
        public string GsdmlFilePath { get; private set; }
        public Device Device { get; private set; } // The TIA object representing the device
        private GSDHandler GsdHandler { get; set; }

        /// <summary>
        /// Modules as parsed from the project (hardware-side information).
        /// </summary>
        public List<IOModuleInfoModel> Modules { get; private set; } = new List<IOModuleInfoModel>();

        /// <summary>
        /// Holds the merged device model information, including the GSDML module definitions.
        /// </summary>
        public ImportedDeviceGSDMLModel DeviceGsdmlModel { get; private set; }

        public ImportedDevice(string deviceName, string gsdmlFilePath, Device tiaDevice)
        {
            DeviceName = deviceName ?? throw new ArgumentNullException(nameof(deviceName));
            GsdmlFilePath = gsdmlFilePath ?? throw new ArgumentNullException(nameof(gsdmlFilePath));
            Device = tiaDevice ?? throw new ArgumentNullException(nameof(tiaDevice));

            InitializeGsdmlHandler();
        }

        /// <summary>
        /// Initializes the GSDML handler and the GSDML model for the device.
        /// </summary>
        private void InitializeGsdmlHandler()
        {
            GsdHandler = new GSDHandler();

            if (!GsdHandler.Initialize(GsdmlFilePath))
            {
                Log.Error("Failed to initialize GSDHandler with file: {FilePath}", GsdmlFilePath);
                return;
            }

            DeviceGsdmlModel = new ImportedDeviceGSDMLModel
            {
                ModuleInfo = new ModuleInfo(GsdHandler),
                Dap = new DeviceAccessPointList(GsdHandler),
                ModuleList = new ModuleList(GsdHandler)
            };

            Log.Information("GSDML Model initialized for device: {DeviceName}", DeviceName);
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

                if (gsdmlModule == null || gsdmlModule.Model.IOData == null)
                {
                    Log.Warning("No GSDML IOData found for module {ModuleName}. Skipping tag table creation.",
                        hwModule.ModuleName);
                    continue; // Skip this module entirely
                }

                // Use GSDML-defined name
                string gsdmlModuleName = gsdmlModule.Model.Name.Replace(" ", "_");

                Log.Debug("Using GSDML Module Name: {GsdmlModuleName} instead of TIA name: {ModuleName}",
                    gsdmlModuleName, hwModule.ModuleName);

                // Create a tag table for this module only if it has IOData
                var tagTable = new TagTableModel
                {
                    TableName = gsdmlModuleName, // Use the GSDML Name
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
                        string tagName = $"{moduleName}_{bitDataItem.TextId.Replace(" ", "_")}"; // Prefix and format

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
                                string currentTagName = $"{moduleName}_{dataItem.TextId.Replace(" ", "_")}_{i}";
                            
                                tagTable.Tags.Add(new TagModel
                                {
                                    Name = currentTagName,
                                    DataType = "Byte", 
                                    Address = currentLogicalAddress,
                                });
                            
                                Log.Debug("Added OctetString Byte Tag: {TagName}, Address: {LogicalAddress}, DataType: Byte",
                                    currentTagName, currentLogicalAddress);
                            }
                            // Advance the offset by the full length.
                            byteOffset += length;
                            // Continue with next DataItem so no further tag is added for OctetString.
                            continue;
                        }

                        default:
                            Log.Warning("Unhandled data type {DataType} for DataItem {TextId}. Skipping.",
                                dataItem.DataType, dataItem.TextId);
                            continue; // Skip unhandled data types
                    }

                    string tagName = $"{moduleName}_{dataItem.TextId.Replace(" ", "_")}"; // Prefix and format

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