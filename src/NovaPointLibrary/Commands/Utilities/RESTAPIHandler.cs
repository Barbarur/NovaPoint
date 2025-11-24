using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Core.HttpService;
using NovaPointLibrary.Core.Logging;


namespace NovaPointLibrary.Commands.Utilities
{
    internal class RESTAPIHandler
    {
        private readonly ILogger _logger;
        private AppInfo _appInfo;

        internal RESTAPIHandler(ILogger logger, AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<string> GetAsync(string uriString)
        {
            _appInfo.IsCancelled();

            HttpMessageWriter messageWriter = new(_logger, _appInfo, HttpMethod.Get, uriString);
            string response = await HttpClientService.SendHttpRequestMessageAsync(_logger, messageWriter, _appInfo.CancelToken);

            return response;
        }

        internal async Task<string> PostAsync(string uriString, string content)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"POST '{uriString}' with content '{content}'");

            HttpMessageWriter messageWriter = new(_logger, _appInfo, HttpMethod.Post, uriString, content: content);
            string response = await HttpClientService.SendHttpRequestMessageAsync(_logger, messageWriter, _appInfo.CancelToken);


            return response;
        }

    }
}
