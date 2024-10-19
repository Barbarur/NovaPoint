using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System.Net.Http.Headers;


namespace NovaPointLibrary.Commands.Utilities
{
    internal class RESTAPIHandler
    {
        private readonly NPLogger _logger;
        private AppInfo _appInfo;

        internal RESTAPIHandler(NPLogger logger, AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<string> GetAsync(string apiUrl)
        {
            _appInfo.IsCancelled();

            string response = await _appInfo.SendHttpRequestMessageAsync(GetRequestMessage, HttpMethod.Get, apiUrl, "");

            return response;
        }

        internal async Task<string> PostAsync(string apiUrl, string content)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"HTTP Request Post API '{apiUrl}' content '{content}'.");

            string response = await _appInfo.SendHttpRequestMessageAsync(GetRequestMessage, HttpMethod.Post, apiUrl, content);

            return response;
        }

        private async Task<HttpRequestMessage> GetRequestMessage(HttpMethod method, string apiUrl, string content = "")
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Writing message for '{method}' in '{apiUrl}'");

            HttpRequestMessage request = new()
            {
                Method = method
            };

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
        
    }
}
