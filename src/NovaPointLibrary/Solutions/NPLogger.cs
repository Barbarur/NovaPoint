using NovaPointLibrary.Commands.Utilities;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace NovaPointLibrary.Solutions
{
    internal class NPLogger
    {
        private readonly Action<LogInfo> _uiAddLog;

        private readonly string _txtPath;
        private readonly string _csvPath;
        private readonly string _errorPath;

        private readonly Stopwatch SW = new();

        static ReaderWriterLockSlim txtRWL = new();
        static ReaderWriterLockSlim csvRWL = new();
        static ReaderWriterLockSlim errorRWL = new();

        private List<string> _cachedKeyValues = new();
        private List<string> _cachedLogTxt = new();

        public NPLogger(Action<LogInfo> uiAddLog, string solutionName, ISolutionParameters parameters)
        {
            _uiAddLog = uiAddLog;

            string userDocumentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string folderPath = Path.Combine(userDocumentsFolder, "NovaPoint", solutionName, DateTime.UtcNow.ToString("yyMMddHHmmss"));
            Directory.CreateDirectory(folderPath);

            string fileName = solutionName + "_" + DateTime.UtcNow.ToString("yyMMddHHmmss");
            _txtPath = Path.Combine(folderPath, fileName + "_Logs.txt");
            _csvPath = Path.Combine(folderPath, fileName + "_Report.csv");
            _errorPath = Path.Combine(folderPath, fileName + "_errors.txt");

            LogTxt(GetType().Name, $"Logs: {_txtPath}");
            LogTxt(GetType().Name, $"Report: {_csvPath}");
            _uiAddLog(LogInfo.FolderInfo(folderPath));

            string v = $"Version: v{VersionControl.GetVersion()}";
            LogTxt(GetType().Name, v);
            _cachedKeyValues.Add(v);

            GetSolutionParameters(parameters);

            SW.Start();

            LogUI(GetType().Name, $"Solution has started, please wait to the end");
        }

        internal void WriteLogFile(string log)
        {
            WriteLogFile(new List<string>() { log });
        }

        internal void WriteLogFile(List<string> logs)
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
                ErrorWritingFile(logs, ex);
            };
        }

        private void ErrorWritingFile(string log, Exception ex)
        {
            List<string> errorLogs = new()
            {
                log,
            };

            ErrorWritingFile(errorLogs, ex);
        }

        private void ErrorWritingFile(List<string> logs, Exception ex)
        {
            List<string> errorLogs = new();
            {
                GetLogLine(GetType().Name, $"========== Error writting below lines ==========");
            };

            errorLogs.AddRange(logs);
            errorLogs.Add(GetLogLine(GetType().Name, $"Exception: {ex.Message}"));
            errorLogs.Add(GetLogLine(GetType().Name, $"Trace: {ex.StackTrace}"));

            WriteErrorLogFile(errorLogs);
        }

        private void WriteErrorLogFile(string log)
        {
            WriteErrorLogFile(new List<string>() { log });
        }

        private void WriteErrorLogFile(List<string> errorLogs)
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
            string logLine = GetLogLine(GetType().Name, property);
            WriteLogFile(logLine);
            _cachedKeyValues.Add(logLine);

            // Keep for using during testing
            //_uiAddLog(LogInfo.TextNotification(property));
        }

        internal static string GetLogLine(string classMethod, string log)
        {
            return $"{DateTime.UtcNow:yyyy/MM/dd HH:mm:ss} - [{classMethod}] - {log}";
        }

        internal void LogTxt(string classMethod, string log)
        {
            string logLine = GetLogLine(classMethod, log);

            _cachedLogTxt.Add(logLine);

            while (_cachedLogTxt.Count > 20)
            {
                _cachedLogTxt.RemoveAt(0);
            }

            WriteLogFile(logLine);
        }

        internal void LogUI(string classMethod, string log)
        {
            LogTxt(classMethod, log);

            _uiAddLog(LogInfo.TextNotification(log));
        }

        internal void ProgressUI(double progress)
        {
            if (progress < 0.01) { return; }

            TimeSpan timeSpan = TimeSpan.FromMilliseconds( (SW.Elapsed.TotalMilliseconds * 100 / progress - SW.Elapsed.TotalMilliseconds) );

            _uiAddLog(LogInfo.ProgressUpdate(progress, timeSpan));
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
                ErrorWritingFile(GetLogLine(GetType().Name, "Error while writting CSV file"), ex);
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
                            sb.Append($"\"{propertyInfo.GetValue(record)}\",");
                        }
                        if (sb.Length > 0) { sb.Length--; }

                        csv.WriteLine(sb.ToString());
                    }
                }
                finally { csvRWL.ExitWriteLock(); }
            }
            catch (Exception ex)
            {
                ErrorWritingFile(GetLogLine(GetType().Name, "Error while writting CSV file"), ex);
            }
        }

        internal void ScriptFinish()
        {
            ScriptFinishNotice();
            LogUI(GetType().Name, $"COMPLETED: Solution has finished correctly!");
        }

        internal void ScriptFinish(Exception ex)
        {
            ScriptFinishNotice();
            _uiAddLog(LogInfo.ErrorNotification(ex.Message));

            LogTxt(GetType().Name, $"Exception: {ex.Message}");
            LogTxt(GetType().Name, $"Trace: {ex.StackTrace}");
        }

        private void ScriptFinishNotice()
        {
            SW.Stop();
            ProgressUI(100);
        }

        internal void ReportError(string classMethod, string type, string URL, Exception ex)
        {
            Guid correlationID = Guid.NewGuid();

            List<string> txtLogs = new();
            List<string> errorLogs = new()
            {
                GetLogLine(classMethod, $"========== {correlationID} ==========")
            };

            errorLogs.AddRange(_cachedLogTxt);

            string logLine = GetLogLine(classMethod, $"Error processing {type} '{URL}'");
            errorLogs.Add(logLine);
            txtLogs.Add(logLine);

            logLine = GetLogLine(classMethod, $"Correlation ID: {correlationID}");
            errorLogs.Add(logLine);
            txtLogs.Add(logLine);

            logLine = GetLogLine(classMethod, $"Exception: {ex.Message}");
            errorLogs.Add(logLine);
            txtLogs.Add(logLine);


            logLine = GetLogLine(classMethod, $"Trace: {ex.StackTrace}");
            errorLogs.Add(logLine);
            txtLogs.Add(logLine);

            WriteErrorLogFile(errorLogs);
            WriteLogFile(txtLogs);

            _uiAddLog(LogInfo.ErrorNotification($"Error processing {type} '{URL}'"));
        }
        
    }
}
