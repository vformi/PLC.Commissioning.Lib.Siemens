using System;
using System.IO;
using Serilog;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Tags;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Software.PLC
{
    /// <summary>
    /// Provides services for importing and exporting PLC tag tables to and from TIA Portal projects.
    /// </summary>
    public class PLCTagsService
    {
        private readonly SoftwareContainerService _softwareContainerService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PLCTagsService"/> class.
        /// </summary>
        public PLCTagsService()
        {
            _softwareContainerService = new SoftwareContainerService();
        }

        /// <summary>
        /// Imports the PLC tag table from an XML file to the specified DeviceItem.
        /// </summary>
        /// <param name="deviceItem">The <see cref="DeviceItem"/> to import the tag table to.</param>
        /// <param name="xmlFilePath">The path to the XML file containing the PLC tags.</param>
        /// <exception cref="FileNotFoundException">Thrown if the specified XML file does not exist.</exception>
        public void ImportTagTable(DeviceItem deviceItem, string xmlFilePath)
        {
            if (!File.Exists(xmlFilePath))
            {
                throw new FileNotFoundException("The specified XML file does not exist.", xmlFilePath);
            }

            var plcSoftware = _softwareContainerService.GetPlcSoftware(deviceItem);
            plcSoftware.TagTableGroup.TagTables.Import(new FileInfo(xmlFilePath), ImportOptions.Override);
            Log.Information("PLC tags imported successfully.");
        }

        /// <summary>
        /// Exports all PLC tag tables from the specified DeviceItem to XML files.
        /// </summary>
        /// <param name="deviceItem">The <see cref="DeviceItem"/> to export the tag tables from.</param>
        /// <param name="exportDirectory">The directory where the XML files will be saved.</param>
        /// <exception cref="DirectoryNotFoundException">Thrown if the specified export directory does not exist.</exception>
        public void ExportAllTagTables(DeviceItem deviceItem, string exportDirectory)
        {
            if (!Directory.Exists(exportDirectory))
            {
                throw new DirectoryNotFoundException("The specified export directory does not exist.");
            }

            var plcSoftware = _softwareContainerService.GetPlcSoftware(deviceItem);

            PlcTagTableSystemGroup plcTagTableSystemGroup = plcSoftware.TagTableGroup;

            // Export all tables in the system group
            ExportTagTables(plcTagTableSystemGroup.TagTables, exportDirectory);

            // Export the tables in underlying user groups
            foreach (PlcTagTableUserGroup userGroup in plcTagTableSystemGroup.Groups)
            {
                ExportUserGroupDeep(userGroup, exportDirectory);
            }

            Log.Information("All PLC tag tables exported successfully.");
        }

        /// <summary>
        /// Exports the specified tag tables to the given directory.
        /// </summary>
        /// <param name="tagTables">The <see cref="PlcTagTableComposition"/> containing the tag tables to export.</param>
        /// <param name="exportDirectory">The directory where the XML files will be saved.</param>
        private void ExportTagTables(PlcTagTableComposition tagTables, string exportDirectory)
        {
            foreach (PlcTagTable table in tagTables)
            {
                string filePath = Path.Combine(exportDirectory, $"{table.Name}.xml");

                // Delete the file if it already exists to prevent export errors
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                table.Export(new FileInfo(filePath), ExportOptions.WithDefaults);
                Log.Information($"Tag table '{table.Name}' exported to '{filePath}'.");
            }
        }

        /// <summary>
        /// Recursively exports the tag tables from the specified user group and its sub-groups.
        /// </summary>
        /// <param name="group">The <see cref="PlcTagTableUserGroup"/> to export the tag tables from.</param>
        /// <param name="exportDirectory">The directory where the XML files will be saved.</param>
        private void ExportUserGroupDeep(PlcTagTableUserGroup group, string exportDirectory)
        {
            ExportTagTables(group.TagTables, exportDirectory);
            foreach (PlcTagTableUserGroup userGroup in group.Groups)
            {
                ExportUserGroupDeep(userGroup, exportDirectory);
            }
        }
    }
}
