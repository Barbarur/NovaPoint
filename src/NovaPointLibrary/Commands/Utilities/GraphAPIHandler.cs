using Microsoft.AspNetCore.Http;
using Microsoft.Graph;
using Newtonsoft.Json;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.Utilities
{
    internal class GraphAPIHandler
    {
        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;

        private readonly HttpClient HttpsClient;
        private readonly string _graphUrl = "https://graph.microsoft.com/v1.0";

        internal GraphAPIHandler(NPLogger logger, AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
            HttpsClient = new();
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

        internal async Task<string> GetResponseJSONAsync(string url)
        {
            _appInfo.IsCancelled(); 
            
            HttpRequestMessage requestMessage = await GetMessage(url, HttpMethod.Get);

            var sendMessage = _appInfo.SendHttpRequestMessageAsync(requestMessage);

            TaskCompletionSource taskCompletionSource = new();

            _appInfo.CancelToken.Register(() => taskCompletionSource.TrySetCanceled());

            var completedTask = await Task.WhenAny(sendMessage, taskCompletionSource.Task);

            if (completedTask != sendMessage || _appInfo.CancelToken.IsCancellationRequested)
            {
                _appInfo.CancelToken.ThrowIfCancellationRequested();
                throw new Exception("Operation canceled.");
            }
            else
            {
                return await sendMessage;
            }
        }

        internal async Task DeleteAsync(string url)
        {
            _appInfo.IsCancelled();

            _logger.LogTxt(GetType().Name, $"Graph API Request Delete '{url}'");

            HttpRequestMessage requestMessage = await GetMessage(url, HttpMethod.Delete);

            var sendMessage = _appInfo.SendHttpRequestMessageAsync(requestMessage);

            TaskCompletionSource taskCompletionSource = new();

            _appInfo.CancelToken.Register(() => taskCompletionSource.TrySetCanceled());

            var completedTask = await Task.WhenAny(sendMessage, taskCompletionSource.Task);

            if (completedTask != sendMessage || _appInfo.CancelToken.IsCancellationRequested)
            {
                _appInfo.CancelToken.ThrowIfCancellationRequested();
                throw new Exception("Operation canceled.");
            }
            else
            {
                string response = await sendMessage;
                _logger.LogTxt(GetType().Name, response);
            }
        }


        private async Task<HttpRequestMessage> GetMessage(string url, HttpMethod method)
        {
            _appInfo.IsCancelled();

            if (url.StartsWith("/"))
            {
                url = url.Substring(1);
            }
            string apiUrl = !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? $"{_graphUrl}/{url}" : url;
            _logger.LogTxt(GetType().Name, $"Writing message for '{method}' in '{apiUrl}'");

            HttpRequestMessage message = new();
            message.Method = method;

            string accessToken = await _appInfo.GetGraphAccessToken();
            message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            message.RequestUri = new Uri(apiUrl);

            // MISSING:
            // Header.Accept
            // Content
            // Additional Headers
            // Content header content type


            return message;
        }

    }

}
