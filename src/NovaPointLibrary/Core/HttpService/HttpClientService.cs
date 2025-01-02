using System.Net;
using static Microsoft.SharePoint.Client.ClientContextExtensions;
using NovaPointLibrary.Core.Logging;


namespace NovaPointLibrary.Core.HttpService
{
    internal class HttpClientService
    {
        private static readonly string _className = "HttpClientService";
        private static readonly HttpClient _client = new()
        {
            Timeout = TimeSpan.FromMinutes(2),
        };

        internal static async Task<string> SendHttpRequestMessageAsync(ILogger logger, HttpMessageWriter messageWriter, CancellationToken cancellationToken)
        {
            int retryMax = 10;
            int retryCount = 0;
            int backoffInterval = 500;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            while (retryCount < retryMax)
            {
                int waitTime = backoffInterval;
                backoffInterval *= 2;
                retryCount++;

                HttpRequestMessage requestMessage = await messageWriter.GetMessageAsync();
                HttpResponseMessage response;
                try
                {
                    response = await _client.SendAsync(requestMessage, cancellationToken);
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    logger.Info(_className, $"The request timed out. Retrying after {waitTime} miliseconds.");

                    await Task.Delay(waitTime, cancellationToken);
                    continue;
                }
                catch (HttpRequestException e) when (e.InnerException is System.Net.Sockets.SocketException)
                {
                    logger.Info(_className, $"Socket exception: {e.Message}. Retrying after {waitTime} miliseconds.");

                    await Task.Delay(waitTime, cancellationToken);
                    continue;
                }
                catch (HttpRequestException ex)
                {
                    logger.Info(_className, $"An error occurred while sending the request: {ex.Message}. Retrying after {waitTime} miliseconds.");
                    await Task.Delay(waitTime, cancellationToken);
                    continue;
                }
                catch (Exception e)
                {
                    logger.Debug(_className, $"ERROR SENDING MESSAGE TO {requestMessage.RequestUri}. EXCEPTION MESSAGE: {e.Message}.");
                    throw;
                }

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    logger.Info(_className, $"Successful response {responseContent}.");
                    return responseContent;
                }
                else if (response != null && (response.StatusCode == HttpStatusCode.TooManyRequests || response.StatusCode == HttpStatusCode.ServiceUnavailable))
                {
                    var retryAfter = response.Headers.RetryAfter;
                    if (retryAfter != null && retryAfter.Delta != null)
                    {
                        waitTime = retryAfter.Delta.Value.Seconds * 1000;
                    }
                    logger.Info(_className, $"API request exceeding usage limits. Retrying after {waitTime} miliseconds.");

                    await Task.Delay(waitTime);
                }
                else if (response == null)
                {
                    string exceptionMessage = $"Response to API request '{requestMessage.RequestUri}' was null.";
                    throw new Exception(exceptionMessage);
                }
                else
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    string exceptionMessage = $"Request to API '{requestMessage.RequestUri}' failed with status code {response.StatusCode} and response content: {responseContent}.";

                    if (response.Headers.TryGetValues("request-id", out IEnumerable<string>? values))
                    {
                        exceptionMessage += $" Request ID: {values.First()}.";
                    }

                    throw new Exception(exceptionMessage);
                }
            }

            throw new MaximumRetryAttemptedException($"Maximum retry attempts {retryCount}, has be attempted.");

        }
    }
}
