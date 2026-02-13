using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Logging;
using NovaPointLibrary.Core.SQLite;
using NovaPointLibrary.Solutions.Report;
using NovaPointLibrary.Solutions;


namespace NovaPointLibrary.Core.Context
{
    public class ContextSolution : IContextManager
    {
        public ILogger Logger { get; init; }
        public IAppClient AppClient { get; init; }
        internal DbHandlerSolution DbHandler {  get; init; }

        internal ContextSolution(LoggerSolution logger, IAppClient appClient, DbHandlerSolution sbHandler)
        {
            Logger = logger;
            AppClient = appClient;
            DbHandler = sbHandler;
        }

        internal void SolutionEnd(Exception? ex = null)
        {
            Logger.End(ex);
            DbHandler.End();
        }
    }
}
