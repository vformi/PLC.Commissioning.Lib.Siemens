using Siemens.Engineering;
using Siemens.Engineering.Connection;
using Siemens.Engineering.Download;
using Siemens.Engineering.Online;
using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Serilog;
using PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions;
using PLC.Commissioning.Lib.Siemens.PLCProject.Enums;

namespace PLC.Commissioning.Lib.Siemens.PLCProject
{
    /// <summary>
    /// Provides services to configure and manage network settings for Siemens PLC projects.
    /// </summary>
    public class NetworkConfigurationService : INetworkConfigurationService
    {
        /// <summary>
        /// The online provider for retrieving connection configurations.
        /// </summary>
        private readonly OnlineProvider _onlineProvider;

        /// <summary>
        /// The download provider for configuring network connections.
        /// </summary>
        private readonly DownloadProvider _downloadProvider;

        /// <summary>
        /// Represents the target configuration settings for PLC operations.
        /// </summary>
        private IConfiguration _targetConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkConfigurationService"/> class.
        /// </summary>
        /// <param name="onlineProvider">The online provider for retrieving connection configurations.</param>
        /// <param name="downloadProvider">The download provider for configuring network connections.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="onlineProvider"/> or <paramref name="downloadProvider"/> is <c>null</c>.
        /// </exception>
        public NetworkConfigurationService(OnlineProvider onlineProvider, DownloadProvider downloadProvider)
        {
            _onlineProvider = onlineProvider ?? throw new ArgumentNullException(nameof(onlineProvider), "OnlineProvider cannot be null");
            _downloadProvider = downloadProvider ?? throw new ArgumentNullException(nameof(downloadProvider), "DownloadProvider cannot be null");
        }

        /// <inheritdoc />
        public void DisplayPLCConnectionPossibilities()
        {
            Log.Debug("\nIterating through available connection possibilities");
            ConnectionConfiguration configuration = _onlineProvider.Configuration;
            foreach (ConfigurationMode mode in configuration.Modes)
            {
                if (mode.Name == "PN/IE")
                {
                    foreach (ConfigurationPcInterface pcInterface in mode.PcInterfaces)
                    {
                        Log.Debug($"Mode name: {mode.Name}");
                        Log.Debug($"  PcInterface name: {pcInterface.Name}");
                        Log.Debug($"  PcInterface number: {pcInterface.Number}");

                        foreach (ConfigurationTargetInterface targetInterface in pcInterface.TargetInterfaces)
                        {
                            Log.Debug($"    TargetInterface: {targetInterface.Name}");
                        }

                        MatchNetworkInterface(pcInterface.Name);
                    }
                }
            }
        }

        /// <inheritdoc />
        public NetworkConfigurationResult ConfigureNetwork(string networkCardName, int interfaceNumber, string targetInterfaceName)
        {
            try
            {
                ConnectionConfiguration configuration = _downloadProvider.Configuration;
                ConfigurationMode mode = configuration.Modes.Find("PN/IE");
                if (mode is null)
                {
                    return NetworkConfigurationResult.ModeNotFound;
                }

                ConfigurationPcInterface pcInterface = mode.PcInterfaces.Find(networkCardName, interfaceNumber);
                if (pcInterface is null)
                {
                    return NetworkConfigurationResult.PcInterfaceNotFound;
                }

                ConfigurationTargetInterface targetInterface = pcInterface.TargetInterfaces.Find(targetInterfaceName);
                if (targetInterface is null)
                {
                    return NetworkConfigurationResult.TargetInterfaceNotFound;
                }

                configuration.ApplyConfiguration(targetInterface);
                _targetConfiguration = targetInterface;
                return NetworkConfigurationResult.Success;
            }
            catch (Exception ex)
            {
                Log.Error($"An unexpected error occurred during network configuration: {ex.Message}");
                return NetworkConfigurationResult.UnexpectedError;
            }
        }

        /// <inheritdoc />
        public IConfiguration GetTargetConfiguration()
        {
            return _targetConfiguration;
        }

        /// <summary>
        /// Matches the network interface with the specified PC interface name and logs the details.
        /// </summary>
        /// <param name="pcInterfaceName">The name of the PC interface to match.</param>
        public static void MatchNetworkInterface(string pcInterfaceName)
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.Description == pcInterfaceName)
                {
                    Log.Debug($"  Interface Type: {nic.NetworkInterfaceType}");
                    Log.Debug($"  Operational Status: {nic.OperationalStatus}");
                    Log.Debug($"  Speed: {nic.Speed / 1_000_000} Mbps");

                    IPInterfaceProperties ipProperties = nic.GetIPProperties();
                    foreach (UnicastIPAddressInformation unicastAddress in ipProperties.UnicastAddresses)
                    {
                        if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            Log.Debug($"  IP Address: {unicastAddress.Address}");
                            Log.Debug($"  Subnet Mask: {unicastAddress.IPv4Mask}");
                        }
                    }

                    foreach (GatewayIPAddressInformation gateway in ipProperties.GatewayAddresses)
                    {
                        if (gateway.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            Log.Debug($"  Gateway Address: {gateway.Address}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Pings the specified IP address using the specified network interface.
        /// </summary>
        /// <param name="networkInterfaceName">The name of the network interface to use for the ping.</param>
        /// <param name="ipAddress">The IP address to ping.</param>
        /// <returns><c>true</c> if the ping is successful; otherwise, <c>false</c>.</returns>
        public bool PingIpAddress(string networkInterfaceName, string ipAddress)
        {
            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.Description == networkInterfaceName)
                    {
                        var pingOptions = new PingOptions
                        {
                            DontFragment = true
                        };

                        byte[] buffer = new byte[32];
                        int timeout = 1000;

                        // Send the ping using the specific network interface
                        using (var ping = new Ping())
                        {
                            var reply = ping.Send(ipAddress, timeout, buffer, pingOptions);

                            if (reply.Status == IPStatus.Success)
                            {
                                Log.Information($"Ping successful - PLC ({ipAddress}) is reachable through {networkInterfaceName}.");
                                return true;
                            }
                            else
                            {
                                Log.Error($"Ping failed - PLC ({ipAddress}) is not reachable through {networkInterfaceName}. Status: {reply.Status}");
                                Log.Information("Below is information about the specified network card:");
                                MatchNetworkInterface(networkInterfaceName);
                                return false;
                            }
                        }
                    }
                }

                Log.Error($"Network interface '{networkInterfaceName}' not found.");
                Log.Information("Is your network card correctly configured? Here are the available connection possibilities:\n");
                DisplayPLCConnectionPossibilities();
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred while pinging IP address '{ipAddress}' using network interface '{networkInterfaceName}': {ex.Message}");
                return false;
            }
        }
    }
}
