using Newtonsoft.Json;

namespace NovaPointLibrary.Commands.Utilities.GraphModel;

public class GraphPasswordCredential
{
    [JsonProperty("customKeyIdentifier")]
    public object? CustomKeyIdentifier { get; set; }
    [JsonProperty("displayName")]
    public string DisplayName { get; set; } = string.Empty;
    [JsonProperty("endDateTime")]
    public DateTime EndDateTime { get; set; } = DateTime.MinValue;
    [JsonProperty("hint")]
    public string Hint { get; set; } = string.Empty;
    [JsonProperty("keyId")]
    public string KeyId { get; set; } = string.Empty;
    [JsonProperty("secretText")]
    public object? SecretText { get; set; }
    [JsonProperty("startDateTime")]
    public DateTime StartDateTime { get; set; } = DateTime.MinValue;
}