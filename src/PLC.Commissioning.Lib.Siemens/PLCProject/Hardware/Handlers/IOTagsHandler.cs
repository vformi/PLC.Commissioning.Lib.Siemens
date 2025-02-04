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
        public List<string> ReadTagTables()
        {
            var items = new List<string>();

            if (_plcSoftware == null)
            {
                Log.Error("PLC Software instance is null. Cannot read tag tables.");
                return items;
            }

            Log.Information("Retrieving PLC tag tables and groups...");

            try
            {
                // Read tag tables at the root level.
                PlcTagTableComposition rootTagTables = _plcSoftware.TagTableGroup.TagTables;
                foreach (PlcTagTable tagTable in rootTagTables)
                {
                    items.Add($"TagTable: {tagTable.Name}");
                    Log.Debug($"Found Tag Table: {tagTable.Name}");
                }

                // Read user groups and their tag tables.
                PlcTagTableUserGroupComposition groups = _plcSoftware.TagTableGroup.Groups;
                foreach (PlcTagTableUserGroup group in groups)
                {
                    // Log and add the group name
                    items.Add($"Group: {group.Name}");
                    Log.Debug($"Found Tag Table Group: {group.Name}");

                    // Iterate through each tag table within the group.
                    foreach (PlcTagTable tagTable in group.TagTables)
                    {
                        items.Add($"TagTable in {group.Name}: {tagTable.Name}");
                        Log.Debug($"Found Tag Table in group {group.Name}: {tagTable.Name}");
                    }
                }

                return items;
            }
            catch (System.Exception ex)
            {
                Log.Error($"Failed to retrieve PLC Tag Tables and Groups: {ex.Message}");
                return items;
            }
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

        private void CreateTag(PlcTagTable tagTable, string tagName, string dataType, string address)
        {
            Log.Debug("Creating tag: {TagName} at {Address} with type {DataType}", tagName, address, dataType);
            tagTable.Tags.Create(tagName, dataType, address);
        }
    }
}
