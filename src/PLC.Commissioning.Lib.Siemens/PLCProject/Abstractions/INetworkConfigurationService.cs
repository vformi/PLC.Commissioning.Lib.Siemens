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
        /// <param name="networkCardName">The name of the network card to use.</param>
        /// <param name="interfaceNumber">The interface number to use.</param>
        /// <param name="targetInterfaceName">The name of the target interface.</param>
        /// <returns>
        /// A <see cref="NetworkConfigurationResult"/> indicating the outcome of the configuration attempt.
        /// </returns>
        NetworkConfigurationResult ConfigureNetwork(string networkCardName, int interfaceNumber, string targetInterfaceName);

        /// <summary>
        /// Gets the configured target network interface.
        /// </summary>
        /// <returns>
        /// The configured <see cref="IConfiguration"/> object, or <c>null</c> if no configuration has been applied.
        /// </returns>
        IConfiguration GetTargetConfiguration();
    }
}