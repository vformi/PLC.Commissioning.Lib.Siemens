namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models
{
    /// <summary>
    /// Represents the GSD module information, containing details such as the name, vendor, and version information.
    /// </summary>
    public class ModuleInfoModel
    {
        public string Name { get; set; }
        public string InfoText { get; set; }
        public string VendorName { get; set; }
        public string OrderNumber { get; set; }
        public string HardwareRelease { get; set; }
        public string SoftwareRelease { get; set; }
    }
}
