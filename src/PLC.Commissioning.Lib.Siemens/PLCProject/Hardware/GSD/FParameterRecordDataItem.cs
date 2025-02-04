using System;
using System.Text;
using System.Xml;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;

/// <summary>
/// Represents the F-Parameter Record Data Item parsed from a GSD file. 
/// This class provides access to safety-related parameters required for PROFINET devices.
/// </summary>
public class FParameterRecordDataItem
{
    /// <summary>
    /// Gets the CRC of the F-Parameter Descriptor.
    /// </summary>
    public string F_ParamDescCRC { get; private set; }

    /// <summary>
    /// Gets the Safety Integrity Level (SIL) of the device.
    /// </summary>
    public string F_SIL { get; private set; }

    /// <summary>
    /// Gets the CRC length of the safety parameters.
    /// </summary>
    public string F_CRC_Length { get; private set; }

    /// <summary>
    /// Gets the block ID for the F-Parameter record.
    /// </summary>
    public string F_Block_ID { get; private set; }

    /// <summary>
    /// Gets the version of the F-Parameter.
    /// </summary>
    public string F_Par_Version { get; private set; }

    /// <summary>
    /// Gets the source address for safety communication.
    /// </summary>
    public string F_Source_Add { get; private set; }

    /// <summary>
    /// Gets the destination address for safety communication.
    /// </summary>
    public string F_Dest_Add { get; private set; }

    /// <summary>
    /// Gets the watchdog time for safety communication.
    /// </summary>
    public string F_WD_Time { get; private set; }

    /// <summary>
    /// Gets the CRC of the F-Parameter record.
    /// </summary>
    public string F_Par_CRC { get; private set; }

    /// <summary>
    /// The GSD handler used for processing F-Parameter Record Data Item data.
    /// </summary>
    private readonly GSDHandler _gsdHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="FParameterRecordDataItem"/> class with the specified GSD handler.
    /// </summary>
    /// <param name="gsdHandler">The GSD handler used for processing F-Parameter data.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="gsdHandler"/> is null.</exception>
    public FParameterRecordDataItem(GSDHandler gsdHandler)
    {
        _gsdHandler = gsdHandler ?? throw new ArgumentNullException(nameof(gsdHandler));
    }

    /// <summary>
    /// Parses an XML node representing an F-Parameter Record Data Item and populates the properties of this class.
    /// </summary>
    /// <param name="fParameterRecordDataItemNode">The XML node containing the F-Parameter Record Data Item data.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="fParameterRecordDataItemNode"/> is null.</exception>
    public void ParseFParameterRecordDataItem(XmlNode fParameterRecordDataItemNode)
    {
        if (fParameterRecordDataItemNode == null)
            throw new ArgumentNullException(nameof(fParameterRecordDataItemNode));

        // Parse specific attributes for F-Parameter Record Data Item
        F_ParamDescCRC = fParameterRecordDataItemNode.Attributes["F_ParamDescCRC"]?.Value;
        F_SIL = fParameterRecordDataItemNode.SelectSingleNode("gsd:F_SIL", _gsdHandler.nsmgr)?.Attributes["DefaultValue"]?.Value;
        F_CRC_Length = fParameterRecordDataItemNode.SelectSingleNode("gsd:F_CRC_Length", _gsdHandler.nsmgr)?.Attributes["DefaultValue"]?.Value;
        F_Block_ID = fParameterRecordDataItemNode.SelectSingleNode("gsd:F_Block_ID", _gsdHandler.nsmgr)?.Attributes["DefaultValue"]?.Value;
        F_Par_Version = fParameterRecordDataItemNode.SelectSingleNode("gsd:F_Par_Version", _gsdHandler.nsmgr)?.Attributes["DefaultValue"]?.Value;
        F_Source_Add = fParameterRecordDataItemNode.SelectSingleNode("gsd:F_Source_Add", _gsdHandler.nsmgr)?.Attributes["AllowedValues"]?.Value;
        F_Dest_Add = fParameterRecordDataItemNode.SelectSingleNode("gsd:F_Dest_Add", _gsdHandler.nsmgr)?.Attributes["AllowedValues"]?.Value;
        F_WD_Time = fParameterRecordDataItemNode.SelectSingleNode("gsd:F_WD_Time", _gsdHandler.nsmgr)?.Attributes["DefaultValue"]?.Value;
        F_Par_CRC = fParameterRecordDataItemNode.SelectSingleNode("gsd:F_Par_CRC", _gsdHandler.nsmgr)?.Attributes["DefaultValue"]?.Value;
    }

    /// <summary>
    /// Returns a string representation of the F-Parameter Record Data Item, including all parsed properties.
    /// </summary>
    /// <returns>A string describing the F-Parameter Record Data Item.</returns>
    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"F_ParamDescCRC: {F_ParamDescCRC}");
        sb.AppendLine($"F_SIL: {F_SIL}");
        sb.AppendLine($"F_CRC_Length: {F_CRC_Length}");
        sb.AppendLine($"F_Block_ID: {F_Block_ID}");
        sb.AppendLine($"F_Par_Version: {F_Par_Version}");
        sb.AppendLine($"F_Source_Add: {F_Source_Add}");
        sb.AppendLine($"F_Dest_Add: {F_Dest_Add}");
        sb.AppendLine($"F_WD_Time: {F_WD_Time}");
        sb.AppendLine($"F_Par_CRC: {F_Par_CRC}");

        return sb.ToString();
    }
}
