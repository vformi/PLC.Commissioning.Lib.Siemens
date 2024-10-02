using Siemens.Engineering.Connection;
using Siemens.Engineering.HW;
using Siemens.Engineering.Online;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Software.Abstractions
{
    /// <summary>
    /// Defines the operations available for controlling and configuring a Siemens PLC.
    /// </summary>
    public interface IController
    {
        /// <summary>
        /// Displays the available PLC connection possibilities.
        /// </summary>
        void DisplayPLCConnectionPossibilities();

        /// <summary>
        /// Configures the network settings for the PLC using the specified network card and interface.
        /// </summary>
        /// <param name="networkCardName">The name of the network card to use.</param>
        /// <param name="interfaceNumber">The interface number to use.</param>
        /// <param name="targetInterfaceName">The name of the target interface.</param>
        /// /// <returns><c>true</c> if the configuration succeeds; otherwise, <c>false</c>.</returns>
        bool TryConfigureNetwork(string networkCardName, int interfaceNumber, string targetInterfaceName);

        /// <summary>
        /// Compiles the project associated with the specified CPU device item.
        /// </summary>
        /// <param name="cpu">The CPU <see cref="DeviceItem"/> to be compiled.</param>
        /// <returns><c>true</c> if the compilation was successful; otherwise, <c>false</c>.</returns>
        bool CompileDevice(DeviceItem cpu);

        /// <summary>
        /// Gets the current online state of the PLC device.
        /// </summary>
        /// <returns>The current <see cref="OnlineState"/> of the PLC device.</returns>
        OnlineState GetOnlineState();

        /// <summary>
        /// Stops the PLC.
        /// </summary>
        /// /// <returns><c>true</c> if the PLC was started successfully; otherwise, <c>false</c>.</returns>
        bool Stop();

        /// <summary>
        /// Starts the PLC
        /// </summary>
        /// <returns><c>true</c> if the PLC was started successfully; otherwise, <c>false</c>.</returns>
        bool Start();

        /// <summary>
        /// Switches the PLC to online mode.
        /// </summary>
        void GoOnline();

        /// <summary>
        /// Switches the PLC to offline mode.
        /// </summary>
        void GoOffline();

        /// <summary>
        /// Sets the target configuration for PLC operations.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfiguration"/> to set as the target configuration.</param>
        void SetTargetConfiguration(IConfiguration configuration);
    }
}
