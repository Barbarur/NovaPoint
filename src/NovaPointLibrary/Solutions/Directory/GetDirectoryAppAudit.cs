using NovaPointLibrary.Commands.Directory.Applications;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Context;

namespace NovaPointLibrary.Solutions.Directory;

public class GetDirectoryAppAudit : ISolution
{
    public static readonly string s_SolutionName = "Directory Application Audit";
    public static readonly string s_SolutionDocs = $"https://github.com/Barbarur/NovaPoint/wiki/Solution-{nameof(GetDirectoryAppAudit)}";

    private readonly ContextSolution _ctx;
    private readonly GetDirectoryAppAuditParameters _param;

    private readonly Dictionary<Guid, GraphServicePrincipal> _resourceSpCache = new();

    private GetDirectoryAppAudit(ContextSolution context, GetDirectoryAppAuditParameters parameters)
    {
        _ctx = context;
        _param = parameters;

        Dictionary<Type, string> solutionReports = new()
        {
            { typeof(GetDirectoryAppAuditRecord), "Report" },
        };
        _ctx.DbHandler.AddSolutionReports(solutionReports);
    }

    public static ISolution Create(ContextSolution context, ISolutionParameters parameters)
    {
        return new GetDirectoryAppAudit(context, (GetDirectoryAppAuditParameters)parameters);
    }

    public async Task RunAsync()
    {
        _ctx.AppClient.IsCancelled();
        
        string raSelectedProperties = """
            ?$select=
            id,
            appId,
            publisherDomain,
            notes,
            web,
            isFallbackPublicClient,
            keyCredentials,
            passwordCredentials,
            requiredResourceAccess
            """;

        var appCmd = new MgRegisteredApp(_ctx);
        var allApps = await appCmd.GetAllAsync(raSelectedProperties);
        var appByAppId = allApps.ToDictionary(a => a.AppId, StringComparer.OrdinalIgnoreCase);

        string spSelectedProperties = """
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
            notes,
            appRoleAssignmentRequired
            """;

        var cmd = new MgServicePrincipal(_ctx);
        var collSps = (await cmd.GetAllAsync(spSelectedProperties)).ToList();
        var spAppIdByObjectId = collSps.ToDictionary(sp => sp.Id, sp => sp.AppId);

        var tenantId = _ctx.AppClient.TenantId;

        ProgressTracker progress = new(_ctx.Logger, collSps.Count);
        foreach (var sp in collSps)
        {
            _ctx.AppClient.IsCancelled();

            if (!_param.IncludeMicrosoftApps && sp.AppOwnerOrganizationId == GraphConstants.MicrosoftTenantId)
            {
                continue;
            }

            if (!_param.IncludeThirdPartyApps
                && sp.AppOwnerOrganizationId.HasValue
                && sp.AppOwnerOrganizationId != tenantId
                && sp.AppOwnerOrganizationId != GraphConstants.MicrosoftTenantId)
            {
                continue;
            }

            var record = new GetDirectoryAppAuditRecord(sp, tenantId);
            var spId = sp.Id;

            try
            {
                var owners = await cmd.GetOwnersAsync(spId);
                record.AddServPrincipalOwners(owners);

                appByAppId.TryGetValue(sp.AppId, out GraphApplication? regApp);
                if (regApp != null)
                {
                    record.EnrichWithRegisteredApp(regApp);
                    var appRegOwners = await appCmd.GetOwnersAsync(regApp.Id);
                    record.AddAppRegOwners(appRegOwners);
                }

                var oauthGrants = await cmd.GetOAuth2PermissionGrantsAsync(spId);
                var delegatedPerms = new List<string>();
                foreach (var grant in oauthGrants)
                {
                    var resourceSp = await GetOrFetchResourceSpAsync(cmd, grant.ResourceId);
                    string consentLabel = grant.ConsentType == "AllPrincipals" ? "Admin" : "User";
                    delegatedPerms.Add($"[{consentLabel}] {resourceSp.DisplayName} ({grant.Scope})");
                }
                var appRoleAssignments = (await cmd.GetAppRoleAssignmentsAsync(spId)).ToList();
                var appPerms = new List<string>();
                foreach (var assignment in appRoleAssignments)
                {
                    var resourceSp = await GetOrFetchResourceSpAsync(cmd, assignment.ResourceId);
                    var role = resourceSp.AppRoles.FirstOrDefault(r => r.Id == assignment.AppRoleId);
                    appPerms.Add($"{assignment.ResourceDisplayName} ({role?.Value ?? assignment.AppRoleId.ToString()})");
                }
                record.AddPermissions(string.Join("; ", delegatedPerms), string.Join("; ", appPerms));

                if (regApp != null)
                    record.CompareApplicationPermissions(regApp, appRoleAssignments, spAppIdByObjectId);
            }
            catch (Exception ex)
            {
                _ctx.Logger.Error(GetType().Name, "ServicePrincipal", sp.Id.ToString(), ex);
                record.Remarks = ex.Message;
            }
            finally
            {
                record.SetAssessment();
            }

            AddRecord(record);
            progress.ProgressUpdateReport();
        }
    }

