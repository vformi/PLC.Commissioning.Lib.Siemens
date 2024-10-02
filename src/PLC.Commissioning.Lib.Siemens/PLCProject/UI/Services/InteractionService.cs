using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.UI.Services
{
    /// <summary>
    /// Provides interaction services for handling UI elements like ComboBox and CheckBox within the Siemens PLC project.
    /// </summary>
    public static class InteractionService
    {
        /// <summary>
        /// Delay in milliseconds between actions to allow the UI to update.
        /// </summary>
        private const int DelayBetweenActions = 100;

        /// <summary>
        /// Handles the interaction with a ComboBox by trying to set its value to one of the expected values.
        /// </summary>
        /// <param name="comboBox">The <see cref="AutomationElement"/> representing the ComboBox to interact with.</param>
        /// <param name="inputChars">Optional list of characters to input into the ComboBox. Defaults to predefined characters if null.</param>
        /// <param name="expectedValues">Optional list of expected values to validate the ComboBox against. Defaults to predefined values if null.</param>
        /// <returns>Returns <c>true</c> if the ComboBox was successfully set to one of the expected values; otherwise, <c>false</c>.</returns>
        public static bool HandleComboBox(AutomationElement comboBox, List<char> inputChars = null, List<string> expectedValues = null)
        {
            try
            {
                // Use default values if parameters are null
                if (inputChars is null)
                {
                    inputChars = new List<char> { 'A', 'C', 'S', 'D' };
                }

                if (expectedValues is null)
                {
                    expectedValues = new List<string>
                    {
                        "Accept all",
                        "Consistent Download",
                        "Stop all",
                        "Download to device",
                        "Start module"
                    };
                }

                // Input all characters
                InputComboBoxCharacters(comboBox, inputChars);

                // Check if the current value matches any of the expected values
                if (CheckComboBoxValue(comboBox, expectedValues))
                {
                    Log.Information("Successfully set ComboBox '{Name}' to one of the expected values.", comboBox.Name);
                    return true;
                }

                Log.Warning("Failed to set any value for ComboBox: '{Name}'.", comboBox.Name);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception in HandleComboBox.");
                return false;
            }
        }

        /// <summary>
        /// Inputs a series of characters into the ComboBox to attempt to set it to an expected value.
        /// </summary>
        /// <param name="comboBox">The <see cref="AutomationElement"/> representing the ComboBox to interact with.</param>
        /// <param name="inputChars">The list of characters to input into the ComboBox.</param>
        private static void InputComboBoxCharacters(AutomationElement comboBox, List<char> inputChars)
        {
            try
            {
                // Focus on the ComboBox and input each character
                comboBox.DoubleClick();
                Thread.Sleep(DelayBetweenActions);

                foreach (var inputChar in inputChars)
                {
                    Keyboard.Type(inputChar);
                    Thread.Sleep(DelayBetweenActions);
                }
                // Press Enter to confirm the selection
                Keyboard.Press(VirtualKeyShort.ENTER);
                Thread.Sleep(DelayBetweenActions); // Allow time for the UI to update
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception in InputComboBoxCharacters.");
            }
        }

        /// <summary>
        /// Checks whether the current value of the ComboBox matches any of the expected values.
        /// </summary>
        /// <param name="comboBox">The <see cref="AutomationElement"/> representing the ComboBox to check.</param>
        /// <param name="expectedValues">The list of expected values to validate against.</param>
        /// <returns>Returns <c>true</c> if the current value matches one of the expected values; otherwise, <c>false</c>.</returns>
        private static bool CheckComboBoxValue(AutomationElement comboBox, List<string> expectedValues)
        {
            try
            {
                // Get the current value of the ComboBox
                var currentValue = comboBox.Patterns.Value.Pattern.Value.Value;

                // Check if the current value matches any of the expected values
                if (expectedValues.Any(expectedValue =>
                        string.Equals(currentValue, expectedValue, StringComparison.OrdinalIgnoreCase)))
                {
                    Log.Information("ComboBox '{Name}' value matches expected value: '{Value}'.", comboBox.Name, currentValue);
                    return true;
                }
                else
                {
                    Log.Debug("ComboBox '{Name}' value '{CurrentValue}' does not match any expected values.", comboBox.Name, currentValue);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception in CheckComboBoxValue.");
                return false;
            }
        }

        /// <summary>
        /// Handles interaction with a CheckBox by clicking on it.
        /// </summary>
        /// <param name="checkBox">The <see cref="AutomationElement"/> representing the CheckBox to interact with.</param>
        /// <returns>Returns <c>true</c> if the CheckBox was successfully clicked; otherwise, <c>false</c>.</returns>
        public static bool HandleCheckBox(AutomationElement checkBox)
        {
            if (checkBox is null)
            {
                Log.Error("CheckBox is null.");
                return false;
            }

            var boundingRect = checkBox.BoundingRectangle;

            if (boundingRect.IsEmpty)
            {
                Log.Error("Failed to retrieve bounding rectangle for CheckBox.");
                return false;
            }

            var centerPoint = boundingRect.Center();
            Mouse.MoveTo(centerPoint);
            Thread.Sleep(DelayBetweenActions); // Small delay to ensure the mouse is in position

            Mouse.LeftClick();

            Log.Information($"Clicked CheckBox at location: {centerPoint}");
            return true;
        }
    }
}
