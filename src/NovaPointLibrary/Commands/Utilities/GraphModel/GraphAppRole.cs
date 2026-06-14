using Newtonsoft.Json;

namespace NovaPointLibrary.Commands.Utilities.GraphModel;

public class GraphAppRole
{
    [JsonProperty("allowedMemberTypes")]
    public List<string> AllowedMemberTypes { get; set; } = [];
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;
    [JsonProperty("displayName")]
    public string DisplayName { get; set; } = string.Empty;
    [JsonProperty("id")]
    public Guid Id { get; set; }
    [JsonProperty("isEnabled")]
    public bool IsEnabled { get; set; } = false;
    [JsonProperty("origin")]
    public string Origin { get; set; } = string.Empty;
    [JsonProperty("value")]
    public string? Value { get; set; }
}