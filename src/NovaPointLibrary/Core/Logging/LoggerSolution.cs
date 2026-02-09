using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Core.SQLite;
using NovaPointLibrary.Solutions;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;


namespace NovaPointLibrary.Core.Logging
{
    internal class LoggerSolution : ILogger
    {
        public Action<LogInfo> UiAddLog {  get; init; }

        private readonly string _threadCode = "0";
        private int _childThreadCounter = 0;

        private static readonly SemaphoreSlim _semaphoreThreadLogger = new(1, 1);

        private readonly List<SolutionLog> _cachedKeyValues = new();
        private readonly List<SolutionLog> _cachedLogs = new();

        private readonly string _solutionName;
        internal readonly string _solutionFolderPath;
        internal readonly string _solutionFileName;

        private  Dictionary<Type, string>? _solutionReports = null;
        private readonly SqliteHandler _sql = SqliteHandler.GetCacheHandler();

        private readonly Stopwatch SW = new();

        private readonly string _txtPath;
        static readonly ReaderWriterLockSlim txtRWL = new();


        // TO RETIRE
        private readonly string _csvPath;
        static readonly ReaderWriterLockSlim csvRWL = new();

        internal LoggerSolution(Action<LogInfo> uiAddLog, string solutionName, ISolutionParameters parameters)
        {
            UiAddLog = uiAddLog;
            _solutionName = solutionName;

            string userDocumentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _solutionFolderPath = Path.Combine(userDocumentsFolder, "NovaPoint", _solutionName, DateTime.UtcNow.ToString("yyMMddHHmmss"));
            Directory.CreateDirectory(_solutionFolderPath);

            _solutionFileName = _solutionName + "_" + DateTime.UtcNow.ToString("yyMMddHHmmss");

            _txtPath = Path.Combine(_solutionFolderPath, _solutionFileName + "_Logs.txt");
            _csvPath = Path.Combine(_solutionFolderPath, _solutionFileName + "_Report.csv");

            Info(GetType().Name, $"Solution folder: {_solutionFolderPath}");

            SolutionLog logEntry = new("Info", _threadCode, GetType().Name, $"Version: v{VersionControl.GetVersion()}");
            WriteLog(logEntry);
            _cachedKeyValues.Add(logEntry);

            GetSolutionParameters(parameters);

            SW.Start();

            UI(GetType().Name, $"Solution has started, please wait to the end");
        }

        internal void AddSolutionReports(Dictionary<Type, string> dicSolutions)
        {
            _solutionReports = dicSolutions;

            ResetCache();
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

        private void HistoryLog(SolutionLog log)
        {
            _cachedLogs.Add(log);

            while (_cachedLogs.Count > 20)
            {
                _cachedLogs.RemoveAt(0);
            }
        }

        public void Info(string classMethod, string log)
        {
            SolutionLog logEntry = new("Info", _threadCode, classMethod, log);
            HistoryLog(logEntry);

            WriteLog(logEntry);
        }

        public void Debug(string classMethod, string log)
        {
            SolutionLog logEntry = new("Debug", _threadCode, classMethod, log);
            HistoryLog(logEntry);

            WriteLog(logEntry);
        }

        public void UI(string classMethod, string log)
        {
            Info(classMethod, log);

            UiAddLog(LogInfo.TextNotification(log));
        }

        public void Progress(double progress)
        {
            if (progress < 0.01) { return; }

            if (progress > 99.99) { progress = 99.99; }

            TimeSpan timeSpan = TimeSpan.FromMilliseconds((SW.Elapsed.TotalMilliseconds * 100 / progress - SW.Elapsed.TotalMilliseconds));

            UiAddLog(LogInfo.ProgressUpdate(progress, timeSpan));
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
            if (ex != null)
            {
                Error(_solutionName, "Solution", _solutionName, ex);
                UiAddLog(LogInfo.ErrorNotification($"Exception: {ex.Message}"));
                UiAddLog(LogInfo.ErrorNotification($"StackTrace: {ex.StackTrace}"));
                UiAddLog(LogInfo.ErrorNotification($"COMPLETED: Solution has finished with errors!"));
            }
            else
            {
                UI(GetType().Name, $"COMPLETED: Solution has finished correctly!");
            }

            SW.Stop();
            TimeSpan timeSpan = TimeSpan.FromMilliseconds((SW.Elapsed.TotalMilliseconds * 100 / 100 - SW.Elapsed.TotalMilliseconds));
            UiAddLog(LogInfo.ProgressUpdate(100, timeSpan));
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
            txtRWL.TryEnterWriteLock(3000);
            try
            {
                var fileStreamer = new FileStream(_txtPath, FileMode.Append, FileAccess.Write);
                using StreamWriter streamWriter = new(fileStreamer);

                streamWriter.WriteLine(log.GetLogEntry());
            }
            finally { txtRWL.ExitWriteLock(); }
        }


        // RECORD PROPERTIES
        private void GetSolutionParameters(ISolutionParameters parameters)
        {
            LogProperty($"Solution parameters");
            LogProperty($"========== ========== ==========");

            GetProperties(parameters);

            LogProperty($"========== ========== ==========");
        }

        private void GetProperties(ISolutionParameters parameters)
        {
            Type type = parameters.GetType();
            PropertyInfo[] collPropertyInfo = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var propertyInfo in collPropertyInfo)
            {
                var oProperty = propertyInfo.GetValue(parameters);

                if (oProperty != null)
                {
                    if (typeof(ISolutionParameters).IsAssignableFrom(oProperty.GetType()))
                    {
                        GetProperties((ISolutionParameters)oProperty);
                    }
                    else
                    {
                        LogProperty($"{propertyInfo.Name}: {oProperty}");
                    }
                }
            }

            parameters.ParametersCheck();
        }

