using CamlBuilder;
using Microsoft.IdentityModel.Logging;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json.Linq;
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

    internal class LogHelper
    {
        private readonly Action<LogInfo> _uiAddLog;
        private readonly string _folderPath;
        private readonly string _txtPath;
        internal readonly string _csvPath;

        private string _classMethod = "LogHelper.Constructor";

        private Stopwatch SW = new();

        internal LogHelper(Action<LogInfo> uiAddLog, string solutionType, string solutionName)
        {
            _uiAddLog = uiAddLog;

            string userDocumentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string folderName = solutionName + "_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            _folderPath = Path.Combine(userDocumentsFolder, "NovaPoint", solutionType, solutionName, folderName);
            System.IO.Directory.CreateDirectory(_folderPath);

            _txtPath = System.IO.Path.Combine(_folderPath, folderName + "_Logs.txt");
            _csvPath = System.IO.Path.Combine(_folderPath, folderName + "_Report.csv");


            AddLogToTxt($"Solution logs can be found at: {_txtPath}");
            AddLogToTxt($"Solution report can be found at: {_csvPath}");
            _uiAddLog(LogInfo.FolderInfo(_folderPath));

            //RecordsLocation();

            _classMethod = $"{solutionName}.RunAsync";

            SW.Start();
        }

        internal LogHelper(LogHelper logHelper, string classMethod)
        {
            _uiAddLog = logHelper._uiAddLog;

            _folderPath = logHelper._folderPath;
            _txtPath = logHelper._txtPath;
            _csvPath = logHelper._csvPath;

            _classMethod = classMethod;
        }

        // TO BE DEPRECATED
        internal void AddLog(string classMethod,string log)
        {

            using StreamWriter txt = new(new FileStream(_txtPath, FileMode.Append, FileAccess.Write));
            txt.WriteLine($"{DateTime.UtcNow:yyyy/MM/dd HH:mm:ss} - [{classMethod}] {log}");
        }


        internal void AddLogToUI(string log)
        {
            AddLogToTxt(log);

            LogInfo logInfo = new(log);
            _uiAddLog(logInfo);
        }

        internal void AddLogToUI(string classMethod, string log)
        {
            AddLogToTxt(classMethod, log);

            LogInfo logInfo = new(log);
            _uiAddLog(logInfo);
        }


        internal void AddProgressToUI(double progress)
        {
            AddLogToTxt($"Progress {progress}%");
            string pendingTime = $"Pending Time: Calculating...";

            if (progress > 1)
            {
                TimeSpan ts = TimeSpan.FromMilliseconds( (SW.Elapsed.TotalMilliseconds * 100 / progress - SW.Elapsed.TotalMilliseconds) );
                pendingTime = $"Pending Time: {ts.Hours}h:{ts.Minutes}m:{ts.Seconds}s";
            }
 
            LogInfo logInfo = new(progress, pendingTime);
            _uiAddLog(logInfo);
        }

        // TO BE DEPRECATED
        internal void AddLogToTxt(string log)
        {
            using StreamWriter txt = new(new FileStream(_txtPath, FileMode.Append, FileAccess.Write));
            txt.WriteLine($"{DateTime.UtcNow:yyyy/MM/dd HH:mm:ss} - [{_classMethod}] {log}");
        }

        internal void AddLogToTxt(string classMethod, string log)
        {
            using StreamWriter txt = new(new FileStream(_txtPath, FileMode.Append, FileAccess.Write));
            txt.WriteLine($"{DateTime.UtcNow:yyyy/MM/dd HH:mm:ss} - [{classMethod}] - {log}");
        }

        internal void AddRecordToCSV(dynamic o)
        {
            string methodName = $"{GetType().Name}.AddRecordToCSV";
            AddLogToTxt(methodName, $"Adding Record to csv report");

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

        //internal void RecordsLocation()
        //{
        //    AddLogToUI($"Solution logs can be found at: {_txtPath}");
        //    AddLogToUI($"Solution report can be found at: {_csvPath}");
        //}

        internal void ScriptStartNotice()
        {
            AddLogToUI($"Solution has started, please wait to the end");
        }

        internal void ScriptFinishSuccessfulNotice()
        {
            ScriptFinishNotice();
            AddLogToUI($"COMPLETED: Solution has finished correctly!");
        }

        internal void ScriptFinishErrorNotice(Exception ex)
        {
            ScriptFinishNotice();
            AddLogToUI(ex.Message);
            AddLogToTxt($"{ex.StackTrace}");
        }

        private void ScriptFinishNotice()
        {
            SW.Stop();
            AddProgressToUI(100);
            //AddLogToUI($"   ");
            //RecordsLocation();
            //AddLogToUI($"   ");
        }
    }
}
