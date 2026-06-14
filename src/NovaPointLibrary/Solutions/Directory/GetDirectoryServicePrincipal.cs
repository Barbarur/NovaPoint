using NovaPointLibrary.Commands.Directory.Applications;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Context;

namespace NovaPointLibrary.Solutions.Directory;

public class GetDirectoryServicePrincipal : ISolution
{
    public static readonly string s_SolutionName = "Directory Enterprise Apps (Service Principals) report";
    public static readonly string s_SolutionDocs = $"https://github.com/Barbarur/NovaPoint/wiki/Solution-{nameof(GetDirectoryServicePrincipal)}";

    private readonly ContextSolution _ctx;
    private readonly GetDirectoryServicePrincipalParameters _param;

    private readonly Dictionary<Guid, GraphServicePrincipal> _resourceSpCache = new();

    private GetDirectoryServicePrincipal(ContextSolution context, GetDirectoryServicePrincipalParameters parameters)
    {
        _ctx = context;
        _param = parameters;

        Dictionary<Type, string> solutionReports = new()
        {
            { typeof(GetDirectoryServicePrincipalRecord), "Report" },
        };
        _ctx.DbHandler.AddSolutionReports(solutionReports);
    }

    public static ISolution Create(ContextSolution context, ISolutionParameters parameters)
    {
        return new GetDirectoryServicePrincipal(context, (GetDirectoryServicePrincipalParameters)parameters);
    }

    public async Task RunAsync()
    {
        _ctx.AppClient.IsCancelled();

        string selectedProperties = """
                                     ?$select=
                                     id,
                                     displayName,
                                     appId,
                                     servicePrincipalType,
                                     appOwnerOrganizationId,
                                     VerifiedPublisher,
                                     signInAudience,
                                     createdDateTime,
                                     samlSingleSignOnSettings,
                                     oauth2PermissionScopes,
                                     replyUrls,
                                     keyCredentials,
                                     passwordCredentials,
                                     appRoleAssignmentRequired,
                                     """;
        
        var cmd = new MgServicePrincipal(_ctx);
        var collSps = (await cmd.GetAllAsync(selectedProperties)).ToList();

        ProgressTracker progress = new(_ctx.Logger, collSps.Count);
        foreach (var sp in collSps)
        {
            _ctx.AppClient.IsCancelled();

            var record = new GetDirectoryServicePrincipalRecord(sp, _ctx.AppClient.TenantId);

            try
            {
                var owners = await cmd.GetOwnersAsync(sp.Id);
                record.AddOwners(owners);

                var oauthGrants = await cmd.GetOAuth2PermissionGrantsAsync(sp.Id);
                var delegatedPerms = new List<string>();
                foreach (var grant in oauthGrants)
                {
                    string resourceName = await GetOrFetchResourceNameAsync(cmd, grant.ResourceId);
                    string consentLabel = grant.ConsentType == "AllPrincipals" ? "Admin" : "User";
                    delegatedPerms.Add($"[{consentLabel}] {resourceName} ({grant.Scope})");
                }
                record.GrantedDelegatedPermissions = string.Join("; ", delegatedPerms);

                var appRoleAssignments = await cmd.GetAppRoleAssignmentsAsync(sp.Id);
                var appPerms = new List<string>();
                foreach (var assignment in appRoleAssignments)
                {
                    string roleValue = await GetOrFetchAppRoleValueAsync(cmd, assignment.ResourceId, assignment.AppRoleId);
                    appPerms.Add($"{assignment.ResourceDisplayName} ({roleValue})");
                }
                record.GrantedApplicationPermissions = string.Join("; ", appPerms);
            }
            catch (Exception ex)
            {
                _ctx.Logger.Error(GetType().Name, "ServicePrincipal", sp.Id.ToString(), ex);
                record.Remarks = ex.Message;
            }

            AddRecord(record);
            progress.ProgressUpdateReport();
        }
    }

    private async Task<string> GetOrFetchResourceNameAsync(MgServicePrincipal cmd, Guid resourceId)
    {
        if (!_resourceSpCache.TryGetValue(resourceId, out var resourceSp))
        {
            resourceSp = await cmd.GetByIdAsync(resourceId, "?$select=id,displayName,appRoles");
            _resourceSpCache[resourceId] = resourceSp;
        }
        return resourceSp.DisplayName;
    }

    private async Task<string> GetOrFetchAppRoleValueAsync(MgServicePrincipal cmd, Guid resourceId, Guid appRoleId)
    {
        if (!_resourceSpCache.TryGetValue(resourceId, out var resourceSp))
        {
            resourceSp = await cmd.GetByIdAsync(resourceId, "?$select=id,displayName,appRoles");
            _resourceSpCache[resourceId] = resourceSp;
        }
        var role = resourceSp.AppRoles.FirstOrDefault(r => r.Id == appRoleId);
        return role?.Value ?? appRoleId.ToString();
    }

