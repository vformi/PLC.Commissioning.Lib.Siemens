using PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Abstractions;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using System;
using Serilog;
using System.Linq;
using System.Collections.Generic;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Models;
using Siemens.Engineering.SW;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Handlers
{
    /// <summary>
    /// Implements the <see cref="IHardwareHandler"/> interface to handle hardware-related tasks in a TIA Portal project.
    /// </summary>
    public class HardwareHandler : IHardwareHandler
    {
        private readonly IProjectHandlerService _projectHandler;
        private bool _disposed;
        /// <summary>
        /// Initializes a new instance of the <see cref="HardwareHandler"/> class with the specified project handler service.
        /// </summary>
        /// <param name="projectHandler">The project handler service used to manage the current project.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="projectHandler"/> is null.</exception>
        public HardwareHandler(IProjectHandlerService projectHandler)
        {
            _projectHandler = projectHandler ?? throw new ArgumentNullException(nameof(projectHandler));
        }

        /// <inheritdoc />
        public (DeviceItem cpuItem, PlcSoftware plcSoftware) FindCPU()
        {
            var project = _projectHandler.Project;
            if (project is null)
            {
                throw new InvalidOperationException("Project cannot be null");
            }
            Log.Debug($"Iterate through {project.Devices.Count} device(s)");

            foreach (Device device in project.Devices)
            {
                DeviceItem cpuItem = device.DeviceItems.FirstOrDefault(i => i.Classification == DeviceItemClassifications.CPU);
                if (cpuItem != null)
                {
                    Log.Information($"Found CPU: {device.Name} with name: {cpuItem.Name}");
                    Log.Debug($"Item Classification: {cpuItem.Classification}");

                    // Retrieve the PLC software associated with this CPU
                    PlcSoftware plcSoftware = GetPlcSoftware(device);
                    if (plcSoftware != null)
                    {
                        Log.Information($"Successfully retrieved PLC software for CPU: {cpuItem.Name}");
                    }
                    else
                    {
                        Log.Warning($"No PLC software found for CPU: {cpuItem.Name}");
                    }

                    return (cpuItem, plcSoftware);
                }
            }

            throw new InvalidOperationException("No CPU found in the project");
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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
        
        /// <inheritdoc />
        public List<IOModuleInfoModel> EnumerateDeviceModules(Device device)
        {
            var modules = new List<IOModuleInfoModel>();

            if (device == null)
            {
                Log.Error("Device is null. Cannot enumerate modules.");
                return modules;
            }

            Log.Information($"Enumerating modules for device: {device.Name}");

            try
            {
                // Iterate through each top-level device item
                foreach (DeviceItem deviceItem in device.DeviceItems)
                {
                    // Retrieve the upper-level GsdId (if any)
                    string upperGsdId = deviceItem.GetAttribute("GsdId")?.ToString();

                    // Iterate through all submodules
                    foreach (DeviceItem subItem in deviceItem.DeviceItems)
                    {
                        // Attempt to locate an existing module in the list by name
                        var ioModuleInfo = modules.FirstOrDefault(m => m.ModuleName == subItem.Name);

                        // If not found, create a new module entry and add it to the list
                        if (ioModuleInfo == null)
                        {
                            ioModuleInfo = new IOModuleInfoModel
                            {
                                ModuleName = subItem.Name,
                                GsdId = upperGsdId
                            };

                            modules.Add(ioModuleInfo);
                        }

                        // Retrieve address information from the submodule
                        foreach (Address address in subItem.Addresses)
                        {
                            if (address.IoType.ToString() == "Input")
                            {
                                ioModuleInfo.InputStartAddress = address.StartAddress;
                                ioModuleInfo.InputLength = address.Length;
                            }
                            else if (address.IoType.ToString() == "Output")
                            {
                                ioModuleInfo.OutputStartAddress = address.StartAddress;
                                ioModuleInfo.OutputLength = address.Length;
                            }
                        }

                        // Log debug information if this module has valid addresses
                        if (ioModuleInfo.InputStartAddress.HasValue || ioModuleInfo.OutputStartAddress.HasValue)
                        {
                            Log.Debug($"Found Valid Module: {ioModuleInfo}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error while enumerating modules: {ex.Message}");
            }

            return modules;
        }
        
        /// <inheritdoc />
        public bool DeleteDevice(Device device)
        {
            try
            {
                if (device == null)
                {
                    Log.Error("DeleteDevice: Device is null.");
                    return false;
                }
                string deviceName = device.DeviceItems[1].Name;
                Log.Information("Delete call for device '{DeviceName}'", deviceName);
                device.Delete();
                Log.Information("Device '{DeviceName}' was deleted successfully.", deviceName);
                return true;
            }
            catch (EngineeringException ex)
            {
                Log.Error($"DeleteDevice: Failed to delete device. {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"DeleteDevice: Unexpected error. {ex.Message}");
                return false;
            }
        }
        
        #region IDisposable Implementation

        /// <summary>
        /// Disposes the resources used by the <see cref="HardwareHandler"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="HardwareHandler"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">A boolean value indicating whether to release managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    if (_projectHandler is IDisposable disposableHandler)
                    {
                        disposableHandler.Dispose();
                    }
                }

                // Mark as disposed
                _disposed = true;
            }
        }

        #endregion

        #region Private methods
        /// <summary>
        /// Retrieves the PLC software from a given device.
        /// </summary>
        /// <param name="device">The device to extract software from.</param>
        /// <returns>The <see cref="PlcSoftware"/> if found; otherwise, <c>null</c>.</returns>
        private PlcSoftware GetPlcSoftware(Device device)
        {
            if (device == null)
            {
                Log.Error("Device is null. Cannot retrieve PLC software.");
                return null;
            }

            foreach (DeviceItem deviceItem in device.DeviceItems)
            {
                SoftwareContainer softwareContainer = deviceItem.GetService<SoftwareContainer>();
                if (softwareContainer != null)
                {
                    global::Siemens.Engineering.HW.Software softwareBase = softwareContainer.Software;
                    PlcSoftware plcSoftware = softwareBase as PlcSoftware;

                    if (plcSoftware != null)
                    {
                        return plcSoftware;
                    }
                }
            }

            return null;
        }
        #endregion
    }
}