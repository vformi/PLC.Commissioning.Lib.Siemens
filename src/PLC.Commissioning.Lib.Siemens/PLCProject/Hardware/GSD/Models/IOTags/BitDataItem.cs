namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models.IOTags
{
    /// <summary>
    /// Represents a bit-level data item within a data item.
    /// </summary>
    public class BitDataItem
    {
        /// <summary>
        /// Gets or sets the bit offset of the bit data item.
        /// </summary>
        public int BitOffset { get; set; }

        /// <summary>
        /// Gets or sets the text identifier of the bit data item.
        /// </summary>
        public string TextId { get; set; }
    }
}