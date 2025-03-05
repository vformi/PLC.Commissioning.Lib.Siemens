using System.Collections.Generic;
using Siemens.Engineering.HW.Features;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Abstractions
{
    public interface ISafetyParameterHandler
    {
        Dictionary<string, object> GetSafetyModuleData(GsdDeviceItem gsdDeviceItem, List<string> parameterSelections = null);
        bool SetSafetyModuleData(GsdDeviceItem gsdDeviceItem, Dictionary<string, object> parameterValues);
        Dictionary<string, object> HandleSafetyParameters(GsdDeviceItem module, List<string> parameterSelections);
    }
}