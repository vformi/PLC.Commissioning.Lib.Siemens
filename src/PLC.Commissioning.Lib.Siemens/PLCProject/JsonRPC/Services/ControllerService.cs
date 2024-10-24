using Serilog;
using Siemens.Simatic.S7.Webserver.API.Enums;
using Siemens.Simatic.S7.Webserver.API.Services.RequestHandling;
using System;
using System.Threading.Tasks;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.JsonRPC.Services
{
    /// <summary>
    /// Service responsible for interacting with the PLC controller to read and change the operating mode.
    /// </summary>
    public class ControllerService
    {
        private readonly IApiRequestHandler _requestHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControllerService"/> class.
        /// </summary>
        /// <param name="requestHandler">The request handler used for making API calls to the PLC.</param>
        public ControllerService(IApiRequestHandler requestHandler)
        {
            _requestHandler = requestHandler;
        }

        /// <summary>
        /// Reads the current operating mode of the PLC.
        /// </summary>
        /// <returns>
        /// A task representing the asynchronous operation. Returns the operating mode as a string if successful, or null if the operation fails.
        /// </returns>
        public async Task<string> GetOperatingModeAsync()
        {
            try
            {
                Log.Information("Reading PLC operating mode.");

                // Send request to read PLC operating mode
                var response = await _requestHandler.PlcReadOperatingModeAsync();

                // Log the result
                Log.Information("PLC operating mode is {Mode}.", response.Result.ToString());

                return response.Result.ToString(); // Return the operating mode as a string
            }
            catch (Exception ex)
            {
                // Log the error
                Log.Error(ex, "Error while reading PLC operating mode.");

                // Return null to indicate the operation failed
                return null;
            }
        }


        /// <summary>
        /// Changes the operating mode of the PLC.
        /// </summary>
        /// <param name="mode">The new operating mode to set (e.g., Run, Stop).</param>
        /// <returns>
        /// A task representing the asynchronous operation. Returns true if the mode change was successful, false otherwise.
        /// </returns>
        public async Task<bool> ChangeOperatingModeAsync(ApiPlcOperatingMode mode)
        {
            try
            {
                Log.Information("Changing PLC mode to {Mode}.", mode);

                // Send request to change the PLC operating mode
                var response = await _requestHandler.PlcRequestChangeOperatingModeAsync(mode);

                // Check if the change was successful
                var success = response.Result;

                // Log the result
                Log.Information("PLC mode change success: {Success}.", success);

                return success; // Return the success flag from the response
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while changing PLC operating mode to {Mode}.", mode);
                return false; // Operation failed
            }
        }
    }
}
