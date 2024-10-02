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
        public ModuleInfoModel Model { get; private set; }

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
            XmlNode moduleInfoNode = _gsdHandler.xmlDoc.SelectSingleNode("//gsd:ModuleInfo", _gsdHandler.nsmgr);
            if (moduleInfoNode != null)
            {
                Model.Name = _gsdHandler.GetExternalText(moduleInfoNode.SelectSingleNode("gsd:Name", _gsdHandler.nsmgr)?.Attributes["TextId"]?.Value);
                Model.InfoText = _gsdHandler.GetExternalText(moduleInfoNode.SelectSingleNode("gsd:InfoText", _gsdHandler.nsmgr)?.Attributes["TextId"]?.Value);
                Model.VendorName = moduleInfoNode.SelectSingleNode("gsd:VendorName", _gsdHandler.nsmgr)?.Attributes["Value"]?.Value;
                Model.OrderNumber = moduleInfoNode.SelectSingleNode("gsd:OrderNumber", _gsdHandler.nsmgr)?.Attributes["Value"]?.Value;
                Model.HardwareRelease = moduleInfoNode.SelectSingleNode("gsd:HardwareRelease", _gsdHandler.nsmgr)?.Attributes["Value"]?.Value;
                Model.SoftwareRelease = moduleInfoNode.SelectSingleNode("gsd:SoftwareRelease", _gsdHandler.nsmgr)?.Attributes["Value"]?.Value;
            }

            Model.Name = _gsdHandler.GetExternalText("DeviceName") ?? Model.Name;
            Model.InfoText = _gsdHandler.GetExternalText("DeviceDescription") ?? Model.InfoText;
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
