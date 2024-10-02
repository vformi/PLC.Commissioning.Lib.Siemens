using System.Collections.Generic;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models
{
    /// <summary>
    /// Represents the Ref item inside the ParameterRecordDataItem GSD List
    /// </summary>
    public class RefModel
    {
        public string ValueItemTarget { get; set; }
        public string DataType { get; set; }
        public int ByteOffset { get; set; }
        public int? BitOffset { get; set; }
        public int? BitLength { get; set; }
        public int? Length { get; set; }
        public string DefaultValue { get; set; }
        public string AllowedValues { get; set; }
        public string TextId { get; set; }
        public string Text { get; set; }
        public Dictionary<string, string> AllowedValueAssignments { get; set; } = new Dictionary<string, string>();
    }
}
