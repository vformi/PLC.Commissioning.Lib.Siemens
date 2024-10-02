using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using System.Collections.Generic;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Abstractions
{
    /// <summary>
    /// Defines the operations for handling hardware-related tasks in a TIA Portal project.
    /// </summary>
    public interface IHardwareHandler
    {
        /// <summary>
        /// Finds and returns the CPU device item from the project.
        /// </summary>
        /// <returns>The CPU <see cref="DeviceItem"/> if found; otherwise, throws an exception.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no CPU is found in the project.</exception>
        DeviceItem FindCPU();

        /// <summary>
        /// Gets a device by its name from the project.
        /// </summary>
        /// <param name="deviceName">The name of the device to search for.</param>
        /// <returns>The <see cref="Device"/> if found; otherwise, <c>null</c>.</returns>
        Device GetDeviceByName(string deviceName);

        /// <summary>
        /// Gets a specific module by its name from a given device.
        /// </summary>
        /// <param name="device">The device to search within.</param>
        /// <param name="moduleName">The name of the module to search for.</param>
        /// <returns>The <see cref="GsdDeviceItem"/> representing the module if found; otherwise, <c>null</c>.</returns>
        GsdDeviceItem GetDeviceModuleByName(Device device, string moduleName);

        /// <summary>
        /// Enumerates and logs all devices in the current project.
        /// </summary>
        void EnumerateProjectDevices();

        /// <summary>
        /// Retrieves all non-CPU devices in the current project, including both grouped and ungrouped devices.
        /// </summary>
        /// <returns>A list of all non-CPU <see cref="Device"/> objects in the project.</returns>
        List<Device> GetDevices();
    }
}
