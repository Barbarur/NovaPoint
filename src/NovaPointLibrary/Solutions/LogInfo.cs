namespace NovaPointLibrary.Solutions
{
    public class LogInfo
    {
        public LogInfoType Type { get; init; }
        public string Text { get; set; } = string.Empty;
        public double PercentageProgress { get; set; } = -1;
        public TimeSpan PendingTime { get; set; } = TimeSpan.Zero;
        public string SolutionFolder { get; set; } = string.Empty;

        public LogInfo(LogInfoType type)
        {
            Type = type;
        }

        public static LogInfo FolderInfo(string folder)
        {
            return new(LogInfoType.Folder)
            {
                SolutionFolder = folder,
            };
        }

        public static LogInfo TextNotification(string text)
        {
            return new(LogInfoType.Normal)
            {
                Text = text,
            };
        }

        public static LogInfo ErrorNotification(string error)
        {
            return new(LogInfoType.Error)
            {
                Text = error,
            };
        }

        public static LogInfo ProgressUpdate(double percentageProgress, TimeSpan pendingTime)
        {
            return new(LogInfoType.Progress)
            {
                PercentageProgress = percentageProgress,
                PendingTime = pendingTime,
            };
        }
    }

    public enum LogInfoType
    {
        Normal,
        Error,
        Warning,
        Success,
        Progress,
        Folder
    }
}
