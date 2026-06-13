using Newtonsoft.Json;

namespace NovaPointLibrary.Commands.Utilities.GraphModel;

public class GraphOauth2PermissionScope
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    [JsonProperty("adminConsentDisplayName")]
    public string AdminConsentDisplayName { get; set; } = string.Empty;
    [JsonProperty("adminConsentDescription")]
    public string AdminConsentDescription { get; set; } = string.Empty;
    [JsonProperty("userConsentDescription")]
    public string UserConsentDescription { get; set; } = string.Empty;
    [JsonProperty("userConsentDisplayName")]
    public string UserConsentDisplayName { get; set; } = string.Empty;
    [JsonProperty("value")]
    public string Value { get; set; } = string.Empty;
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;
    [JsonProperty("isEnabled")]
    public bool IsEnabled { get; set; } = false;
}