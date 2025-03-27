using System.Collections.Generic;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Abstractions;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models;
using Siemens.Engineering.HW.Features;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.Abstractions
{
    public interface IParameterHandler
    {
        List<ParsedValueModel> GetModuleData(GsdDeviceItem gsdDeviceItem, List<string> parameterSelections = null);
        bool SetModuleData(GsdDeviceItem gsdDeviceItem, Dictionary<string, object> parameterValues);
        Dictionary<string, object> HandleRegularParameters(GsdDeviceItem module, List<string> parameterSelections = null);
    }
}