using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Core.Logging
{
    internal class LoggerThread : ILogger
    {
        private readonly ILogger _parentLogger;
        public Action<LogInfo> _uiAddLog {  get; init; }
        private readonly string _threadCode;
        private int _childThreadCounter = 0;

        private static readonly SemaphoreSlim _semaphoreThreadLogger = new(1, 1);

        internal readonly List<string> _cachedLogs = new();

        internal LoggerThread(ILogger parentLogger, string threadCode)
        {
            _parentLogger = parentLogger;
            _uiAddLog = parentLogger._uiAddLog;
            _threadCode = threadCode;
        }

        public async Task<ILogger> GetSubThreadLogger()
        {
            LoggerThread childThread;
            await _semaphoreThreadLogger.WaitAsync();
            try
            {
                childThread = new(this, _threadCode + "." + _childThreadCounter);
                _childThreadCounter++;
            }
            finally
            {
                _semaphoreThreadLogger.Release();
            }
            return childThread;
        }

        private string FormatLogEntry(string classMethod, string log)
        {
            return $"{DateTime.UtcNow:yyyy/MM/dd HH:mm:ss} [{_threadCode}] - [{classMethod}] - {log}";
        }

        private void CacheLog(string log)
        {
            _cachedLogs.Add(log);

            while (_cachedLogs.Count > 20)
            {
                _cachedLogs.RemoveAt(0);
            }
        }

        public void Info(string classMethod, string log)
        {
            string logEntry = FormatLogEntry(classMethod, log);

            CacheLog(logEntry);

            WriteFile(new List<string>() { logEntry });
        }

        public void Debug(string classMethod, string log)
        {
            return;
            //string logEntry = FormatLogEntry(classMethod, log);

            //CacheLog(logEntry);

            //WriteFile(new List<string>() { logEntry });
        }

        public void UI(string classMethod, string log)
        {
            Info(classMethod, log);

            _uiAddLog(LogInfo.TextNotification(log));
        }

        public void Progress(double progress)
        {
            _parentLogger.Progress(progress);
        }

        public void Error(string classMethod, string type, string URL, Exception ex)
        {
            Guid correlationID = Guid.NewGuid();

            List<string> infoLogs = new();
            List<string> errorLogs = new()
            {
                string.Empty,
                string.Empty,
                string.Empty,
                FormatLogEntry(classMethod, $"========== {correlationID} ==========")
            };

            errorLogs.AddRange(_cachedLogs);

            string logEntry = FormatLogEntry(classMethod, $"Error processing {type} '{URL}'");
            errorLogs.Add(logEntry);
            infoLogs.Add(logEntry);

            logEntry = FormatLogEntry(classMethod, $"Correlation ID: {correlationID}");
            errorLogs.Add(logEntry);
            infoLogs.Add(logEntry);

            logEntry = FormatLogEntry(classMethod, $"Exception: {ex.Message}");
            errorLogs.Add(logEntry);
            infoLogs.Add(logEntry);


            logEntry = FormatLogEntry(classMethod, $"Trace: {ex.StackTrace}");
            errorLogs.Add(logEntry);
            infoLogs.Add(logEntry);

            WriteFileError(errorLogs);
            WriteFile(infoLogs);

            _uiAddLog(LogInfo.ErrorNotification($"Error processing {type} '{URL}'."));
        }

        public void WriteFile(List<string> logs)
        {
            _parentLogger.WriteFile(logs);
        }

        public void WriteFileError(List<string> logs)
        {
            _parentLogger.WriteFileError(logs);
        }

    }
}
