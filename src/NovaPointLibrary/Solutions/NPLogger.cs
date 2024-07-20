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

        public NPLogger(Action<LogInfo> uiAddLog, string solutionName, ISolutionParameters parameters)
        {
            _uiAddLog = uiAddLog;

            string userDocumentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string folderPath = Path.Combine(userDocumentsFolder, "NovaPoint", solutionName, DateTime.UtcNow.ToString("yyMMddHHmmss"));
            Directory.CreateDirectory(folderPath);

            string fileName = solutionName + "_" + DateTime.UtcNow.ToString("yyMMddHHmmss");
            _txtPath = Path.Combine(folderPath, fileName + "_Logs.txt");
            _csvPath = Path.Combine(folderPath, fileName + "_Report.csv");
            _errorPath = Path.Combine(folderPath, fileName + "_error.txt");

            LogTxt(GetType().Name, $"Logs: {_txtPath}");
            LogTxt(GetType().Name, $"Report: {_csvPath}");
            _uiAddLog(LogInfo.FolderInfo(folderPath));

            SolutionProperties(parameters);

            SW.Start();

            LogUI(GetType().Name, $"Solution has started, please wait to the end");
        }

        private void SolutionProperties(ISolutionParameters parameters)
        {
            LogTxt(GetType().Name, $"Solution properties");
            LogTxt(GetType().Name, $"========== ========== ==========");

            LogProperties(parameters);

            LogTxt(GetType().Name, $"========== ========== ==========");
        }

        private void LogProperties(ISolutionParameters parameters)
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
                        LogProperties((ISolutionParameters)oProperty);
                    }
                    else
                    {
                        LogTxt(GetType().Name, $"{propertyInfo.Name}: {oProperty}");
                        // Keep for using during testing
                        //LogUI(GetType().Name, $"{propertyInfo.Name}: {oProperty}");
                    }
                }
            }

            parameters.ParametersCheck();
        }


        internal void LogTxt(string classMethod, string log)
        {
            string logLine = $"{DateTime.UtcNow:yyyy/MM/dd HH:mm:ss} - [{classMethod}] - {log}";
            try
            {
                if (!txtRWL.IsWriteLockHeld) { txtRWL.TryEnterWriteLock(3000); }
                try
                {
                    var x = new FileStream(_txtPath, FileMode.Append, FileAccess.Write);
                    using StreamWriter txt = new(x);

                    txt.WriteLine(logLine);
                }
                finally { txtRWL.ExitWriteLock(); }
            }
            catch (Exception ex)
            {
                LogError(logLine, ex);
            };
        }

        internal void LogUI(string classMethod, string log)
        {
            LogTxt(classMethod, log);

            _uiAddLog(LogInfo.TextNotification(log));
        }

        internal void ProgressUI(double progress)
        {
            string pendingTime = $"Pending Time: Calculating...";

            if (progress > 1)
            {
                TimeSpan ts = TimeSpan.FromMilliseconds( (SW.Elapsed.TotalMilliseconds * 100 / progress - SW.Elapsed.TotalMilliseconds) );
                pendingTime = $"Pending Time: {ts.Hours}h:{ts.Minutes}m:{ts.Seconds}s";
            }

            _uiAddLog(LogInfo.ProgressUpdate(progress, pendingTime));
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
                LogError("Error while writting CSV file", ex);
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
                LogError("Error while writting CSV file", ex);
            }
        }

        static ReaderWriterLockSlim errorRWL = new();
        private void LogError(string log, Exception ex)
        {
            errorRWL.TryEnterWriteLock(3000);
            try
            {
                var fileStream = new FileStream(_errorPath, FileMode.Append, FileAccess.Write);
                using StreamWriter streamWriter = new(fileStream);

                streamWriter.WriteLine(log);
                streamWriter.WriteLine($"Exception: {ex.Message}");
                streamWriter.WriteLine($"Trace: {ex.StackTrace}");
            }
            finally
            {
                errorRWL.ExitWriteLock();
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

        internal void ReportError(string type, string URL, Exception ex)
        {
            ReportError(type, URL, ex.Message, ex.StackTrace);
        }

        internal void ReportError(string type, string URL, string exMessage, string? exStackTrace = null)
        {
            _uiAddLog(LogInfo.ErrorNotification($"Error processing {type} '{URL}'"));
            LogTxt(GetType().Name, $"Error processing {type} '{URL}'");

            LogTxt(GetType().Name, $"Exception: {exMessage}");
            LogTxt(GetType().Name, $"Trace: {exStackTrace}");
        }

    }
}
