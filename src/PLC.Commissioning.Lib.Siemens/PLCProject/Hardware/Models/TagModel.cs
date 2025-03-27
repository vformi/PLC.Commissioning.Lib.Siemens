namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Models
{
    /// <summary>
    /// Represents an individual PLC Tag definition.
    /// </summary>
    public class TagModel
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public string Address { get; set; }
    }
}