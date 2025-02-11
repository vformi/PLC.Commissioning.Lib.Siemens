using Siemens.Engineering;
using System;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions
{
    /// <summary>
    /// Defines methods for managing interactions with the Siemens TIA Portal.
    /// </summary>
    public interface ISiemensManagerService : IDisposable
    {
        /// <summary>
        /// Starts a new instance of TIA Portal.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if TIA Portal cannot be started.</exception>
        void StartTIA();

        /// <summary>
        /// The current instance of the TIA Portal.
        /// </summary>
        TiaPortal TiaPortal { get; }
    }
}