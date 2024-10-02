namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models
{
    /// <summary>
    /// Represents the final parsed module information
    /// </summary>
    public class ParsedValueModel
    {
        public string ValueItemTarget { get; set; }
        public object Value { get; set; }
        public string Parameter { get; set; }
    }
}
