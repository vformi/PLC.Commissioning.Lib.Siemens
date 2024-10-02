using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.UI.Services
{
    /// <summary>
    /// Provides services for interacting with tree items in the UI, specifically for identifying, expanding, and collapsing expandable items.
    /// </summary>
    public static class TreeItemService
    {
        /// <summary>
        /// Retrieves all expandable tree items under the specified parent element.
        /// </summary>
        /// <param name="parent">The parent <see cref="AutomationElement"/> containing the tree items.</param>
        /// <param name="expandableItems">When this method returns, contains a list of expandable tree items, or an empty list if none were found.</param>
        /// <returns>Returns <c>true</c> if any expandable tree items were found; otherwise, <c>false</c>.</returns>
        public static bool GetExpandableTreeItems(AutomationElement parent, out List<AutomationElement> expandableItems)
        {
            expandableItems = new List<AutomationElement>();
            try
            {
                var children = parent.FindAllChildren();
                foreach (var child in children)
                {
                    if (child.ControlType == ControlType.TreeItem && IsExpandable(child))
                    {
                        expandableItems.Add(child);
                    }
                }

                if (expandableItems.Any())
                {
                    Log.Debug("{Count} expandable TreeItems found.", expandableItems.Count);
                    return true;
                }
                else
                {
                    Log.Debug("No expandable TreeItems found.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception in GetExpandableTreeItems.");
                return false;
            }
        }

        /// <summary>
        /// Determines whether a specified tree item is expandable (either expandable or collapsible).
        /// </summary>
        /// <param name="treeItem">The <see cref="AutomationElement"/> representing the tree item to check.</param>
        /// <returns>Returns <c>true</c> if the tree item is expandable or collapsible; otherwise, <c>false</c>.</returns>
        public static bool IsExpandable(AutomationElement treeItem)
        {
            if (treeItem is null)
            {
                Log.Error("TreeItem is null.");
                return false;
            }

            // Check if the TreeItem supports the InvokePattern
            if (treeItem.Patterns.Invoke.IsSupported)
            {
                var legacyPattern = treeItem.Patterns.LegacyIAccessible.Pattern;
                string defaultAction = legacyPattern.DefaultAction;

                // Log the DefaultAction for debugging
                Log.Debug($"TreeItem '{treeItem.Name}' DefaultAction: '{defaultAction}'");

                // If the DefaultAction is "Collapse", the TreeItem is currently expanded.
                // If the DefaultAction is "Expand", the TreeItem is currently collapsed.
                if (defaultAction.Equals("Collapse", StringComparison.OrdinalIgnoreCase) ||
                    defaultAction.Equals("Expand", StringComparison.OrdinalIgnoreCase))
                {
                    Log.Debug($"TreeItem '{treeItem.Name}' is expandable.");
                    return true;
                }
            }

            Log.Debug($"TreeItem '{treeItem.Name}' is not expandable.");
            return false;
        }

        /// <summary>
        /// Expands the specified tree item.
        /// </summary>
        /// <param name="treeItem">The <see cref="AutomationElement"/> representing the tree item to expand.</param>
        /// <returns>Returns <c>true</c> if the tree item was successfully expanded; otherwise, <c>false</c>.</returns>
        public static bool ExpandTreeItem(AutomationElement treeItem)
        {
            if (treeItem is null)
            {
                Log.Error("TreeItem is null.");
                return false;
            }

            try
            {
                if (treeItem.Patterns.Invoke.IsSupported)
                {
                    var invokePattern = treeItem.Patterns.Invoke.Pattern;
                    invokePattern.Invoke();
                    Log.Debug($"Expanded TreeItem: {treeItem.Name}");
                    return true;
                }
                else
                {
                    Log.Warning($"TreeItem '{treeItem.Name}' does not support ExpandCollapse or Invoke patterns.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error expanding TreeItem: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Collapses the specified tree item.
        /// </summary>
        /// <param name="treeItem">The <see cref="AutomationElement"/> representing the tree item to collapse.</param>
        /// <returns>Returns <c>true</c> if the tree item was successfully collapsed or if it was already collapsed; otherwise, <c>false</c>.</returns>
        public static bool CollapseTreeItem(AutomationElement treeItem)
        {
            if (treeItem is null)
            {
                Log.Error("TreeItem is null.");
                return false;
            }

            try
            {
                if (treeItem.Patterns.Invoke.IsSupported)
                {
                    var legacyPattern = treeItem.Patterns.LegacyIAccessible.Pattern;
                    string defaultAction = legacyPattern.DefaultAction;

                    // If the DefaultAction is "Collapse", it means the item is currently expanded.
                    if (defaultAction.Equals("Collapse", StringComparison.OrdinalIgnoreCase))
                    {
                        var invokePattern = treeItem.Patterns.Invoke.Pattern;
                        invokePattern.Invoke();
                        Log.Debug($"Collapsed TreeItem: {treeItem.Name}");
                        return true;
                    }
                    else
                    {
                        Log.Debug($"TreeItem '{treeItem.Name}' is already collapsed.");
                        return true;
                    }
                }
                else
                {
                    Log.Warning($"TreeItem '{treeItem.Name}' does not support Invoke patterns.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error collapsing TreeItem: {ex.Message}");
                return false;
            }
        }
    }
}