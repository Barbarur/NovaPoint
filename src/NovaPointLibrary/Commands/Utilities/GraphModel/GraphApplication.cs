using Newtonsoft.Json;

namespace NovaPointLibrary.Commands.Utilities.GraphModel;

public class GraphApplication
{
    [JsonProperty("@odata.context")]
    public string? Context { get; set; }
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    // [JsonProperty("deletedDateTime")]
    // public object? DeletedDateTime { get; set; }
    [JsonProperty("appId")]
    public string AppId { get; set; } = string.Empty;
    [JsonProperty("applicationTemplateId")]
    public string ApplicationTemplateId { get; set; } = string.Empty;
    // [JsonProperty("disabledByMicrosoftStatus")]
    // public object? DisabledByMicrosoftStatus { get; set; }
    [JsonProperty("createdByAppId")]
    public string CreatedByAppId { get; set; } = string.Empty;
    [JsonProperty("createdDateTime")]
    public DateTime CreatedDateTime { get; set; } = DateTime.MinValue;
    [JsonProperty("displayName")]
    public string DisplayName { get; set; } = string.Empty;
    // [JsonProperty("description")]
    // public object? Description { get; set; }
    // [JsonProperty("groupMembershipClaims")]
    // public object? GroupMembershipClaims { get; set; }
    [JsonProperty("identifierUris")]
    public List<object> IdentifierUris { get; set; } = [];
    // [JsonProperty("isDeviceOnlyAuthSupported")]
    // public object? IsDeviceOnlyAuthSupported { get; set; }
    // [JsonProperty("isDisabled")]
    // public object? IsDisabled { get; set; }
    [JsonProperty("isFallbackPublicClient")]
    public bool IsFallbackPublicClient { get; set; } = false;
    // [JsonProperty("nativeAuthenticationApisEnabled")]
    // public object? NativeAuthenticationApisEnabled { get; set; }
    [JsonProperty("notes")]
    public string Notes { get; set; } = string.Empty;
    [JsonProperty("publisherDomain")]
    public string PublisherDomain { get; set; } = string.Empty;
    // [JsonProperty("serviceManagementReference")]
    // public object? ServiceManagementReference { get; set; }
    [JsonProperty("signInAudience")]
    public string SignInAudience { get; set; } = string.Empty;
    // [JsonProperty("tags")]
    // public List<object> Tags { get; set; } = [];
    // [JsonProperty("tokenEncryptionKeyId")]
    // public object? TokenEncryptionKeyId { get; set; }
    // [JsonProperty("uniqueName")]
    // public object? UniqueName { get; set; }
    // [JsonProperty("samlMetadataUrl")]
    // public object? SamlMetadataUrl { get; set; }
    // [JsonProperty("defaultRedirectUri")]
    // public object? DefaultRedirectUri { get; set; }
    // [JsonProperty("certification")]
    // public object? Certification { get; set; }
    // [JsonProperty("optionalClaims")]
    // public object? OptionalClaims { get; set; }
    // [JsonProperty("servicePrincipalLockConfiguration")]
    // public object? ServicePrincipalLockConfiguration { get; set; }
    // [JsonProperty("requestSignatureVerification")]
    // public object? RequestSignatureVerification { get; set; }
    // [JsonProperty("addIns")]
    // public List<object> AddIns { get; set; } = [];
    [JsonProperty("api")]
    public Api Api { get; set; } = new();
    [JsonProperty("appRoles")]
    public List<GraphAppRole> AppRoles { get; set; } = [];
    [JsonProperty("info")]
    public Info Info { get; set; } = new();
    [JsonProperty("keyCredentials")]
    public List<GraphKeyCredential> KeyCredentials { get; set; } = [];
    [JsonProperty("parentalControlSettings")]
    public ParentalControlSettings ParentalControlSettings { get; set; } = new();
    [JsonProperty("passwordCredentials")]
    public List<GraphPasswordCredential> PasswordCredentials { get; set; } = [];
    [JsonProperty("publicClient")]
    public PublicClient PublicClient { get; set; } = new();
    // [JsonProperty("requiredResourceAccess")]
    // public List<object> RequiredResourceAccess { get; set; } = [];
    [JsonProperty("verifiedPublisher")]
    public GraphVerifiedPublisher VerifiedPublisher { get; set; } = new();
    [JsonProperty("web")]
    public Web Web { get; set; } = new();
    [JsonProperty("spa")]
    public Spa Spa { get; set; } = new();
}

public class Api
{
    [JsonProperty("acceptMappedClaims")]
    public object? AcceptMappedClaims { get; set; }
    [JsonProperty("knownClientApplications")]
    public List<object> KnownClientApplications { get; set; } = [];
    [JsonProperty("requestedAccessTokenVersion")]
    public object? RequestedAccessTokenVersion { get; set; }
    [JsonProperty("oauth2PermissionScopes")]
    public List<GraphOauth2PermissionScope> Oauth2PermissionScopes { get; set; } = [];
    [JsonProperty("preAuthorizedApplications")]
    public List<object> PreAuthorizedApplications { get; set; } = [];
}

public class ImplicitGrantSettings
{
    [JsonProperty("enableAccessTokenIssuance")]
    public bool EnableAccessTokenIssuance { get; set; } = false;
    [JsonProperty("enableIdTokenIssuance")]
    public bool EnableIdTokenIssuance { get; set; } = false;
}

public class Info
{
    [JsonProperty("logoUrl")]
    public object? LogoUrl { get; set; }
    [JsonProperty("marketingUrl")]
    public object? MarketingUrl { get; set; }
    [JsonProperty("privacyStatementUrl")]
    public object? PrivacyStatementUrl { get; set; }
    [JsonProperty("supportUrl")]
    public object? SupportUrl { get; set; }
    [JsonProperty("termsOfServiceUrl")]
    public object? TermsOfServiceUrl { get; set; }
}


public class ParentalControlSettings
{
    [JsonProperty("countriesBlockedForMinors")]
    public List<object> CountriesBlockedForMinors { get; set; } = [];
    [JsonProperty("legalAgeGroupRule")]
    public string LegalAgeGroupRule { get; set; } = string.Empty;
}

public class PublicClient
{
    [JsonProperty("redirectUris")]
    public List<object> RedirectUris { get; set; } = [];
}

public class RedirectUriSetting
{
    [JsonProperty("uri")]
    public string Uri { get; set; } = string.Empty;
    [JsonProperty("index")]
    public object? Index { get; set; }
}

public class Spa
{
    [JsonProperty("redirectUris")]
    public List<string> RedirectUris { get; set; } = [];
}


public class Web
{
    [JsonProperty("homePageUrl")]
    public string HomePageUrl { get; set; } = string.Empty;
    [JsonProperty("logoutUrl")]
    public string LogoutUrl { get; set; } = string.Empty;
    [JsonProperty("redirectUris")]
    public List<string> RedirectUris { get; set; } = [];
    [JsonProperty("implicitGrantSettings")]
    public ImplicitGrantSettings ImplicitGrantSettings { get; set; } = new();
    [JsonProperty("redirectUriSettings")]
    public List<RedirectUriSetting> RedirectUriSettings { get; set; } = [];
}
