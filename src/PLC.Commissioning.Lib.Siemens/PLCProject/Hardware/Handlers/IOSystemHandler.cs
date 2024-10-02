using PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions;
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
        /// Represents the project handler implementing methods from the interface <see cref="IProjectHandlerService"/>
        /// </summary>
        private readonly IProjectHandlerService _projectHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="IOSystemHandler"/> class with the specified project handler service.
        /// </summary>
        /// <param name="projectHandler">The project handler service used to manage the current project.</param>
        public IOSystemHandler(IProjectHandlerService projectHandler)
        {
            _projectHandler = projectHandler ?? throw new ArgumentNullException(nameof(projectHandler));
        }

        /// <summary>
        /// Finds and returns the first subnet and its associated IO system in the current project.
        /// </summary>
        /// <returns>A tuple containing the <see cref="Subnet"/> and <see cref="IoSystem"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no subnet or IO system is found.</exception>
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

        /// <summary>
        /// Connects the specified device to the given subnet and IO system.
        /// </summary>
        /// <param name="device">The <see cref="Device"/> to be connected.</param>
        /// <param name="subnet">The <see cref="Subnet"/> to connect to.</param>
        /// <param name="ioSystem">The <see cref="IoSystem"/> to connect to.</param>
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

        /// <summary>
        /// Retrieves the network interface associated with the specified device.
        /// </summary>
        /// <param name="device">The <see cref="Device"/> from which to retrieve the network interface.</param>
        /// <returns>The <see cref="NetworkInterface"/> of the device.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no network interface is found for the device.</exception>
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

        /// <summary>
        /// Sets the IP address for the specified device and verifies it.
        /// </summary>
        /// <param name="device">The <see cref="Device"/> for which to set the IP address.</param>
        /// <param name="ipAddress">The IP address to assign to the device.</param>
        /// <returns><c>true</c> if the IP address was set and verified successfully; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Sets the Profinet name for the specified device and verifies it.
        /// </summary>
        /// <param name="device">The <see cref="Device"/> for which to set the Profinet name.</param>
        /// <param name="profinetName">The Profinet name to assign to the device.</param>
        /// <returns><c>true</c> if the Profinet name was set and verified successfully; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Gets the IP address for the specified device and returns it.
        /// </summary>
        /// <param name="device">The <see cref="Device"/> for which to set the IP address.</param>
        /// <returns>the IP address as string if it was possible to get it othervise null.</returns>
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

        /// <summary>
        /// Gets the IP address for the PLC.
        /// </summary>
        /// <param name="cpu">The <see cref="DeviceItem"/> for which to set the IP address.</param>
        /// <returns>the IP address as string if it was possible to get it othervise null.</returns>
        public string GetPLCIPAddress(DeviceItem cpu)
        {
            try
            {
                // Get the network interface for the device
                NetworkInterface networkInterface = cpu.DeviceItems[2].GetService<NetworkInterface>(); // little bit hardcoded, but should work most of the times in our case 
                // if not we will implement a more sophisticated solution of how to get the DeviceItem (PROFINET interface)

                // Assuming there is a Node object associated with the network interface
                Node node = networkInterface.Nodes[0]; // Node (IE1)

                string address = node.GetAttribute("Address").ToString();

                Log.Information($"Device {cpu.Name} has IP: {address}.");
                return address;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to get IP address for device {cpu.Name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Retrieves identification attributes for the specified device.
        /// </summary>
        /// <param name="device">The <see cref="Device"/> for which to retrieve the identification attributes.</param>
        /// <returns>A dictionary containing the identification attributes and their corresponding values.</returns>
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
    }
}
