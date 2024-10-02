using FlaUI.UIA3;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using System;
using Serilog;
using PLC.Commissioning.Lib.Siemens.PLCProject.UI.Services;
using System.Threading;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.UI
{
    /// <summary>
    /// Handles the UI automation tasks for downloading and managing PLC operations in TIA Portal.
    /// </summary>
    public class UIDownloadHandler : IDisposable
    {
        /// <summary>
        /// Represents the main application window for UI interactions.
        /// </summary>
        private Window _mainWindow = null;

        /// <summary>
        /// Provides automation capabilities for UI interactions using UIA3 (UI Automation version 3).
        /// </summary>
        private readonly UIA3Automation _automation;

        /// <summary>
        /// Indicates whether the object has been disposed.
        /// </summary>
        private bool _disposed = false;

        #region Constants

        /// <summary>
        /// The interval in milliseconds between checks in the WaitUntil method.
        /// </summary>
        private const int PollingIntervalMilliseconds = 100;

        /// <summary>
        /// The maximum time to wait for the Download button to be enabled.
        /// </summary>
        private static readonly TimeSpan DownloadButtonEnabledTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Delay after clicking the Download button to allow the UI to update.
        /// </summary>
        private static readonly TimeSpan AfterDownloadButtonClickDelay = TimeSpan.FromSeconds(5);

        /// <summary>
        /// The maximum time to handle the Trust dialog.
        /// </summary>
        private static readonly TimeSpan TrustDialogHandlingTimeout = TimeSpan.FromSeconds(20);

        /// <summary>
        /// The maximum time to handle the Synchronization dialog.
        /// </summary>
        private static readonly TimeSpan SynchronizationDialogHandlingTimeout = TimeSpan.FromSeconds(20);

        /// <summary>
        /// The time to wait for the Load Preview when synchronization is handled.
        /// </summary>
        private static readonly TimeSpan LoadPreviewWaitTimeoutWhenSyncHandled = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The time to wait for the Load Preview when synchronization is not handled.
        /// </summary>
        private static readonly TimeSpan LoadPreviewWaitTimeoutWhenSyncNotHandled = TimeSpan.FromSeconds(20);

        /// <summary>
        /// The time to wait for the Load Postview dialog.
        /// </summary>
        private static readonly TimeSpan LoadPostviewWaitTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The maximum total time to wait for dialogs in the loop.
        /// </summary>
        private static readonly TimeSpan MaxWaitTimeForDialogs = TimeSpan.FromSeconds(60);

        /// <summary>
        /// The wait time after opening diagnostics before clicking the Run or Stop button.
        /// </summary>
        private static readonly TimeSpan DiagnosticsOpenWaitTime = TimeSpan.FromSeconds(5);

        /// <summary>
        /// A short sleep duration in milliseconds to avoid tight looping.
        /// </summary>
        private const int ShortSleepMilliseconds = 500;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="UIDownloadHandler"/> class and ensures the project view is active.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the main window cannot be initialized and project view cannot be activated.</exception>
        public UIDownloadHandler()
        {
            _automation = new UIA3Automation();

            if (!EnsureProjectViewIsActive())
            {
                throw new InvalidOperationException("Failed to ensure project view is active.");
            }
        }

        /// <summary>
        /// Ensures that the TIA Portal is in the project view. If not, initializes the main window.
        /// </summary>
        /// <returns><c>true</c> if the project view is active or successfully initialized; otherwise, <c>false</c>.</returns>
        private bool EnsureProjectViewIsActive()
        {
            if (_mainWindow is null)
            {
                if (HelperService.IsInProjectView(_automation, out _mainWindow))
                {
                    Log.Information("Already in project view.");
                    return true;
                }

                if (!HelperService.InitializeMainWindow(_automation, out _mainWindow))
                {
                    Log.Error("Failed to initialize main window.");
                    return false;
                }
            }

            Log.Information("Project view is active.");
            return true;
        }

        /// <summary>
        /// Performs the download procedure for the PLC, handling all necessary dialogs and steps.
        /// </summary>
        /// <returns><c>true</c> if the download procedure completed successfully; otherwise, <c>false</c>.</returns>
        public bool DownloadProcedure()
        {
            // Focus on the PLC. Return false if this step fails.
            if (!HelperService.FocusPLC(_mainWindow))
            {
                Log.Error("Failed to focus on PLC.");
                return false;
            }

            AutomationElement downloadButton = null;
            // Wait until the Download button is enabled, or timeout after a specified duration.
            if (!WaitUntil(() =>
            {
                var (isEnabled, button) = HelperService.IsDownloadButtonEnabled(_mainWindow);
                downloadButton = button;
                return isEnabled;
            }, DownloadButtonEnabledTimeout))
            {
                Log.Error("Download button was not enabled within the expected time.");
                return false;
            }

            // Click the Download button if it was successfully retrieved and enabled.
            if (downloadButton != null)
            {
                downloadButton.Click();
                Log.Information("Clicked the Download button successfully.");
            }
            else
            {
                Log.Error("Failed to click the Download button.");
                return false;
            }

            // Static wait after download button click to allow the UI to update.
            Thread.Sleep(AfterDownloadButtonClickDelay);

            bool trustHandled = false;
            bool syncHandled = false;

            // Wait for Trust dialog and Synchronization dialog simultaneously
            var maxWaitTime = MaxWaitTimeForDialogs;
            var startTime = DateTime.UtcNow;

            while ((DateTime.UtcNow - startTime) < maxWaitTime && (!trustHandled || !syncHandled))
            {
                if (!trustHandled && HelperService.IsTrustDialogVisible(_mainWindow))
                {
                    if (WaitUntil(() => HelperService.HandleTrustDialog(_mainWindow), TrustDialogHandlingTimeout))
                    {
                        Log.Information("Handled the trust dialog.");
                        trustHandled = true;
                    }
                    else
                    {
                        Log.Error("Failed to handle the trust dialog.");
                        return false;
                    }
                }

                if (!syncHandled && HelperService.IsSynchronizationDialogVisible(_mainWindow))
                {
                    if (WaitUntil(() => HelperService.HandleSynchronizationDialog(_mainWindow), SynchronizationDialogHandlingTimeout))
                    {
                        Log.Information("Handled the synchronization dialog.");
                        syncHandled = true;
                    }
                    else
                    {
                        Log.Error("Failed to handle the synchronization dialog.");
                        return false;
                    }
                }

                // Check if Load Preview is visible and skip the rest of the loop
                if (HelperService.IsLoadPreviewVisible(_mainWindow))
                {
                    Log.Information("Load Message dialog appeared, skipping iteration.");
                    break;
                }

                // Short sleep to avoid tight looping
                Thread.Sleep(ShortSleepMilliseconds);
            }

            if (!trustHandled)
            {
                Log.Information("Trust dialog did not appear.");
            }

            if (!syncHandled)
            {
                Log.Information("Synchronization dialog did not appear.");
            }

            // Determine the appropriate timeout based on whether synchronization was handled
            var loadPreviewWaitTimeout = syncHandled ? LoadPreviewWaitTimeoutWhenSyncHandled : LoadPreviewWaitTimeoutWhenSyncNotHandled;

            // Now check for the Load Preview dialog.
            if (WaitUntil(() => HelperService.IsLoadPreviewVisible(_mainWindow), loadPreviewWaitTimeout) &&
                HelperService.AcceptLoadPreview(_mainWindow))
            {
                Log.Information("Handled the load preview dialog.");
            }
            else
            {
                Log.Error("Failed to handle the load preview.");
                return false;
            }

            // Final check for Load Postview
            if (!WaitUntil(() => HelperService.IsLoadPostviewVisible(_mainWindow), LoadPostviewWaitTimeout) ||
                !HelperService.AcceptLoadPostview(_mainWindow))
            {
                Log.Error("Failed to handle the load postview.");
                return false;
            }

            // If all steps are successful, return true.
            Log.Information("Download procedure completed successfully.");
            return true;
        }

        /// <summary>
        /// Starts the PLC by going online, opening diagnostics, and clicking the Run button.
        /// </summary>
        /// <returns><c>true</c> if the PLC was started successfully; otherwise, <c>false</c>.</returns>
        public bool StartPLC()
        {
            // Focus on the PLC. Return false if this step fails.
            if (!HelperService.FocusPLC(_mainWindow))
            {
                Log.Error("Failed to focus on PLC.");
                return false;
            }

            // Open online diagnostics. Return false if this step fails.
            if (!HelperService.OpenOnlineDiagnostics(_mainWindow))
            {
                Log.Error("Failed to open online diagnostics.");
                return false;
            }

            // Wait after opening diagnostics to allow the UI to update.
            Thread.Sleep(DiagnosticsOpenWaitTime);

            // Click the Run button. Return false if this step fails.
            if (!HelperService.ClickRunButton(_mainWindow))
            {
                Log.Error("Failed to click Run button.");
                return false;
            }

            // If all steps are successful, return true.
            Log.Information("PLC started successfully through UI.");
            return true;
        }

        /// <summary>
        /// Stops the PLC by going online, opening diagnostics, and clicking the Stop button.
        /// </summary>
        /// <returns><c>true</c> if the PLC was stopped successfully; otherwise, <c>false</c>.</returns>
        public bool StopPLC()
        {
            // Focus on the PLC. Return false if this step fails.
            if (!HelperService.FocusPLC(_mainWindow))
            {
                Log.Error("Failed to focus on PLC.");
                return false;
            }

            // Open online diagnostics. Return false if this step fails.
            if (!HelperService.OpenOnlineDiagnostics(_mainWindow))
            {
                Log.Error("Failed to open online diagnostics.");
                return false;
            }

            // Wait after opening diagnostics to allow the UI to update.
            Thread.Sleep(DiagnosticsOpenWaitTime);

            // Click the Stop button. Return false if this step fails.
            if (!HelperService.ClickStopButton(_mainWindow))
            {
                Log.Error("Failed to click Stop button.");
                return false;
            }

            // If all steps are successful, return true.
            Log.Information("PLC stopped successfully.");
            return true;
        }

        /// <summary>
        /// Closes the TIA Portal by finding and clicking the close button on the title bar.
        /// </summary>
        /// <returns><c>true</c> if the TIA Portal was closed successfully; otherwise, <c>false</c>.</returns>
        public bool CloseTIA()
        {
            Log.Debug("Closing TIA Portal.");

            if (_mainWindow != null && AutomationService.FindElement(_mainWindow, out var titleBar, ControlType.TitleBar, automationId: "TitleBar"))
            {
                if (AutomationService.FindElement(titleBar, out var closeButton, ControlType.Button, name: "Close"))
                {
                    closeButton.Click();
                    Log.Information("TIA Portal closed successfully.");
                    return true;
                }
                else
                {
                    Log.Error("Failed to find the close button on the title bar.");
                    return false;
                }
            }
            else
            {
                Log.Error("Failed to find the title bar.");
                return false;
            }
        }

        /// <summary>
        /// Waits until a condition is met or a timeout occurs.
        /// </summary>
        /// <param name="condition">The condition to wait for.</param>
        /// <param name="timeout">The maximum time to wait for the condition.</param>
        /// <returns><c>true</c> if the condition was met within the timeout; otherwise, <c>false</c>.</returns>
        private bool WaitUntil(Func<bool> condition, TimeSpan timeout)
        {
            var endTime = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < endTime)
            {
                if (condition())
                {
                    return true;
                }
                Thread.Sleep(PollingIntervalMilliseconds); // Polling interval
            }
            return false;
        }

        /// <summary>
        /// Disposes of the resources used by the <see cref="UIDownloadHandler"/> class.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _automation?.Dispose();
                _disposed = true;
            }
        }
    }
}
