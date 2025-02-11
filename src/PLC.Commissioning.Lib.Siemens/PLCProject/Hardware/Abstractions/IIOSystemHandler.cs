using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using System;
using System.Collections.Generic;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Abstractions
{
    /// <summary>
    /// Provides methods for handling IO systems, subnets, and their connections within a TIA Portal project hardware configuration.
    /// </summary>
    public interface IIOSystemHandler : IDisposable
    {
        /// <summary>
        /// Finds and returns the first subnet and its associated IO system in the current project.
        /// </summary>
        /// <returns>A tuple containing the <see cref="Subnet"/> and <see cref="IoSystem"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no subnet or IO system is found.</exception>
        (Subnet, IoSystem) FindSubnetAndIoSystem();

        /// <summary>
        /// Connects the specified device to the given subnet and IO system.
        /// </summary>
        /// <param name="device">The <see cref="Device"/> to be connected.</param>
        /// <param name="subnet">The <see cref="Subnet"/> to connect to.</param>
        /// <param name="ioSystem">The <see cref="IoSystem"/> to connect to.</param>
        void ConnectDeviceToIoSystem(Device device, Subnet subnet, IoSystem ioSystem);

        /// <summary>
        /// Retrieves the network interface associated with the specified device.
        /// </summary>
        /// <param name="device">The <see cref="Device"/> from which to retrieve the network interface.</param>
        /// <returns>The <see cref="NetworkInterface"/> of the device.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no network interface is found for the device.</exception>
        NetworkInterface GetDeviceNetworkInterface(Device device);

        /// <summary>
        /// Sets the IP address for the specified device and verifies it.
        /// </summary>
        /// <param name="device">The <see cref="Device"/> for which to set the IP address.</param>
        /// <param name="ipAddress">The IP address to assign to the device.</param>
        /// <returns><c>true</c> if the IP address was set and verified successfully; otherwise, <c>false</c>.</returns>
        bool SetDeviceIPAddress(Device device, string ipAddress);

        /// <summary>
        /// Sets the Profinet name for the specified device and verifies it.
        /// </summary>
        /// <param name="device">The <see cref="Device"/> for which to set the Profinet name.</param>
        /// <param name="profinetName">The Profinet name to assign to the device.</param>
        /// <returns><c>true</c> if the Profinet name was set and verified successfully; otherwise, <c>false</c>.</returns>
        bool SetProfinetName(Device device, string profinetName);

        /// <summary>
        /// Retrieves identification attributes for the specified device.
        /// </summary>
        /// <param name="device">The <see cref="Device"/> for which to retrieve the identification attributes.</param>
        /// <returns>A dictionary containing the identification attributes and their corresponding values.</returns>
        Dictionary<string, string> GetDeviceIdentificationAttributes(Device device);

        /// <summary>
        /// Gets the IP address for the PLC by identifying the appropriate <see cref="DeviceItem"/> based on the 'InterfaceOperatingMode' attribute.
        /// </summary>
        /// <param name="cpu">The <see cref="DeviceItem"/> that represents the PLC device.</param>
        /// <returns>The IP address as a string if found; otherwise, null.</returns>
        string GetPLCIPAddress(DeviceItem cpu);

        /// <summary>
        /// Gets the IP address for the specified device and returns it.
        /// </summary>
        /// <param name="device">The <see cref="Device"/> for which to set the IP address.</param>
        /// <returns>the IP address as string if it was possible to get it othervise null.</returns>
        string GetDeviceIPAddress(Device device);
    }
}
