using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.Utilities.RESTModel
{
    public class RESTErrorContent
    {
        [JsonProperty("odata.error")]
        public Error Error { get; set; }
    }
    public class Error
    {
        [JsonProperty("code")]
        public string Code { get; set; } = string.Empty;
        [JsonProperty("message")]
        public Message Message { get; set; }
    }
    
    public class Message
    {
        [JsonProperty("lang")]
        public string Language { get; set; } = string.Empty;
        [JsonProperty("value")]
        public string Value { get; set; } = string.Empty;
    }


}