    private async Task<GraphServicePrincipal> GetOrFetchResourceSpAsync(MgServicePrincipal cmd, Guid resourceId)
    {
        if (!_resourceSpCache.TryGetValue(resourceId, out var resourceSp))
        {
            resourceSp = await cmd.GetByIdAsync(resourceId, "?$select=id,displayName,appRoles");
            _resourceSpCache[resourceId] = resourceSp;
        }
        return resourceSp;
    }

    private void AddRecord(GetDirectoryAppAuditRecord record)
    {
        _ctx.DbHandler.WriteRecord(record);
    }
}

internal class GetDirectoryAppAuditRecord : ISolutionRecord
{
    private readonly Guid? _ownerOrgId;
    private readonly Guid _tenantId;

    // Application Properties
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
    public string ServicePrincipalType { get; set; } = string.Empty;
    public string AppOwnerOrganizationId { get; set; } = string.Empty;
    public string AppOwnerOrganization { get; set; } = string.Empty;
    public string PublisherName { get; set; } = string.Empty;
    public string PublisherDomain { get; set; } = string.Empty;
    public string ApplicationAudience { get; set; } = string.Empty;
    public string CreatedDateTime { get; set; } = string.Empty;
    public string EnterpriseAppNotes { get; set; } = string.Empty;
    public string RegisteredAppNotes { get; set; } = string.Empty;

    // Owners & Administration
    public int ServPrincipalOwnersCount { get; set; }
    public string ServPrincipalOwners { get; set; } = string.Empty;
    public int AppRegOwnersCount { get; set; }
    public string AppRegOwners { get; set; } = string.Empty;

    // Sign-On Configuration
    public bool SamlSso { get; set; }
    public bool ExposesApi { get; set; }
    public string ReplyUrls { get; set; } = string.Empty;
    public bool HasWildcardReplyUrl { get; set; }
    public bool ImplicitFlowEnabled { get; set; }
    public bool AccessTokenFlowEnabled { get; set; }
    public bool AllowPublicClient { get; set; }

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
    public bool HasExcessiveApplicationPermissions { get; set; }
    public bool PotentialExcessiveDelegatedPermissions { get; set; }
    public bool HasUnconsentedApplicationPermissions { get; set; }
    public bool HasOrphanedApplicationPermissions { get; set; }
    public string GrantedDelegatedPermissions { get; set; } = string.Empty;
    public string GrantedApplicationPermissions { get; set; } = string.Empty;

    public string NeedsReview { get; set; } = string.Empty;

    // Remarks
    public string Remarks { get; set; } = string.Empty;

    public GetDirectoryAppAuditRecord() { }

