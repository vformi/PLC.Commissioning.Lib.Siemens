using Siemens.Engineering.HW;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions
{
    /// <summary>
    /// Defines a service for compiling a Siemens PLC project.
    /// </summary>
    public interface ICompilerService
    {
        /// <summary>
        /// Compiles the project associated with the specified CPU device item.
        /// </summary>
        /// <param name="cpu">The CPU <see cref="DeviceItem"/> to be compiled.</param>
        /// <returns><c>true</c> if the compilation was successful; otherwise, <c>false</c>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the CPU device item is not part of a device or if the device does not support compilation.
        /// </exception>
        bool CompileProject(DeviceItem cpu);
    }
}
