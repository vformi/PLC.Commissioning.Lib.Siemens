using System.Collections.Generic;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models
{
    /// <summary>
    /// Represents a value item in the GSD file, including its assignments.
    /// </summary>
    public class ValueItem
    {
        /// <summary>
        /// Gets or sets the ID of the value item.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Gets the list of assignments associated with the value item.
        /// </summary>
        public List<Assign> Assignments { get; } = new List<Assign>();
    }
}
