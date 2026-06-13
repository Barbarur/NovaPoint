using NovaPointLibrary.Commands.Directory.Applications;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Context;

namespace NovaPointLibrary.Solutions.Directory;

public class GetDirectoryApp : ISolution
{
    public static readonly string s_SolutionName = "Directory Applications report";
    public static readonly string s_SolutionDocs = $"https://github.com/Barbarur/NovaPoint/wiki/Solution-{nameof(GetDirectoryApp)}";

    private ContextSolution Ctx;
    private readonly GetDirectoryAppParameters _param;

    private Dictionary<string, string> _spObjectIdByAppId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, GraphServicePrincipal> _resourceSpCache = new(StringComparer.OrdinalIgnoreCase);

    private GetDirectoryApp(ContextSolution context, GetDirectoryAppParameters parameters)
    {
        Ctx = context;
        _param = parameters;
        //
        Dictionary<Type, string> solutionReports = new()
        {
            { typeof(GetDirectoryAppRecord), "Report" },
        };
        Ctx.DbHandler.AddSolutionReports(solutionReports);
    }
    
    public static ISolution Create(ContextSolution context, ISolutionParameters parameters)
    {
        return new GetDirectoryApp(context, (GetDirectoryAppParameters)parameters);
    }
    
    public async Task RunAsync()
    {
        var spCmd = new MgServicePrincipal(Ctx);
        var allSps = await spCmd.GetAllAsync("?$select=id,appId");
        _spObjectIdByAppId = allSps.ToDictionary(sp => sp.AppId, sp => sp.Id, StringComparer.OrdinalIgnoreCase);

        string selectedProperties = $"""
                                     ?$select=
                                     id,
                                     displayName,
                                     appId,
                                     publisherDomain,
                                     signInAudience,
                                     createdDateTime,
                                     notes,
                                     verifiedPublisher,
                                     web,
                                     spa,
                                     publicClient,
                                     isFallbackPublicClient,
                                     keyCredentials,
                                     passwordCredentials,
                                     requiredResourceAccess
                                     """;

        var appCmd = new MgRegisteredApp(Ctx);
        var collApps = (await appCmd.GetAllAsync(selectedProperties)).ToList();

        ProgressTracker progress = new(Ctx.Logger, collApps.Count);
        foreach (var app in collApps)
        {
            Ctx.AppClient.IsCancelled();
            var record = new GetDirectoryAppRecord(app);
            try
            {
                var owners = await appCmd.GetOwnersAsync(app.Id);
                record.AddOwners(owners);
                await BuildPermissionsAsync(record, app, spCmd);
                record.SetAssessment();
            }
            catch (Exception ex)
            {
                Ctx.Logger.Error(GetType().Name, app.DisplayName, app.Id, ex);
                record.Remarks = ex.Message;
            }
            AddRecord(record);
            progress.ProgressUpdateReport();
        }
    }
    
    private void AddRecord(GetDirectoryAppRecord record)
    {
        Ctx.DbHandler.WriteRecord(record);
    }

    private async Task BuildPermissionsAsync(GetDirectoryAppRecord record, GraphApplication app, MgServicePrincipal spCmd)
    {
        List<GraphOAuth2PermissionGrant> grants;
        List<GraphAppRoleAssignment> roleAssignments;

        if (_spObjectIdByAppId.TryGetValue(app.AppId, out var spObjectId))
        {
            var spId = Guid.Parse(spObjectId);
            grants = (await spCmd.GetOAuth2PermissionGrantsAsync(spId)).ToList();
            roleAssignments = (await spCmd.GetAppRoleAssignmentsAsync(spId)).ToList();
        }
        else
        {
            grants = [];
            roleAssignments = [];
        }

        var delegatedPerms = new List<string>();
        var appPerms = new List<string>();

        foreach (var resourceAccess in app.RequiredResourceAccess)
        {
            var resourceSp = await GetOrFetchResourceSpAsync(resourceAccess.ResourceAppId, spCmd);
            if (resourceSp == null) throw new InvalidOperationException($"No resource found with App Id '{resourceAccess.ResourceAppId}'");

            var scopeEntries = resourceAccess.ResourceAccess.Where(r => r.Type == "Scope").ToList();
            if (scopeEntries.Any())
                delegatedPerms.Add($"{resourceSp.DisplayName} ({BuildScopePermissions(resourceSp, scopeEntries, grants)})");

            var roleEntries = resourceAccess.ResourceAccess.Where(r => r.Type == "Role").ToList();
            if (roleEntries.Any())
                appPerms.Add($"{resourceSp.DisplayName} ({BuildRolePermissions(resourceSp, roleEntries, roleAssignments)})");
        }

        record.AddPermissions(string.Join("; ", delegatedPerms), string.Join("; ", appPerms));
    }

