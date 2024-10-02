using Serilog;
using Siemens.Engineering.Online;
using System;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Software.PLC
{
    /// <summary>
    /// Provides services to manage the PLC Online state. 
    /// Mostly just for connection checking whether we can go online with a specified networkcard
    /// </summary>
    public class OnlineProviderService
    {
        private readonly OnlineProvider _onlineProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="OnlineProviderService"/> class.
        /// </summary>
        /// <param name="onlineProvider">The <see cref="OnlineProvider"/> instance used to control the online/offline state of the PLC device.</param>
        public OnlineProviderService(OnlineProvider onlineProvider)
        {
            _onlineProvider = onlineProvider;
        }

        /// <summary>
        /// Gets the current online state of the PLC device.
        /// </summary>
        /// <returns>The current <see cref="OnlineState"/> of the PLC device.</returns>
        public OnlineState GetOnlineState()
        {
            Log.Information("The device state is {0}", _onlineProvider.State);
            return _onlineProvider.State;
        }

        /// <summary>
        /// Switches the PLC device to online mode.
        /// </summary>
        public void GoOnline()
        {
            _onlineProvider.GoOnline();
            Log.Information("The device has been switched to online mode.");
        }

        /// <summary>
        /// Switches the PLC device to offline mode.
        /// </summary>
        public void GoOffline()
        {
            _onlineProvider.GoOffline();
            Log.Information("The device has been switched to offline mode.");
        }
    }
}
