namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Models
{
    public class IOModuleInfoModel
    {
        /// <summary>
        /// Structure to store module information.
        /// </summary>
        public string ModuleName { get; set; }
        public string GsdId { get; set; }

        // Separate properties for Input and Output
        public int? InputStartAddress { get; set; }
        public int? InputLength { get; set; }
        
        public int? OutputStartAddress { get; set; }
        public int? OutputLength { get; set; }
        
        /// <summary>
        /// Returns a string representation of the module info.
        /// </summary>
        public override string ToString()
        {
            return $"Module: {ModuleName}, GSD ID: {GsdId}, " +
                   $"Input: [Start={InputStartAddress}, Length={InputLength}] " +
                   $"Output: [Start={OutputStartAddress}, Length={OutputLength}]";
        }
    }
}