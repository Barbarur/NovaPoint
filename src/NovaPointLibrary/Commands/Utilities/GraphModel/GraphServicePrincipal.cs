using Newtonsoft.Json;

namespace NovaPointLibrary.Commands.Utilities.GraphModel;

public class GraphServicePrincipal
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("appId")]
    public string AppId { get; set; } = string.Empty;

    [JsonProperty("servicePrincipalType")]
    public string ServicePrincipalType { get; set; } = string.Empty;

    [JsonProperty("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonProperty("appOwnerOrganizationId")]
    public string AppOwnerOrganizationId { get; set; } = string.Empty;

    [JsonProperty("createdDateTime")]
    public DateTime? CreatedDateTime { get; set; }

    [JsonProperty("deletedDateTime")]
    public DateTime? DeletedDateTime { get; set; }

    [JsonProperty("signInAudience")]
    public string SignInAudience { get; set; } = string.Empty;

    [JsonProperty("appRoleAssignmentRequired")]
    public bool AppRoleAssignmentRequired { get; set; }

    [JsonProperty("replyUrls")]
    public List<string> ReplyUrls { get; set; } = [];

    [JsonProperty("samlSingleSignOnSettings")]
    public SamlSingleSignOnSettings? SamlSingleSignOnSettings { get; set; }

    [JsonProperty("oauth2PermissionScopes")]
    public List<GraphOauth2PermissionScope> Oauth2PermissionScopes { get; set; } = [];

    [JsonProperty("verifiedPublisher")]
    public GraphVerifiedPublisher VerifiedPublisher { get; set; } = new();

    [JsonProperty("keyCredentials")]
    public List<GraphKeyCredential> KeyCredentials { get; set; } = [];

    [JsonProperty("passwordCredentials")]
    public List<GraphPasswordCredential> PasswordCredentials { get; set; } = [];

    [JsonProperty("appRoles")]
    public List<GraphAppRole> AppRoles { get; set; } = [];
}

public class SamlSingleSignOnSettings
{
    [JsonProperty("relayState")]
    public string? RelayState { get; set; }
}