    private void AddRecord(GetDirectoryServicePrincipalRecord record)
    {
        _ctx.DbHandler.WriteRecord(record);
    }
}

internal class GetDirectoryServicePrincipalRecord : ISolutionRecord
{
    // Application Properties
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
    public string ServicePrincipalType { get; set; } = string.Empty;
    public string AppOwnerOrganizationId { get; set; } = string.Empty;
    public string AppOwnerOrganization { get; set; } = string.Empty;
    public string PublisherName { get; set; } = string.Empty;
    public string ApplicationAudience { get; set; } = string.Empty;
    public string CreatedDateTime { get; set; } = string.Empty;
    
    // Owners & Administration
    public int OwnersTotal { get; set; }
    public string Owners { get; set; } = string.Empty;
    
    // Sign-On Configuration
    public bool SamlSso { get; set; }
    public bool ExposesApi { get; set; }
    public string ReplyUrls { get; set; } = string.Empty;
    public bool HasWildcardReplyUrl { get; set; }
    
    public int SecretCount { get; set; }
    public int SecretsValid { get; set; }
    public int SecretExpired { get; set; }
    public int SecretExpiresIn30Days { get; set; }
    public int CertCount { get; set; }
    public int CertsValid { get; set; }
    public int CertExpired { get; set; }
    public int CertExpiresIn30Days { get; set; }

    // Users & Groups
    public bool AssignmentRequired { get; set; }

    // Permissions
    public string GrantedDelegatedPermissions { get; set; } = string.Empty;
    public string GrantedApplicationPermissions { get; set; } = string.Empty;
    
    // Remarks
    public string Remarks { get; set; } = string.Empty;

    public GetDirectoryServicePrincipalRecord() { }

    internal GetDirectoryServicePrincipalRecord(GraphServicePrincipal sp, Guid tenantId)
    {
        Id = sp.Id.ToString();
        DisplayName = sp.DisplayName;
        AppId = sp.AppId;
        ServicePrincipalType = sp.ServicePrincipalType;
        AppOwnerOrganizationId = sp.AppOwnerOrganizationId?.ToString() ?? string.Empty;
        AppOwnerOrganization = sp.AppOwnerOrganizationId switch
        {
            null                                               => string.Empty,
            var id when id == tenantId                         => "Internal",
            var id when id == GraphConstants.MicrosoftTenantId => "Microsoft",
            _                                                  => "Third Party"
        };

        PublisherName = sp.VerifiedPublisher.DisplayName ?? string.Empty;

        ApplicationAudience = sp.SignInAudience switch
        {
            "AzureADMyOrg" => "Single tenant",
            "AzureADMultipleOrgs" => "Multitenant",
            "AzureADandPersonalMicrosoftAccount" => "Multitenant and Personal",
            "PersonalMicrosoftAccount" => "Personal only",
            _ => string.IsNullOrEmpty(sp.SignInAudience) ? string.Empty : $"Unknown '{sp.SignInAudience}'"
        };

        CreatedDateTime = sp.CreatedDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;


        
        SamlSso = sp.SamlSingleSignOnSettings != null;
        ExposesApi = sp.Oauth2PermissionScopes.Any();
        ReplyUrls = string.Join("; ", sp.ReplyUrls);
        HasWildcardReplyUrl = sp.ReplyUrls.Any(u => u.Contains('*'));
        
        SecretCount = sp.PasswordCredentials.Count;
        foreach (var secret in sp.PasswordCredentials)
        {
            if (secret.EndDateTime.HasValue && secret.EndDateTime < DateTime.UtcNow)
                SecretExpired++;
            else
            {
                SecretsValid++;
                if (secret.EndDateTime.HasValue && secret.EndDateTime < DateTime.UtcNow.AddDays(30))
                    SecretExpiresIn30Days++;
            }
        }

        CertCount = sp.KeyCredentials.Count;
        foreach (var cert in sp.KeyCredentials)
        {
            if (cert.EndDateTime.HasValue && cert.EndDateTime < DateTime.UtcNow)
                CertExpired++;
            else
            {
                CertsValid++;
                if (cert.EndDateTime.HasValue && cert.EndDateTime < DateTime.UtcNow.AddDays(30))
                    CertExpiresIn30Days++;
            }
        }
        
        
        
        AssignmentRequired = sp.AppRoleAssignmentRequired;

    }

    internal void AddOwners(IEnumerable<GraphUser> collOwners)
    {
        var ownersList = collOwners.ToList();
        OwnersTotal = ownersList.Count;
        Owners = string.Join("; ", ownersList.Select(o =>
            string.IsNullOrEmpty(o.UserPrincipalName) ? o.DisplayName : o.UserPrincipalName));
    }
    
}

public class GetDirectoryServicePrincipalParameters : ISolutionParameters
{
}
