﻿using PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Abstractions;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using System;
using Serilog;
using System.Linq;
using System.Collections.Generic;
using System.Drawing.Printing;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Handlers
{
    /// <summary>
    /// Implements the <see cref="IHardwareHandler"/> interface to handle hardware-related tasks in a TIA Portal project.
    /// </summary>
    public class HardwareHandler : IHardwareHandler
    {
        private readonly IProjectHandlerService _projectHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="HardwareHandler"/> class with the specified project handler service.
        /// </summary>
        /// <param name="projectHandler">The project handler service used to manage the current project.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="projectHandler"/> is null.</exception>
        public HardwareHandler(IProjectHandlerService projectHandler)
        {
            _projectHandler = projectHandler ?? throw new ArgumentNullException(nameof(projectHandler));
        }

        /// <summary>
        /// Finds and returns the CPU device item from the project.
        /// </summary>
        /// <returns>The CPU <see cref="DeviceItem"/> if found; otherwise, throws an exception.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no CPU is found in the project or if the project is null.</exception>
        public DeviceItem FindCPU()
        {
            var project = _projectHandler.Project;
            if (project is null)
            {
                throw new InvalidOperationException("Project cannot be null");
            }
            Log.Debug($"\nIterate through {project.Devices.Count} device(s)");

            foreach (Device device in project.Devices)
            {
                DeviceItem cpuItem = device.DeviceItems.FirstOrDefault(i => i.Classification == DeviceItemClassifications.CPU);
                if (cpuItem != null)
                {
                    Log.Information($"Found CPU: {device.Name} with name: {cpuItem.Name}");
                    Log.Debug($"Item Classification: {cpuItem.Classification}");
                    return cpuItem;
                }
            }

            throw new InvalidOperationException("No CPU found in the project");
        }

        /// <summary>
        /// Gets a device by its name from the project.
        /// </summary>
        /// <param name="deviceName">The name of the device to search for.</param>
        /// <returns>The <see cref="Device"/> if found; otherwise, <c>null</c>.</returns>
        public Device GetDeviceByName(string deviceName)
        {
            var project = _projectHandler.Project;
            Device device = null;
            Log.Information($"Searching for device named {deviceName} in project {project?.Name}.");
            if (project is null)
            {
                Log.Error("Error, project is not provided.");
                return null;
            }
            if (string.IsNullOrWhiteSpace(deviceName))
            {
                Log.Error("Error, device name is not provided.");
                return null;
            }

            try
            {
                foreach (Device projectDevice in project.Devices)
                {
                    if (projectDevice.DeviceItems.Any(d => d.Name == deviceName))
                    {
                        device = projectDevice;
                        break;
                    }
                }

                // try to search for ungrouped device, if its not a Siemens native device! 
                if (device is null)
                {
                    foreach (Device ungroupedDevice in project.UngroupedDevicesGroup.Devices)
                    {
                        if (ungroupedDevice.DeviceItems.Any(d => d.Name == deviceName))
                        {
                            device = ungroupedDevice;
                            break;
                        }
                    }
                }
            }
            catch (EngineeringException ex)
            {
                Log.Error($"Cannot find device item with {deviceName} name. {ex.Message}");
                return null;
            }

            if (device != null)
            {
                Log.Information($"Successfully found {device.DeviceItems[1].Name} in project {project.Name}.");
                return device;
            }
            else
            {
                Log.Warning($"Device named {deviceName} was not found in project {project.Name}.");
                return null;
            }
        }

        /// <summary>
        /// Gets a specific module by its name from a given device. 
        /// </summary>
        /// <param name="device">The device to search within.</param>
        /// <param name="moduleName">The name of the module to search for.</param>
        /// <returns>The <see cref="GsdDeviceItem"/> representing the module if found; otherwise, <c>null</c>.</returns>
        public GsdDeviceItem GetDeviceModuleByName(Device device, string moduleName)
        {
            var project = _projectHandler.Project;
            GsdDeviceItem module = null;
            Log.Information($"Searching for module named {moduleName} in device {device.DeviceItems[1].Name}.");
            if (project is null)
            {
                Log.Error("Error, project is not provided.");
                return module;
            }
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                Log.Error("Error, device module is not provided.");
                return module;
            }

            try
            {
                foreach (DeviceItem i in device.DeviceItems)
                {
                    foreach (DeviceItem j in i.DeviceItems)
                    {
                        var gsdName = j.GetAttribute("ShortDesignation");
                        if (gsdName.ToString() == moduleName)
                        {
                            GsdDeviceItem gsdDeviceItem = j.GetService<GsdDeviceItem>();
                            module = gsdDeviceItem;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex) // Engineering Exception
            {
                Log.Error($"Cannot find module item {moduleName} in device {device.DeviceItems[1].Name}. {ex.Message}");
                return module;
            }

            if (module != null)
            {
                Log.Information($"Successfully found {moduleName} in device {device.DeviceItems[1].Name}.");
            }
            else
            {
                Log.Warning($"Module named {moduleName} was not found in device {device.DeviceItems[1].Name}.");
            }

            return module;
        }

        /// <summary>
        /// Gets DAP from a given device. 
        /// </summary>
        /// <param name="device">The device to search within.</param>
        /// <returns>The <see cref="GsdDeviceItem"/> representing the module if found; otherwise, <c>null</c>.</returns>
        public GsdDeviceItem GetDeviceDAP(Device device)
        {
            var project = _projectHandler.Project;
            GsdDeviceItem module = null;
            Log.Information($"Searching for device {device.DeviceItems[1].Name} DAP.");
            if (project is null)
            {
                Log.Error("Error, project is not provided.");
                return module;
            }
           
            try
            {
                foreach (DeviceItem i in device.DeviceItems)
                {
                    foreach (DeviceItem j in i.DeviceItems)
                    {
                        var gsdName = j.GetAttribute("GsdId");
                        if (gsdName.ToString() == "DAP")
                        {
                            GsdDeviceItem gsdDeviceItem = j.GetService<GsdDeviceItem>();
                            module = gsdDeviceItem;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex) // Engineering Exception
            {
                Log.Error($"Cannot find device {device.DeviceItems[1].Name} DAP. {ex.Message}");
                return module;
            }

            if (module != null)
            {
                Log.Information($"Successfully found device {device.DeviceItems[1].Name} DAP.");
            }
            else
            {
                Log.Warning($"Device {device.DeviceItems[1].Name} DAP was not found.");
            }

            return module;
        }

        /// <summary>
        /// Enumerates and logs all devices in the current project.
        /// </summary>
        public void EnumerateProjectDevices()
        {
            var project = _projectHandler.Project;
            Log.Information($"\nEnumerating devices in project {project?.Name}:");

            if (project is null)
            {
                Log.Error("Error, project is not provided.");
                return;
            }

            try
            {
                foreach (Device device in project.Devices)
                {
                    Log.Information($"Device: {device.DeviceItems[1].Name}");
                    // More detailed print out, not really necessary 
                    foreach (DeviceItem deviceItem in device.DeviceItems)
                    {
                        Log.Debug($"  Item Name: {deviceItem.Name}");
                        Log.Debug($"  Item Classification: {deviceItem.Classification}");
                    }

                }

                foreach (Device ungroupedDevice in project.UngroupedDevicesGroup.Devices)
                {
                    Log.Information($"Ungrouped Device: \"{ungroupedDevice.DeviceItems[1].Name}\"");
                    // More detailed print out, not really necessary 
                    foreach (DeviceItem deviceItem in ungroupedDevice.DeviceItems)
                    {
                        Log.Debug($"  Item Name: {deviceItem.Name}");
                        Log.Debug($"  Item Classification: {deviceItem.Classification}");
                    }

                }
            }
            catch (EngineeringException ex)
            {
                Log.Error($"An error occurred while enumerating devices: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves all non-CPU devices in the current project, including both grouped and ungrouped devices.
        /// </summary>
        /// <returns>A list of all non-CPU <see cref="Device"/> objects in the project.</returns>
        public List<Device> GetDevices()
        {
            var devices = new List<Device>();
            var project = _projectHandler.Project;

            if (project is null)
            {
                Log.Error("Error, project is not provided.");
                return devices;
            }

            try
            {
                // Add grouped devices, excluding CPUs
                foreach (Device device in project.Devices)
                {
                    if (device.DeviceItems.Any(item => item.Classification == DeviceItemClassifications.CPU))
                    {
                        Log.Information($"Skipping CPU Device: {device.DeviceItems[1].Name}");
                        continue;
                    }

                    devices.Add(device);
                    Log.Information($"Grouped Device: {device.DeviceItems[1].Name}");
                }

                // Add ungrouped devices, excluding CPUs
                foreach (Device ungroupedDevice in project.UngroupedDevicesGroup.Devices)
                {
                    if (ungroupedDevice.DeviceItems.Any(item => item.Classification == DeviceItemClassifications.CPU))
                    {
                        Log.Information($"Skipping CPU Device: {ungroupedDevice.DeviceItems[1].Name}");
                        continue;
                    }

                    devices.Add(ungroupedDevice);
                    Log.Information($"Ungrouped Device: \"{ungroupedDevice.DeviceItems[1].Name}\"");
                }
            }
            catch (EngineeringException ex)
            {
                Log.Error($"An error occurred while retrieving devices: {ex.Message}");
            }

            return devices;
        }
    }
}
