using Newtonsoft.Json;

namespace NovaPointLibrary.Commands.Utilities.GraphModel;

public class GraphOAuth2PermissionGrant
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [JsonProperty("consentType")]
    public string ConsentType { get; set; } = string.Empty;

    [JsonProperty("resourceId")]
    public string ResourceId { get; set; } = string.Empty;

    [JsonProperty("scope")]
    public string Scope { get; set; } = string.Empty;
}
