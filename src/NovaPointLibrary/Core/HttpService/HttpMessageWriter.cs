using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Logging;
using System.Net.Http.Headers;

namespace NovaPointLibrary.Core.HttpService
{
    internal class HttpMessageWriter(
        ILogger logger,
        IAppClient appInfo,
        HttpMethod method,
        string uriString,
        string accept = "application/json",
        string content = "",
        Dictionary<string, string>? additionalHeaders = null)
    {
        internal async Task<HttpRequestMessage> GetMessageAsync()
        {
            if (uriString.Contains("SharePoint.com", StringComparison.OrdinalIgnoreCase) && uriString.Contains("_api", StringComparison.OrdinalIgnoreCase))
            {
                return GetMessage(await appInfo.GetSPOAccessToken(uriString));
            }
            else if (uriString.Contains("https://graph.microsoft.com", StringComparison.OrdinalIgnoreCase))
            {
                return GetMessage(await appInfo.GetGraphAccessToken());
            }
            else
            {
                throw new InvalidOperationException("This is neither a Graph or Rest API");
            }
        }

        private HttpRequestMessage GetMessage(string accessToken)
        {
            HttpRequestMessage message = new(method, uriString);

            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            message.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(accept));

            if (additionalHeaders != null)
            {
                foreach (var header in additionalHeaders)
                {
                    message.Headers.Add(header.Key, header.Value);
                }
            }

            if (method == HttpMethod.Post || method == HttpMethod.Put || method.Method == "PATCH")
            {
                message.Content = new StringContent(content, System.Text.Encoding.UTF8);
                message.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            }

            LogMessage(message);

            return message;
        }

        private void LogMessage(HttpRequestMessage request)
        {
            //logger.Info(GetType().Name, $"=== HttpRequestMessage ===");
            logger.Info(GetType().Name, $"Method: {request.Method}, Request URI: {request.RequestUri}");

            //logger.Debug(GetType().Name, $"Headers:");
            //foreach (var header in request.Headers)
            //{
            //    logger.Debug(GetType().Name, $"{header.Key}: {header.Value}");
            //}

            //if (request.Content != null)
            //{
            //    logger.Debug(GetType().Name, $"Content Headers:");
            //    foreach (var header in request.Content.Headers)
            //    {
            //        logger.Debug(GetType().Name, $"{header.Key}: {header.Value}");
            //    }

            //    // Read and log the content body (for non-GET requests)
            //    if (request.Method != HttpMethod.Get)
            //    {
            //        var content = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            //        logger.Debug(GetType().Name, $"Body: {content}");
            //    }
            //}

            //logger.Debug(GetType().Name, $"==========================");
        }
    }
}