    internal GetDirectoryAppAuditRecord(GraphServicePrincipal sp, Guid tenantId)
    {
        _ownerOrgId = sp.AppOwnerOrganizationId;
        _tenantId = tenantId;

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
        EnterpriseAppNotes = string.IsNullOrWhiteSpace(sp.Notes) ? string.Empty : sp.Notes;

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

    internal void AddServPrincipalOwners(IEnumerable<GraphUser> collOwners)
    {
        var ownersList = collOwners.ToList();
        ServPrincipalOwnersCount = ownersList.Count;
        ServPrincipalOwners = string.Join("; ", ownersList.Select(o =>
            string.IsNullOrEmpty(o.UserPrincipalName) ? o.DisplayName : $"{o.DisplayName} ({o.UserPrincipalName})"));
    }

    internal void AddAppRegOwners(IEnumerable<GraphUser> collOwners)
    {
        var ownersList = collOwners.ToList();
        AppRegOwnersCount = ownersList.Count;
        AppRegOwners = string.Join("; ", ownersList.Select(o =>
            string.IsNullOrEmpty(o.UserPrincipalName) ? o.DisplayName : $"{o.DisplayName} ({o.UserPrincipalName})"));
    }

    internal void AddPermissions(string delegated, string application)
    {
        GrantedDelegatedPermissions = delegated;
        GrantedApplicationPermissions = application;
        HasExcessiveApplicationPermissions = PermissionAssessment.ExcessiveKeywords.Any(k => application.Contains(k, StringComparison.OrdinalIgnoreCase));
        PotentialExcessiveDelegatedPermissions = PermissionAssessment.ExcessiveKeywords.Any(k => delegated.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    internal void CompareApplicationPermissions(GraphApplication regApp, IEnumerable<GraphAppRoleAssignment> assignments, Dictionary<Guid, string> spAppIdByObjectId)
    {
        var declaredRoles = regApp.RequiredResourceAccess
            .SelectMany(r => r.ResourceAccess
                .Where(a => a.Type == "Role")
                .Select(a => (ResourceAppId: r.ResourceAppId.ToLowerInvariant(), RoleId: a.Id)))
            .ToHashSet();

        var grantedRoles = assignments
            .Where(a => spAppIdByObjectId.ContainsKey(a.ResourceId))
            .Select(a => (ResourceAppId: spAppIdByObjectId[a.ResourceId].ToLowerInvariant(), RoleId: a.AppRoleId))
            .ToHashSet();

        HasUnconsentedApplicationPermissions = declaredRoles.Any(r => !grantedRoles.Contains(r));
        HasOrphanedApplicationPermissions = grantedRoles.Any(r => !declaredRoles.Contains(r));
    }

    internal void EnrichWithRegisteredApp(GraphApplication app)
    {
        PublisherDomain = app.PublisherDomain;
        RegisteredAppNotes = string.IsNullOrWhiteSpace(app.Notes) ? string.Empty : app.Notes;
        ImplicitFlowEnabled = app.Web.ImplicitGrantSettings.EnableIdTokenIssuance;
        AccessTokenFlowEnabled = app.Web.ImplicitGrantSettings.EnableAccessTokenIssuance;
        AllowPublicClient = app.IsFallbackPublicClient ?? false;
    }

    internal void SetAssessment()
    {
        var flags = new List<string>();
        if (SecretExpired > 0) flags.Add("Expired secret");
        if (CertExpired > 0) flags.Add("Expired certificate");
        if (SecretExpiresIn30Days > 0) flags.Add("Secret expiring soon");
        if (CertExpiresIn30Days > 0) flags.Add("Certificate expiring soon");
        if (HasWildcardReplyUrl) flags.Add("Wildcard reply URL");
        if (ImplicitFlowEnabled) flags.Add("Implicit flow enabled");
        if (AccessTokenFlowEnabled) flags.Add("Access token flow enabled");
        if (HasExcessiveApplicationPermissions) flags.Add("Excessive application permissions");
        if (PotentialExcessiveDelegatedPermissions) flags.Add("Potential excessive delegated permissions");
        if (HasUnconsentedApplicationPermissions) flags.Add("Unconsented application permissions");
        if (HasOrphanedApplicationPermissions) flags.Add("Orphaned application permissions");
        if (_ownerOrgId == _tenantId && ExposesApi) flags.Add("Exposes API");
        if (_ownerOrgId == _tenantId && AllowPublicClient) flags.Add("Public client flows enabled");
        bool expectsOwner = _ownerOrgId == _tenantId || AssignmentRequired;
        if (expectsOwner && ServPrincipalOwnersCount == 0) flags.Add("Service principal: no owners");
        if (_ownerOrgId == _tenantId && AppRegOwnersCount == 0) flags.Add("App registration: no owners");
        NeedsReview = string.Join(" | ", flags);
    }
}

public class GetDirectoryAppAuditParameters : ISolutionParameters
{
    public bool IncludeThirdPartyApps { get; set; } = false;
    public bool IncludeMicrosoftApps { get; set; } = false;
}
