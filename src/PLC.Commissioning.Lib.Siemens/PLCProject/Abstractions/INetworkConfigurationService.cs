using PLC.Commissioning.Lib.Siemens.PLCProject.Enums;
using Siemens.Engineering.Connection;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions
{
    /// <summary>
    /// Defines methods for configuring network settings and matching network interfaces.
    /// </summary>
    public interface INetworkConfigurationService
    {
        /// <summary>
        /// Displays the available PLC connection possibilities.
        /// </summary>
        void DisplayPLCConnectionPossibilities();

        /// <summary>
        /// Configures the network settings using the specified network card and target interface.
        /// </summary>
        NetworkConfigurationResult ConfigureNetwork(string networkCardName, int interfaceNumber, string targetInterfaceName);

        /// <summary>
        /// Gets the configured target network interface.
        /// </summary>
        IConfiguration GetTargetConfiguration();
    }
}