    private async Task<GraphServicePrincipal?> GetOrFetchResourceSpAsync(string resourceAppId, MgServicePrincipal spCmd)
    {
        if (!_resourceSpCache.TryGetValue(resourceAppId, out var resourceSp))
        {
            resourceSp = await spCmd.GetByAppIdAsync(resourceAppId, "&$select=id,appId,displayName,appRoles,oauth2PermissionScopes");
            if (resourceSp != null)
                _resourceSpCache[resourceAppId] = resourceSp;
        }
        return resourceSp;
    }

    private string BuildScopePermissions(GraphServicePrincipal resourceSp, IEnumerable<GraphResourceAccess> entries, IEnumerable<GraphOAuth2PermissionGrant> grants)
    {
        var parts = new List<string>();
        foreach (var entry in entries)
        {
            var scope = resourceSp.Oauth2PermissionScopes.FirstOrDefault(s => s.Id.Equals(entry.Id, StringComparison.OrdinalIgnoreCase));
            var permValue = scope?.Value ?? entry.Id;
            var grant = grants.FirstOrDefault(g =>
                g.Scope.Split(' ').Contains(permValue, StringComparer.OrdinalIgnoreCase));
            var consentLabel = grant == null ? "Not Consented" : (grant.ConsentType == "AllPrincipals" ? "Admin" : "User");
            parts.Add($"{permValue} [{consentLabel}]");
        }
        return string.Join("; ", parts);
    }

    private string BuildRolePermissions(GraphServicePrincipal resourceSp, IEnumerable<GraphResourceAccess> entries, IEnumerable<GraphAppRoleAssignment> roleAssignments)
    {
        var parts = new List<string>();
        foreach (var entry in entries)
        {
            var role = resourceSp.AppRoles.FirstOrDefault(r => r.Id.Equals(entry.Id, StringComparison.OrdinalIgnoreCase));
            var permValue = role?.Value ?? entry.Id;
            var consentLabel = roleAssignments.Any(a => a.AppRoleId.Equals(entry.Id, StringComparison.OrdinalIgnoreCase))
                ? "Consented" : "Not Consented";
            parts.Add($"{permValue} [{consentLabel}]");
        }
        return string.Join("; ", parts);
    }
}

