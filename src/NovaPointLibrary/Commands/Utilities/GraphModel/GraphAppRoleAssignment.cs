using Newtonsoft.Json;

namespace NovaPointLibrary.Commands.Utilities.GraphModel;

public class GraphAppRoleAssignment
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("appRoleId")]
    public string AppRoleId { get; set; } = string.Empty;

    [JsonProperty("resourceId")]
    public string ResourceId { get; set; } = string.Empty;

    [JsonProperty("resourceDisplayName")]
    public string ResourceDisplayName { get; set; } = string.Empty;

    [JsonProperty("principalId")]
    public string PrincipalId { get; set; } = string.Empty;

    [JsonProperty("principalDisplayName")]
    public string PrincipalDisplayName { get; set; } = string.Empty;

    [JsonProperty("principalType")]
    public string PrincipalType { get; set; } = string.Empty;

    [JsonProperty("createdDateTime")]
    public DateTime CreatedDateTime { get; set; } = DateTime.MinValue;
}
