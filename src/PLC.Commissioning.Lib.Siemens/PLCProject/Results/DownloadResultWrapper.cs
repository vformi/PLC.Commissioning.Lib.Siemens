using Siemens.Engineering.Download;
using System.Collections.Generic;
using System.Linq;
using PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Results
{
    public class DownloadResultWrapper : IResult
    {
        private readonly DownloadResult _downloadResult;

        public DownloadResultWrapper(DownloadResult downloadResult)
        {
            _downloadResult = downloadResult;
        }

        public string State => _downloadResult.State.ToString();
        public int WarningCount => _downloadResult.WarningCount;
        public int ErrorCount => _downloadResult.ErrorCount;
        public IEnumerable<IResultMessage> Messages => _downloadResult.Messages.Select(m => new DownloadResultMessageWrapper(m));
    }
}
