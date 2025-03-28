using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Models;
using Serilog;
using Siemens.Engineering.HW;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.OpcUa;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Software.Handlers
{
    public class OPCHandler
    {
        private readonly PlcSoftware _plcSoftware;
        private readonly DeviceItem _cpuDeviceItem;
        private readonly DeviceItem _opcUaDeviceItem;

        // The resource name to your embedded "BaseServerInterface.xml"
        // Make sure this matches your actual embedded resource path/filename.
        private static readonly string EmbeddedXmlResourceName = 
            "PLC.Commissioning.Lib.Siemens.PLCProject.Software.Resources.BaseServerInterface.xml";

        public OPCHandler(PlcSoftware plcSoftware, DeviceItem cpuDeviceItem = null)
        {
            _plcSoftware = plcSoftware ?? throw new ArgumentNullException(nameof(plcSoftware));
            _cpuDeviceItem = cpuDeviceItem;

            // Perform OPC UA setup check if cpuDeviceItem is provided
            if (_cpuDeviceItem != null)
            {
                _opcUaDeviceItem = FindOpcUaDeviceItem();
                if (_opcUaDeviceItem == null)
                {
                    throw new InvalidOperationException("No OPC UA device item found in the CPU.");
                }

                object opcUaServerValue = _opcUaDeviceItem.GetAttribute("OpcUaServer");
                if (!(opcUaServerValue is bool) || !(bool)opcUaServerValue)
                {
                    throw new InvalidOperationException("OPC UA server is not enabled. Please enable OPC UA server before proceeding.");
                }
            }
        }

        private DeviceItem FindOpcUaDeviceItem()
        {
            if (_cpuDeviceItem == null) return null;
            foreach (var deviceItem in _cpuDeviceItem.DeviceItems)
            {
                if (deviceItem.Name != null && deviceItem.Name.Contains("OPC UA"))
                {
                    return deviceItem;
                }
            }
            return null;
        }

        #region Main methods

        /// <summary>
        /// High-level convenience method:
        /// 1) Generate ONE .xml for ALL devices using precomputed tag table definitions,
        /// 2) Create a server interface in TIA,
        /// 3) Import that single NodeSet file.
        /// </summary>
        /// <param name="deviceTagTables">List of tuples containing devices and their tag table definitions.</param>
        /// <param name="interfaceName">Name of the server interface in TIA Portal.</param>
        /// <returns>true on success; false otherwise</returns>
        public bool GenerateAndImportServerInterface(IList<(ProjectDevice device, List<TagTableModel> tagTables)> deviceTagTables, string interfaceName)
        {
            // Check if we should generate OPC UA server interface based on CPU attribute
            if (_cpuDeviceItem != null && !ShouldGenerateOpcUaInterface())
            {
                return true; 
            }
            // 1) Generate a single .xml for all devices
            string tempXml = Path.Combine(Path.GetTempPath(), interfaceName + "_UA.xml");
            bool generated = GenerateServerInterfaceXmlForDevices(tempXml, deviceTagTables);
            if (!generated)
            {
                Log.Error("Failed to generate server interface XML for devices.");
                return false;
            }

            // 2) Create the interface in TIA Portal
            bool created = CreateAndImportServerInterface(interfaceName, tempXml);
            if (!created)
            {
                Log.Error("Failed to create/import the server interface in TIA Portal.");
                return false;
            }

            // Cleanup the temporary XML file
            try
            {
                if (File.Exists(tempXml))
                {
                    File.Delete(tempXml);
                    Log.Debug("Temporary XML file '{TempXml}' deleted successfully.", tempXml);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to delete temporary XML file '{TempXml}'.", tempXml);
            }
            Log.Information("OPC UA interface successfully created for all devices: {Count} total.", deviceTagTables.Count);
            return true;
        }

        /// <summary>
        /// Creates a new Server Interface in the TIA OPC UA provider and imports the given XML file.
        /// Returns true/false on success/failure (logs any errors).
        /// </summary>
        public bool CreateAndImportServerInterface(string interfaceName, string importFilePath)
        {
            Log.Information("Creating a new server interface '{InterfaceName}' in TIA OPC UA provider...", interfaceName);

            OpcUaProvider provider = _plcSoftware.GetService<OpcUaProvider>();
            if (provider == null)
            {
                Log.Error("OPC UA provider is not available on the PLC software.");
                return false;
            }

            OpcUaCommunicationGroup commGroup = provider.CommunicationGroup;
            ServerInterfaceGroup serverInterfaceGroup = commGroup?.ServerInterfaceGroup;
            ServerInterfaceComposition serverInterfaces = serverInterfaceGroup?.ServerInterfaces;

            if (serverInterfaces == null)
            {
                Log.Error("Server interface composition is not available from the OPC UA provider.");
                return false;
            }

            // Create a new server interface with the requested name
            ServerInterface serverInterface = serverInterfaces.Create(interfaceName);
            if (serverInterface == null)
            {
                Log.Error("Failed to create a new server interface named '{InterfaceName}'.", interfaceName);
                return false;
            }

            // Set author if desired
            serverInterface.Author = "PLCCommissioningLib";

            // Import the generated XML
            if (!string.IsNullOrWhiteSpace(importFilePath) && File.Exists(importFilePath))
            {
                try
                {
                    serverInterface.Import(new FileInfo(importFilePath));
                    Log.Debug("Imported temporary XML '{FilePath}' into server interface '{InterfaceName}'.",
                                    importFilePath, interfaceName);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to import XML from '{FilePath}'.", importFilePath);
                    return false;
                }
            }
            else
            {
                Log.Warning("No valid XML file to import (File not found: '{FilePath}').", importFilePath);
            }

            Log.Information("Server interface '{InterfaceName}' created & configured successfully.", interfaceName);
            return true;
        }

        #endregion

        #region Generating the NodeSet XML for multiple devices/tags

        /// <summary>
        /// Generates a single NodeSet XML that includes:
        /// - All Siemens data types (from the base template),
        /// - The top-level object "Devices" (NodeId="ns=2;i=1"),
        /// - A UAObject for each device, with UAVariables for each tag.
        /// Saves to <paramref name="outputXmlPath"/>.
        /// </summary>
        /// <param name="outputXmlPath">Path where the XML file will be saved.</param>
        /// <param name="deviceTagTables">List of tuples containing devices and their tag table definitions.</param>
        /// <returns>true if successful; false otherwise</returns>
        public bool GenerateServerInterfaceXmlForDevices(
            string outputXmlPath,
            IList<(ProjectDevice device, List<TagTableModel> tagTables)> deviceTagTables)
        {
            XDocument doc;
            try
            {
                doc = LoadEmbeddedXmlTemplate();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load the embedded base server interface template.");
                return false;
            }

            XElement root = doc.Root;
            if (root == null || root.Name.LocalName != "UANodeSet")
            {
                Log.Error("The loaded XML does not have a <UANodeSet> root element. Aborting.");
                return false;
            }

            XNamespace opcNs = "http://opcfoundation.org/UA/2011/03/UANodeSet.xsd";
            XElement devicesObject = root
                .Descendants(opcNs + "UAObject")
                .FirstOrDefault(e => (string)e.Attribute("NodeId") == "ns=2;i=1");

            if (devicesObject == null)
            {
                Log.Error("Template is missing <UAObject NodeId='ns=2;i=1'> (the 'Devices' object). Check your XML template.");
                return false;
            }

            XElement devicesRefs = devicesObject.Element(opcNs + "References");
            if (devicesRefs == null)
            {
                devicesRefs = new XElement(opcNs + "References");
                devicesObject.Add(devicesRefs);
            }

            foreach (var (importedDevice, tagTables) in deviceTagTables)
            {
                string deviceName = importedDevice.DeviceName;
                Log.Debug("Adding device '{DeviceName}' to NodeSet.", deviceName);

                // String-based NodeId for the device
                string deviceNodeIdString = $"ns=2;s=Devices.{deviceName}";

                XElement deviceObject = new XElement(opcNs + "UAObject",
                    new XAttribute("NodeId", deviceNodeIdString),
                    new XAttribute("BrowseName", $"2:{deviceName}"),
                    new XElement(opcNs + "DisplayName", deviceName),
                    new XElement(opcNs + "References",
                        new XElement(opcNs + "Reference",
                            new XAttribute("ReferenceType", "HasTypeDefinition"),
                            "i=58"
                        )
                    )
                );

                root.Add(deviceObject);

                devicesRefs.Add(
                    new XElement(opcNs + "Reference",
                        new XAttribute("ReferenceType", "Organizes"),
                        deviceNodeIdString
                    )
                );

                XElement deviceRefs = deviceObject.Element(opcNs + "References");

                foreach (var table in tagTables)
                {
                    foreach (var tag in table.Tags)
                    {
                        string tagName = tag.Name;
                        string dataType = tag.DataType.ToUpper();

                        // String-based NodeId for the tag
                        string tagNodeIdString = $"ns=2;s=Devices.{deviceName}.{tagName}";
                        string tagBrowseName = $"2:{tagName}";

                        XElement uaVariable = new XElement(opcNs + "UAVariable",
                            new XAttribute("NodeId", tagNodeIdString),
                            new XAttribute("BrowseName", tagBrowseName),
                            new XAttribute("DataType", dataType),
                            new XAttribute("AccessLevel", "3"),
                            new XElement(opcNs + "DisplayName", tagName),
                            new XElement(opcNs + "References",
                                new XElement(opcNs + "Reference",
                                    new XAttribute("ReferenceType", "HasTypeDefinition"),
                                    "i=63"
                                )
                            ),
                            new XElement(opcNs + "Extensions",
                                new XElement(opcNs + "Extension",
                                    new XElement(XName.Get("VariableMapping",
                                        "http://www.siemens.com/OPCUA/2017/SimaticNodeSetExtensions"),
                                        $"\"{tagName}\""
                                    )
                                )
                            )
                        );

                        root.Add(uaVariable);

                        deviceRefs.Add(
                            new XElement(opcNs + "Reference",
                                new XAttribute("ReferenceType", "HasComponent"),
                                tagNodeIdString
                            )
                        );
                    }
                }
            }

            try
            {
                doc.Save(outputXmlPath);
                Log.Debug("Saved combined server interface XML for {Count} device(s) to '{Path}'.",
                                deviceTagTables.Count, outputXmlPath);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save NodeSet XML to '{Path}'.", outputXmlPath);
                return false;
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Loads the embedded XML template ("BaseServerInterface.xml") from this assembly
        /// and returns it as an XDocument. Throws if not found.
        /// </summary>
        private XDocument LoadEmbeddedXmlTemplate()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(EmbeddedXmlResourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"Embedded resource '{EmbeddedXmlResourceName}' not found.");
                }
                return XDocument.Load(stream);
            }
        }
        
        private bool ShouldGenerateOpcUaInterface()
        {
            if (_opcUaDeviceItem == null)
            {
                Log.Warning("No OPC UA device item found for PLC. Defaulting to generate custom server interface.");
                return true; // If no OPC UA device, default to generate
            }
            try
            {
                object attributeValue = _opcUaDeviceItem.GetAttribute("OpcUaStandardServerInterface");
                bool boolValue = (bool)attributeValue; // Cast directly since it's always a boolean

                if (boolValue)
                {
                    Log.Information("PLC '{DeviceName}' has Siemens OPC UA Standard Server Interface enabled. Skipping custom server interface generation.", 
                        _opcUaDeviceItem.Name);
                    return false; // Don't generate if attribute is true (S7-1500 case)
                }
        
                Log.Information("PLC '{DeviceName}' has Siemens OPC UA Standard Server Interface disabled. Proceeding with custom server interface generation.", 
                    _opcUaDeviceItem.Name);
                return true; // Generate if attribute is false
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking Siemens OPC UA Standard Server Interface for PLC '{DeviceName}'. Defaulting to generate custom interface.", 
                    _opcUaDeviceItem.Name);
                return true; // Default to generating in case of error
            }
        }
        
        #endregion
    }
}
