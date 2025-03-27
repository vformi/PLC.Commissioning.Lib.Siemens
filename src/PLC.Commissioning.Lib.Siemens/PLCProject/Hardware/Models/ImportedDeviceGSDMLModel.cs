using System.Collections.Generic;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Models
{
    /// <summary>
    /// Represents the data model for an imported device,
    /// including the merged GSDML data.
    /// </summary>
    public class ImportedDeviceGSDMLModel
    {
        /// <summary>
        /// The parsed overall module information from the GSDML file.
        /// </summary>
        public ModuleInfo ModuleInfo { get; set; }

        /// <summary>
        /// The Device Access Point (DAP) data from the GSDML file.
        /// </summary>
        public DeviceAccessPointList Dap { get; set; }

        /// <summary>
        /// The complete list of modules from the GSDML file.
        /// </summary>
        public ModuleList ModuleList { get; set; }
    }
}