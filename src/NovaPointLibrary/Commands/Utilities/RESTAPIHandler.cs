using Newtonsoft.Json;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Commands.Utilities.RESTModel;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.Utilities
{
    internal class RESTAPIHandler
    {
        private readonly NPLogger _logger;
        private AppInfo _appInfo;
        private readonly HttpClient HttpsClient;

        internal RESTAPIHandler(NPLogger logger, AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
            HttpsClient = new();
        }

        internal async Task Post(string apiUrl, string content)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"HTTP Request Post API '{apiUrl}' content '{content}'");

            HttpRequestMessage requestMessage = await GetRequestMessage(HttpMethod.Post, apiUrl, content);

            var sendMessage = SendMessageAsync(requestMessage);

            TaskCompletionSource taskCompletionSource = new();

            _appInfo.CancelToken.Register(() => taskCompletionSource.TrySetCanceled());

            var completedTask = await Task.WhenAny(sendMessage, taskCompletionSource.Task);

            if (completedTask != sendMessage || _appInfo.CancelToken.IsCancellationRequested)
            {
                _appInfo.CancelToken.ThrowIfCancellationRequested();
            }
            else
            {
                string response = await sendMessage;
                _logger.LogTxt(GetType().Name, response);
            }
        }

        private async Task<HttpRequestMessage> GetRequestMessage(HttpMethod method, string apiUrl, string content)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Writing message for '{method}' in '{apiUrl}'");

            HttpRequestMessage request = new();

            string spoAccessToken = await _appInfo.GetSPOAccessToken(apiUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", spoAccessToken);
            
            request.Method = method;
            request.RequestUri = new Uri(apiUrl);
                        
            request.Headers.Add("Accept", "application/json;odata=verbose");
            //message.Version = new Version(2, 0);
            
            request.Content = new StringContent(content, System.Text.Encoding.UTF8);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            return request;
        }

        private async Task<string> SendMessageAsync(HttpRequestMessage message)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Sending HTTP request message");

            HttpResponseMessage response = await HttpsClient.SendAsync(message, _appInfo.CancelToken);
            //HttpResponseMessage response = await HttpsClient.SendAsync(message);

            //while (response.StatusCode == (HttpStatusCode)429)
            //{
            //    var retryAfter = response.Headers.RetryAfter;
            //    await Task.Delay(retryAfter.Delta.Value.Seconds * 1000);
            //    response = await HttpsClient.SendAsync(message);
            //}

            if (response.IsSuccessStatusCode)
            {
                _logger.LogTxt(GetType().Name, $"Successful response");
                var responseContent = await response.Content.ReadAsStringAsync();
                return responseContent;
            }
            else
            {
                if (response.StatusCode == (HttpStatusCode)503)
                {
                    throw new Exception("Error 503. The service is unavailable.");
                }
                else
                {
                    string content = await response.Content.ReadAsStringAsync();
                    _logger.LogTxt(GetType().Name, $"Error response:{content}");

                    RESTErrorContent? oErrorContent = JsonConvert.DeserializeObject<RESTErrorContent>(content);
                    string errorMessage = oErrorContent?.Error.Message.Value != null ? oErrorContent.Error.Message.Value.ToString() : "Unknown Error";

                    throw new Exception(errorMessage);
                }
            }
        }
    }
}
