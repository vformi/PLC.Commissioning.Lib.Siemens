# PLC.Commissioning.Lib.Siemens

**PLC.Commissioning.Lib.Siemens** provides a Siemens-specific implementation for PLC commissioning, extending the interfaces defined in [PLC.Commissioning.Lib.Abstractions](https://github.com/vformi/PLC.Commissioning.Lib.Abstractions). This repository is part of a modular structure designed to support automated testing of PROFINET sensors.

> **Note**: This repository is a submodule of the main [PLC.Commissioning.Lib](https://github.com/vformi/PLC.Commissioning.Lib) project. It is intended to be used as part of the main project and not independently.

# Supported languages
- .NET Standard 2.0

### Example usage
```csharp
using System;
using System.Collections.Generic;
using PLC.Commissioning.Lib.Abstractions;
using PLC.Commissioning.Lib.Enums;
using Siemens.Engineering.Download;

namespace PLC.Commissioning.Lib.App
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Configure logging (console + file, debug level)
                PLCFactory.ConfigureLogger(
                    pythonCallback: null,
                    writeToConsole: true,
                    writeToFile: true,
                    logLevel: LogLevel.Debug);

                // Create and use a Siemens PLC controller
                using var plc = PLCFactory.CreateController<IPLCControllerSiemens>(Manufacturer.Siemens);
                {
                    // 1. Load PLC configuration from a JSON file
                    plc.Configure("configuration.json");

                    // 2. Initialize the PLC with safety features enabled
                    plc.Initialize(safety: true);

                    // 3. Import PROFINET devices with GSDML files
                    var gsdmlFiles = new List<string>
                    {
                        "example_gsdml_file.xml"
                    };
                    var devicesResult = plc.ImportDevices("example_aml_file.aml", gsdmlFiles);
                    if (devicesResult.IsFailed)
                    {
                        Console.WriteLine($"Import failed: {devicesResult.Errors[0].Message}");
                        return;
                    }
                    var device = devicesResult.Value.First().Value;

                    // 4. Set device parameters (e.g., barcode scanner settings)
                    var parametersToSet = new Dictionary<string, object>
                    {
                        { "Code type 1", "2/5 Interleaved" },
                        { "Number of digits 1", 10 }
                    };
                    var setParamsResult = plc.SetDeviceParameters(device, "DAP", parametersToSet);
                    if (setParamsResult.IsFailed)
                    {
                        Console.WriteLine($"Set parameters failed: {setParamsResult.Errors[0].Message}");
                        return;
                    }

                    // 5. Compile and download the configuration to the PLC
                    plc.Compile();
                    plc.Download(DownloadOptions.Hardware | DownloadOptions.Software);

                    Console.WriteLine("PLC commissioning completed successfully!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during commissioning: {ex.Message}");
            }
        }
    }
}
```