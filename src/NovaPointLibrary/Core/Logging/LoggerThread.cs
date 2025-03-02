using NovaPointLibrary.Solutions;


namespace NovaPointLibrary.Core.Logging
{
    internal class LoggerThread : ILogger
    {
        public Action<LogInfo> UiAddLog { get; init; }

        private readonly ILogger _parentLogger;
        private readonly string _threadCode;
        private int _childThreadCounter = 0;

        private static readonly SemaphoreSlim _semaphoreThreadLogger = new(1, 1);

        public LoggerThread(ILogger parentLogger, string threadCode)
        {
            _parentLogger = parentLogger;
            UiAddLog = parentLogger.UiAddLog;
            _threadCode = threadCode;

        }

        public async Task<ILogger> GetSubThreadLogger()
        {
            await _semaphoreThreadLogger.WaitAsync();
            try
            {
                LoggerThread childThread = new(this, _threadCode + "." + _childThreadCounter);
                _childThreadCounter++;
                return childThread;
            }
            finally
            {
                _semaphoreThreadLogger.Release();
            }
        }

        public void Info(string classMethod, string log)
        {
            SolutionLog logEntry = new("Info", _threadCode, classMethod, log);

            WriteLog(logEntry);
        }

        public void Debug(string classMethod, string log)
        {
            SolutionLog logEntry = new("Debug", _threadCode, classMethod, log);

            WriteLog(logEntry);
        }

        public void UI(string classMethod, string log)
        {
            Info(classMethod, log);

            UiAddLog(LogInfo.TextNotification(log));
        }

        public void Progress(double progress)
        {
            _parentLogger.Progress(progress);
        }

        public void Error(string classMethod, string type, string URL, Exception ex)
        {
            List<SolutionLog> infoLogs = new()
            {
                new("Error", _threadCode, classMethod, $"Error processing {type} '{URL}'"),
                new("Error", _threadCode, classMethod, $"Exception: {ex.Message}"),
                new("Error", _threadCode, classMethod, $"Trace: {ex.StackTrace}"),
            };

            WriteLog(infoLogs);

            UiAddLog(LogInfo.ErrorNotification($"Error processing {type} '{URL}'."));
        }

        public void End(Exception? ex = null)
        {
            throw new("End method shouldn't be used on second thread logger");
        }

        private void WriteLog(List<SolutionLog> collRecord)
        {
            foreach (var record in collRecord)
            {
                WriteLog(record);
            }
        }

        public void WriteLog(SolutionLog log)
        {
            _parentLogger.WriteLog(log);
        }

        public void WriteRecord<T>(T record)
        {
            _parentLogger.WriteRecord(record);
        }

    }
}
