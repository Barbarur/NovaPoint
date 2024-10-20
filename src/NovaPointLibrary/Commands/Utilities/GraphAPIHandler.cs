using Newtonsoft.Json;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Logging;

namespace NovaPointLibrary.Commands.Utilities
{
    internal class GraphAPIHandler
    {
        private readonly LoggerSolution _logger;
        private readonly AppInfo _appInfo;

        private readonly string _graphUrl = "https://graph.microsoft.com/v1.0";

        internal GraphAPIHandler(LoggerSolution logger, AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<IEnumerable<T>> GetCollectionAsync<T>(string url)
        {
            _appInfo.IsCancelled();

            List<T> results = new();

            var request = await GetObjectAsync<GraphtResultCollection<T>>(url);

            if (request !=null && request.Items.Any())
            {
                results.AddRange(request.Items);
                while (!string.IsNullOrEmpty(request.NextLink))
                {
                    request = await GetObjectAsync<GraphtResultCollection<T>>(request.NextLink);
                    if (request != null && request.Items.Any())
                    {
                        results.AddRange(request.Items);
                    }
                }
            }

            return results;
        }

        internal async Task<T> GetObjectAsync<T>(string url)
        {
            _appInfo.IsCancelled();

            string responseContent = await GetResponseJSONAsync(url);

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

        internal async Task<string> GetResponseJSONAsync(string apiUrl)
        {
            _appInfo.IsCancelled();

            string response = await _appInfo.SendHttpRequestMessageAsync(GetRequestMessage, HttpMethod.Get, apiUrl, "");

            return response;
        }

        internal async Task DeleteAsync(string apiUrl)
        {
            _appInfo.IsCancelled();

            string response = await _appInfo.SendHttpRequestMessageAsync(GetRequestMessage, HttpMethod.Delete, apiUrl, "");

            _logger.Info(GetType().Name, response);
        }


        private async Task<HttpRequestMessage> GetRequestMessage(HttpMethod method, string apiUrl, string content = "")
        {
            _appInfo.IsCancelled();

            if (apiUrl.StartsWith("/"))
            {
                apiUrl = apiUrl.Substring(1);
            }
            string uri = !apiUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? $"{_graphUrl}/{apiUrl}" : apiUrl;
            _logger.Info(GetType().Name, $"Writing message for '{method}' in '{uri}'");

            HttpRequestMessage message = new();
            message.Method = method;

            string accessToken = await _appInfo.GetGraphAccessToken();
            message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            message.RequestUri = new Uri(uri);

            // MISSING:
            // Header.Accept
            // Content
            // Additional Headers
            // Content header content type


            return message;
        }

    }

}
