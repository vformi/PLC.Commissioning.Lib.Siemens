namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models
{
    /// <summary>
    /// Represents the GSD module item information that is usable in our case
    /// </summary>
    public class ModuleItemModel
    {
        public string ID { get; set; }
        public string ModuleIdentNumber { get; set; }
        public string Name { get; set; }
        public string InfoText { get; set; }
        public ParameterRecordDataItem ParameterRecordDataItem { get; set; }
    }
}
