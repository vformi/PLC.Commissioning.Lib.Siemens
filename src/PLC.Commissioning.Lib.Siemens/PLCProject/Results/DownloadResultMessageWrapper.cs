using Siemens.Engineering.Download;
using System;
using System.Collections.Generic;
using System.Linq;
using PLC.Commissioning.Lib.Siemens.PLCProject.Abstractions;

namespace PLC.Commissioning.Lib.Siemens.PLCProject.Results
{
    public class DownloadResultMessageWrapper : IResultMessage
    {
        private readonly DownloadResultMessage _message;

        public DownloadResultMessageWrapper(DownloadResultMessage message)
        {
            _message = message;
        }

        public DateTime DateTime => _message.DateTime;
        public string State => _message.State.ToString();
        public string Description => _message.Message;
        public IEnumerable<IResultMessage> Messages => _message.Messages.Select(m => new DownloadResultMessageWrapper(m));
    }
}
