using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using System;
using System.Collections.Generic;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Models;
using Siemens.Engineering.SW;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Abstractions
{
    /// <summary>
    /// Defines the operations for handling hardware-related tasks in a TIA Portal project.
    /// </summary>
    public interface IHardwareHandler : IDisposable
    {
        /// <summary>
        /// Finds and returns the CPU device item from the project along with its PLC software.
        /// </summary>
        /// <returns>A tuple containing the CPU <see cref="DeviceItem"/> and its <see cref="PlcSoftware"/>.
        /// If no CPU is found, throws an exception.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no CPU is found in the project or if the project is null.</exception>
        (DeviceItem cpuItem, PlcSoftware plcSoftware) FindCPU();
        
        /// <summary>
        /// Gets a device by its name from the project.
        /// </summary>
        /// <param name="deviceName">The name of the device to search for.</param>
        /// <returns>The <see cref="ProjectDevice"/> if found; otherwise, <c>null</c>.</returns>
        ProjectDevice GetDeviceByName(string deviceName);

        /// <summary>
        /// Gets a specific module by its name from a given device.
        /// </summary>
        /// <param name="device">The device to search within.</param>
        /// <param name="moduleName">The name of the module to search for.</param>
        /// <returns>The <see cref="GsdDeviceItem"/> representing the module if found; otherwise, <c>null</c>.</returns>
        GsdDeviceItem GetDeviceModuleByName(Device device, string moduleName);

        /// <summary>
        /// Gets the DAP from a given device.
        /// </summary>
        /// <param name="device">The device to search within.</param>
        /// <returns>The <see cref="GsdDeviceItem"/> representing the DAP if found; otherwise, <c>null</c>.</returns>
        GsdDeviceItem GetDeviceDAP(Device device);

        /// <summary>
        /// Enumerates and logs all devices in the current project.
        /// </summary>
        void EnumerateProjectDevices();

        /// <summary>
        /// Retrieves all non-CPU devices in the current project, including both grouped and ungrouped devices.
        /// </summary>
        /// <returns>A list of all non-CPU <see cref="Device"/> objects in the project.</returns>
        List<ProjectDevice> GetDevices();

        /// <summary>
        /// Enumerates device modules, identifies modules, and retrieves address information.
        /// </summary>
        /// <param name="device">The device to scan for modules.</param>
        /// <returns>A list of module details, including their addresses.</returns>
        List<IOModuleInfoModel> EnumerateDeviceModules(Device device);
        
        /// <summary>
        /// Deletes the specified device from the project.
        /// </summary>
        /// <param name="device">The device to be deleted.</param>
        /// <returns>
        /// <c>true</c> if the device was successfully deleted;
        /// <c>false</c>if the device is null or if an exception occurs during deletion.
        /// </returns>
        bool DeleteDevice(Device device);
    }
}
