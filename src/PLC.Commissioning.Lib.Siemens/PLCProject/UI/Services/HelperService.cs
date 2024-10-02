using FlaUI.UIA3;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Serilog;
using System.Linq;
using System.Threading;
using System;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.UI.Services
{
    /// <summary>
    /// Provides helper services for interacting with the Siemens PLC project UI.
    /// </summary>
    public static class HelperService
    {
        // Global variable to track the online state
        private static bool _isOnline = false;
        
        /// <summary>
        /// Initializes the main window by finding and interacting with the necessary UI elements.
        /// </summary>
        /// <param name="automation">The <see cref="UIA3Automation"/> instance used to interact with the UI.</param>
        /// <param name="mainWindow">When this method returns, contains the main <see cref="Window"/> if found, or null if not found.</param>
        /// <returns>Returns <c>true</c> if the main window was successfully initialized; otherwise, <c>false</c>.</returns>
        public static bool InitializeMainWindow(UIA3Automation automation, out Window mainWindow)
        {
            mainWindow = null;
            if (!AutomationService.FindElement(automation.GetDesktop(), out var mainWindowElement, ControlType.Window, name: "ADWorkbench"))
                return false;

            mainWindow = mainWindowElement.AsWindow();
            if (mainWindow is null)
            {
                Log.Error("Failed to cast main window element to Window.");
                return false;
            }

            if (!AutomationService.FindElement(mainWindow, out var portalFrame, ControlType.Pane, name: "PortalAndWorkFrame"))
                return false;

            if (!AutomationService.FindElement(portalFrame, out var contextNavigatorView, ControlType.Pane, name: "Siemens.Automation.FrameApplication.ContextNavigatorView"))
                return false;

            if (!AutomationService.FindElement(contextNavigatorView, out var contextNavigator, ControlType.Pane, name: "ContextNavigator"))
                return false;

            if (!AutomationService.FindElement(contextNavigator, out var goToProjectView, ControlType.ListItem, name: "ContextNavigator.GoToProjectView.Title"))
                return false;

            goToProjectView.Click();
            Log.Debug("Clicked 'GoToProjectView.Title' successfully.");

            return true;
        }

        /// <summary>
        /// Checks if the application is currently in the Project View.
        /// </summary>
        /// <param name="automation">The <see cref="UIA3Automation"/> instance used to interact with the UI.</param>
        /// <param name="mainWindow">When this method returns, contains the main <see cref="Window"/> if found, or null if not found.</param>
        /// <returns>Returns <c>true</c> if the application is in Project View; otherwise, <c>false</c>.</returns>
        public static bool IsInProjectView(UIA3Automation automation, out Window mainWindow)
        {
            mainWindow = null;

            if (!AutomationService.FindElement(automation.GetDesktop(), out var mainWindowElement, ControlType.Window, name: "ADWorkbench"))
                return false;

            mainWindow = mainWindowElement.AsWindow();
            if (mainWindow is null)
            {
                Log.Error("Failed to cast main window element to Window.");
                return false;
            }

            if (!AutomationService.FindElement(mainWindow, out var statusBarView, ControlType.Pane, name: "Siemens.Automation.FrameApplication.StatusBarView"))
                return false;

            if (!AutomationService.FindElement(statusBarView, out var statusBar, ControlType.StatusBar, name: "StatusBar"))
                return false;

            if (!AutomationService.FindElement(statusBar, out var viewSwitcher, ControlType.Button, name: "ViewSwitcher"))
                return false;

            var legacyPattern = viewSwitcher.Patterns.LegacyIAccessible.PatternOrDefault;
            if (legacyPattern is null)
            {
                Log.Error("LegacyIAccessiblePattern not available for ViewSwitcher.");
                return false;
            }

            var description = legacyPattern.Description;

            if (description == "Text=Portal view")
            {
                Log.Debug("In Project View.");
                return true;
            }

            Log.Debug("Not in Project View.");
            return false;
        }

        /// <summary>
        /// Focuses on the PLC item in the navigation tree within the main window.
        /// </summary>
        /// <param name="mainWindow">The main <see cref="Window"/> where the PLC item should be found.</param>
        /// <returns>Returns <c>true</c> if the PLC item was successfully focused; otherwise, <c>false</c>.</returns>
        public static bool FocusPLC(Window mainWindow)
        {
            // Step 1: Find the required elements in the UI hierarchy
            if (!AutomationService.FindElement(mainWindow, out var appNavigationContainer, ControlType.Pane, name: "Siemens.Automation.FrameApplication.ApplicationNavigationContainer"))
                return false;

            if (!AutomationService.FindElement(appNavigationContainer, out var appNavigationFrame, ControlType.Pane, name: "Siemens.Automation.FrameApplication.ApplicationNavigationFrame"))
                return false;

            if (!AutomationService.FindElement(appNavigationFrame, out var hardwareNavigationFrame, ControlType.Pane, name: "Siemens.Automation.FrameApplication.HardwareNavigationFrame"))
                return false;

            if (!AutomationService.FindElement(hardwareNavigationFrame, out var navigationTreeHostFrame, ControlType.Pane, name: "Siemens.Automation.FrameApplication.NavigationTreeHostFrame"))
                return false;

            if (!AutomationService.FindElement(navigationTreeHostFrame, out var projectNavigatorViewFrame, ControlType.Pane, name: "Siemens.Automation.FrameApplication.ProjectNavigatorViewFrame"))
                return false;

            if (!AutomationService.FindElement(projectNavigatorViewFrame, out var deviceTreePane, ControlType.Pane, name: "ViewID_Siemens.Automation.FrameApplication.Navigation.Device.Tree"))
                return false;

            if (!AutomationService.FindElement(deviceTreePane, out var projectNavigationTree, ControlType.Table, name: "ProjectNavigationTree"))
                return false;

            var editItems = deviceTreePane.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit));
            var plcItem = editItems[3]; // fourth item in the table

            // Step 2: Find the PLC item in the tree
            if (plcItem is null)
            {
                Log.Debug($"PLC not found.");
                return false;
            }

            // Step 3: Use LegacyIAccessiblePattern to check if the PLC item is already selected (expanded)
            var legacyPattern = plcItem.Patterns.LegacyIAccessible.PatternOrDefault;
            if (legacyPattern is null)
            {
                Log.Error($"LegacyIAccessiblePattern not available for the PLC item.");
                return false;
            }

            // Check the state to determine if the item is already expanded
            var state = legacyPattern.State.ToString();

            // If the item is already selected (expanded), no further action is needed
            if (state.Contains("STATE_SYSTEM_EXPANDED"))
            {
                // plcItem.Focus();
                Log.Information($"PLC item is already expanded. No action needed.");
                return true;
            }

            // Step 4: Expand the PLC item if it is not already expanded
            try
            {
                var invokePattern = plcItem.Patterns.Invoke.PatternOrDefault;
                if (invokePattern != null)
                {
                    invokePattern.Invoke();
                    Log.Information($"Successfully invoked and expanded the PLC item");
                }
                else
                {
                    Log.Error($"InvokePattern not available for the PLC item");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to interact with the PLC item. Error: {ex.Message}");
                return false;
            }
        }

        public static (bool, AutomationElement) IsDownloadButtonEnabled(Window mainWindow)
        {
            if (!AutomationService.FindElement(mainWindow, out var applicationHeaderPane, ControlType.Pane, name: "ApplicationHeader"))
                return (false, null);

            if (!AutomationService.FindElement(applicationHeaderPane, out var mainToolbar, ControlType.ToolBar, name: "MainToolbar"))
                return (false, null);

            if (!AutomationService.FindElement(mainToolbar, out var downloadButton, ControlType.Button, name: "Application.Download_ICO_PE_gTbLoadToTargetSystem"))
                return (false, null);

            var legacyPattern = downloadButton.Patterns.LegacyIAccessible.PatternOrDefault;
            var state = legacyPattern.State.ToString();

            if (state.Contains("STATE_SYSTEM_FOCUSABLE"))
            {
                return (true, downloadButton);
            }
            return (false, null);
        }

        /// <summary>
        /// Clicks the "Go Online" button in the main toolbar of the application.
        /// </summary>
        /// <param name="mainWindow">The main <see cref="Window"/> where the Go Online button should be found.</param>
        /// <returns>Returns <c>true</c> if the Go Online button was successfully clicked; otherwise, <c>false</c>.</returns>
        public static bool ClickGoOnlineButton(Window mainWindow)
        {
            if (_isOnline)
            {
                Log.Information("Already online, no need to click GoOnline button again.");
                return true;
            }

            if (!AutomationService.FindElement(mainWindow, out var applicationHeaderPane, ControlType.Pane, name: "ApplicationHeader"))
                return false;

            if (!AutomationService.FindElement(applicationHeaderPane, out var mainToolbar, ControlType.ToolBar, name: "MainToolbar"))
                return false;

            if (!AutomationService.FindElement(mainToolbar, out var goOnlineButton, ControlType.Button, name: "Application.GoOnline_ICO_PE_gTbGoOnline"))
                return false;

            goOnlineButton.Click();
            Log.Information("Clicked GoOnline button successfully.");
            _isOnline = true;

            return true;
        }

        /// <summary>
        /// Clicks the "Go Offline" button in the main toolbar of the application.
        /// </summary>
        /// <param name="mainWindow">The main <see cref="Window"/> where the Go Offline button should be found.</param>
        /// <returns>Returns <c>true</c> if the Go Offline button was successfully clicked; otherwise, <c>false</c>.</returns>
        public static bool ClickGoOfflineButton(Window mainWindow)
        {
            if (!AutomationService.FindElement(mainWindow, out var applicationHeaderPane, ControlType.Pane, name: "ApplicationHeader"))
                return false;

            if (!AutomationService.FindElement(applicationHeaderPane, out var mainToolbar, ControlType.ToolBar, name: "MainToolbar"))
                return false;

            if (!AutomationService.FindElement(mainToolbar, out var goOfflineButton, ControlType.Button, name: "Application.GoOffline_ICO_PE_gTbGoOffline"))
                return false;

            goOfflineButton.Click();
            Log.Information("Clicked GoOffline button successfully.");
            _isOnline = false;

            return true;
        }

        /// <summary>
        /// Opens the "Online & Diagnostics" view in the navigation tree of the application.
        /// </summary>
        /// <param name="mainWindow">The main <see cref="Window"/> where the navigation tree should be found.</param>
        /// <returns>Returns <c>true</c> if the "Online & Diagnostics" view was successfully opened; otherwise, <c>false</c>.</returns>
        public static bool OpenOnlineDiagnostics(Window mainWindow)
        {
            if (!AutomationService.FindElement(mainWindow, out var appNavigationContainer, ControlType.Pane, name: "Siemens.Automation.FrameApplication.ApplicationNavigationContainer"))
                return false;

            if (!AutomationService.FindElement(appNavigationContainer, out var appNavigationFrame, ControlType.Pane, name: "Siemens.Automation.FrameApplication.ApplicationNavigationFrame"))
                return false;

            if (!AutomationService.FindElement(appNavigationFrame, out var hardwareNavigationFrame, ControlType.Pane, name: "Siemens.Automation.FrameApplication.HardwareNavigationFrame"))
                return false;

            if (!AutomationService.FindElement(hardwareNavigationFrame, out var navigationTreeHostFrame, ControlType.Pane, name: "Siemens.Automation.FrameApplication.NavigationTreeHostFrame"))
                return false;

            if (!AutomationService.FindElement(navigationTreeHostFrame, out var projectNavigatorViewFrame, ControlType.Pane, name: "Siemens.Automation.FrameApplication.ProjectNavigatorViewFrame"))
                return false;

            if (!AutomationService.FindElement(projectNavigatorViewFrame, out var deviceTreePane, ControlType.Pane, name: "ViewID_Siemens.Automation.FrameApplication.Navigation.Device.Tree"))
                return false;

            if (!AutomationService.FindElement(deviceTreePane, out var projectNavigationTree, ControlType.Table, name: "ProjectNavigationTree"))
                return false;

            var onlineDiag = deviceTreePane.FindFirstDescendant(cf => cf
                .ByValue($"IndentLevel=3;Text=Online & diagnostics")
                .And(cf.ByControlType(ControlType.Edit)));
            if (onlineDiag == null)
            {
                Log.Debug($"Online & diagnostics not found");
                return false;
            }

            onlineDiag.DoubleClick();
            Log.Information($"Successfully double clicked on Online & diagnostics.");

            return true;
        }

        /// <summary>
        /// Clicks the "Run" button in the online diagnostics view.
        /// </summary>
        /// <param name="mainWindow">The main <see cref="Window"/> where the Run button should be found.</param>
        /// <returns>Returns <c>true</c> if the Run button was successfully clicked; otherwise, <c>false</c>.</returns>

        public static bool ClickRunButton(Window mainWindow)
        {
            return ClickStateButton(mainWindow, "RUN", "Successfully clicked the RUN button.");
        }

        /// <summary>
        /// Clicks the "Stop" button in the online diagnostics view.
        /// </summary>
        /// <param name="mainWindow">The main <see cref="Window"/> where the Stop button should be found.</param>
        /// <returns>Returns <c>true</c> if the Stop button was successfully clicked; otherwise, <c>false</c>.</returns>
        public static bool ClickStopButton(Window mainWindow)
        {
            return ClickStateButton(mainWindow, "STOP", "Successfully clicked the STOP button.");
        }

        /// <summary>
        /// Clicks a state button (RUN/STOP) in the online diagnostics view.
        /// </summary>
        /// <param name="mainWindow">The main <see cref="Window"/> where the state button should be found.</param>
        /// <param name="buttonName">The name of the button to be clicked (e.g., "RUN" or "STOP").</param>
        /// <param name="successMessage">The success message to log if the button is clicked successfully.</param>
        /// <returns>Returns <c>true</c> if the state button was successfully clicked; otherwise, <c>false</c>.</returns>
        private static bool ClickStateButton(Window mainWindow, string buttonName, string successMessage)
        {
            // Locate the 'runStopControlPane' within the specified hierarchy
            if (!AutomationService.FindElement(mainWindow, out var onlineFrame, ControlType.Pane, name: "Siemens.Automation.FrameApplication.TaskCardContainerFrame", automationId: "HierarchyFrameControl"))
                return false;

            if (!AutomationService.FindElement(onlineFrame, out var onlineTaskcard, ControlType.Pane, name: "TaskCardFrame"))
                return false;

            if (!AutomationService.FindElement(onlineTaskcard, out var hwDiagnosticsGroup, ControlType.Group, name: "HwcnDiagnostic.OnlineCPU"))
                return false;

            if (!AutomationService.FindElement(hwDiagnosticsGroup, out var hwDiagnosticsPane, ControlType.Pane, name: "HwcnDiagnostic.OnlineCPU"))
                return false;

            if (!AutomationService.FindElement(hwDiagnosticsPane, out var opModePalletePane, ControlType.Pane, name: "accOpModePalette"))
                return false;

            if (!AutomationService.FindElement(opModePalletePane, out var palettePanelPane, ControlType.Pane, name: "m_PalettePanel"))
                return false;

            if (!AutomationService.FindElement(palettePanelPane, out var opcModePalleteUserControlPane, ControlType.Pane, name: "accOpModePaletteUserControl"))
                return false;

            if (!AutomationService.FindElement(opcModePalleteUserControlPane, out var runStopControlPane, ControlType.Pane, name: "runStopControl"))
                return false;

            if (!AutomationService.FindElement(runStopControlPane, out var button, ControlType.Button, name: buttonName))
                return false;

            // Use LegacyIAccessiblePattern to check the state of the button
            var legacyPattern = button.Patterns.LegacyIAccessible.PatternOrDefault;
            if (legacyPattern is null)
            {
                Log.Error($"LegacyIAccessiblePattern not available for {buttonName} button.");
                return false;
            }

            // Get the state
            var state = legacyPattern.State.ToString();

            // Check if the button is already pressed
            if (state.Contains("STATE_SYSTEM_PRESSED"))
            {
                Log.Information($"{buttonName} button is already pressed. No action needed.");
                return true;
            }

            // If not pressed, click the button
            button.Click();
            Log.Information(successMessage);
            HandleInteractionPopUp(mainWindow);

            return true;
        }

        /// <summary>
        /// Handles interaction with the synchronization dialog that appears during certain operations.
        /// </summary>
        /// <param name="mainWindow">The main <see cref="Window"/> where the synchronization dialog should be found.</param>
        /// <returns>Returns <c>true</c> if the synchronization dialog was successfully handled; otherwise, <c>false</c>.</returns>
        public static bool HandleSynchronizationDialog(Window mainWindow)
        {
            // TODO: If the first one is not able to be found, skip the rest and put a diag message 
            if (!AutomationService.FindElement(mainWindow, out var synchronizationDialog, ControlType.Window, automationId: "SynchronizationDialog"))
                return false;

            if (!AutomationService.FindElement(synchronizationDialog, out var groupBox, ControlType.Pane, automationId: "m_GroupBox1"))
                return false;

            if (!AutomationService.FindElement(groupBox, out var doNotSynchronizeButton, ControlType.Button, automationId: "m_ButtonDoNotSynchronize"))
                return false;

            doNotSynchronizeButton.Click();
            Log.Information("Clicked 'Continue without synchronization' button.");

            return true;
        }

        /// <summary>
        /// Checks if the Synchronization Dialog is currently visible.
        /// </summary>
        /// <param name="mainWindow">The main <see cref="Window"/> where the Synchronization Dialog might be present.</param>
        /// <returns>Returns <c>true</c> if the Synchronization Dialog is visible; otherwise, <c>false</c>.</returns>
        public static bool IsSynchronizationDialogVisible(Window mainWindow)
        {
            // Attempt to find the Synchronization Dialog window.
            return AutomationService.FindElement(mainWindow, out var synchronizationDialog, ControlType.Window, automationId: "SynchronizationDialog");
        }


        /// <summary>
        /// Accepts the load preview by interacting with the necessary UI elements.
        /// </summary>
        /// <param name="mainWindow">The main <see cref="Window"/> where the load preview should be accepted.</param>
        /// <returns>Returns <c>true</c> if the load preview was successfully accepted; otherwise, <c>false</c>.</returns>
        public static bool AcceptLoadPreview(Window mainWindow)
        {
            if (!AutomationService.FindElement(mainWindow, out var loadMessageDialog, ControlType.Window, automationId: "LoadMessageDialog"))
                return false;

            if (!AutomationService.FindElement(loadMessageDialog, out var gridControlTable, ControlType.Table, automationId: "Grid-Control"))
                return false;

            if (TreeItemService.GetExpandableTreeItems(gridControlTable, out var expandableItems))
            {
                if (expandableItems.Count >= 2)
                {
                    if (!TreeItemService.CollapseTreeItem(expandableItems[1]))
                        return false;

                    Thread.Sleep(100);

                    if (!TreeItemService.GetExpandableTreeItems(gridControlTable, out expandableItems))
                        return false;

                    if (!TreeItemService.ExpandTreeItem(expandableItems.Last()))
                        return false;
                }
            }

            var interactiveElements = gridControlTable.FindAllDescendants().Where(e => e.ControlType == ControlType.ComboBox || e.ControlType == ControlType.CheckBox).ToList();

            for (int i = 0; i < interactiveElements.Count && i < 5; i++) // go only till 5 - not to go for the unavailable checkboxes 
            {
                var element = interactiveElements[i];
                bool success;

                switch (element.ControlType)
                {
                    case ControlType.ComboBox:
                        success = InteractionService.HandleComboBox(element);
                        break;
                    case ControlType.CheckBox:
                        success = InteractionService.HandleCheckBox(element);
                        break;
                    default:
                        success = true;
                        break;
                }

                if (!success)
                    return false;
            }

            if (!AutomationService.FindElement(loadMessageDialog, out var buttonGroup, ControlType.Pane, automationId: "m_GroupBox1"))
                return false;

            if (!AutomationService.FindElement(buttonGroup, out var loadButton, ControlType.Button, name: "LOAD.Download"))
                return false;

            loadButton.Click();
            Log.Information("Clicked 'LOAD.Download' button.");

            return true;
        }

        /// <summary>
        /// Accepts the load postview by interacting with the necessary UI elements.
        /// </summary>
        /// <param name="mainWindow">The main <see cref="Window"/> where the load postview should be accepted.</param>
        /// <returns>Returns <c>true</c> if the load postview was successfully accepted; otherwise, <c>false</c>.</returns>
        public static bool AcceptLoadPostview(Window mainWindow)
        {
            if (!AutomationService.FindElement(mainWindow, out var loadMessageDialog, ControlType.Window, automationId: "LoadMessageDialog"))
                return false;

            if (!AutomationService.FindElement(loadMessageDialog, out var gridControlTable, ControlType.Table, automationId: "Grid-Control"))
                return false;

            var interactiveElements = gridControlTable.FindAllDescendants().Where(e => e.ControlType == ControlType.ComboBox || e.ControlType == ControlType.CheckBox).ToList();

            foreach (var element in interactiveElements)
            {
                bool success;
                switch (element.ControlType)
                {
                    case ControlType.ComboBox:
                        success = InteractionService.HandleComboBox(element);
                        break;
                    case ControlType.CheckBox:
                        success = InteractionService.HandleCheckBox(element);
                        break;
                    default:
                        success = true;
                        break;
                }

                if (!success)
                    return false;
            }

            if (!AutomationService.FindElement(loadMessageDialog, out var buttonGroup, ControlType.Pane, automationId: "m_GroupBox1"))
                return false;

            if (!AutomationService.FindElement(buttonGroup, out var finishButton, ControlType.Button, name: "LOAD.Finish"))
                return false;

            finishButton.Click();
            Log.Information("Successfully clicked Load button.");
            return true;
        }

        /// <summary>
        /// Checks if the Load Preview Dialog is currently visible.
        /// </summary>
        /// <param name="mainWindow">The main <see cref="Window"/> where the Load Preview Dialog might be present.</param>
        /// <returns>Returns <c>true</c> if the Load Preview Dialog is visible; otherwise, <c>false</c>.</returns>
        public static bool IsLoadPreviewVisible(Window mainWindow)
        {
            return AutomationService.FindElement(mainWindow, out var loadPreviewDialog, ControlType.Window, automationId: "LoadMessageDialog");
        }

        /// <summary>
        /// Checks if the Load Postview Dialog is currently visible.
        /// </summary>
        /// <param name="mainWindow">The main <see cref="Window"/> where the Load Postview Dialog might be present.</param>
        /// <returns>Returns <c>true</c> if the Load Postview Dialog is visible; otherwise, <c>false</c>.</returns>
        public static bool IsLoadPostviewVisible(Window mainWindow)
        {
            return AutomationService.FindElement(mainWindow, out var loadPostviewDialog, ControlType.Window, automationId: "LoadMessageDialog");
        }

        /// <summary>
        /// Handles any interaction pop-up dialogs that appear during operations.
        /// </summary>
        /// <param name="mainWindow">The main <see cref="Window"/> where the interaction pop-up should be handled.</param>
        /// <returns>Returns <c>true</c> if the interaction pop-up was successfully handled; otherwise, <c>false</c>.</returns>
        public static bool HandleInteractionPopUp(Window mainWindow)
        {
            if (!AutomationService.FindElement(mainWindow, out var popUp, ControlType.Window, name: "PEMessageBox"))
                return false;

            if (!AutomationService.FindElement(popUp, out var okButton, ControlType.Button, name: "OkButton"))
                return false;

            okButton.Click();
            Log.Information("Successfully clicked the 'OK' button.");

            return true;
        }

        /// <summary>
        /// Handles certificates pop-up window
        /// </summary>
        /// <param name="mainWindow">The main <see cref="Window"/> where the interaction pop-up should be handled.</param>
        /// <returns>Returns <c>true</c> if the interaction pop-up was successfully handled; otherwise, <c>false</c>.</returns>
        public static bool HandleTrustDialog(Window mainWindow)
        {
            if (!AutomationService.FindElement(mainWindow, out var popUp, ControlType.Window, automationId: "DialogFrameControl"))
                return false;

            if (!AutomationService.FindElement(popUp, out var trustButton, ControlType.Button, name: "ButtonTrustCaption"))
                return false;

            trustButton.Click();
            Log.Information("Successfully clicked the 'Consider as trusted and make connection' button.");

            return true;
        }

        /// <summary>
        /// Checks if the Trust Dialog is currently visible.
        /// </summary>
        /// <param name="mainWindow">The main <see cref="Window"/> where the Trust Dialog might be present.</param>
        /// <returns>Returns <c>true</c> if the Trust Dialog is visible; otherwise, <c>false</c>.</returns>
        public static bool IsTrustDialogVisible(Window mainWindow)
        {
            // Attempt to find the Trust Dialog pop-up window.
            return AutomationService.FindElement(mainWindow, out var popUp, ControlType.Window, automationId: "DialogFrameControl");
        }
    }
}
