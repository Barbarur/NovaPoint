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
        public Action<LogInfo> _uiAddLog {  get; init; }

        private readonly string _threadCode = "0";
        private int _childThreadCounter = 0;

        private static readonly SemaphoreSlim _semaphoreThreadLogger = new(1, 1);

        private readonly string _txtPath;
        private readonly string _csvPath;
        private readonly string _errorPath;

        private readonly Stopwatch SW = new();

        static readonly ReaderWriterLockSlim txtRWL = new();
        static readonly ReaderWriterLockSlim csvRWL = new();
        static readonly ReaderWriterLockSlim errorRWL = new();

        private readonly List<string> _cachedKeyValues = new();
        private readonly List<string> _cachedLogs = new();

        private readonly string _solutionFolderPath;
        private readonly string _solutionFileName;

        private readonly SqliteHandler _sql;

        internal LoggerSolution(Action<LogInfo> uiAddLog, string solutionName, ISolutionParameters parameters)
        {
            _uiAddLog = uiAddLog;

            string userDocumentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _solutionFolderPath = Path.Combine(userDocumentsFolder, "NovaPoint", solutionName, DateTime.UtcNow.ToString("yyMMddHHmmss"));
            Directory.CreateDirectory(_solutionFolderPath);

            _solutionFileName = solutionName + "_" + DateTime.UtcNow.ToString("yyMMddHHmmss");
            _txtPath = Path.Combine(_solutionFolderPath, _solutionFileName + "_Logs.txt");
            _csvPath = Path.Combine(_solutionFolderPath, _solutionFileName + "_Report.csv");
            _errorPath = Path.Combine(_solutionFolderPath, _solutionFileName + "_errors.txt");

            Info(GetType().Name, $"Logs: {_txtPath}");
            Info(GetType().Name, $"Report: {_csvPath}");
            _uiAddLog(LogInfo.FolderInfo(_solutionFolderPath));

            string v = $"Version: v{VersionControl.GetVersion()}";
            Info(GetType().Name, v);
            _cachedKeyValues.Add(v);

            GetSolutionParameters(parameters);

            _sql = SqliteHandler.GetCacheHandler();

            SW.Start();

            UI(GetType().Name, $"Solution has started, please wait to the end");
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
            string logEntry = FormatLogEntry(classMethod, log);

            CacheLog(logEntry);

            WriteFile(new List<string>() { logEntry });
        }

        public void UI(string classMethod, string log)
        {
            Info(classMethod, log);

            _uiAddLog(LogInfo.TextNotification(log));
        }

        public void Progress(double progress)
        {
            if (progress < 0.01) { return; }

            TimeSpan timeSpan = TimeSpan.FromMilliseconds((SW.Elapsed.TotalMilliseconds * 100 / progress - SW.Elapsed.TotalMilliseconds));

            _uiAddLog(LogInfo.ProgressUpdate(progress, timeSpan));
        }

        public void Error(string classMethod, string type, string URL, Exception ex)
        {
            Guid correlationID = Guid.NewGuid();

            List<string> txtLogs = new();
            List<string> errorLogs = new()
            {
                string.Empty,
                string.Empty,
                string.Empty,
                FormatLogEntry(classMethod, $"========== {correlationID} ==========")
            };

            errorLogs.AddRange(_cachedLogs);

            string logLine = FormatLogEntry(classMethod, $"Error processing {type} '{URL}'");
            errorLogs.Add(logLine);
            txtLogs.Add(logLine);

            logLine = FormatLogEntry(classMethod, $"Correlation ID: {correlationID}");
            errorLogs.Add(logLine);
            txtLogs.Add(logLine);

            logLine = FormatLogEntry(classMethod, $"Exception: {ex.Message}");
            errorLogs.Add(logLine);
            txtLogs.Add(logLine);


            logLine = FormatLogEntry(classMethod, $"Trace: {ex.StackTrace}");
            errorLogs.Add(logLine);
            txtLogs.Add(logLine);

            WriteFileError(errorLogs);
            WriteFile(txtLogs);

            _uiAddLog(LogInfo.ErrorNotification($"Error processing {type} '{URL}'."));
        }


        

        

        public void WriteFile(List<string> logs)
        {
            try
            {
                txtRWL.TryEnterWriteLock(3000);
                try
                {
                    var filestreamer = new FileStream(_txtPath, FileMode.Append, FileAccess.Write);
                    using StreamWriter streamWriter = new(filestreamer);

                    foreach (var log in logs)
                    {
                        streamWriter.WriteLine(log);
                    }
                }
                finally { txtRWL.ExitWriteLock(); }
            }
            catch (Exception ex)
            {
                ErrorWritingFile(logs , ex);
            };
        }

        public void WriteFileError(List<string> errorLogs)
        {
            try
            {
                errorRWL.TryEnterWriteLock(3000);
                try
                {
                    var fileStream = new FileStream(_errorPath, FileMode.Append, FileAccess.Write);
                    using StreamWriter streamWriter = new(fileStream);

                    var csvFileLenth = new System.IO.FileInfo(_errorPath).Length;
                    if (csvFileLenth == 0)
                    {
                        foreach (var keyValue in _cachedKeyValues)
                        {
                            streamWriter.WriteLine(keyValue);
                        }
                    }

                    foreach (var log in errorLogs)
                    {
                        streamWriter.WriteLine(log);
                    }
                }
                finally
                {
                    errorRWL.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                _uiAddLog(LogInfo.TextNotification(ex.Message));
            }
        }

        private void ErrorWritingFile(List<string> logs, Exception ex)
        {
            List<string> errorLogs = new()
            {
                string.Empty,
                string.Empty,
                string.Empty,
                FormatLogEntry(GetType().Name, $"========== Error writting below lines =========="),
            };

            errorLogs.AddRange(logs);
            errorLogs.Add(FormatLogEntry(GetType().Name, $"Exception: {ex.Message}"));
            errorLogs.Add(FormatLogEntry(GetType().Name, $"Trace: {ex.StackTrace}"));

            errorLogs.Add(string.Empty);
            errorLogs.Add(string.Empty);
            errorLogs.Add(string.Empty);

            WriteFileError(errorLogs);
        }



        private void GetSolutionParameters(ISolutionParameters parameters)
        {
            LogProperty($"Solution parameters");
            LogProperty($"========== ========== ==========");

            GetProperties(parameters);

            LogProperty($"========== ========== ==========");
        }

        private void GetProperties(ISolutionParameters parameters)
        {
            Type solutiontype = parameters.GetType();
            PropertyInfo[] collPropertyInfo = solutiontype.GetProperties(BindingFlags.Public | BindingFlags.Instance);

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
            string logEntry = FormatLogEntry(GetType().Name, property);
            _cachedKeyValues.Add(logEntry);

            WriteFile(new List<string>() { logEntry });

            // Keep for using during testing
            //_uiAddLog(LogInfo.TextNotification(property));
        }


        internal void SolutionFinish()
        {
            SolutionFinishNotice();
            UI(GetType().Name, $"COMPLETED: Solution has finished correctly!");
        }

        internal void SolutionFinish(Exception ex)
        {
            SolutionFinishNotice();
            _uiAddLog(LogInfo.ErrorNotification(ex.Message));

            Info(GetType().Name, $"Exception: {ex.Message}");
            Info(GetType().Name, $"Trace: {ex.StackTrace}");
        }

        private void SolutionFinishNotice()
        {
            SW.Stop();
            Progress(100);
        }




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
                string logEntry = FormatLogEntry(GetType().Name, "Error while writting CSV file");
                ErrorWritingFile(new List<string>() { logEntry }, ex);
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
                string logEntry = FormatLogEntry(GetType().Name, "Error while writting CSV file");
                ErrorWritingFile(new List<string>() { logEntry }, ex);
            }
        }

        internal void ResetCache(Type type)
        {
            _sql.ResetTableQuery(this, type);
        }

        internal void RecordSql(ISolutionRecord record)
        {
            _sql.InsertValue(this, record);
        }

        internal void ExportCacheToCsv<T>(Type type, string reportName)
        {
            string reportPath = Path.Combine(_solutionFolderPath, _solutionFileName + $"_{reportName}.csv");

            foreach (var record in GetCachedRecords<T>())
            {
                try
                {
                    Type solutiontype = type;
                    PropertyInfo[] properties = solutiontype.GetProperties(BindingFlags.Public | BindingFlags.Instance);

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
                catch (Exception ex)
                {
                    string logEntry = FormatLogEntry(GetType().Name, "Error while writting CSV file");
                    ErrorWritingFile(new List<string>() { logEntry }, ex);
                }
            }
        }

        private IEnumerable<T> GetCachedRecords<T>()
        {
            int batchCount = 0;
            int batchSize = 5000;

            IEnumerable<T> collRecords;
            do
            {
                int offset = batchSize * batchCount;
                string query = @$"
                    SELECT * 
                    FROM {typeof(T).Name} 
                    LIMIT {batchSize} OFFSET {offset};";

                collRecords = _sql.GetRecords<T>(this, query);

                foreach (var record in collRecords)
                {
                    yield return record;
                }

                batchCount++;

            } while (collRecords.Any());

        }

        internal void ClearCache(Type type)
        {
            _sql.DropTable(this, type);
        }
    }
}
