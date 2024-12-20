using System.Collections.Generic;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models.IOTags
{
    /// <summary>
    /// Represents a data item within IO data, which may have associated bit data items.
    /// </summary>
    public class DataItem
    {
        /// <summary>
        /// Gets or sets the data type of the data item.
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the data item is used as bits.
        /// </summary>
        public bool UseAsBits { get; set; }

        /// <summary>
        /// Gets or sets the text identifier of the data item.
        /// </summary>
        public string TextId { get; set; }

        /// <summary>
        /// Gets or sets the list of bit data items associated with this data item.
        /// </summary>
        public List<BitDataItem> BitDataItems { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataItem"/> class.
        /// </summary>
        public DataItem()
        {
            BitDataItems = new List<BitDataItem>();
        }
    }
}