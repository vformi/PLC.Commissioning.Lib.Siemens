using System.Collections.Generic;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Models
{
    /// <summary>
    /// Represents a tag table definition (one per module).
    /// </summary>
    public class TagTableModel
    {
        public string TableName { get; set; }
        public List<TagModel> Tags { get; set; } = new List<TagModel>();
    }
}