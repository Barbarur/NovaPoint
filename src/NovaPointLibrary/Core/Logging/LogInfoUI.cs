using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Core.Logging
{
    internal class LogInfoUI
    {
        public LogInfoUIType Type { get; set; }
        public string Text { get; set; } = String.Empty;
        public double PercentageProgress { get; set; } = -1;
        public TimeSpan PendingTime { get; set; } = TimeSpan.Zero;

        public LogInfoUI(LogInfoUIType type)
        {
            Type = type;
        }

        public static LogInfoUI TextNormal(string text)
        {
            LogInfoUI li = new(LogInfoUIType.Normal)
            {
                Text = text,
            };
            return li;
        }

        public static LogInfoUI textError(string text)
        {
            LogInfoUI li = new(LogInfoUIType.Error)
            {
                Text = text,
            };
            return li;
        }

        public static LogInfoUI ProgressUpdate(double percentageProgress, TimeSpan pendingTime)
        {
            LogInfoUI li = new(LogInfoUIType.Progress)
            {
                PercentageProgress = percentageProgress,
                PendingTime = pendingTime,
            };
            return li;
        }
    }

    public enum LogInfoUIType
    {
        Normal,
        Error,
        Warning,
        Success,
        Progress
    }
}
