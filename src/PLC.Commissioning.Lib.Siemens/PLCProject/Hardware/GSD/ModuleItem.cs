using System;
using System.Text;
using System.Xml;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Abstractions;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models.IOTags;

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
        /// Gets the safety parameter record data item associated with the module item model.
        /// </summary>
        public FParameterRecordDataItem FParameterRecordDataItem => Model.FParameterRecordDataItem;

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
                string nameTextId = moduleInfoNode.SelectSingleNode("gsd:Name", _gsdHandler.nsmgr)?.Attributes["TextId"]
                    ?.Value;
                string infoTextId = moduleInfoNode.SelectSingleNode("gsd:InfoText", _gsdHandler.nsmgr)
                    ?.Attributes["TextId"]?.Value;
                Model.Name = _gsdHandler.GetExternalText(nameTextId);
                Model.InfoText = _gsdHandler.GetExternalText(infoTextId);
            }

            XmlNode parameterRecordDataItemNode = moduleItemNode.SelectSingleNode(
                "gsd:VirtualSubmoduleList/gsd:VirtualSubmoduleItem/gsd:RecordDataList/gsd:ParameterRecordDataItem",
                _gsdHandler.nsmgr);

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

            // Parse IO Data
            XmlNode ioDataNode = moduleItemNode.SelectSingleNode("gsd:VirtualSubmoduleList/gsd:VirtualSubmoduleItem/gsd:IOData", _gsdHandler.nsmgr);
            if (ioDataNode != null)
            {
                var ioData = new IOData();

                // Parse Outputs
                XmlNode outputNode = ioDataNode.SelectSingleNode("gsd:Output", _gsdHandler.nsmgr);
                if (outputNode != null)
                {
                    foreach (XmlNode dataItemNode in outputNode.SelectNodes("gsd:DataItem", _gsdHandler.nsmgr))
                    {
                        var dataItem = new DataItem
                        {
                            DataType = dataItemNode.Attributes["DataType"]?.Value,
                            UseAsBits = bool.TryParse(dataItemNode.Attributes["UseAsBits"]?.Value, out var useAsBits) &&
                                        useAsBits,
                            TextId = _gsdHandler.GetExternalText(dataItemNode.Attributes["TextId"]?.Value)
                        };

                        foreach (XmlNode bitDataItemNode in dataItemNode.SelectNodes("gsd:BitDataItem",
                                     _gsdHandler.nsmgr))
                        {
                            dataItem.BitDataItems.Add(new BitDataItem
                            {
                                BitOffset = int.TryParse(bitDataItemNode.Attributes["BitOffset"]?.Value,
                                    out var bitOffset)
                                    ? bitOffset
                                    : 0,
                                TextId = _gsdHandler.GetExternalText(bitDataItemNode.Attributes["TextId"]?.Value)
                            });
                        }

                        ioData.Outputs.Add(dataItem);
                    }
                }

                // Parse Inputs (if applicable)
                XmlNode inputNode = ioDataNode.SelectSingleNode("gsd:Input", _gsdHandler.nsmgr);
                if (inputNode != null)
                {
                    foreach (XmlNode dataItemNode in inputNode.SelectNodes("gsd:DataItem", _gsdHandler.nsmgr))
                    {
                        var dataItem = new DataItem
                        {
                            DataType = dataItemNode.Attributes["DataType"]?.Value,
                            UseAsBits = bool.TryParse(dataItemNode.Attributes["UseAsBits"]?.Value, out var useAsBits) &&
                                        useAsBits,
                            TextId = _gsdHandler.GetExternalText(dataItemNode.Attributes["TextId"]?.Value)
                        };

                        foreach (XmlNode bitDataItemNode in dataItemNode.SelectNodes("gsd:BitDataItem",
                                     _gsdHandler.nsmgr))
                        {
                            dataItem.BitDataItems.Add(new BitDataItem
                            {
                                BitOffset = int.TryParse(bitDataItemNode.Attributes["BitOffset"]?.Value,
                                    out var bitOffset)
                                    ? bitOffset
                                    : 0,
                                TextId = _gsdHandler.GetExternalText(bitDataItemNode.Attributes["TextId"]?.Value)
                            });
                        }

                        ioData.Inputs.Add(dataItem);
                    }
                }

                // Add IOData to Model only if it contains data
                if (ioData.Inputs.Count > 0 || ioData.Outputs.Count > 0)
                {
                    Model.IOData = ioData;
                }
            }
        }

        /// <summary>
        /// Returns a string representation of the module item, including its name, info text, IO data, and parameters.
        /// </summary>
        /// <returns>A string describing the module item.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"Name: {Model.Name}");
            sb.AppendLine($"ID: {Model.ID}");
            sb.AppendLine($"Info Text: {Model.InfoText}");

            // IO Data Section
            if (Model.IOData != null && (Model.IOData.Outputs.Count > 0 || Model.IOData.Inputs.Count > 0))
            {
                sb.AppendLine("IO Data:");

                // Outputs
                if (Model.IOData.Outputs.Count > 0)
                {
                    sb.AppendLine("  Outputs:");
                    foreach (var output in Model.IOData.Outputs)
                    {
                        sb.AppendLine($"    DataItem: {_gsdHandler.GetExternalText(output.TextId) ?? output.TextId}, " +
                                      $"DataType: {output.DataType}, " +
                                      $"Used as Bits: {output.UseAsBits}");

                        foreach (var bitDataItem in output.BitDataItems)
                        {
                            sb.AppendLine($"      BitOffset: {bitDataItem.BitOffset}, " +
                                          $"Text: {_gsdHandler.GetExternalText(bitDataItem.TextId) ?? bitDataItem.TextId}");
                        }
                    }
                }

                // Inputs
                if (Model.IOData.Inputs.Count > 0)
                {
                    sb.AppendLine("  Inputs:");
                    foreach (var input in Model.IOData.Inputs)
                    {
                        sb.AppendLine($"    DataItem: {_gsdHandler.GetExternalText(input.TextId) ?? input.TextId}, " +
                                      $"DataType: {input.DataType}, " +
                                      $"Used as Bits: {input.UseAsBits}");

                        foreach (var bitDataItem in input.BitDataItems)
                        {
                            sb.AppendLine($"      BitOffset: {bitDataItem.BitOffset}, " +
                                          $"Text: {_gsdHandler.GetExternalText(bitDataItem.TextId) ?? bitDataItem.TextId}");
                        }
                    }
                }
            }

            // Parameters Section
            if (Model.ParameterRecordDataItem != null)
            {
                sb.AppendLine("Parameters:");
                sb.AppendLine(Model.ParameterRecordDataItem.ToString());
            }
            else
            {
                sb.AppendLine("No parameters available.");
            }

            return sb.ToString();
        }
    }
}
