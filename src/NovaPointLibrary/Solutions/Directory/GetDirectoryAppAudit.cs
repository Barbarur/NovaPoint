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
            isDisabled,
            keyCredentials,
            passwordCredentials,
            requiredResourceAccess
            """;

        var appCmd = new MgRegisteredApp(_ctx);
        var allApps = await appCmd.GetAllAsync(raSelectedProperties, beta: true);
        var appByAppId = allApps.ToDictionary(a => a.AppId, StringComparer.OrdinalIgnoreCase);

        string spSelectedProperties = """
            ?$select=
            id,
            displayName,
            appId,
            servicePrincipalType,
            accountEnabled,
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

        Dictionary<string, GraphServicePrincipalSignInActivity>? signInActivities = null;
        if (_param.IncludeSignInActivity)
        {
            var signInCmd = new MgServicePrincipalSignInActivity(_ctx);
            signInActivities = await signInCmd.GetAllAsync();
        }

        ProgressTracker progress = new(_ctx.Logger, collSps.Count);
        foreach (var sp in collSps)
        {
            _ctx.AppClient.IsCancelled();

            if (!_param.IncludeNonApplicationServicePrincipals
                && !string.Equals(sp.ServicePrincipalType, "Application", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

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

            bool fetchOwners = sp.AppOwnerOrganizationId switch
            {
                var id when id == tenantId                         => _param.IncludeSpOwnersInternalApps,
                var id when id == GraphConstants.MicrosoftTenantId => _param.IncludeSpOwnersMicrosoftApps,
                _                                                  => _param.IncludeSpOwnersThirdPartyApps
            };

            // Synchronous enrichment before any async work
            appByAppId.TryGetValue(sp.AppId, out GraphApplication? regApp);
            if (regApp != null)
                record.EnrichWithRegisteredApp(regApp);

            // Launch all independent Graph API calls concurrently
            Task<IEnumerable<GraphUser>>? spOwnersTask =
                fetchOwners ? cmd.GetOwnersAsync(spId) : null;

            Task<IEnumerable<GraphUser>>? appRegOwnersTask =
                regApp != null ? appCmd.GetOwnersAsync(regApp.Id) : null;

            var oauthGrantsTask        = cmd.GetOAuth2PermissionGrantsAsync(spId);
            var appRoleAssignmentsTask = cmd.GetAppRoleAssignmentsAsync(spId);

            if (spOwnersTask != null)
            {
                try { record.AddServPrincipalOwners(await spOwnersTask); }
                catch (Exception ex)
                {
                    _ctx.Logger.Error(GetType().Name, "GetOwnersAsync(SP)", spId.ToString(), ex);
                    record.AppendRemark($"SP owners error: {ex.Message}");
                }
            }

            if (appRegOwnersTask != null)
            {
                try { record.AddAppRegOwners(await appRegOwnersTask); }
                catch (Exception ex)
                {
                    _ctx.Logger.Error(GetType().Name, "GetOwnersAsync(AppReg)", regApp!.Id.ToString(), ex);
                    record.AppendRemark($"App reg owners error: {ex.Message}");
                }
            }

            IEnumerable<GraphOAuth2PermissionGrant>? oauthGrants = null;
            try { oauthGrants = await oauthGrantsTask; }
            catch (Exception ex)
            {
                _ctx.Logger.Error(GetType().Name, "GetOAuth2PermissionGrantsAsync", spId.ToString(), ex);
                record.AppendRemark($"Delegated permissions error: {ex.Message}");
            }

            IList<GraphAppRoleAssignment>? appRoleAssignments = null;
            try { appRoleAssignments = (await appRoleAssignmentsTask).ToList(); }
            catch (Exception ex)
            {
                _ctx.Logger.Error(GetType().Name, "GetAppRoleAssignmentsAsync", spId.ToString(), ex);
                record.AppendRemark($"Application permissions error: {ex.Message}");
            }

            var delegatedPerms = new List<string>();
            if (oauthGrants != null)
            {
                foreach (var grant in oauthGrants)
                {
                    try
                    {
                        var resourceSp = await GetOrFetchResourceSpAsync(cmd, grant.ResourceId);
                        string consentLabel = grant.ConsentType == "AllPrincipals" ? "Admin" : "User";
                        delegatedPerms.Add($"[{consentLabel}] {resourceSp.DisplayName} ({grant.Scope})");
                    }
                    catch (Exception ex)
                    {
                        _ctx.Logger.Error(GetType().Name, "ResourceSP(delegated)", grant.ResourceId.ToString(), ex);
                        record.AppendRemark($"Resource SP lookup error ({grant.ResourceId}): {ex.Message}");
                    }
                }
            }

            var appPerms = new List<string>();
            if (appRoleAssignments != null)
            {
                foreach (var assignment in appRoleAssignments)
                {
                    try
                    {
                        var resourceSp = await GetOrFetchResourceSpAsync(cmd, assignment.ResourceId);
                        var role = resourceSp.AppRoles.FirstOrDefault(r => r.Id == assignment.AppRoleId);
                        appPerms.Add($"{assignment.ResourceDisplayName} ({role?.Value ?? assignment.AppRoleId.ToString()})");
                    }
                    catch (Exception ex)
                    {
                        _ctx.Logger.Error(GetType().Name, "ResourceSP(appRole)", assignment.ResourceId.ToString(), ex);
                        record.AppendRemark($"Resource SP lookup error ({assignment.ResourceId}): {ex.Message}");
                    }
                }
            }

            record.AddPermissions(string.Join("; ", delegatedPerms), string.Join("; ", appPerms));

            if (regApp != null && appRoleAssignments != null)
                record.CompareApplicationPermissions(regApp, appRoleAssignments, spAppIdByObjectId);

            if (signInActivities != null)
            {
                signInActivities.TryGetValue(sp.AppId, out GraphServicePrincipalSignInActivity? signInActivity);
                record.EnrichWithSignInActivity(signInActivity);
            }

            record.SetAssessment();

            AddRecord(record);
            progress.ProgressUpdateReport();
        }
    }

    private async Task<GraphServicePrincipal> GetOrFetchResourceSpAsync(MgServicePrincipal cmd, Guid resourceId)
    {
        if (!_resourceSpCache.TryGetValue(resourceId, out var resourceSp))
        {
            var fetched = await cmd.GetByIdAsync(resourceId, "?$select=id,displayName,appRoles");
            resourceSp = fetched ?? new GraphServicePrincipal { Id = resourceId, DisplayName = resourceId.ToString() };
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
    private bool _spOwnersFetched;
    private bool _signInActivityFetched;

    // Application Properties
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
    public string AppActivationStatus { get; set; } = string.Empty;
    public bool SignInEnabled { get; set; }
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

    private int _spSecretValid;
    private int _spSecretExpired;
    private int _spSecretExpiresIn30Days;
    private int _spCertValid;
    private int _spCertExpired;
    private int _spCertExpiresIn30Days;
    private int _appRegSecretValid;
    private int _appRegSecretExpired;
    private int _appRegSecretExpiresIn30Days;
    private int _appRegCertValid;
    private int _appRegCertExpired;
    private int _appRegCertExpiresIn30Days;

    public string SpSecretStatus { get; set; } = "None";
    public string SpCertStatus { get; set; } = "None";
    public string SpSecretCertDetails { get; set; } = string.Empty;
    public string AppRegSecretStatus { get; set; } = "None";
    public string AppRegCertStatus { get; set; } = "None";
    public string AppRegSecretCertDetails { get; set; } = string.Empty;

    // Sign-In Activity
    public bool HasSignInLast30Days { get; set; }
    public bool HasDelegatedSignInLast30Days { get; set; }
    public bool HasApplicationSignInLast30Days { get; set; }

    // Users & Groups
    public bool AssignmentRequired { get; set; }

    // Permissions
    public string GrantedDelegatedPermissions { get; set; } = string.Empty;
    public string GrantedApplicationPermissions { get; set; } = string.Empty;
    public bool HasExcessiveApplicationPermissions { get; set; }
    public bool PotentialExcessiveDelegatedPermissions { get; set; }
    public bool HasUnconsentedApplicationPermissions { get; set; }
    public bool HasOrphanedApplicationPermissions { get; set; }

    public string NeedsReview { get; set; } = string.Empty;

    // Remarks
    public string Remarks { get; set; } = string.Empty;

    internal void AppendRemark(string msg)
    {
        Remarks = string.IsNullOrEmpty(Remarks) ? msg : Remarks + " | " + msg;
    }

    public GetDirectoryAppAuditRecord() { }

    internal GetDirectoryAppAuditRecord(GraphServicePrincipal sp, Guid tenantId)
    {
        _ownerOrgId = sp.AppOwnerOrganizationId;
        _tenantId = tenantId;

        Id = sp.Id.ToString();
        DisplayName = sp.DisplayName;
        AppId = sp.AppId;
        ServicePrincipalType = sp.ServicePrincipalType;
        SignInEnabled = sp.AccountEnabled;
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

        foreach (var secret in sp.PasswordCredentials)
        {
            if (secret.EndDateTime.HasValue && secret.EndDateTime < DateTime.UtcNow)
                _spSecretExpired++;
            else
            {
                _spSecretValid++;
                if (secret.EndDateTime.HasValue && secret.EndDateTime < DateTime.UtcNow.AddDays(30))
                    _spSecretExpiresIn30Days++;
            }
        }
        SpSecretStatus = BuildCredentialStatus(_spSecretValid, _spSecretExpiresIn30Days, _spSecretExpired);

        foreach (var cert in sp.KeyCredentials)
        {
            if (cert.EndDateTime.HasValue && cert.EndDateTime < DateTime.UtcNow)
                _spCertExpired++;
            else
            {
                _spCertValid++;
                if (cert.EndDateTime.HasValue && cert.EndDateTime < DateTime.UtcNow.AddDays(30))
                    _spCertExpiresIn30Days++;
            }
        }
        SpCertStatus = BuildCredentialStatus(_spCertValid, _spCertExpiresIn30Days, _spCertExpired);
        SpSecretCertDetails = BuildDetailedCredentialSummary(_spSecretValid, _spSecretExpired, _spCertValid, _spCertExpired);

        AssignmentRequired = sp.AppRoleAssignmentRequired;
    }

    internal void AddServPrincipalOwners(IEnumerable<GraphUser> collOwners)
    {
        _spOwnersFetched = true;
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

    internal void EnrichWithSignInActivity(GraphServicePrincipalSignInActivity? activity)
    {
        _signInActivityFetched = true;

        if (activity == null) return;

        var cutoff = DateTime.UtcNow.AddDays(-30);

        HasSignInLast30Days = MostRecent(activity.LastSignInActivity) > cutoff;
        HasDelegatedSignInLast30Days = MostRecent(activity.DelegatedClientSignInActivity) > cutoff;
        HasApplicationSignInLast30Days = MostRecent(activity.ApplicationAuthenticationClientSignInActivity) > cutoff;
    }

    private static DateTime? MostRecent(GraphSignInActivity? a) =>
        a == null ? null : new[] { a.LastSignInDateTime, a.LastNonInteractiveSignInDateTime, a.LastSuccessfulSignInDateTime }
            .Where(d => d.HasValue).Max();

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
        AppActivationStatus = (app.IsDisabled ?? false) ? "Deactivated" : "Active";
        RegisteredAppNotes = string.IsNullOrWhiteSpace(app.Notes) ? string.Empty : app.Notes;
        ImplicitFlowEnabled = app.Web.ImplicitGrantSettings.EnableIdTokenIssuance;
        AccessTokenFlowEnabled = app.Web.ImplicitGrantSettings.EnableAccessTokenIssuance;
        AllowPublicClient = app.IsFallbackPublicClient ?? false;

        foreach (var secret in app.PasswordCredentials)
        {
            if (secret.EndDateTime.HasValue && secret.EndDateTime < DateTime.UtcNow)
                _appRegSecretExpired++;
            else
            {
                _appRegSecretValid++;
                if (secret.EndDateTime.HasValue && secret.EndDateTime < DateTime.UtcNow.AddDays(30))
                    _appRegSecretExpiresIn30Days++;
            }
        }
        AppRegSecretStatus = BuildCredentialStatus(_appRegSecretValid, _appRegSecretExpiresIn30Days, _appRegSecretExpired);

        foreach (var cert in app.KeyCredentials)
        {
            if (cert.EndDateTime.HasValue && cert.EndDateTime < DateTime.UtcNow)
                _appRegCertExpired++;
            else
            {
                _appRegCertValid++;
                if (cert.EndDateTime.HasValue && cert.EndDateTime < DateTime.UtcNow.AddDays(30))
                    _appRegCertExpiresIn30Days++;
            }
        }
        AppRegCertStatus = BuildCredentialStatus(_appRegCertValid, _appRegCertExpiresIn30Days, _appRegCertExpired);
        AppRegSecretCertDetails = BuildDetailedCredentialSummary(_appRegSecretValid, _appRegSecretExpired, _appRegCertValid, _appRegCertExpired);
    }

    private static string BuildCredentialStatus(int valid, int expiresIn30Days, int expired)
    {
        if (valid == 0 && expired == 0) return "None";
        return valid == 0 ? "Expired" : "Valid";
    }

    private static string BuildDetailedCredentialSummary(int secretValid, int secretExpired, int certValid, int certExpired)
    {
        var parts = new List<string>();
        if (secretValid > 0 || secretExpired > 0)
        {
            var s = new List<string>();
            if (secretValid > 0) s.Add($"{secretValid} valid");
            if (secretExpired > 0) s.Add($"{secretExpired} expired");
            parts.Add($"Secret: {string.Join(", ", s)}");
        }
        if (certValid > 0 || certExpired > 0)
        {
            var c = new List<string>();
            if (certValid > 0) c.Add($"{certValid} valid");
            if (certExpired > 0) c.Add($"{certExpired} expired");
            parts.Add($"Certificate: {string.Join(", ", c)}");
        }
        return string.Join(" | ", parts);
    }

    internal void SetAssessment()
    {
        var flags = new List<string>();
        if (!SignInEnabled) flags.Add("Application disabled");
        if (AppActivationStatus == "Deactivated") flags.Add("Application deactivated");
        if (_spSecretExpired > 0 && _spSecretValid == 0) flags.Add("Service principal: Expired secret");
        else if (_spSecretExpired > 0) flags.Add("Service principal: Need clean up old secret");
        if (_spCertExpired > 0 && _spCertValid == 0) flags.Add("Service principal: Expired certificate");
        else if (_spCertExpired > 0) flags.Add("Service principal: Need clean up old certificate");
        if (_spSecretExpiresIn30Days > 0) flags.Add("Service principal: Secret expiring soon");
        if (_spCertExpiresIn30Days > 0) flags.Add("Service principal: Certificate expiring soon");
        if (_appRegSecretExpired > 0 && _appRegSecretValid == 0) flags.Add("App registration: Expired secret");
        else if (_appRegSecretExpired > 0) flags.Add("App registration: Need clean up old secret");
        if (_appRegCertExpired > 0 && _appRegCertValid == 0) flags.Add("App registration: Expired certificate");
        else if (_appRegCertExpired > 0) flags.Add("App registration: Need clean up old certificate");
        if (_appRegSecretExpiresIn30Days > 0) flags.Add("App registration: Secret expiring soon");
        if (_appRegCertExpiresIn30Days > 0) flags.Add("App registration: Certificate expiring soon");
        if (HasWildcardReplyUrl) flags.Add("Wildcard reply URL");
        if (ImplicitFlowEnabled) flags.Add("Implicit flow enabled");
        if (AccessTokenFlowEnabled) flags.Add("Access token flow enabled");
        if (HasExcessiveApplicationPermissions) flags.Add("Excessive application permissions");
        if (PotentialExcessiveDelegatedPermissions) flags.Add("Potential excessive delegated permissions");
        if (HasUnconsentedApplicationPermissions) flags.Add("Unconsented application permissions");
        if (HasOrphanedApplicationPermissions) flags.Add("Orphaned application permissions");
        if (_ownerOrgId == _tenantId && ExposesApi) flags.Add("Exposes API");
        if (_ownerOrgId == _tenantId && AllowPublicClient) flags.Add("Public client flows enabled");
        if (_spOwnersFetched)
        {
            bool expectsOwner = _ownerOrgId == _tenantId || AssignmentRequired;
            if (expectsOwner && ServPrincipalOwnersCount == 0) flags.Add("Service principal: No owners");
        }
        if (_ownerOrgId == _tenantId && AppRegOwnersCount == 0) flags.Add("App registration: No owners");
        if (_signInActivityFetched && !HasSignInLast30Days) flags.Add("No recent sign-in (30 days)");
        NeedsReview = string.Join(" | ", flags);
    }
}

public class GetDirectoryAppAuditParameters : ISolutionParameters
{
    public bool IncludeThirdPartyApps { get; set; } = false;
    public bool IncludeMicrosoftApps { get; set; } = false;

    public bool IncludeSpOwnersInternalApps { get; set; } = true;
    public bool IncludeSpOwnersMicrosoftApps { get; set; } = false;
    public bool IncludeSpOwnersThirdPartyApps { get; set; } = false;

    public bool IncludeSignInActivity { get; set; } = false;

    public bool IncludeNonApplicationServicePrincipals { get; set; } = false;
}
