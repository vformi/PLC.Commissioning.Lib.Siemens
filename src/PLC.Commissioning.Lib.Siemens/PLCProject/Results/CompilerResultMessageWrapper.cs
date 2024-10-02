using Siemens.Engineering.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions;

// according to the docs, the compile has more returns, hence the easiest way was to implement separate files
namespace PLC.Commissioning.Lib.Siemens.PLCProject.Results
{
    public class CompilerResultMessageWrapper : IResultMessage
    {
        private readonly CompilerResultMessage _message;

        public CompilerResultMessageWrapper(CompilerResultMessage message)
        {
            _message = message;
        }

        public DateTime DateTime => _message.DateTime;
        public string State => _message.State.ToString();
        public string Description => _message.Description;
        public string Path => _message.Path;
        public int WarningCount => _message.WarningCount;
        public int ErrorCount => _message.ErrorCount;
        public IEnumerable<IResultMessage> Messages => _message.Messages.Select(m => new CompilerResultMessageWrapper(m));
    }
}
