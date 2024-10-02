using PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions;
using Serilog;
using System.Collections.Generic;
using System;
using System.Text;

// Contains methods specifically for logging operation results.
namespace PLC.Commissioning.Lib.Siemens.PLCProject
{
    /// <summary>
    /// Logs detailed information about operation results, including state, warnings, errors, and messages.
    /// </summary>
    public class OperationLogger
    {
        /// <summary>
        /// The result object containing operation details to be logged.
        /// </summary>
        private readonly IResult _result;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationLogger"/> class with the specified result.
        /// </summary>
        /// <param name="result">The result object containing operation details to be logged.</param>
        /// <exception cref="ArgumentException">Thrown when the provided result is null.</exception>
        public OperationLogger(IResult result)
        {
            _result = result ?? throw new ArgumentException("Unsupported result type", nameof(result));
        }

        /// <summary>
        /// Logs the results of an operation, including state, warning count, error count, and detailed messages.
        /// </summary>
        /// <param name="operationName">The name of the operation to include in the log.</param>
        public void LogResults(string operationName)
        {
            if (_result != null)
            {
                var sb = new StringBuilder();

                sb.AppendLine($"{operationName} State: " + _result.State);
                sb.AppendLine("Warning Count: " + _result.WarningCount);
                sb.AppendLine("Error Count: " + _result.ErrorCount);
                sb.Append(RecursivelyWriteMessages(_result.Messages));

                Log.Debug(sb.ToString());
            }
        }

        /// <summary>
        /// Recursively builds a string containing detailed messages from the result, including nested messages.
        /// </summary>
        /// <param name="messages">The collection of result messages to process.</param>
        /// <param name="indent">The indentation string for formatting nested messages.</param>
        /// <returns>A string containing formatted messages.</returns>
        private static string RecursivelyWriteMessages(IEnumerable<IResultMessage> messages, string indent = "")
        {
            var sb = new StringBuilder();
            indent += "\t";

            foreach (IResultMessage message in messages)
            {
                sb.AppendLine(indent + "DateTime: " + message.DateTime);
                sb.AppendLine(indent + "State: " + message.State);
                sb.AppendLine(indent + "Message: " + message.Description);
                sb.Append(RecursivelyWriteMessages(message.Messages, indent));
            }

            return sb.ToString();
        }
    }
}
