using NovaPointLibrary.Solutions;


namespace NovaPointLibrary.Core.Logging
{
    public interface ILogger
    {
        Action<LogInfo> UiAddLog { get; init; }

        Task<ILogger> GetSubThreadLogger();
        void Info(string classMethod, string log);
        void Debug(string classMethod, string log);
        void UI(string classMethod, string log);
        void Progress(double progress);
        void Error(string classMethod, string type, string URL, Exception ex);
        void End(Exception? ex = null);

        void WriteLog(SolutionLog log);

        void DynamicCSV(dynamic o);
    }
}
