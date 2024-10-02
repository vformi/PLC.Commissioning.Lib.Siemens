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
        /// Starts the TIA Portal with the configured mode (with or without a user interface).
        /// </summary>
        void StartTIA();

        /// <summary>
        /// Gets the current instance of the TIA Portal.
        /// </summary>
        /// <returns>The <see cref="TiaPortal"/> instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the TIA Portal is not started.</exception>
        TiaPortal GetTiaPortal();
    }
}
