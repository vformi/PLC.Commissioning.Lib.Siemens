using System.Collections.Generic;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models
{
    /// <summary>
    /// Represents a device access point item within the GSD file.
    /// </summary>
    public class DeviceAccessPointItemModel
    {
        // Required attributes
        public string ID { get; set; }
        public string PhysicalSlots { get; set; }
        public string ModuleIdentNumber { get; set; }

        // Optional attributes (stored as strings mostly)
        public int MinDeviceInterval { get; set; }
        public string ImplementationType { get; set; }
        public string DNS_CompatibleName { get; set; }
        public string AddressAssignment { get; set; }
        public bool CheckDeviceID_Allowed { get; set; }
        public int FixedInSlots { get; set; }
        public int ObjectUUID_LocalIndex { get; set; }
        public string NameOfStationNotTransferable { get; set; }
        public bool MultipleWriteSupported { get; set; }
        public bool DeviceAccessSupported { get; set; }
        public string NumberOfDeviceAccessAR { get; set; }
        public bool SharedDeviceSupported { get; set; }
        public bool SharedInputSupported { get; set; }
        public string ResetToFactoryModes { get; set; }
        public string LLDP_NoD_Supported { get; set; }
        public string WebServer { get; set; }
        public string AdaptsRealIdentification { get; set; }
        public string PNIO_Version { get; set; }
            
        public ParameterRecordDataItem ParameterRecordDataItem { get; set; }
        public FParameterRecordDataItem FParameterRecordDataItem { get; set; }
    }
}