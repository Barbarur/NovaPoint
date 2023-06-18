using AngleSharp.Css.Dom;
using Microsoft.Graph;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions
{
    public class LogInfo
    {
        public string ClassMethod { get; set; } = string.Empty;
        public string MainClassInfo { get; set; } = string.Empty;
        public string DetailInfo { get; set; } = string.Empty;
        public double PercentageProgress { get; set; } = -1;
        public string PendingTime { get; set; } = string.Empty;
        public string SolutionFolder { get; set; } = string.Empty;

        public LogInfo()
        {
        }

        public LogInfo(string mainInfo)
        {
            MainClassInfo = mainInfo;
        }

        public LogInfo(Action<LogInfo> logger)
        {
            logger(this);
        }
        public LogInfo(double percentageProgress, string pendingTime)
        {
            PercentageProgress = percentageProgress;
            PendingTime = pendingTime;
        }

        public static LogInfo FolderInfo(string folder)
        {
            LogInfo logInfo = new();
            logInfo.SolutionFolder = folder;
            return logInfo;
        }
        internal void Clear()
        {
            MainClassInfo = String.Empty;
            DetailInfo = String.Empty; 
            PercentageProgress = 0;
        }
    }
}