        private void LogProperty(string property)
        {
            SolutionLog logEntry = new("Info", _threadCode, GetType().Name, property);
            _cachedKeyValues.Add(logEntry);


            WriteLog(logEntry);

            // Keep for using during testing
            //_uiAddLog(LogInfo.TextNotification(property));
        }


        // SOLUTION FINISH
        //internal void SolutionFinish()
        //{
        //    SolutionFinishNotice();
        //    UI(GetType().Name, $"COMPLETED: Solution has finished correctly!");
        //}

        internal void SolutionFinish(Exception? ex = null)
        {
            Info(GetType().Name, "Finishing solution");
            SolutionFinishNotice();

            if (ex != null)
            {
                Error(_solutionName, "Solution", _solutionName, ex);
                UiAddLog(LogInfo.ErrorNotification($"Exception: {ex.Message}"));
                UiAddLog(LogInfo.ErrorNotification($"StackTrace: {ex.StackTrace}"));
                UiAddLog(LogInfo.ErrorNotification($"COMPLETED: Solution has finished with errors!"));
            }
            else
            {
                UI(GetType().Name, $"COMPLETED: Solution has finished correctly!");
            }

            SW.Stop();
        }

        private void SolutionFinishNotice()
        {
            ExportAllReports();

            ClearCache();
        }



        // SQL MANAGEMENT
        private void ResetCache()
        {
            if (_solutionReports == null) { return; }

            foreach (var key in _solutionReports.Keys)
            {
                _sql.ResetTable(this, key);
            }
        }

        public void WriteRecord<T>(T record)
        {
            _sql.InsertValue(this, record);
        }

        private void ClearCache()
        {
            if (_solutionReports == null) { return; }

            foreach (var key in _solutionReports.Keys)
            {
                _sql.DropTable(this, key);
            }
        }

        private void ExportAllReports()
        {
            if (_solutionReports == null) { return; }

            Info(GetType().Name, "Exporting all reports");

            foreach (var entry in _solutionReports)
            {
                var type = entry.Key;
                var reportName = entry.Value;

                var method = typeof(LoggerSolution).GetMethod(nameof(ExportReportToCsv), BindingFlags.NonPublic | BindingFlags.Instance);
                var genericMethod = method.MakeGenericMethod(type);
                genericMethod.Invoke(this, new object[] { reportName });
            }
        }

