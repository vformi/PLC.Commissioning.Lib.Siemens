using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models;

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
    public Dictionary<string, FParameter> Parameters { get; private set; }
    
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
        Parameters = new Dictionary<string, FParameter>();
    }

    /// <summary>
    /// Parses an XML node representing an F-Parameter Record Data Item and populates the properties of this class.
    /// </summary>
    /// <param name="fParameterRecordDataItemNode">The XML node containing the F-Parameter Record Data Item data.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="fParameterRecordDataItemNode"/> is null.</exception>
    /// <summary>
    /// Parses an XML node representing an F-Parameter Record Data Item and ensures required attributes exist.
    /// </summary>
    /// <param name="fParameterRecordDataItemNode">The XML node containing the F-Parameter Record Data Item data.</param>
    public void ParseFParameterRecordDataItem(XmlNode fParameterRecordDataItemNode)
    {
        if (fParameterRecordDataItemNode == null)
            throw new ArgumentNullException(nameof(fParameterRecordDataItemNode));

        F_ParamDescCRC = fParameterRecordDataItemNode.Attributes["F_ParamDescCRC"]?.Value
                         ?? throw new InvalidOperationException("Missing F_ParamDescCRC attribute.");

        foreach (XmlNode child in fParameterRecordDataItemNode.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                var param = new FParameter(child.Name, child);
                Parameters[child.Name] = param;
            }
        }
    }
    
    public FParameter GetParameter(string name)
    {
        if (Parameters.TryGetValue(name, out var param))
            return param;
        throw new KeyNotFoundException($"F-Parameter '{name}' not found.");
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"F_ParamDescCRC: {F_ParamDescCRC}");
        foreach (var param in Parameters.Values)
        {
            sb.AppendLine(param.ToString());
        }
        return sb.ToString();
    }
}
