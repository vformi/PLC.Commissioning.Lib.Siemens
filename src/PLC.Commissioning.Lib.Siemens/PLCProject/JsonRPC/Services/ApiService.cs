using Serilog;
using Siemens.Simatic.S7.Webserver.API.Models;
using Siemens.Simatic.S7.Webserver.API.Services.RequestHandling;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.JsonRPC.Services
{
    /// <summary>
    /// Service for interacting with the Siemens API.
    /// Handles operations such as browsing API methods, login, logout, etc.
    /// </summary>
    public class ApiService
    {
        private readonly IApiRequestHandler _requestHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiService"/> class.
        /// </summary>
        /// <param name="requestHandler">The request handler used for making API calls.</param>
        public ApiService(IApiRequestHandler requestHandler)
        {
            _requestHandler = requestHandler;
        }

        /// <summary>
        /// Sends a request to browse available API methods.
        /// This method retrieves a list of available API methods from the PLC.
        /// </summary>
        /// <returns>
        /// A task representing the asynchronous operation. The task result contains
        /// an IEnumerable of <see cref="ApiClass"/> representing the available API methods.
        /// Returns null if the operation fails.
        /// </returns>
        public async Task<IEnumerable<ApiClass>> BrowseMethodsAsync()
        {
            try
            {
                Log.Information("Browsing API methods.");

                // Send the API browse request
                var apiBrowseResponse = await _requestHandler.ApiBrowseAsync();

                // Log the number of methods retrieved
                var methodCount = apiBrowseResponse.Result.Count;
                Log.Information("Fetched {MethodCount} API methods.", methodCount);

                // Debug log to print each method
                foreach (var method in apiBrowseResponse.Result)
                {
                    Log.Debug("API Method: {MethodName}", method.Name);
                }

                // Return the result containing the API methods
                return apiBrowseResponse.Result;
            }
            catch (Exception ex)
            {
                // Log the error
                Log.Error(ex, "Error while browsing API methods.");
                return null; // Return null to indicate failure
            }
        }
    }
}
