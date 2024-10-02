using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using Serilog;
using System;
using System.Runtime.InteropServices;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.UI.Services
{
    /// <summary>
    /// Provides services for automating UI interactions with elements in the PLC Commissioning project.
    /// </summary>
    public static class AutomationService
    {
        /// <summary>
        /// Finds a UI element that matches the specified control type, name, and automation ID.
        /// </summary>
        /// <param name="parent">The parent <see cref="AutomationElement"/> under which to search for the element.</param>
        /// <param name="result">When this method returns, contains the found <see cref="AutomationElement"/>, or null if no element was found.</param>
        /// <param name="controlType">The type of control to search for.</param>
        /// <param name="name">The name of the element to search for (optional).</param>
        /// <param name="automationId">The automation ID of the element to search for (optional).</param>
        /// <returns>Returns <c>true</c> if the element was found; otherwise, <c>false</c>.</returns>
        /// <exception cref="Exception">Logs and handles any exceptions that occur during the search.</exception>
        public static bool FindElement(AutomationElement parent, out AutomationElement result, ControlType controlType, string name = null, string automationId = null)
        {
            result = null;
            try
            {
                var conditionFactory = parent.Automation.ConditionFactory;
                ConditionBase condition = conditionFactory.ByControlType(controlType);

                if (!string.IsNullOrEmpty(name))
                    condition = condition.And(conditionFactory.ByName(name));

                if (!string.IsNullOrEmpty(automationId))
                    condition = condition.And(conditionFactory.ByAutomationId(automationId));

                result = parent.FindFirstDescendant(condition);

                if (result is null)
                {
                    Log.Debug("Element not found. ControlType: '{ControlType}', Name: '{Name}', AutomationId: '{AutomationId}'.", controlType, name, automationId);
                    return false;
                }

                Log.Debug("Element found. ControlType: '{ControlType}', Name: '{Name}', AutomationId: '{AutomationId}'.", controlType, name, automationId);
                return true;
            }
            catch (COMException) // Specific handling for timeout
            {
                Log.Warning("Operation timed out while searching for element. ControlType: '{ControlType}', Name: '{Name}', AutomationId: '{AutomationId}'.", controlType, name, automationId);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception in FindElement. ControlType: '{ControlType}', Name: '{Name}', AutomationId: '{AutomationId}'.", controlType, name, automationId);
                return false;
            }
        }


        /// <summary>
        /// Finds a UI element based on its index among the children of the specified parent element.
        /// </summary>
        /// <param name="parent">The parent <see cref="AutomationElement"/> under which to search for the child element.</param>
        /// <param name="childIndex">The zero-based index of the child element to find.</param>
        /// <param name="result">When this method returns, contains the found <see cref="AutomationElement"/>, or null if no element was found at the specified index.</param>
        /// <returns>Returns <c>true</c> if the element was found at the specified index; otherwise, <c>false</c>.</returns>
        /// <exception cref="Exception">Logs and handles any exceptions that occur during the search.</exception>
        public static bool FindElementByChildIndex(AutomationElement parent, int childIndex, out AutomationElement result)
        {
            result = null;
            try
            {
                var children = parent.FindAllChildren();
                if (childIndex >= 0 && childIndex < children.Length)
                {
                    result = children[childIndex];
                    Log.Debug("Child element found at index {ChildIndex}. ControlType: '{ControlType}', Name: '{Name}'.", childIndex, result.ControlType, result.Name);
                    return true;
                }
                Log.Debug("Child element at index {ChildIndex} not found.", childIndex);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception in FindElementByChildIndex.");
                return false;
            }
        }
    }
}
