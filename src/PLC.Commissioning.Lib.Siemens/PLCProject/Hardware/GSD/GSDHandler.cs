using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Abstractions;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD
{
    /// <summary>
    /// Handles the loading and parsing of the GSD XML file.
    /// </summary>
    public class GSDHandler
    {
        /// <summary>
        /// Gets the loaded XML document representing the GSD file.
        /// </summary>
        public XmlDocument xmlDoc { get; private set; }

        /// <summary>
        /// Gets the XML namespace manager for the GSD document.
        /// </summary>
        public XmlNamespaceManager nsmgr { get; set; }

        /// <summary>
        /// Stores the value items parsed from the GSD file, indexed by their IDs.
        /// </summary>
        private readonly Dictionary<string, ValueItem> _valueItems = new Dictionary<string, ValueItem>();

        /// <summary>
        /// Initializes the GSDHandler by loading and validating the GSD XML file.
        /// </summary>
        /// <param name="xmlFilePath">The path to the GSD XML file to load.</param>
        /// <returns><c>true</c> if the XML was loaded and validated successfully; otherwise, <c>false</c>.</returns>
        public bool Initialize(string xmlFilePath)
        {
            try
            {
                xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlFilePath);

                nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsmgr.AddNamespace("gsd", "http://www.profibus.com/GSDML/2003/11/DeviceProfile");

                ParseValueList();
                return true;
            }
            catch (XmlException ex)
            {
                Log.Error($"Failed to load GSD file. XML is malformed: {ex.Message}");
                return false;
            }
            catch (FileNotFoundException ex)
            {
                Log.Error($"GSD file not found: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"An unexpected error occurred while loading GSD file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Prints the loaded GSD XML document to the console in a formatted manner.
        /// </summary>
        public void PrintGSD()
        {
            using (var stringWriter = new System.IO.StringWriter())
            using (var xmlTextWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true }))
            {
                xmlDoc.WriteTo(xmlTextWriter);
                xmlTextWriter.Flush();
                Console.WriteLine(stringWriter.GetStringBuilder().ToString());
            }
        }

        /// <summary>
        /// Retrieves the external text associated with the specified TextId.
        /// </summary>
        /// <param name="textId">The TextId of the external text to retrieve.</param>
        /// <returns>The external text associated with the TextId, or null if not found.</returns>
        public string GetExternalText(string textId)
        {
            if (string.IsNullOrEmpty(textId))
                return null;

            XmlNode textNode = xmlDoc.SelectSingleNode($"//gsd:ExternalTextList/gsd:PrimaryLanguage/gsd:Text[@TextId='{textId}']", nsmgr);
            return textNode?.Attributes["Value"]?.Value;
        }

        /// <summary>
        /// Parses the value list from the GSD file and stores it in a dictionary.
        /// </summary>
        private void ParseValueList()
        {
            XmlNodeList valueItemNodes = xmlDoc.SelectNodes("//gsd:ValueList/gsd:ValueItem", nsmgr);
            foreach (XmlNode valueItemNode in valueItemNodes)
            {
                var valueItem = new ValueItem { ID = valueItemNode.Attributes["ID"].Value };
                foreach (XmlNode assignNode in valueItemNode.SelectNodes("gsd:Assignments/gsd:Assign", nsmgr))
                {
                    var assign = new Assign
                    {
                        Content = assignNode.Attributes["Content"].Value,
                        Text = GetExternalText(assignNode.Attributes["TextId"].Value)
                    };
                    valueItem.Assignments.Add(assign);
                }
                _valueItems[valueItem.ID] = valueItem;
            }
        }

        /// <summary>
        /// Retrieves the value item associated with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the value item to retrieve.</param>
        /// <returns>The <see cref="ValueItem"/> with the specified ID, or null if not found.</returns>
        public ValueItem GetValueItem(string id)
        {
            _valueItems.TryGetValue(id, out var valueItem);
            return valueItem;
        }
    }
}