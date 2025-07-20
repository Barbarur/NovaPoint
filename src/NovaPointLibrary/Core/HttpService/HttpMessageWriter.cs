using NovaPointLibrary.Commands.Authentication;
using System.Net.Http.Headers;

namespace NovaPointLibrary.Core.HttpService
{
    internal class HttpMessageWriter
    {
        private AppInfo _appInfo;

        private readonly HttpMethod _method;
        private readonly string _UriString;
        private readonly string _content;
        public HttpMessageWriter(AppInfo appInfo, HttpMethod method, string apiUrl, string content = "")
        {
            _appInfo = appInfo;
            _method = method;
            _UriString = apiUrl;
            _content = content;
        }

        internal async Task<HttpRequestMessage> GetMessageAsync()
        {
            if (_UriString.Contains("SharePoint.com", StringComparison.OrdinalIgnoreCase) && _UriString.Contains("_api", StringComparison.OrdinalIgnoreCase))
            {
                return await GetRestMessage();
            }
            else if (_UriString.Contains("https://graph.microsoft.com", StringComparison.OrdinalIgnoreCase))
            {
                return await GetGraphMessage();
            }
            else
            {
                throw new InvalidOperationException("This is neither a Graph or Rest API");
            }
        }

        private async Task<HttpRequestMessage> GetRestMessage()
        {
            HttpRequestMessage request = new()
            {
                Method = _method,
                RequestUri = new Uri(_UriString)
            };

            string accessToken = await _appInfo.GetSPOAccessToken(_UriString);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));


            if (_method == HttpMethod.Post || _method == HttpMethod.Put || _method.Method == "PATCH")
            {
                request.Content = new StringContent(_content, System.Text.Encoding.UTF8);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            }

            return request;
        }

        private async Task<HttpRequestMessage> GetGraphMessage()
        {
            HttpRequestMessage message = new(_method, _UriString);

            string accessToken = await _appInfo.GetGraphAccessToken();
            message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);


            // MISSING:
            // Header.Accept
            // Content
            // Additional Headers
            // Content header content type


            return message;
        }
    }
}
