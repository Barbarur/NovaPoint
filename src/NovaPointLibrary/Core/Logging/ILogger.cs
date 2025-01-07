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
        public Action<LogInfo> UiAddLog { get; init; }

        public Task<ILogger> GetSubThreadLogger();
        public void Info(string classMethod, string log);
        public void Debug(string classMethod, string log);
        public void UI(string classMethod, string log);
        public void Progress(double progress);
        public void Error(string classMethod, string type, string URL, Exception ex);

        public void WriteLog(SolutionLog log);

    }
}
