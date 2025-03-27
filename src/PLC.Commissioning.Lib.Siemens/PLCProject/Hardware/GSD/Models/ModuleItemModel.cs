using System.Collections.Generic;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models.IOTags;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models
{
    /// <summary>
    /// Represents the GSD module item information that is usable in our case.
    /// </summary>
    public class ModuleItemModel
    {
        /// <summary>
        /// Gets or sets the unique identifier of the module.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Gets or sets the module identification number.
        /// </summary>
        public string ModuleIdentNumber { get; set; }

        /// <summary>
        /// Gets or sets the name of the module.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the informational text about the module.
        /// </summary>
        public string InfoText { get; set; }

        /// <summary>
        /// Gets or sets the parameter record data item associated with the module.
        /// </summary>
        public ParameterRecordDataItem ParameterRecordDataItem { get; set; }
        
        /// <summary>
        /// Gets or sets the safety parameter record data item associated with the module.
        /// </summary>
        public FParameterRecordDataItem FParameterRecordDataItem { get; set; }

        /// <summary>
        /// Gets or sets the IO data associated with the module.
        /// </summary>
        public IOData IOData { get; set; }
    }
}