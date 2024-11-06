# PLC.Commissioning.Lib.Siemens

**PLC.Commissioning.Lib.Siemens** provides a Siemens-specific implementation for PLC commissioning, extending the interfaces defined in [PLC.Commissioning.Lib.Abstractions](https://github.com/vformi/PLC.Commissioning.Lib.Abstractions). This repository is part of a modular structure designed to support automated testing of PROFINET sensors.

> **Note**: This repository is a submodule of the main [PLC.Commissioning.Lib](https://github.com/vformi/PLC.Commissioning.Lib) project. It is intended to be used as part of the main project and not independently.

# Supported languages
- .NET Standard 2.0

### Example usage
```csharp
Dictionary<string, object> parametersToSet = new Dictionary<string, object>
{
    {"Mode", "With ACK"},
};

// Begin PLC commissioning
using (var plc = PLCFactory.CreateController<IPLCControllerSiemens>(Manufacturer.Siemens))
{
    // Step 1: Load PLC configuration
    plc.Configure("configuration.json");
    // Step 2: Initialize the PLC without safety features
    plc.Initialize(safety: false);
    // Step 3: Import the device configuration into the PLC
    var device = plc.ImportDevice("device.aml");
    // Step 4: Retrieve specific device parameters for verification
    plc.GetDeviceParameters(device, gsdPath, "[M10] Activation");
    // Step 5: Apply specific parameters to the device
    plc.SetDeviceParameters(device, gsdPath, "[M10] Activation", parametersToSet);
    // Step 6: Compile the configuration to prepare for download
    plc.Compile();
    // Step 7: Download the configuration to the PLC with specified options
    plc.Download(downloadOptions);
    // Step 8: Start the PLC to finalize commissioning
    plc.Start();
}
```