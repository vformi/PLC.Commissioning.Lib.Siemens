using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD
{
    /// <summary>
    /// Manages a list of device access point items, parsing and storing them properly.
    /// </summary>
    public class DeviceAccessPointList
    {
        /// <summary>
        /// Gets or sets the list of device access point items.
        /// </summary>
        public List<DeviceAccessPointItem> DeviceAccessPointItems { get; set; } = new List<DeviceAccessPointItem>();

        /// <summary>
        /// The GSD handler used for processing module data.
        /// </summary>
        private readonly GSDHandler _gsdHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAccessPointList"/> class with the specified GSD handler.
        /// </summary>
        /// <param name="gsdHandler">The GSD handler used to parse the device access points.</param>
        public DeviceAccessPointList(GSDHandler gsdHandler)
        {
            _gsdHandler = gsdHandler ?? throw new ArgumentNullException(nameof(gsdHandler));
            ParseDeviceAccessPointItems();
        }

        /// <summary>
        /// Parses the device access point items from the GSD file and adds them to the list.
        /// </summary>
        private void ParseDeviceAccessPointItems()
        {
            XmlNodeList dapItemNodes = _gsdHandler.xmlDoc.SelectNodes("//gsd:DeviceAccessPointList/gsd:DeviceAccessPointItem", _gsdHandler.nsmgr);
            foreach (XmlNode dapItemNode in dapItemNodes)
            {
                var dapItem = new DeviceAccessPointItem(_gsdHandler);
                dapItem.ParseDeviceAccessPointItem(dapItemNode);
                DeviceAccessPointItems.Add(dapItem);
            }
        }

        /// <summary>
        /// Retrieves a device access point item by its ID and checks if it has changeable parameters.
        /// </summary>
        /// <param name="id">The ID of the device access point item to retrieve.</param>
        /// <returns>A tuple containing the <see cref="DeviceAccessPointItem"/> and a boolean indicating if it has changeable parameters.</returns>
        public (DeviceAccessPointItem dapItem, bool hasChangeableParameters) GetDeviceAccessPointItemByID(string id)
        {
            DeviceAccessPointItem dapItem = DeviceAccessPointItems.Find(item => item.Model.ID == id);

            if (dapItem != null)
            {
                bool hasChangeableParameters = dapItem.Model.ParameterRecordDataItem != null;
                return (dapItem, hasChangeableParameters);
            }

            return (null, false);
        }

        /// <summary>
        /// ToString method of DeviceAccessPointList class 
        /// </summary>
        /// <returns>A string that represents all device access point items.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Device access point:");

            foreach (var item in DeviceAccessPointItems)
            {
                sb.AppendLine(item.ToString());
            }

            return sb.ToString();
        }
    }
}
