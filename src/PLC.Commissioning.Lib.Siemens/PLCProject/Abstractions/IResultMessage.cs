using System.Collections.Generic;
using System;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions
{
    /// <summary>
    /// Represents a message related to the result of an operation, including state, description, and timestamp.
    /// </summary>
    public interface IResultMessage
    {
        DateTime DateTime { get; }
        string State { get; }
        string Description { get; }
        IEnumerable<IResultMessage> Messages { get; }
    }
}
