using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;
using Serilog;
using System;
using System.Xml;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Abstractions;
using System.Text;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD
{
    /// <summary>
    /// Represents a device access point item within a Siemens PLC project, handling the parsing of data from GSD files.
    /// </summary>
    public class DeviceAccessPointItem : IDeviceItem
    {
        /// <summary>
        /// The GSD handler used for processing device access point data.
        /// </summary>
        private readonly GSDHandler _gsdHandler;

        /// <summary>
        /// Gets the model containing the parsed device access point item information.
        /// </summary>
        public DeviceAccessPointItemModel Model { get; private set; }

        /// <summary>
        /// Gets the parameter record data item associated with the device access point model.
        /// </summary>
        public ParameterRecordDataItem ParameterRecordDataItem => Model.ParameterRecordDataItem;
        
        /// <summary>
        /// Gets the F parameter record data item associated with the device access point model.
        /// </summary>
        public FParameterRecordDataItem FParameterRecordDataItem => Model.FParameterRecordDataItem;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAccessPointItem"/> class with the specified GSD handler.
        /// </summary>
        /// <param name="gsdHandler">The GSD handler used for processing device access point data.</param>
        public DeviceAccessPointItem(GSDHandler gsdHandler)
        {
            _gsdHandler = gsdHandler ?? throw new ArgumentNullException(nameof(gsdHandler));
            Model = new DeviceAccessPointItemModel();
        }

        /// <summary>
        /// Parses an XML node representing a device access point item and populates the corresponding model.
        /// </summary>
        /// <param name="dapItemNode">The XML node containing the device access point item data.</param>
        public void ParseDeviceAccessPointItem(XmlNode dapItemNode)
        {
            if (dapItemNode == null)
            {
                throw new ArgumentNullException(nameof(dapItemNode), "DAP item node cannot be null.");
            }

            // Required attributes
            Model.ID = dapItemNode.Attributes["ID"]?.Value;
            if (string.IsNullOrWhiteSpace(Model.ID))
            {
                throw new InvalidOperationException("Missing or empty required 'ID' attribute in Device Access Point item.");
            }

            Model.PhysicalSlots = dapItemNode.Attributes["PhysicalSlots"]?.Value;
            if (string.IsNullOrWhiteSpace(Model.PhysicalSlots))
            {
                throw new InvalidOperationException("Missing or empty required 'PhysicalSlots' attribute in Device Access Point item.");
            }

            Model.ModuleIdentNumber = dapItemNode.Attributes["ModuleIdentNumber"]?.Value;
            if (string.IsNullOrWhiteSpace(Model.ModuleIdentNumber))
            {
                throw new InvalidOperationException("Missing or empty required 'ModuleIdentNumber' attribute in Device Access Point item.");
            }

            // Optional attributes with parsing where required by model
            if (!int.TryParse(dapItemNode.Attributes["MinDeviceInterval"]?.Value, out int minDeviceInterval))
            {
                Log.Warning("Invalid or missing 'MinDeviceInterval' attribute in DAP item. Defaulting to 0.");
                minDeviceInterval = 0;
            }
            Model.MinDeviceInterval = minDeviceInterval;

            Model.ImplementationType = dapItemNode.Attributes["ImplementationType"]?.Value;
            Model.DNS_CompatibleName = dapItemNode.Attributes["DNS_CompatibleName"]?.Value;
            Model.AddressAssignment = dapItemNode.Attributes["AddressAssignment"]?.Value;

            if (!bool.TryParse(dapItemNode.Attributes["CheckDeviceID_Allowed"]?.Value, out bool checkDeviceIdAllowed))
            {
                Log.Warning("Invalid or missing 'CheckDeviceID_Allowed' attribute in DAP item. Defaulting to false.");
                checkDeviceIdAllowed = false;
            }
            Model.CheckDeviceID_Allowed = checkDeviceIdAllowed;

            if (!int.TryParse(dapItemNode.Attributes["FixedInSlots"]?.Value, out int fixedInSlots))
            {
                Log.Warning("Invalid or missing 'FixedInSlots' attribute in DAP item. Defaulting to 0.");
                fixedInSlots = 0;
            }
            Model.FixedInSlots = fixedInSlots;

            if (!int.TryParse(dapItemNode.Attributes["ObjectUUID_LocalIndex"]?.Value, out int objectUuidLocalIndex))
            {
                Log.Warning("Invalid or missing 'ObjectUUID_LocalIndex' attribute in DAP item. Defaulting to 0.");
                objectUuidLocalIndex = 0;
            }
            Model.ObjectUUID_LocalIndex = objectUuidLocalIndex;

            Model.NameOfStationNotTransferable = dapItemNode.Attributes["NameOfStationNotTransferable"]?.Value;

            if (!bool.TryParse(dapItemNode.Attributes["MultipleWriteSupported"]?.Value, out bool multipleWriteSupported))
            {
                Log.Warning("Invalid or missing 'MultipleWriteSupported' attribute in DAP item. Defaulting to false.");
                multipleWriteSupported = false;
            }
            Model.MultipleWriteSupported = multipleWriteSupported;

            if (!bool.TryParse(dapItemNode.Attributes["DeviceAccessSupported"]?.Value, out bool deviceAccessSupported))
            {
                Log.Warning("Invalid or missing 'DeviceAccessSupported' attribute in DAP item. Defaulting to false.");
                deviceAccessSupported = false;
            }
            Model.DeviceAccessSupported = deviceAccessSupported;

            Model.NumberOfDeviceAccessAR = dapItemNode.Attributes["NumberOfDeviceAccessAR"]?.Value;

            if (!bool.TryParse(dapItemNode.Attributes["SharedDeviceSupported"]?.Value, out bool sharedDeviceSupported))
            {
                Log.Warning("Invalid or missing 'SharedDeviceSupported' attribute in DAP item. Defaulting to false.");
                sharedDeviceSupported = false;
            }
            Model.SharedDeviceSupported = sharedDeviceSupported;

            if (!bool.TryParse(dapItemNode.Attributes["SharedInputSupported"]?.Value, out bool sharedInputSupported))
            {
                Log.Warning("Invalid or missing 'SharedInputSupported' attribute in DAP item. Defaulting to false.");
                sharedInputSupported = false;
            }
            Model.SharedInputSupported = sharedInputSupported;

            // Keep ResetToFactoryModes as string
            Model.ResetToFactoryModes = dapItemNode.Attributes["ResetToFactoryModes"]?.Value;

            Model.LLDP_NoD_Supported = dapItemNode.Attributes["LLDP_NoD_Supported"]?.Value;
            Model.WebServer = dapItemNode.Attributes["WebServer"]?.Value;
            Model.AdaptsRealIdentification = dapItemNode.Attributes["AdaptsRealIdentification"]?.Value;
            Model.PNIO_Version = dapItemNode.Attributes["PNIO_Version"]?.Value;

            // Parse ParameterRecordDataItem
            XmlNode parameterRecordDataItemNode = dapItemNode.SelectSingleNode("gsd:VirtualSubmoduleList/gsd:VirtualSubmoduleItem/gsd:RecordDataList/gsd:ParameterRecordDataItem", _gsdHandler.nsmgr);
            if (parameterRecordDataItemNode != null)
            {
                Model.ParameterRecordDataItem = new ParameterRecordDataItem(_gsdHandler);
                Model.ParameterRecordDataItem.ParseParameterRecordDataItem(parameterRecordDataItemNode);
            }
            else
            {
                Model.ParameterRecordDataItem = null;
            }

            // Parse FParameterRecordDataItem
            XmlNode fParameterRecordDataItemNode = dapItemNode.SelectSingleNode("gsd:VirtualSubmoduleList/gsd:VirtualSubmoduleItem/gsd:RecordDataList/gsd:F_ParameterRecordDataItem", _gsdHandler.nsmgr);
            if (fParameterRecordDataItemNode != null)
            {
                Model.FParameterRecordDataItem = new FParameterRecordDataItem(_gsdHandler);
                Model.FParameterRecordDataItem.ParseFParameterRecordDataItem(fParameterRecordDataItemNode);
            }
            else
            {
                Model.FParameterRecordDataItem = null;
            }
        }

        /// <summary>
        /// Returns a string representation of the device access point item, including its properties and parameter record data.
        /// </summary>
        /// <returns>A string describing the device access point item.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"ID: {Model.ID}");
            sb.AppendLine($"Physical Slots: {Model.PhysicalSlots}");
            sb.AppendLine($"Module Ident Number: {Model.ModuleIdentNumber}");
            sb.AppendLine($"Min Device Interval: {Model.MinDeviceInterval}");
            sb.AppendLine($"Implementation Type: {Model.ImplementationType}");
            sb.AppendLine($"DNS Compatible Name: {Model.DNS_CompatibleName}");
            sb.AppendLine($"Address Assignment: {Model.AddressAssignment}");
            sb.AppendLine($"Check Device ID Allowed: {Model.CheckDeviceID_Allowed}");
            sb.AppendLine($"Fixed In Slots: {Model.FixedInSlots}");
            sb.AppendLine($"Object UUID Local Index: {Model.ObjectUUID_LocalIndex}");
            sb.AppendLine($"Name Of Station Not Transferable: {Model.NameOfStationNotTransferable}");
            sb.AppendLine($"Multiple Write Supported: {Model.MultipleWriteSupported}");
            sb.AppendLine($"Device Access Supported: {Model.DeviceAccessSupported}");
            sb.AppendLine($"Number Of Device Access AR: {Model.NumberOfDeviceAccessAR}");
            sb.AppendLine($"Shared Device Supported: {Model.SharedDeviceSupported}");
            sb.AppendLine($"Shared Input Supported: {Model.SharedInputSupported}");
            sb.AppendLine($"Reset To Factory Modes: {Model.ResetToFactoryModes}");
            sb.AppendLine($"LLDP NoD Supported: {Model.LLDP_NoD_Supported}");
            sb.AppendLine($"Web Server: {Model.WebServer}");
            sb.AppendLine($"Adapts Real Identification: {Model.AdaptsRealIdentification}");
            sb.AppendLine($"PNIO Version: {Model.PNIO_Version}");

            if (Model.ParameterRecordDataItem != null)
            {
                sb.AppendLine(Model.ParameterRecordDataItem.ToString());
            }
            else
            {
                sb.AppendLine("Device Access Point has no parameters to be changed.");
            }

            return sb.ToString();
        }
    }
}
