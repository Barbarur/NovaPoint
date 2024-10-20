using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Core.Logging
{
    internal interface ILogger
    {
        internal Action<LogInfo> _uiAddLog { get; init; }

        public Task<ILogger> GetSubThreadLogger();
        internal void Info(string classMethod, string log);
        internal void Debug(string classMethod, string log);
        internal void UI(string classMethod, string log);
        internal void Progress(double progress);
        internal void Error(string classMethod, string type, string URL, Exception ex);
        internal void WriteFile(List<string> logs);
        internal void WriteFileError(List<string> logs);
    }
}