        private void ExportReportToCsv<ISolutionRecord>(string reportName)
        {
            Info(GetType().Name, $"Exporting report {reportName}");

            string reportPath = Path.Combine(_solutionFolderPath, _solutionFileName + $"_{reportName}.csv");

            foreach (var record in _sql.GetAllRecords<ISolutionRecord>(this))
            {
                Type solutionType = typeof(ISolutionRecord);
                PropertyInfo[] properties = solutionType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                StringBuilder sb = new();
                using StreamWriter csv = new(new FileStream(reportPath, FileMode.Append, FileAccess.Write));
                {
                    var csvFileLenth = new System.IO.FileInfo(reportPath).Length;
                    if (csvFileLenth == 0)
                    {
                        foreach (var propertyInfo in properties)
                        {
                            sb.Append($"\"{propertyInfo.Name}\",");
                        }
                        if (sb.Length > 0) { sb.Length--; }

                        csv.WriteLine(sb.ToString());
                        sb.Clear();
                    }

                    foreach (var propertyInfo in properties)
                    {
                        string s = $"{propertyInfo.GetValue(record)}";
                        sb.Append($"\"{s.Replace("\"", "'")}\",");
                    }
                    if (sb.Length > 0) { sb.Length--; }
                    string output = Regex.Replace(sb.ToString(), @"\r\n?|\n", "");

                    csv.WriteLine(sb.ToString());
                }
            }
        }


        // TO RETIRE
        internal void DynamicCSV(dynamic o)
        {
            try
            {
                csvRWL.TryEnterWriteLock(3000);
                try
                {
                    StringBuilder sb = new();
                    using StreamWriter csv = new(new FileStream(_csvPath, FileMode.Append, FileAccess.Write));
                    {
                        var csvFileLenth = new System.IO.FileInfo(_csvPath).Length;
                        if (csvFileLenth == 0)
                        {
                            // https://learn.microsoft.com/en-us/dotnet/api/system.dynamic.expandoobject?redirectedfrom=MSDN&view=net-7.0#enumerating-and-deleting-members
                            foreach (var property in (IDictionary<String, Object>)o)
                            {
                                sb.Append($"\"{property.Key}\",");
                            }
                            if (sb.Length > 0) { sb.Length--; }
                            csv.WriteLine(sb.ToString());
                            sb.Clear();
                        }

                        foreach (var property in (IDictionary<String, Object>)o)
                        {
                            sb.Append($"\"{property.Value}\",");
                        }
                        if (sb.Length > 0) { sb.Length--; }

                        csv.WriteLine(sb.ToString());
                    }
                }
                finally { csvRWL.ExitWriteLock(); }
            }
            catch (Exception ex)
            {
                Error(GetType().Name, "Solution", _solutionName, ex);
            }
        }

        internal void RecordCSV(ISolutionRecord record)
        {
            try
            {
                csvRWL.TryEnterWriteLock(3000);
                try
                {
                    Type solutiontype = record.GetType();
                    PropertyInfo[] properties = solutiontype.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);

                    StringBuilder sb = new();
                    using StreamWriter csv = new(new FileStream(_csvPath, FileMode.Append, FileAccess.Write));
                    {
                        var csvFileLenth = new System.IO.FileInfo(_csvPath).Length;
                        if (csvFileLenth == 0)
                        {
                            foreach (var propertyInfo in properties)
                            {
                                sb.Append($"\"{propertyInfo.Name}\",");
                            }
                            if (sb.Length > 0) { sb.Length--; }

                            csv.WriteLine(sb.ToString());
                            sb.Clear();
                        }

                        foreach (var propertyInfo in properties)
                        {
                            string s = $"{propertyInfo.GetValue(record)}";
                            sb.Append($"\"{s.Replace("\"", "'")}\",");
                        }
                        if (sb.Length > 0) { sb.Length--; }
                        string output = Regex.Replace(sb.ToString(), @"\r\n?|\n", "");

                        csv.WriteLine(sb.ToString());
                    }
                }
                finally { csvRWL.ExitWriteLock(); }
            }
            catch (Exception ex)
            {
                Error(GetType().Name, "Solution", _solutionName, ex);
            }
        }

    }

}
