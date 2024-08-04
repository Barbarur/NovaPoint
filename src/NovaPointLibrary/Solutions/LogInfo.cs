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
        public string TextBase { get; set; } = string.Empty;
        public string TextError { get; set; } = string.Empty;

        public string MainClassInfo { get; set; } = string.Empty;
        public double PercentageProgress { get; set; } = -1;
        public TimeSpan PendingTime { get; set; } = TimeSpan.Zero;
        public string SolutionFolder { get; set; } = string.Empty;

        public static LogInfo FolderInfo(string folder)
        {
            LogInfo logInfo = new()
            {
                SolutionFolder = folder
            };
            return logInfo;
        }

        public static LogInfo TextNotification(string text)
        {
            LogInfo li= new()
            {
                TextBase = text,
            };
            return li;
        }

        public static LogInfo ErrorNotification(string error)
        {
            LogInfo li = new()
            {
                TextError = error,
            };
            return li;
        }

        public static LogInfo ProgressUpdate(double percentageProgress, TimeSpan pendingTime)
        {
            LogInfo li = new()
            {
            PercentageProgress = percentageProgress,
            PendingTime = pendingTime,
            };
            return li;
        }
    }
}
