using CamlBuilder;
using Microsoft.IdentityModel.Logging;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json.Linq;
using NovaPointLibrary.Solutions.Automation;
using NovaPointLibrary.Solutions.Report;
using PnP.Framework.Diagnostics;
using PnP.Framework.Modernization.Telemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions
{
    internal class NPLogger
    {
        private readonly Action<LogInfo> _uiAddLog;

        private readonly string _txtPath;
        private readonly string _csvPath;

        private readonly Stopwatch SW = new();

        // TO BE DEPRECATED
        internal NPLogger(Action<LogInfo> uiAddLog, string solutionType, string solutionName)
        {
            string methodName = $"{GetType().Name}.Main";

            _uiAddLog = uiAddLog;

            string userDocumentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string folderName = solutionName + "_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            string folderPath = Path.Combine(userDocumentsFolder, "NovaPoint", solutionName, folderName);
            Directory.CreateDirectory(folderPath);

            _txtPath = Path.Combine(folderPath, folderName + "_Logs.txt");
            _csvPath = Path.Combine(folderPath, folderName + "_Report.csv");


            LogTxt(methodName, $"Solution logs can be found at: {_txtPath}");
            LogTxt(methodName, $"Solution report can be found at: {_csvPath}");
            _uiAddLog(LogInfo.FolderInfo(folderPath));

            SW.Start();

            LogUI(methodName, $"Solution has started, please wait to the end");
        }

        public NPLogger(Action<LogInfo> uiAddLog, ISolution solution)
        {
            string methodName = $"{GetType().Name}.Main";

            _uiAddLog = uiAddLog;

            string solutionName = solution.GetType().Name;

            string userDocumentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string folderName = solutionName + "_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            string folderPath = Path.Combine(userDocumentsFolder, "NovaPoint", solutionName, folderName);
            System.IO.Directory.CreateDirectory(folderPath);

            _txtPath = System.IO.Path.Combine(folderPath, folderName + "_Logs.txt");
            _csvPath = System.IO.Path.Combine(folderPath, folderName + "_Report.csv");

            LogTxt(methodName, $"Solution logs can be found at: {_txtPath}");
            LogTxt(methodName, $"Solution report can be found at: {_csvPath}");
            _uiAddLog(LogInfo.FolderInfo(folderPath));

            SolutionProperties(solution.Parameters);

            SW.Start();

            LogUI(methodName, $"Solution has started, please wait to the end");

        }

        private void SolutionProperties(ISolutionParameters parameters)
        {
            string methodName = $"{GetType().Name}.SolutionProperties";
            LogTxt(methodName, $"Start adding Solution properties");


            Type solutiontype = parameters.GetType();
            PropertyInfo[] properties = solutiontype.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var propertyInfo in properties)
            {
                LogTxt(methodName, $"{propertyInfo.Name}: {propertyInfo.GetValue(parameters)}");
            }

            LogTxt(methodName, $"Finish adding Solution properties");
        }

        
        internal void LogTxt(string classMethod, string log)
        {
            using StreamWriter txt = new(new FileStream(_txtPath, FileMode.Append, FileAccess.Write));
            txt.WriteLine($"{DateTime.UtcNow:yyyy/MM/dd HH:mm:ss} - [{classMethod}] - {log}");
        }

        internal void LogUI(string classMethod, string log)
        {
            LogTxt(classMethod, log);

            LogInfo logInfo = new(log);
            _uiAddLog(logInfo);
        }

        internal void ProgressUI(double progress)
        {
            LogTxt("ProgressUI", $"Progress {progress}%");
            string pendingTime = $"Pending Time: Calculating...";

            if (progress > 1)
            {
                TimeSpan ts = TimeSpan.FromMilliseconds( (SW.Elapsed.TotalMilliseconds * 100 / progress - SW.Elapsed.TotalMilliseconds) );
                pendingTime = $"Pending Time: {ts.Hours}h:{ts.Minutes}m:{ts.Seconds}s";
            }
 
            LogInfo logInfo = new(progress, pendingTime);
            _uiAddLog(logInfo);
        }

        internal void RecordCSV(dynamic o)
        {
            string methodName = $"{GetType().Name}.AddRecordToCSV";
            LogTxt(methodName, $"Adding Record to csv report");

            StringBuilder sb = new();
            using StreamWriter csv = new(new FileStream(_csvPath, FileMode.Append, FileAccess.Write));
            {
                var csvFileLenth = new System.IO.FileInfo(_csvPath).Length;
                if (csvFileLenth == 0)
                {
                    // https://learn.microsoft.com/en-us/dotnet/api/system.dynamic.expandoobject?redirectedfrom=MSDN&view=net-7.0#enumerating-and-deleting-members
                    foreach (var property in (IDictionary<String, Object>)o)
                    {
                        sb.Append($"{property.Key},");
                    }
                    
                    csv.WriteLine(sb.ToString());
                    sb.Clear();
                }

                foreach (var property in (IDictionary<String, Object>)o)
                {
                    sb.Append($"{property.Value},");
                }

                csv.WriteLine(sb.ToString());
            }
        }

        internal void ScriptFinish()
        {
            ScriptFinishNotice();
            LogUI($"{GetType().Name}.ScriptFinish", $"COMPLETED: Solution has finished correctly!");
        }

        internal void ScriptFinish(Exception ex)
        {
            ScriptFinishNotice();
            LogUI($"{GetType().Name}.ScriptFinish", ex.Message);
            LogTxt($"{GetType().Name}.ScriptFinish", $"{ex.StackTrace}");
        }

        private void ScriptFinishNotice()
        {
            SW.Stop();
            ProgressUI(100);
        }

        internal void ReportError(string type, string URL, Exception ex)
        {
            LogUI($"{GetType().Name}.ScriptFinish", $"Error processing {type} '{URL}'");
            LogTxt($"{GetType().Name}.ScriptFinish", $"Exception: {ex.Message}");
            LogTxt($"{GetType().Name}.ScriptFinish", $"Trace: {ex.StackTrace}");
        }








        // TO BE DEPRECATED
        internal void ScriptStartNotice()
        {
            AddLogToUI($"Solution has started, please wait to the end");
        }

        // TO BE DEPRECATED
        internal void AddLogToTxt(string log)
        {
            using StreamWriter txt = new(new FileStream(_txtPath, FileMode.Append, FileAccess.Write));
            txt.WriteLine($"{DateTime.UtcNow:yyyy/MM/dd HH:mm:ss} - [Logger.AddLogToTxt] {log}");
        }

        // TO BE DEPRECATED
        internal NPLogger(NPLogger logHelper, string classMethod)
        {
            _uiAddLog = logHelper._uiAddLog;
            _txtPath = logHelper._txtPath;
            _csvPath = logHelper._csvPath;
        }

        // TO BE DEPRECATED
        internal void AddLogToUI(string log)
        {
            AddLogToTxt(log);

            LogInfo logInfo = new(log);
            _uiAddLog(logInfo);
        }
    }
}
