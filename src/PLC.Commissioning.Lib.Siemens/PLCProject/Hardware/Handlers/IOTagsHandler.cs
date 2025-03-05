using Siemens.Engineering.SW.Tags;
using Siemens.Engineering.SW;
using Serilog;
using System.Collections.Generic;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Handlers
{
    /// <summary>
    /// Handles operations related to PLC Tag Tables.
    /// </summary>
    public class IOTagsHandler
    {
        /// <summary>
        /// The PLC software instance associated with this handler.
        /// </summary>
        private readonly PlcSoftware _plcSoftware;

        /// <summary>
        /// Initializes a new instance of the <see cref="IOTagsHandler"/> class.
        /// </summary>
        /// <param name="plcSoftware">The PLC software instance to operate on.</param>
        public IOTagsHandler(PlcSoftware plcSoftware)
        {
            _plcSoftware = plcSoftware ?? throw new System.ArgumentNullException(nameof(plcSoftware));
        }
        
        /// <summary>
        /// Reads and returns all PLC tag tables.
        /// </summary>
        /// <returns>A list of tag table names.</returns>
        public Dictionary<string, List<string>> ReadPLCTagTables()
        {
            var tagTableDictionary = new Dictionary<string, List<string>>();

            if (_plcSoftware == null)
            {
                Log.Error("PLC Software instance is null. Cannot read tag tables.");
                return tagTableDictionary;
            }

            Log.Information("Retrieving PLC tag tables and groups...");

            try
            {
                // Read tag tables at the root level (if any exist outside groups)
                PlcTagTableComposition rootTagTables = _plcSoftware.TagTableGroup.TagTables;
                if (rootTagTables.Count > 0)
                {
                    tagTableDictionary["Root"] = new List<string>();
                    foreach (PlcTagTable tagTable in rootTagTables)
                    {
                        tagTableDictionary["Root"].Add(tagTable.Name);
                        Log.Debug($"Found Tag Table: {tagTable.Name}");
                    }
                }

                // Read user groups and their tag tables
                PlcTagTableUserGroupComposition groups = _plcSoftware.TagTableGroup.Groups;
                foreach (PlcTagTableUserGroup group in groups)
                {
                    if (!tagTableDictionary.ContainsKey(group.Name))
                    {
                        tagTableDictionary[group.Name] = new List<string>();
                    }

                    // Iterate through each tag table within the group
                    foreach (PlcTagTable tagTable in group.TagTables)
                    {
                        tagTableDictionary[group.Name].Add(tagTable.Name);
                        Log.Debug($"Found Tag Table in group {group.Name}: {tagTable.Name}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"Failed to retrieve PLC Tag Tables and Groups: {ex.Message}");
            }

            return tagTableDictionary;
        }

        /// <summary>
        /// Creates tag tables and tags based on `ImportedDevice`.
        /// </summary>
        public void CreateTagTables(ImportedDevice device)
        {
            if (_plcSoftware == null)
            {
                Log.Error("PLC Software instance is null. Cannot create tag tables.");
                return;
            }

            Log.Information("Creating PLC tag tables for device: {DeviceName}", device.DeviceName);

            // Create a main group for the device
            PlcTagTableUserGroup deviceGroup = CreateTagTableGroup(device.DeviceName);

            var tagTableDefinitions = device.GetTagTableDefinitions();
            foreach (var tableDefinition in tagTableDefinitions)
            {
                // Create a tag table for each module
                PlcTagTable tagTable = CreateTagTable(deviceGroup, tableDefinition.TableName);

                // Create tags inside the tag table
                foreach (var tagDefinition in tableDefinition.Tags)
                {
                    CreateTag(tagTable, tagDefinition.Name, tagDefinition.DataType, tagDefinition.Address);
                }
            }
        }
        
        /// <summary>
        /// Deletes the tag table group (and all tag tables within) for the specified device.
        /// Assumes that the group was created using the device's unique DeviceName.
        /// </summary>
        /// <param name="device">The ImportedDevice whose tags are to be deleted.</param>
        public void DeleteDeviceTagTables(ImportedDevice device)
        {
            if (_plcSoftware == null)
            {
                Log.Error("PLC Software instance is null. Cannot delete tag tables.");
                return;
            }

            Log.Information("Deleting PLC tag tables for device: {DeviceName}", device.DeviceName);

            // Find the user group that was created with the device name.
            PlcTagTableUserGroup group = _plcSoftware.TagTableGroup.Groups.Find(device.DeviceName);
            if (group != null)
            {
                Log.Debug("Found tag table group '{DeviceName}'. Deleting group...", device.DeviceName);
                group.Delete();
            }
            else
            {
                Log.Warning("Tag table group for device '{DeviceName}' not found. Nothing to delete.", device.DeviceName);
            }
        }

        #region Private methods
        /// <summary>
        /// Creates a tag table user group in the PLC software, ensuring it does not already exist.
        /// </summary>
        /// <param name="groupName">The name of the group to create.</param>
        /// <returns>The created or existing <see cref="PlcTagTableUserGroup"/>.</returns>
        private PlcTagTableUserGroup CreateTagTableGroup(string groupName)
        {
            PlcTagTableSystemGroup systemGroup = _plcSoftware.TagTableGroup;
            PlcTagTableUserGroupComposition groupComposition = systemGroup.Groups;

            PlcTagTableUserGroup group = groupComposition.Find(groupName);
            if (group == null)
            {
                Log.Debug("Creating tag group: {GroupName}", groupName);
                group = groupComposition.Create(groupName);
            }
            else
            {
                Log.Debug("Tag group already exists: {GroupName}", groupName);
            }

            return group;
        }
        
        /// <summary>
        /// Creates a new PLC tag table inside the specified user group.
        /// </summary>
        /// <param name="parentGroup">The parent group where the tag table will be created.</param>
        /// <param name="tableName">The name of the tag table.</param>
        /// <returns>The created or existing <see cref="PlcTagTable"/>.</returns>
        private PlcTagTable CreateTagTable(PlcTagTableUserGroup parentGroup, string tableName)
        {
            PlcTagTable tagTable = parentGroup.TagTables.Find(tableName);
            if (tagTable == null)
            {
                Log.Debug("Creating tag table: {TableName}", tableName);
                tagTable = parentGroup.TagTables.Create(tableName);
            }
            else
            {
                Log.Debug("Tag table already exists: {TableName}", tableName);
            }

            return tagTable;
        }
        
        /// <summary>
        /// Creates a new tag inside the specified tag table.
        /// </summary>
        /// <param name="tagTable">The tag table where the tag will be created.</param>
        /// <param name="tagName">The name of the tag.</param>
        /// <param name="dataType">The data type of the tag.</param>
        /// <param name="address">The memory address associated with the tag.</param>
        private void CreateTag(PlcTagTable tagTable, string tagName, string dataType, string address)
        {
            Log.Debug("Creating tag: {TagName} at {Address} with type {DataType}", tagName, address, dataType);
            tagTable.Tags.Create(tagName, dataType, address);
        }
        #endregion
    }
}
