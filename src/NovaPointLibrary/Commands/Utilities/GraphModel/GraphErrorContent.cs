using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.Utilities.GraphModel
{
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    internal class GraphErrorContent
    {
        public Error Error { get; set; }
    }
    public class Error
    {
        [JsonProperty("code")]
        public string Code { get; set; } = string.Empty;
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
        [JsonProperty("innerError")]
        public InnerError InnerError { get; set; }
    }

    public class InnerError
    {
        public DateTime date { get; set; }

        [JsonProperty("request-id")]
        public string RequestIid { get; set; } = string.Empty;

        [JsonProperty("client-request-id")]
        public string ClientrequestId { get; set; } = string.Empty;
    }
}
