using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD
{
    /// <summary>
    /// Represents a parameter record data item (it is a list of parameters) from the GSD, including parsing its details and associated references.
    /// </summary>
    public class ParameterRecordDataItem
    {
        /// <summary>
        /// Gets the DS number for the parameter record.
        /// </summary>
        public int DsNumber { get; set; }

        /// <summary>
        /// Gets the length of the parameter record in bytes.
        /// </summary>
        public int LengthInBytes { get; set; }

        /// <summary>
        /// Gets or sets the list of (<Ref/>) parameters associated with the parameter record data item.
        /// </summary>
        public List<RefModel> Refs { get; set; } = new List<RefModel>();

        private readonly GSDHandler _gsdHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterRecordDataItem"/> class.
        /// </summary>
        /// <param name="gsdHandler">The GSD handler used to parse the data.</param>
        public ParameterRecordDataItem(GSDHandler gsdHandler)
        {
            _gsdHandler = gsdHandler ?? throw new ArgumentNullException(nameof(gsdHandler));
        }

        /// <summary>
        /// Parses the parameter record data item from the provided XML node.
        /// </summary>
        /// <param name="parameterRecordDataItemNode">The XML node representing the parameter record data item.</param>
        public void ParseParameterRecordDataItem(XmlNode parameterRecordDataItemNode)
        {
            if (parameterRecordDataItemNode == null)
            {
                throw new ArgumentNullException(nameof(parameterRecordDataItemNode), "ParameterRecordDataItem node cannot be null.");
            }

            // Mandatory Index and Length attributes
            if (!int.TryParse(parameterRecordDataItemNode.Attributes["Index"]?.Value, out int dsNumber))
            {
                throw new InvalidOperationException("Missing or invalid 'Index' attribute in ParameterRecordDataItem.");
            }
            DsNumber = dsNumber;

            if (!int.TryParse(parameterRecordDataItemNode.Attributes["Length"]?.Value, out int lengthInBytes))
            {
                throw new InvalidOperationException("Missing or invalid 'Length' attribute in ParameterRecordDataItem.");
            }
            LengthInBytes = lengthInBytes;

            // Ensure <Name> exists and has a valid TextId
            XmlNode nameNode = parameterRecordDataItemNode.SelectSingleNode("gsd:Name", _gsdHandler.nsmgr);
            if (nameNode == null || nameNode.Attributes["TextId"] == null)
            {
                throw new InvalidOperationException("Missing required <Name> node or 'TextId' attribute in ParameterRecordDataItem.");
            }

            // Validate <Ref> nodes (they must exist)
            XmlNodeList refNodes = parameterRecordDataItemNode.SelectNodes("gsd:Ref", _gsdHandler.nsmgr);
            if (refNodes.Count == 0)
            {
                throw new InvalidOperationException("A ParameterRecordDataItem must contain at least one <Ref> node.");
            }

            var textTracker = new Dictionary<string, int>();

            foreach (XmlNode refNode in refNodes)
            {
                // Mandatory attributes for <Ref>
                if (refNode.Attributes["ByteOffset"] == null)
                {
                    throw new InvalidOperationException("Each <Ref> node must have a 'ByteOffset' attribute.");
                }
                if (refNode.Attributes["DataType"] == null)
                {
                    throw new InvalidOperationException("Each <Ref> node must have a 'DataType' attribute.");
                }
                if (refNode.Attributes["TextId"] == null)
                {
                    throw new InvalidOperationException("Each <Ref> node must have a 'TextId' attribute.");
                }

                int byteOffset = int.Parse(refNode.Attributes["ByteOffset"].Value);

                var refItem = new RefModel
                {
                    ValueItemTarget = refNode.Attributes["ValueItemTarget"]?.Value,
                    DataType = refNode.Attributes["DataType"].Value,  // Mandatory
                    ByteOffset = byteOffset,
                    BitOffset = refNode.Attributes["BitOffset"] != null ? int.Parse(refNode.Attributes["BitOffset"].Value) : (int?)null,
                    BitLength = refNode.Attributes["BitLength"] != null ? int.Parse(refNode.Attributes["BitLength"].Value) : (int?)null,
                    Length = refNode.Attributes["Length"] != null ? int.Parse(refNode.Attributes["Length"].Value) : (int?)null,
                    DefaultValue = refNode.Attributes["DefaultValue"]?.Value,
                    AllowedValues = refNode.Attributes["AllowedValues"]?.Value,
                    TextId = refNode.Attributes["TextId"].Value, // Mandatory
                    Text = _gsdHandler.GetExternalText(refNode.Attributes["TextId"].Value) // Must resolve
                };

                // Ensure Text is unique
                if (!string.IsNullOrEmpty(refItem.Text))
                {
                    if (textTracker.ContainsKey(refItem.Text))
                    {
                        textTracker[refItem.Text]++;
                        refItem.Text += $"_{textTracker[refItem.Text]}";
                    }
                    else
                    {
                        textTracker[refItem.Text] = 0;
                    }
                }

                if (!string.IsNullOrEmpty(refItem.ValueItemTarget))
                {
                    var valueItem = _gsdHandler.GetValueItem(refItem.ValueItemTarget);
                    if (valueItem != null)
                    {
                        foreach (var assign in valueItem.Assignments)
                        {
                            refItem.AllowedValueAssignments[assign.Content] = assign.Text;
                        }
                    }
                }

                Refs.Add(refItem);
            }
        }



        /// <summary>
        /// To string method of class ParameterRecordDataItem
        /// </summary>
        /// <returns>A string that represents the parameter record data item.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"DsNumber: {DsNumber}, LengthInBytes: {LengthInBytes}");
            foreach (var refItem in Refs)
            {
                sb.Append("  Ref -");

                if (!string.IsNullOrEmpty(refItem.Text)) sb.Append($" Parameter: {refItem.Text}");
                if (!string.IsNullOrEmpty(refItem.DataType)) sb.Append($", DataType: {refItem.DataType}");
                sb.Append($", ByteOffset: {refItem.ByteOffset}");

                if (refItem.BitOffset.HasValue) sb.Append($", BitOffset: {refItem.BitOffset}");
                if (refItem.BitLength.HasValue) sb.Append($", BitLength: {refItem.BitLength}");
                if (refItem.Length.HasValue) sb.Append($", Length: {refItem.Length}");
                if (!string.IsNullOrEmpty(refItem.DefaultValue)) sb.Append($", DefaultValue: {refItem.DefaultValue}");

                if (!string.IsNullOrEmpty(refItem.AllowedValues))
                {
                    sb.Append($", AllowedValues: {refItem.AllowedValues}");
                    if (refItem.AllowedValueAssignments.Count > 0)
                    {
                        sb.Append(" (");
                        foreach (var i in refItem.AllowedValueAssignments)
                        {
                            sb.Append($"{i.Key} - {i.Value}, ");
                        }
                        sb.Length -= 2; // Remove the trailing comma and space
                        sb.Append(")");
                    }
                }

                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
