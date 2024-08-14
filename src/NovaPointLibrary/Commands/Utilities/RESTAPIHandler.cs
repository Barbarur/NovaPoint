using AngleSharp.Css.Dom;
using Microsoft.Graph;
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

        internal async Task<string> PostAsync(string apiUrl, string content)
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
                throw new("Unknow error");
            }
            else
            {
                string response = await sendMessage;
                _logger.LogTxt(GetType().Name, response);
                return response;
            }
        }

        private async Task<HttpRequestMessage> GetRequestMessage(HttpMethod method, string apiUrl, string content)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Writing message for '{method}' in '{apiUrl}'");

            HttpRequestMessage request = new();
            request.Method = method;

            string accessToken = await _appInfo.GetSPOAccessToken(apiUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            
            request.RequestUri = new Uri(apiUrl);
                        
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));


            if (method == HttpMethod.Post || method == HttpMethod.Put || method.Method == "PATCH")
            {
                request.Content = new StringContent(content, System.Text.Encoding.UTF8);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            }


            return request;
        }

        private async Task<string> SendMessageAsync(HttpRequestMessage message)
        {
            _appInfo.IsCancelled();

            HttpResponseMessage response = await HttpsClient.SendAsync(message, _appInfo.CancelToken);
            
            while (response.StatusCode == (HttpStatusCode)429)
            {
                var retryAfter = response.Headers.RetryAfter;
                if (retryAfter == null || retryAfter.Delta == null) { break; }
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
                if (response.StatusCode == (HttpStatusCode)503)
                {
                    throw new Exception("Error 503. The service is unavailable.");
                }
                else if (response.StatusCode == (HttpStatusCode)401)
                {
                    throw new Exception("Error 401. Unauthorized.");
                }
                else
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    string exceptionMessage = $"Request to SharePoint REST API {message.RequestUri} failed with status code {response.StatusCode} and response content: {responseContent}";

                    throw new Exception(exceptionMessage);
                }
            }
        }
    }
}
