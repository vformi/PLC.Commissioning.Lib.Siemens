using PLC.Commissioning.Lib.Siemens.PLCProject.Results;
using Serilog;
using Siemens.Engineering.Connection;
using Siemens.Engineering.Download;
using Siemens.Engineering.Download.Configurations;
using System;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Software.PLC
{
    /// <summary>
    /// Provides services to perform various operations on a PLC, including starting, stopping, and downloading configurations.
    /// </summary>
    public class PLCOperationsService
    {
        private readonly DownloadProvider _downloadProvider;
        private IConfiguration _targetConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="PLCOperationsService"/> class.
        /// </summary>
        /// <param name="downloadProvider">The <see cref="DownloadProvider"/> instance used to perform download operations on the PLC.</param>
        public PLCOperationsService(DownloadProvider downloadProvider)
        {
            _downloadProvider = downloadProvider;
        }

        /// <summary>
        /// Stops the PLC by using specific download delegates.
        /// Logs the result of the operation and returns a boolean indicating success or failure.
        /// </summary>
        /// <returns><c>true</c> if the PLC was stopped successfully; otherwise, <c>false</c>.</returns>
        public bool StopPLC()
        {
            try
            {
                Log.Debug($"Attempting to switch PLC to STOP mode");
                DownloadConfigurationDelegate preDownloadDelegate = PreConfigureDownload;
                DownloadConfigurationDelegate postDownloadDelegate = PostConfigureStop;
                DownloadResult result = _downloadProvider.Download(_targetConfiguration, preDownloadDelegate, postDownloadDelegate, DownloadOptions.Hardware);

                // Use OperationLogger to log the download results
                var logger = new OperationLogger(new DownloadResultWrapper(result));
                logger.LogResults("StopPLC operation");

                return result.State == DownloadResultState.Success; 
            }
            catch (Exception ex)
            {
                Log.Error($"StopPLC operation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Starts the PLC by using specific download delegates.
        /// Logs the result of the operation and returns a boolean indicating success or failure.
        /// </summary>
        /// <returns><c>true</c> if the PLC was started successfully; otherwise, <c>false</c>.</returns>
        public bool StartPLC()
        {
            try
            {
                Log.Debug($"Attempting to switch PLC to RUN mode");
                DownloadConfigurationDelegate preDownloadDelegate = PreConfigureDownload;
                DownloadConfigurationDelegate postDownloadDelegate = PostConfigureDownload;
                DownloadResult result = _downloadProvider.Download(_targetConfiguration, preDownloadDelegate, postDownloadDelegate, DownloadOptions.Hardware);

                // Use OperationLogger to log the download results
                var logger = new OperationLogger(new DownloadResultWrapper(result));
                logger.LogResults("StartPLC operation");

                return result.State == DownloadResultState.Success; 
            }
            catch (Exception ex)
            {
                Log.Error($"StartPLC operation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Downloads the specified configuration options to the PLC.
        /// Logs the result of the operation and returns a boolean indicating success or failure.
        /// </summary>
        /// <param name="options">The <see cref="DownloadOptions"/> to be used during the download process.</param>
        /// <returns><c>true</c> if the download was successful; otherwise, <c>false</c>.</returns>
        public bool DownloadToPLC(DownloadOptions options)
        {
            try
            {
                DownloadConfigurationDelegate preDownloadDelegate = PreConfigureDownload;
                DownloadConfigurationDelegate postDownloadDelegate = PostConfigureDownload;
                DownloadResult result = _downloadProvider.Download(_targetConfiguration, preDownloadDelegate, postDownloadDelegate, options);

                // Use OperationLogger to log the download results
                var logger = new OperationLogger(new DownloadResultWrapper(result));
                logger.LogResults("DownloadToPLC operation");

                // Return true if the download was successful, false otherwise
                return result.State == DownloadResultState.Success; // Adjust based on actual success property
            }
            catch (Exception ex)
            {
                Log.Error($"DownloadToPLC operation failed: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// Sets the target configuration that will be applied for the download process to the PLC.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfiguration"/> to be set as the target configuration.</param>
        public void SetTargetConfiguration(IConfiguration configuration)
        {
            _targetConfiguration = configuration;
        }

        /// <summary>
        /// Configures the pre download process to configure all the necessary stuff.
        /// </summary>
        /// <param name="downloadConfiguration">The <see cref="DownloadConfiguration"/> to be pre-configured.</param>
        private void PreConfigureDownload(DownloadConfiguration downloadConfiguration)
        {
            if (downloadConfiguration is StopModules stopModules)
            {
                stopModules.CurrentSelection = StopModulesSelections.StopAll;
                return;
            }
            if (downloadConfiguration is DifferentTargetConfiguration diffTgt)
            {
                diffTgt.CurrentSelection = DifferentTargetConfigurationSelections.AcceptAll;
                return;
            }
            if (downloadConfiguration is UpgradeTargetDevice upgradeTargetDevice)
            {
                upgradeTargetDevice.Checked = true;
                return;
            }
            if (downloadConfiguration is DowngradeTargetDevice downgradeTargetDevice)
            {
                downgradeTargetDevice.Checked = true;
                return;
            }
            if (downloadConfiguration is AlarmTextLibrariesDownload alarmTextLibraries)
            {
                alarmTextLibraries.CurrentSelection = AlarmTextLibrariesDownloadSelections.ConsistentDownload;
                return;
            }
            if (downloadConfiguration is CheckBeforeDownload checkBeforeDownload)
            {
                checkBeforeDownload.Checked = true;
                return;
            }
            if (downloadConfiguration is ConsistentBlocksDownload consistentBlocksDownload)
            {
                consistentBlocksDownload.CurrentSelection = ConsistentBlocksDownloadSelections.ConsistentDownload;
                return;
            }
            if (downloadConfiguration is OverwriteSystemData overwriteSystemData)
            {
                overwriteSystemData.CurrentSelection = OverwriteSystemDataSelections.Overwrite;
                return;
            }
            if (downloadConfiguration is DataBlockReinitialization dataBlockReinit)
            {
                dataBlockReinit.CurrentSelection = DataBlockReinitializationSelections.StopPlcAndReinitialize;
                return;
            }
        }

        /// <summary>
        /// Configures the post download process to stop the PLC.
        /// </summary>
        /// <param name="downloadConfiguration">The <see cref="DownloadConfiguration"/> to be post-configured for stopping the PLC.</param>
        private void PostConfigureStop(DownloadConfiguration downloadConfiguration)
        {
            if (downloadConfiguration is StartModules startModules)
            {
                startModules.CurrentSelection = StartModulesSelections.NoAction;
            }
        }

        /// <summary>
        /// Configures the post download process after to start the PLC after a normal download. 
        /// </summary>
        /// <param name="downloadConfiguration">The <see cref="DownloadConfiguration"/> to be post-configured for starting the PLC.</param>
        private void PostConfigureDownload(DownloadConfiguration downloadConfiguration)
        {
            if (downloadConfiguration is StartModules startModules)
            {
                startModules.CurrentSelection = StartModulesSelections.StartModule;
            }
        }
    }
}
