﻿using PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Abstractions;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using System;
using System.Linq;
using Serilog;
using System.Collections.Generic;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Handlers
{
    /// <summary>
    /// Implements the <see cref="IIOSystemHandler"/> interface to manage IO systems and their connections in a Siemens PLC project.
    /// </summary>
    public class IOSystemHandler : IIOSystemHandler
    {
        /// <summary>
        /// Represents the project handler implementing methods from the interface <see cref="IProjectHandlerService"/>.
        /// </summary>
        private readonly IProjectHandlerService _projectHandler;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="IOSystemHandler"/> class with the specified project handler service.
        /// </summary>
        /// <param name="projectHandler">The project handler service used to manage the current project.</param>
        public IOSystemHandler(IProjectHandlerService projectHandler)
        {
            _projectHandler = projectHandler ?? throw new ArgumentNullException(nameof(projectHandler));
        }

        /// <inheritdoc />
        public (Subnet, IoSystem) FindSubnetAndIoSystem()
        {
            var project = _projectHandler.Project;
            var subnet = project.Subnets.FirstOrDefault();
            var ioSystem = subnet?.IoSystems.FirstOrDefault();
            Log.Information($"\nSearching for Subnet and IO System in {project.Name}");
            if (subnet is null || ioSystem is null)
            {
                throw new InvalidOperationException("Subnet or IoSystem not found in the project.");
            }

            Log.Information($"Found Subnet: {subnet.Name}");
            Log.Information($"Found IO System: {ioSystem.Name}");

            return (subnet, ioSystem);
        }

        /// <inheritdoc />
        public void ConnectDeviceToIoSystem(Device device, Subnet subnet, IoSystem ioSystem)
        {
            NetworkInterface networkInterface = GetDeviceNetworkInterface(device);
            Log.Information($"Attempting to connect device {device.DeviceItems[1].Name} to subnet {subnet.Name}");

            var existingSubnetConnection = networkInterface.Nodes.FirstOrDefault()?.ConnectedSubnet;
            if (existingSubnetConnection != null)
            {
                Log.Information($"Device {device.DeviceItems[1].Name} is already connected to Subnet {existingSubnetConnection.Name}.");
            }
            else
            {
                networkInterface.Nodes.First().ConnectToSubnet(subnet);
            }

            var existingIoSystemConnection = networkInterface.IoConnectors.FirstOrDefault()?.ConnectedToIoSystem;
            if (existingIoSystemConnection != null)
            {
                Log.Information($"Device {device.DeviceItems[1].Name} is already connected to IO System {existingIoSystemConnection.Name}.");
            }
            else
            {
                networkInterface.IoConnectors.First().ConnectToIoSystem(ioSystem);
            }

            Log.Information($"Device {device.DeviceItems[1].Name} connected to Subnet {subnet.Name} and IO System {ioSystem.Name}.");
        }

        /// <inheritdoc />
        public NetworkInterface GetDeviceNetworkInterface(Device device)
        {
            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            DeviceItem pnio = null;
            foreach (DeviceItem i in device.DeviceItems)
            {
                foreach (DeviceItem j in i.DeviceItems)
                {
                    var typeName = j.GetAttribute("TypeName");
                    if (typeName?.ToString() == "PN-IO")
                    {
                        pnio = j; // Assign the PN-IO DeviceItem
                        break; // Exit the loop once we find the PN-IO item
                    }
                }
            }

            if (pnio is null)
            {
                throw new InvalidOperationException("DeviceItem with TypeName 'PN-IO' not found.");
            }

            NetworkInterface networkInterface = pnio.GetService<NetworkInterface>();
            if (networkInterface is null)
            {
                throw new InvalidOperationException("NetworkInterface not found in the 'PN-IO' DeviceItem.");
            }

            return networkInterface;
        }

        /// <inheritdoc />
        public bool SetDeviceIPAddress(Device device, string ipAddress)
        {
            try
            {
                // Get the network interface for the device
                NetworkInterface networkInterface = GetDeviceNetworkInterface(device);

                // Assuming there is a Node object associated with the network interface
                Node node = networkInterface.Nodes[0]; // Node (IE1)

                // Set the IP address attribute for the node
                node.SetAttribute("Address", ipAddress);

                // Verify the IP address was set correctly
                string setAddress = node.GetAttribute("Address").ToString();

                if (setAddress != ipAddress)
                {
                    Log.Error($"IP address verification failed. Expected: {ipAddress}, but found: {setAddress}");
                    return false;
                }

                // Log the successful setting of the IP address
                Log.Information($"Set IP: {ipAddress} to Device: {device.DeviceItems[1].Name}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to set IP address for device {device.DeviceItems[1].Name}: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public bool SetProfinetName(Device device, string profinetName)
        {
            try
            {
                // Get the network interface for the device
                NetworkInterface networkInterface = GetDeviceNetworkInterface(device);

                // Assuming there is a Node object associated with the network interface
                Node node = networkInterface.Nodes[0]; // Node (IE1)

                // Set the automatic generation of the Profinet name to false
                node.SetAttribute("PnDeviceNameAutoGeneration", false);

                // Set the Profinet name for the device
                node.SetAttribute("PnDeviceName", profinetName);

                // Verify the Profinet name was set correctly
                string setProfinetName = node.GetAttribute("PnDeviceName").ToString();

                if (setProfinetName != profinetName)
                {
                    Log.Error($"Profinet name verification failed. Expected: {profinetName}, but found: {setProfinetName}");
                    return false;
                }

                // Log the successful setting of the Profinet name
                Log.Information($"Set Profinet name: {profinetName} to Device: {device.DeviceItems[1].Name}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to set Profinet name for device {device.DeviceItems[1].Name}: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public string GetDeviceIPAddress(Device device)
        {
            try
            {
                // Get the network interface for the device
                NetworkInterface networkInterface = GetDeviceNetworkInterface(device);

                // Assuming there is a Node object associated with the network interface
                Node node = networkInterface.Nodes[0]; // Node (IE1)

                string address = node.GetAttribute("Address").ToString();

                Log.Information($"Device {device.DeviceItems[1].Name} has IP: {address}.");
                return address;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to get IP address for device {device.DeviceItems[1].Name}: {ex.Message}");
                return null;
            }
        }

        /// <inheritdoc />
        public string GetPLCIPAddress(DeviceItem cpu)
        {
            try
            {
                // Iterate through all DeviceItems to find the one with the required attribute
                foreach (var deviceItem in cpu.DeviceItems)
                {
                    string operatingMode = null;
                    try
                    {
                        operatingMode = deviceItem.GetAttribute("InterfaceOperatingMode")?.ToString();
                    }
                    catch (Exception ex)
                    {
                        Log.Debug($"Skipping DeviceItem {deviceItem.Name} as it does not support 'InterfaceOperatingMode'");
                        continue;
                    }

                    if (!string.IsNullOrEmpty(operatingMode) && operatingMode == "IoController")
                    {
                        // Get the network interface for the identified DeviceItem
                        var networkInterface = deviceItem.GetService<NetworkInterface>();
                        if (networkInterface != null && networkInterface.Nodes.Count > 0)
                        {
                            var node = networkInterface.Nodes[0];
                            string address = node.GetAttribute("Address").ToString();
                            Log.Information($"Device {cpu.Name} has IP: {address}");
                            return address;
                        }
                    }
                }

                Log.Warning($"No DeviceItem with 'InterfaceOperatingMode=IoController' found for device: {cpu.Name}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to get IP address for device {cpu.Name}: {ex.Message}");
                return null;
            }
        }

        /// <inheritdoc />
        public Dictionary<string, string> GetDeviceIdentificationAttributes(Device device)
        {
            var attributes = new Dictionary<string, string>();

            try
            {
                if (device is null)
                {
                    Log.Error("The device is null.");
                    return null;
                }

                DeviceItem deviceItem = device.DeviceItems[1];
                if (deviceItem is null)
                {
                    Log.Error("DeviceItem is null.");
                    return null;
                }

                // List of required identification attributes
                var requiredAttributes = new List<string>
                {
                    "FirmwareVersion",
                    "OrderNumber",
                    "TypeName"
                };

                // Retrieve the attributes using a single call
                var retrievedAttributes = deviceItem.GetAttributes(requiredAttributes);

                // Populate the dictionary with the retrieved attributes
                for (int i = 0; i < requiredAttributes.Count; i++)
                {
                    if (retrievedAttributes[i] != null)
                    {
                        attributes[requiredAttributes[i]] = retrievedAttributes[i].ToString();
                    }
                }

                // Log the retrieval of the attributes
                Log.Information($"Retrieved identification attributes for Device: {deviceItem.Name}");

                return attributes;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to retrieve identification attributes for device {device.DeviceItems[1].Name}: {ex.Message}");
                return null;
            }
        }
        
        #region IDisposable Implementation
        
        /// <summary>
        /// Disposes the resources used by the <see cref="IOSystemHandler"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="IOSystemHandler"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">A boolean value indicating whether to release managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    // If the _projectHandler needs to be disposed, do so here
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
    }
}