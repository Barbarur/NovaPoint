using Newtonsoft.Json;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.HttpService;
using NovaPointLibrary.Core.Logging;

namespace NovaPointLibrary.Commands.Utilities
{
    internal class GraphAPIHandler
    {
        private readonly ILogger _logger;
        private readonly IAppClient _appInfo;

        private static readonly string _graphUrl = "https://graph.microsoft.com/v1.0";

        internal GraphAPIHandler(ILogger logger, IAppClient appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<IEnumerable<T>> GetCollectionAsync<T>(string url)
        {
            List<T> results = [];
            int pageCount = 1;

            var request = await GetObjectAsync<GraphtResultCollection<T>>(url);

            if (request !=null && request.Items.Any())
            {
                results.AddRange(request.Items);
                while (!string.IsNullOrEmpty(request.NextLink))
                {
                    _appInfo.IsCancelled();

                    request = await GetObjectAsync<GraphtResultCollection<T>>(request.NextLink);
                    pageCount++;
                    if (request != null && request.Items.Any())
                    {
                        results.AddRange(request.Items);
                    }
                }
            }

            _logger.Info(GetType().Name, $"Retrieved {results.Count} item(s) in {pageCount} page(s) from '{url}'");

            return results;
        }

        internal async Task<T> GetObjectAsync<T>(string url)
        {
            string responseContent = await GetJSONAsync(url);

            if (responseContent != null)
            {
                var response = JsonConvert.DeserializeObject<T>(responseContent);
                
                return response;
            }
            else
            {
                throw new Exception("Response is null");
            }
        }

        private async Task<string> GetJSONAsync(string apiEndpoint)
        {
            _appInfo.IsCancelled();

            HttpMessageWriter messageWriter = new(_logger, _appInfo, HttpMethod.Get, GetUriString(apiEndpoint));
            string response = await HttpClientService.SendHttpRequestMessageAsync(_logger, messageWriter, _appInfo.CancelToken);

            return response;
        }

        internal async Task<string> GetAsync(string apiEndpoint, string accept, Dictionary<string, string>? additionalHeaders = null)
        {
            _appInfo.IsCancelled();

            HttpMessageWriter messageWriter = new(_logger, _appInfo, HttpMethod.Get, GetUriString(apiEndpoint), accept: accept, additionalHeaders: additionalHeaders);
            string response = await HttpClientService.SendHttpRequestMessageAsync(_logger, messageWriter, _appInfo.CancelToken);

            return response;
        }

        internal async Task<string> PostAsync(string apiEndpoint, string content)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"POST '{apiEndpoint}' with content '{content}'");

            HttpMessageWriter messageWriter = new(_logger, _appInfo, HttpMethod.Post, GetUriString(apiEndpoint), content: content);
            string response = await HttpClientService.SendHttpRequestMessageAsync(_logger, messageWriter, _appInfo.CancelToken);

            return response;
        }

        internal async Task DeleteAsync(string apiEndpoint)
        {
            _appInfo.IsCancelled();

            HttpMessageWriter messageWriter = new(_logger, _appInfo, HttpMethod.Delete, GetUriString(apiEndpoint));
            string response = await HttpClientService.SendHttpRequestMessageAsync(_logger, messageWriter, _appInfo.CancelToken);

            _logger.Info(GetType().Name, response);
        }

        public static string GetUriString(string apiEndpoint)
        {
            if (apiEndpoint.StartsWith('/'))
            {
                apiEndpoint = apiEndpoint.Substring(1);
            }
            string uri = !apiEndpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? $"{_graphUrl}/{apiEndpoint}" : apiEndpoint;

            return uri;
        }

    }

}
