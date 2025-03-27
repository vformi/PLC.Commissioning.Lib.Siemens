using System;
using System.Text;
using System.Xml;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD
{
    /// <summary>
    /// Parses and manages the module information from a GSD file.
    /// </summary>
    public class ModuleInfo
    {
        /// <summary>
        /// Gets the model containing the parsed module information.
        /// </summary>
        public virtual ModuleInfoModel Model { get; private set; }

        /// <summary>
        /// The GSD handler used for processing module data.
        /// </summary>
        private GSDHandler _gsdHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleInfo"/> class with the specified GSD handler.
        /// </summary>
        /// <param name="gsdHandler">The GSD handler used for processing module data.</param>
        public ModuleInfo(GSDHandler gsdHandler)
        {
            _gsdHandler = gsdHandler;
            Model = new ModuleInfoModel();
            ParseModuleInfo();
        }

        /// <summary>
        /// Parses the module information from the GSD file and populates the <see cref="Model"/>.
        /// </summary>
        private void ParseModuleInfo()
        {
            // Must find the ModuleInfo node
            XmlNode moduleInfoNode = _gsdHandler.xmlDoc.SelectSingleNode("//gsd:ModuleInfo", _gsdHandler.nsmgr);
            
            // Because it's "strict," we assume it MUST exist. If it's missing, you'll get a NullReferenceException below.
            XmlNode nameNode = moduleInfoNode.SelectSingleNode("gsd:Name", _gsdHandler.nsmgr);
            XmlNode infoTextNode = moduleInfoNode.SelectSingleNode("gsd:InfoText", _gsdHandler.nsmgr);
            XmlNode vendorNameNode = moduleInfoNode.SelectSingleNode("gsd:VendorName", _gsdHandler.nsmgr);
            XmlNode orderNumberNode = moduleInfoNode.SelectSingleNode("gsd:OrderNumber", _gsdHandler.nsmgr);
            XmlNode hwReleaseNode = moduleInfoNode.SelectSingleNode("gsd:HardwareRelease", _gsdHandler.nsmgr);
            XmlNode swReleaseNode = moduleInfoNode.SelectSingleNode("gsd:SoftwareRelease", _gsdHandler.nsmgr);

            // Strictly read each attribute (throws if missing)
            string nameTextId = nameNode.Attributes["TextId"].Value;
            string infoTextId = infoTextNode.Attributes["TextId"].Value;

            Model.Name = _gsdHandler.GetExternalText(nameTextId);
            Model.InfoText = _gsdHandler.GetExternalText(infoTextId);
            Model.VendorName = vendorNameNode.Attributes["Value"].Value;
            Model.OrderNumber = orderNumberNode.Attributes["Value"].Value;
            Model.HardwareRelease = hwReleaseNode.Attributes["Value"].Value;
            Model.SoftwareRelease = swReleaseNode.Attributes["Value"].Value;
        }

        /// <summary>
        /// ToString method of ModuleInfo class.
        /// </summary>
        /// <returns>A string that represents the module information.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Name: {Model.Name}");
            sb.AppendLine($"Info Text: {Model.InfoText}");
            sb.AppendLine($"Vendor Name: {Model.VendorName}");
            sb.AppendLine($"Order Number: {Model.OrderNumber}");
            sb.AppendLine($"Hardware Release: {Model.HardwareRelease}");
            sb.AppendLine($"Software Release: {Model.SoftwareRelease}");

            return sb.ToString();
        }
    }
}
