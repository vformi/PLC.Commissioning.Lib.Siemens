using PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions;
using PLC.Commissioning.Lib.Siemens.PLCProject.Results;
using Serilog;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using System;

namespace PLC.Commissioning.Lib.Siemens.PLCProject
{
    /// <summary>
    /// Implements the <see cref="ICompilerService"/> to compile Siemens PLC projects.
    /// </summary>
    public class CompilerService : ICompilerService
    {
        /// <inheritdoc />
        public bool CompileProject(DeviceItem cpu)
        {
            var device = cpu.Parent as Device;
            if (device is null)
            {
                throw new InvalidOperationException("The CPU device item is not part of a device.");
            }

            Log.Information($"Compiling device {device.Name}");

            CompilerResult compilerResult;
            try
            {
                ICompilable compileService = device.GetService<ICompilable>();
                if (compileService is null)
                {
                    throw new InvalidOperationException($"The device {device.Name} does not support compilation.");
                }

                compilerResult = compileService.Compile();
            }
            catch (EngineeringException ex)
            {
                throw new InvalidOperationException($"Compilation of {device.Name} failed.", ex);
            }

            var logger = new OperationLogger(new CompilerResultWrapper(compilerResult));
            logger.LogResults($"Compilation of {device.Name}");

            switch (compilerResult.State)
            {
                case CompilerResultState.Success:
                    Log.Information($"Program was successfully compiled - {compilerResult.State}. PLC has {compilerResult.WarningCount} warnings.{Environment.NewLine}");
                    break;
                case CompilerResultState.Information:
                    Log.Information($"Program compilation ended with information - {compilerResult.State}. PLC has {compilerResult.WarningCount} warnings.{Environment.NewLine}");
                    break;
                case CompilerResultState.Warning:
                    Log.Information($"Program compilation ended with warnings - {compilerResult.State}. PLC has {compilerResult.WarningCount} warnings.{Environment.NewLine}");
                    break;
                default:
                    Log.Information($"Program compilation was not successful - {compilerResult.State}. PLC has {compilerResult.ErrorCount} errors and {compilerResult.WarningCount} warnings.{Environment.NewLine}");
                    return false;
            }

            return true;
        }
    }
}
