using System.Collections.Generic;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions
{
    /// <summary>
    /// Defines a contract for logging operation results.
    /// </summary>
    public interface IOperationLogger
    {
        /// <summary>
        /// Logs the results of an operation, including state, warning count, error count, and detailed messages.
        /// </summary>
        /// <param name="operationName">The name of the operation to include in the log.</param>
        void LogResults(string operationName);
    }
}