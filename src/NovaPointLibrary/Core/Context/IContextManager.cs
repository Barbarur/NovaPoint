using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Logging;

namespace NovaPointLibrary.Core.Context
{
    internal interface IContextManager
    {
        IAppClient AppClient { get; }
        ILogger Logger { get; }
    }
}
