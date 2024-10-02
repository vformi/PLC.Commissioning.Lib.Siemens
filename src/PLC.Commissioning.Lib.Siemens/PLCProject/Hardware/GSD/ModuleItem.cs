using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Abstractions;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models;
using Serilog;
using System;
using System.Text;
using System.Xml;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD
{
    /// <summary>
    /// Represents a module item within a Siemens PLC project, handling the parsing of module data from GSD files.
    /// </summary>
    public class ModuleItem : IDeviceItem
    {
        /// <summary>
        /// The GSD handler used for processing module data.
        /// </summary>
        private readonly GSDHandler _gsdHandler;

        /// <summary>
        /// Gets the model containing the parsed module item information.
        /// </summary>
        public ModuleItemModel Model { get; private set; }

        /// <summary>
        /// Gets the parameter record data item associated with the module item model.
        /// </summary>
        public ParameterRecordDataItem ParameterRecordDataItem => Model.ParameterRecordDataItem;


        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleItem"/> class with the specified GSD handler.
        /// </summary>
        /// <param name="gsdHandler">The GSD handler used for processing module data.</param>
        public ModuleItem(GSDHandler gsdHandler)
        {
            _gsdHandler = gsdHandler ?? throw new ArgumentNullException(nameof(gsdHandler));
            Model = new ModuleItemModel();
        }

        /// <summary>
        /// Parses an XML node representing a module item and populates the corresponding model.
        /// </summary>
        /// <param name="moduleItemNode">The XML node containing the module item data.</param>
        public void ParseModuleItem(XmlNode moduleItemNode)
        {
            // Initialize Model properties
            Model.ID = moduleItemNode.Attributes["ID"]?.Value;
            Model.ModuleIdentNumber = moduleItemNode.Attributes["ModuleIdentNumber"]?.Value;

            XmlNode moduleInfoNode = moduleItemNode.SelectSingleNode("gsd:ModuleInfo", _gsdHandler.nsmgr);
            if (moduleInfoNode != null)
            {
                string nameTextId = moduleInfoNode.SelectSingleNode("gsd:Name", _gsdHandler.nsmgr)?.Attributes["TextId"]?.Value;
                string infoTextId = moduleInfoNode.SelectSingleNode("gsd:InfoText", _gsdHandler.nsmgr)?.Attributes["TextId"]?.Value;
                Model.Name = _gsdHandler.GetExternalText(nameTextId);
                Model.InfoText = _gsdHandler.GetExternalText(infoTextId);
            }

            XmlNode parameterRecordDataItemNode = moduleItemNode.SelectSingleNode("gsd:VirtualSubmoduleList/gsd:VirtualSubmoduleItem/gsd:RecordDataList/gsd:ParameterRecordDataItem", _gsdHandler.nsmgr);

            // Check if parameters exist
            if (parameterRecordDataItemNode != null)
            {
                Model.ParameterRecordDataItem = new ParameterRecordDataItem(_gsdHandler);
                Model.ParameterRecordDataItem.ParseParameterRecordDataItem(parameterRecordDataItemNode);
            }
            else
            {
                // Handle case where no parameters are available
                Model.ParameterRecordDataItem = null; 
            }
        }

        /// <summary>
        /// Returns a string representation of the module item, including its name, info text, and parameter record data.
        /// </summary>
        /// <returns>A string describing the module item.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Name: {Model.Name}");
            sb.AppendLine($"Info Text: {Model.InfoText}");

            if (Model.ParameterRecordDataItem != null)
            {
                sb.AppendLine(Model.ParameterRecordDataItem.ToString());
            }
            else
            {
                sb.AppendLine("Module has no parameters to be changed.");
            }

            return sb.ToString();
        }
    }
}
