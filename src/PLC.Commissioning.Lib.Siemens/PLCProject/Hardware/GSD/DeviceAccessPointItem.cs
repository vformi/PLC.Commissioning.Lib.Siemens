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
            // Initialize Model properties
            Model.ID = dapItemNode.Attributes["ID"]?.Value;
            Model.PhysicalSlots = dapItemNode.Attributes["PhysicalSlots"]?.Value;
            Model.ModuleIdentNumber = dapItemNode.Attributes["ModuleIdentNumber"]?.Value;
            Model.MinDeviceInterval = dapItemNode.Attributes["MinDeviceInterval"]?.Value;
            Model.ImplementationType = dapItemNode.Attributes["ImplementationType"]?.Value;
            Model.DNS_CompatibleName = dapItemNode.Attributes["DNS_CompatibleName"]?.Value;
            Model.AddressAssignment = dapItemNode.Attributes["AddressAssignment"]?.Value;
            Model.CheckDeviceID_Allowed = bool.Parse(dapItemNode.Attributes["CheckDeviceID_Allowed"]?.Value ?? "false");
            Model.FixedInSlots = int.Parse(dapItemNode.Attributes["FixedInSlots"]?.Value ?? "0");
            Model.ObjectUUID_LocalIndex = dapItemNode.Attributes["ObjectUUID_LocalIndex"]?.Value;
            Model.NameOfStationNotTransferable = bool.Parse(dapItemNode.Attributes["NameOfStationNotTransferable"]?.Value ?? "false");
            Model.MultipleWriteSupported = bool.Parse(dapItemNode.Attributes["MultipleWriteSupported"]?.Value ?? "false");
            Model.DeviceAccessSupported = bool.Parse(dapItemNode.Attributes["DeviceAccessSupported"]?.Value ?? "false");
            Model.NumberOfDeviceAccessAR = int.Parse(dapItemNode.Attributes["NumberOfDeviceAccessAR"]?.Value ?? "0");
            Model.SharedDeviceSupported = bool.Parse(dapItemNode.Attributes["SharedDeviceSupported"]?.Value ?? "false");
            Model.SharedInputSupported = bool.Parse(dapItemNode.Attributes["SharedInputSupported"]?.Value ?? "false");
            Model.ResetToFactoryModes = int.Parse(dapItemNode.Attributes["ResetToFactoryModes"]?.Value ?? "0");
            Model.LLDP_NoD_Supported = bool.Parse(dapItemNode.Attributes["LLDP_NoD_Supported"]?.Value ?? "false");
            Model.WebServer = dapItemNode.Attributes["WebServer"]?.Value;
            Model.AdaptsRealIdentification = bool.Parse(dapItemNode.Attributes["AdaptsRealIdentification"]?.Value ?? "false");
            Model.PNIO_Version = dapItemNode.Attributes["PNIO_Version"]?.Value;

            // Parsing ParameterRecordDataItems inside this DAP Item
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
