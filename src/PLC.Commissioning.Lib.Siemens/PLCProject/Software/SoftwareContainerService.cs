using System;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Software
{
    internal class SoftwareContainerService
    {
        /// <summary>
        /// Retrieves the PlcSoftware from the specified DeviceItem.
        /// </summary>
        /// <param name="deviceItem">The DeviceItem to get the PlcSoftware from.</param>
        /// <returns>The PlcSoftware if found, otherwise null.</returns>
        public PlcSoftware GetPlcSoftware(DeviceItem deviceItem)
        {
            var plcSoftware = deviceItem.GetService<SoftwareContainer>().Software as PlcSoftware;
            return plcSoftware;
        }
    }
}
