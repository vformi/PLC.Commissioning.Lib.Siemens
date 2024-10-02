using System;
using System.Collections.Generic;
using System.Text;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Enums
{
    /// <summary>
    /// Represents the result of a network configuration operation.
    /// </summary>
    public enum NetworkConfigurationResult
    {
        Success,
        ModeNotFound,
        PcInterfaceNotFound,
        TargetInterfaceNotFound,
        UnexpectedError
    }
}
