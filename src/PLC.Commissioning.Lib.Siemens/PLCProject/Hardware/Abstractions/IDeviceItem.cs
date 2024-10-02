namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Abstractions
{
    /// <summary>
    /// Represents a device item in the GSD, providing access to parameter record data.
    /// </summary>
    public interface IDeviceItem
    {
        /// <summary>
        /// Gets the parameter record data item associated with the device.
        /// </summary>
        ParameterRecordDataItem ParameterRecordDataItem { get; }
    }
}
