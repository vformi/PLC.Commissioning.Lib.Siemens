using System.Collections.Generic;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions
{
    /// <summary>
    /// Represents the result of an operation, including state information, warning count, error count, and related messages.
    /// </summary>
    public interface IResult
    {
        string State { get; }
        int WarningCount { get; }
        int ErrorCount { get; }
        IEnumerable<IResultMessage> Messages { get; }
    }
}
