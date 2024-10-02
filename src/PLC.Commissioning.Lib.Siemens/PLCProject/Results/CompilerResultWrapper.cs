using Siemens.Engineering.Compiler;
using System.Collections.Generic;
using System.Linq;
using PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Results
{
    public class CompilerResultWrapper : IResult
    {
        private readonly CompilerResult _compilerResult;

        public CompilerResultWrapper(CompilerResult compilerResult)
        {
            _compilerResult = compilerResult;
        }

        public string State => _compilerResult.State.ToString();
        public int WarningCount => _compilerResult.WarningCount;
        public int ErrorCount => _compilerResult.ErrorCount;
        public IEnumerable<IResultMessage> Messages => _compilerResult.Messages.Select(m => new CompilerResultMessageWrapper(m));
    }
}
