using Siemens.Simatic.S7.Webserver.API.Services.RequestHandling;
using Siemens.Simatic.S7.Webserver.API.Enums;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using Siemens.Simatic.S7.Webserver.API.Models.Responses;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.JsonRPC.Services
{
    /// <summary>
    /// Service responsible for handling PLC variable operations such as reading, writing, and browsing variables.
    /// </summary>
    public class VariableService
    {
        private readonly IApiRequestHandler _requestHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableService"/> class.
        /// </summary>
        /// <param name="requestHandler">The request handler used for making API calls to the PLC.</param>
        public VariableService(IApiRequestHandler requestHandler)
        {
            _requestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler), "Request handler cannot be null.");
        }

        /// <summary>
        /// Reads a variable from the PLC.
        /// </summary>
        /// <typeparam name="T">The expected data type of the variable being read.</typeparam>
        /// <param name="variableName">The name of the variable to read.</param>
        /// <param name="mode">The mode in which to read the variable ("simple" or "raw").</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>
        /// A task representing the asynchronous operation. Returns the value of the variable read or null if the operation fails.
        /// </returns>
        public async Task<ApiResultResponse<T>> ReadAsync<T>(string variableName, ApiPlcProgramReadOrWriteMode? mode = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(variableName))
            {
                Log.Error("Invalid variable name provided for Read operation.");
                return null;
            }

            try
            {
                Log.Information("Reading variable {Variable} with mode {Mode}.", variableName, mode ?? ApiPlcProgramReadOrWriteMode.Simple);

                // Send request to read the variable
                var response = await _requestHandler.PlcProgramReadAsync<T>(variableName, mode, cancellationToken);

                // Log the result of the read operation
                Log.Information("Read response for {Variable}: {Response}", variableName, response.Result);

                return response;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while reading variable {Variable}.", variableName);
                return null; // Return null to indicate failure
            }
        }

        /// <summary>
        /// Writes a value to a variable in the PLC.
        /// </summary>
        /// <param name="variableName">The name of the variable to write to.</param>
        /// <param name="value">The value to be written to the variable.</param>
        /// <param name="mode">The mode in which to write the variable ("simple" or "raw").</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>
        /// A task representing the asynchronous operation. Returns true if the write operation succeeds, otherwise false.
        /// </returns>
        public async Task<bool> WriteAsync(string variableName, object value, ApiPlcProgramReadOrWriteMode? mode = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(variableName))
            {
                Log.Error("Invalid variable name provided for Write operation.");
                return false;
            }

            if (value is null)
            {
                Log.Error("Null value provided for writing to variable {Variable}.", variableName);
                return false;
            }

            try
            {
                Log.Information("Writing to variable {Variable} with value {Value} and mode {Mode}.", variableName, value, mode ?? ApiPlcProgramReadOrWriteMode.Simple);

                // Send request to write the variable
                var response = await _requestHandler.PlcProgramWriteAsync(variableName, value, mode, cancellationToken);

                // Log the result of the write operation
                Log.Information("Write response for {Variable}: {Response}", variableName, response.Result);

                return response.Result; // Return true if successful
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while writing to variable {Variable}.", variableName);
                return false; // Return false to indicate failure
            }
        }

        /// <summary>
        /// Browses variables or child elements in the PLC.
        /// </summary>
        /// <param name="mode">The browsing mode ("var" to browse a specific variable, "children" to browse child elements).</param>
        /// <param name="variableName">The name of the variable or element to browse (optional in "children" mode).</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>
        /// A task representing the asynchronous operation. Returns the list of variables or metadata or null if the operation fails.
        /// </returns>
        public async Task<ApiPlcProgramBrowseResponse> BrowseAsync(ApiPlcProgramBrowseMode mode, string variableName = null, CancellationToken cancellationToken = default)
        {
            try
            {
                Log.Information("Browsing variable {Variable} with mode {Mode}.", variableName ?? "root", mode);

                // Send request to browse the variables
                var response = await _requestHandler.PlcProgramBrowseAsync(mode, variableName, cancellationToken);

                // Log the result of the browse operation
                Log.Information("Browse response for {Variable}: {Response}", variableName ?? "root", response.Result);

                return response;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while browsing variable {Variable}.", variableName ?? "root");
                return null; // Return null to indicate failure
            }
        }
    }
}