internal class GetDirectoryAppRecord : ISolutionRecord
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string PublisherDomain { get; set; } = string.Empty;
    public string ApplicationAudience { get; set; } = string.Empty;
    public string CreatedDate { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string VerifiedPublisher { get; set; } = string.Empty;
    public bool HasWildcardRedirectUri { get; set; } = false;
    public string RedirectUris { get; set; } = string.Empty;
    public string LogoutUrl { get; set; } = string.Empty;
    public bool ImplicitFlowEnabled { get; set; } = false;
    public bool AccessTokenFlowEnabled { get; set; } = false;
    public bool AllowPublicClient { get; set; } = false;
    public int SecretCount { get; set; } = 0;
    public int SecretsValid { get; set; } = 0;
    public int SecretExpired { get; set; } = 0;
    public int SecretExpiringIn30Days { get; set; } = 0;
    public int CertCount { get; set; } = 0;
    public int CertsValid { get; set; } = 0;
    public int CertExpired { get; set; } = 0;
    public int CertExpiringIn30Days { get; set; } = 0;
    public int OwnersCount { get; set; } = 0;
    public string Owners { get; set; } = string.Empty;
    public string DelegatedPermissions { get; set; } = string.Empty;
    public string ApplicationPermissions { get; set; } = string.Empty;
    public bool HasExcessiveAccess { get; set; } = false;
    public bool PotentialExcessiveAccess { get; set; } = false;
    public string NeedsReview { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;

    public GetDirectoryAppRecord() { }
    
    internal GetDirectoryAppRecord(GraphApplication app)
    {
        this.Id = app.Id;
        this.DisplayName = app.DisplayName;
        this.ClientId = app.AppId;
        this.PublisherDomain = app.PublisherDomain;

        if (app.SignInAudience == "AzureADMyOrg")
        {
            this.ApplicationAudience = "Single tenant";
        }
        else if (app.SignInAudience == "AzureADMultipleOrgs")
        {
            this.ApplicationAudience = "Multitenant";
        }
        else if (app.SignInAudience == "AzureADandPersonalMicrosoftAccount")
        {
            this.ApplicationAudience = "Multitenant and Personal";
        }
        else if (app.SignInAudience == "PersonalMicrosoftAccount")
        {
            this.ApplicationAudience = "Personal only";
        }
        else
        {
            this.ApplicationAudience = $"Unknown '{app.SignInAudience}'";
        }

        this.CreatedDate = app.CreatedDateTime.ToString("yyyy-MM-dd HH:mm:ss");
        this.Notes = string.IsNullOrWhiteSpace(app.Notes) ? string.Empty : app.Notes ;

        VerifiedPublisher = string.IsNullOrEmpty(app.VerifiedPublisher.DisplayName)
            ? "No"
            : $"Yes - {app.VerifiedPublisher.DisplayName}";

        var allRedirectUris = app.Web.RedirectUris
            .Concat(app.Spa.RedirectUris)
            .Concat(app.PublicClient.RedirectUris)
            .ToList();
        RedirectUris = string.Join("; ", allRedirectUris);
        HasWildcardRedirectUri = allRedirectUris.Any(u => u.Contains('*'));
        LogoutUrl = string.IsNullOrWhiteSpace(app.Web.LogoutUrl) ? string.Empty : app.Web.LogoutUrl;
        ImplicitFlowEnabled = app.Web.ImplicitGrantSettings.EnableIdTokenIssuance;
        AccessTokenFlowEnabled = app.Web.ImplicitGrantSettings.EnableAccessTokenIssuance;
        AllowPublicClient = app.IsFallbackPublicClient;

        SecretCount = app.PasswordCredentials.Count;
        foreach (var secret in app.PasswordCredentials)
        {
            if (secret.EndDateTime.HasValue && secret.EndDateTime < DateTime.UtcNow)
                SecretExpired++;
            else
            {
                SecretsValid++;
                if (secret.EndDateTime.HasValue && secret.EndDateTime < DateTime.UtcNow.AddDays(30))
                    SecretExpiringIn30Days++;
            }
        }

        CertCount = app.KeyCredentials.Count;
        foreach (var cert in app.KeyCredentials)
        {
            if (cert.EndDateTime.HasValue && cert.EndDateTime < DateTime.UtcNow)
                CertExpired++;
            else
            {
                CertsValid++;
                if (cert.EndDateTime.HasValue && cert.EndDateTime < DateTime.UtcNow.AddDays(30))
                    CertExpiringIn30Days++;
            }
        }
    }

    internal void AddOwners(IEnumerable<GraphUser> collOwners)
    {
        var ownersList = collOwners.ToList();
        OwnersCount = ownersList.Count;
        Owners = ownersList.Count == 0
            ? "None"
            : string.Join(" | ", ownersList.Select(o =>
                string.IsNullOrEmpty(o.UserPrincipalName) ? o.DisplayName : $"{o.DisplayName} ({o.UserPrincipalName})"));
    }

    internal void AddPermissions(string delegated, string application)
    {
        DelegatedPermissions = delegated;
        ApplicationPermissions = application;
        var excessiveKeywords = new[] { "write", "edit", "manage", "fullcontrol" };
        HasExcessiveAccess = excessiveKeywords.Any(k => application.Contains(k, StringComparison.OrdinalIgnoreCase));
        PotentialExcessiveAccess = excessiveKeywords.Any(k => delegated.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    internal void SetAssessment()
    {
        var flags = new List<string>();
        if (SecretExpired > 0)        flags.Add("Expired secret");
        if (CertExpired > 0)          flags.Add("Expired certificate");
        if (HasWildcardRedirectUri)   flags.Add("Wildcard redirect URI");
        if (ImplicitFlowEnabled)      flags.Add("Implicit flow enabled");
        if (AccessTokenFlowEnabled)   flags.Add("Access token flow enabled");
        if (HasExcessiveAccess)       flags.Add("Excessive permissions");
        if (PotentialExcessiveAccess) flags.Add("Potential excessive access");
        if (OwnersCount == 0)         flags.Add("No owners");
        NeedsReview = string.Join(" | ", flags);
    }
}

public class GetDirectoryAppParameters : ISolutionParameters
{
    

}