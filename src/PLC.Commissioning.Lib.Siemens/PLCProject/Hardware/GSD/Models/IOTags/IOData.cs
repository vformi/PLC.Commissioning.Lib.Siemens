using System.Collections.Generic;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models.IOTags
{
    /// <summary>
    /// Represents the IO data of a module, including inputs and outputs.
    /// </summary>
    public class IOData
    {
        /// <summary>
        /// Gets or sets the list of input data items.
        /// </summary>
        public List<DataItem> Inputs { get; set; }

        /// <summary>
        /// Gets or sets the list of output data items.
        /// </summary>
        public List<DataItem> Outputs { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IOData"/> class.
        /// </summary>
        public IOData()
        {
            Inputs = new List<DataItem>();
            Outputs = new List<DataItem>();
        }
    }
}