

using PnP.Framework.Diagnostics;
using System.IO;

namespace NovaPointLibrary.Core.Logging
{
    public class SolutionLog
    {
        internal string TimeStamp;
        internal string LogType;
        internal string ThreadCode;
        internal string ClassMethod;
        internal string Message;

        internal SolutionLog(string logType, string threadCode, string classMethod, string log)
        {
            TimeStamp = $"{DateTime.UtcNow:yyyy/MM/dd HH:mm:ss}";
            LogType = logType;
            ThreadCode = threadCode;
            ClassMethod = classMethod;
            Message = $"\"{log.Replace("\"", "'")}\"";
        }

        internal string GetLogEntry()
        {
            return $"[{TimeStamp}] , [{LogType}] , [{ThreadCode}] , [{ClassMethod}] , {Message}";
        }
    }
}
