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

        public NPLogger(Action<LogInfo> uiAddLog, string solutionName, ISolutionParameters parameters)
        {
            _uiAddLog = uiAddLog;

            string userDocumentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string folderPath = Path.Combine(userDocumentsFolder, "NovaPoint", solutionName, DateTime.UtcNow.ToString("yyMMddHHmmss"));
            Directory.CreateDirectory(folderPath);

            string fileName = solutionName + "_" + DateTime.UtcNow.ToString("yyMMddHHmmss");
            _txtPath = Path.Combine(folderPath, fileName + "_Logs.txt");
            _csvPath = Path.Combine(folderPath, fileName + "_Report.csv");

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

            //parameters.ParametersCheck();
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
            using StreamWriter txt = new(new FileStream(_txtPath, FileMode.Append, FileAccess.Write));
            txt.WriteLine($"{DateTime.UtcNow:yyyy/MM/dd HH:mm:ss} - [{classMethod}] - {log}");
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

        internal void RecordCSV(ISolutionRecord record)
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
                        sb.Append($"{propertyInfo.Name},");
                    }

                    csv.WriteLine(sb.ToString());
                    sb.Clear();
                }

                foreach (var propertyInfo in properties)
                {
                    sb.Append($"{propertyInfo.GetValue(record)},");
                }

                csv.WriteLine(sb.ToString());
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
