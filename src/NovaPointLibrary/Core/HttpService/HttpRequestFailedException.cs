using System.Net;

namespace NovaPointLibrary.Core.HttpService
{
    // Carries the status code so callers can tell an expected answer, such as a 404 for a
    // deleted object, from a real failure instead of parsing the exception message.
    internal class HttpRequestFailedException(HttpStatusCode statusCode, string message) : Exception(message)
    {
        internal HttpStatusCode StatusCode { get; init; } = statusCode;
    }
}
