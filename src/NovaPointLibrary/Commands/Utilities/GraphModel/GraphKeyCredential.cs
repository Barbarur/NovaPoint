using Newtonsoft.Json;

namespace NovaPointLibrary.Commands.Utilities.GraphModel;

public class GraphKeyCredential
{
        [JsonProperty("@odata.type")]
        public string? DataType { get; set; }
        [JsonProperty("customKeyIdentifier")]
        public string CustomKeyIdentifier { get; set; } = string.Empty;
        [JsonProperty("displayName")]
        public string DisplayName { get; set; } = string.Empty;
        [JsonProperty("endDateTime")]
        public DateTime EndDateTime { get; set; } = DateTime.MinValue;
        [JsonProperty("key")]
        public object? Key { get; set; }
        [JsonProperty("keyId")]
        public string KeyId { get; set; } = string.Empty;
        [JsonProperty("startDateTime")]
        public DateTime StartDateTime { get; set; } = DateTime.MinValue;
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;
        [JsonProperty("usage")]
        public string Usage { get; set; } = string.Empty;
}