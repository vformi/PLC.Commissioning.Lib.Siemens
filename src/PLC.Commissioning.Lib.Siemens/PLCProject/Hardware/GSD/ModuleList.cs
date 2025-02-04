using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD
{
    /// <summary>
    /// Manages a list of modules, parsing and storing them properly.
    /// </summary>
    public class ModuleList
    {
        /// <summary>
        /// Gets or sets the list of module items.
        /// </summary>
        public List<ModuleItem> ModuleItems { get; set; } = new List<ModuleItem>();

        /// <summary>
        /// The GSD handler used for processing module data.
        /// </summary>
        private readonly GSDHandler _gsdHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleList"/> class with the specified GSD handler.
        /// </summary>
        /// <param name="gsdHandler">The GSD handler used to parse the modules.</param>
        public ModuleList(GSDHandler gsdHandler)
        {
            _gsdHandler = gsdHandler ?? throw new ArgumentNullException(nameof(gsdHandler));
            ParseModuleItems();
        }

        /// <summary>
        /// Parses the module items from the GSD file and adds them to the list.
        /// </summary>
        private void ParseModuleItems()
        {
            XmlNodeList moduleItemNodes = _gsdHandler.xmlDoc.SelectNodes("//gsd:ModuleList/gsd:ModuleItem", _gsdHandler.nsmgr);
            foreach (XmlNode moduleItemNode in moduleItemNodes)
            {
                var moduleItem = new ModuleItem(_gsdHandler);
                moduleItem.ParseModuleItem(moduleItemNode);
                ModuleItems.Add(moduleItem);
            }
        }

        /// <summary>
        /// Retrieves a module item by its name and checks if it has changeable parameters or F parameters.
        /// </summary>
        /// <param name="name">The name of the module item to retrieve.</param>
        /// <returns>A tuple containing the <see cref="ModuleItem"/> and a boolean indicating if it has changeable parameters.</returns>
        public (ModuleItem moduleItem, bool hasChangeableParameters) GetModuleItemByName(string name)
        {
            ModuleItem moduleItem = ModuleItems.Find(item => item.Model.Name == name);

            if (moduleItem != null)
            {
                bool hasChangeableParameters = 
                    moduleItem.Model.ParameterRecordDataItem != null || 
                    moduleItem.Model.FParameterRecordDataItem != null;

                return (moduleItem, hasChangeableParameters);
            }

            return (null, false);
        }


        /// <summary>
        /// ToString method of ModuleList class 
        /// </summary>
        /// <returns>A string that represents all module items.</returns>
        public override string ToString()
        {
            Log.Information("Module List:");

            var sb = new StringBuilder();

            foreach (var item in ModuleItems)
            {
                sb.AppendLine(item.ToString());
            }

            return sb.ToString();
        }
    }
}
