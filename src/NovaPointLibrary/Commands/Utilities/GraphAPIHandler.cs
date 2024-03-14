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

            var sendMessage = SendMessageAsync(requestMessage);

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

        private async Task<HttpRequestMessage> GetMessage(string url, HttpMethod method)
        {
            _appInfo.IsCancelled();

            if (url.StartsWith("/"))
            {
                url = url.Substring(1);
            }

            string accessToken = await _appInfo.GetGraphAccessToken();

            HttpRequestMessage message = new();
            message.Method = method;
            message.RequestUri = !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? new Uri($"{_graphUrl}/{url}") : new Uri(url);
            message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            _logger.LogTxt(GetType().Name, $"Request Message Uir {message.RequestUri}");

            return message;
        }

        private async Task<string> SendMessageAsync(HttpRequestMessage message)
        {
            _appInfo.IsCancelled();

            HttpResponseMessage response = await HttpsClient.SendAsync(message);

            while (response.StatusCode == (HttpStatusCode)429)
            {
                var retryAfter = response.Headers.RetryAfter;
                await Task.Delay(retryAfter.Delta.Value.Seconds * 1000);
                response = await HttpsClient.SendAsync(message);
            }

            if (response.IsSuccessStatusCode)
            {
                _logger.LogTxt(GetType().Name, $"Successful response");
                var responseContent = await response.Content.ReadAsStringAsync();
                return responseContent;
            }
            else
            {
                _logger.LogTxt(GetType().Name, $"Error response");

                string content = await response.Content.ReadAsStringAsync();
                _logger.LogTxt(GetType().Name,$"Error Content:{content}");

                var oErrorContent = JsonConvert.DeserializeObject<GraphErrorContent>(content);
                string errorMessage = oErrorContent.Error.Message.ToString();

                throw new Exception(errorMessage);
            }
        }
    }

    internal class GraphtResultContent<T>
    {
        /// <summary>
        /// Context information detailing the type of message returned
        /// </summary>
        [JsonProperty("@odata.context")]
        public string Context { get; set; }

        /// <summary>
        /// NextLink detailing the link to query to fetch the next batch of results
        /// </summary>
        [JsonProperty("nextLink")]
        public string NextLink { get; set; }

        /// <summary>
        /// OData NextLink detailing the link to query to fetch the next batch of results
        /// </summary>
        [JsonProperty("@odata.nextLink")]
        public string ODataNextLink // { get; set; }
        {
            get { return NextLink; }
            set { NextLink = value; }
        }

        /// <summary>
        /// The items contained in the results
        /// </summary>
        [JsonProperty("value")]
        public IEnumerable<T> Items { get; set; }
    }
}
