using System.Collections.Generic;
using System.Xml;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models
{
    public class FParameter
    {
        public string Name { get; }
        public string DefaultValue { get; }
        public string AllowedValues { get; }
        public string Changeable { get; }
        public string Visible { get; }

        public FParameter(string name, XmlNode node)
        {
            Name = name;
            DefaultValue = node.Attributes["DefaultValue"]?.Value;
            AllowedValues = node.Attributes["AllowedValues"]?.Value;
            Changeable = node.Attributes["Changeable"]?.Value;
            Visible = node.Attributes["Visible"]?.Value;
        }

        public override string ToString()
        {
            var parts = new List<string> { $"{Name}:" };

            if (!string.IsNullOrEmpty(DefaultValue))
                parts.Add($"Default={DefaultValue}");

            if (!string.IsNullOrEmpty(AllowedValues))
                parts.Add($"Allowed={AllowedValues}");

            if (!string.IsNullOrEmpty(Changeable))
                parts.Add($"Changeable={Changeable}");

            if (!string.IsNullOrEmpty(Visible))
                parts.Add($"Visible={Visible}");

            return string.Join(", ", parts);
        }
    }
}