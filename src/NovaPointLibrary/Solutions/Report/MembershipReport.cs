using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.AzureAD.Groups;
using NovaPointLibrary.Commands.Directory;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.SiteGroup;
using NovaPointLibrary.Core.Logging;
using System.Linq.Expressions;


namespace NovaPointLibrary.Solutions.Report
{
    public class MembershipReport
    {
        public static readonly string s_SolutionName = "Site Membership report";
        public static readonly string s_SolutionDocs = $"https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-{typeof(MembershipReport).Name}";

        private MembershipReportParameters _param;
        private readonly LoggerSolution _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private static readonly Expression<Func<SiteProperties, object>>[] _sitePropertiesExpressions =
        [
            p => p.Title,
            p => p.Url,
            p => p.GroupId,
            p => p.Template,
            p => p.IsTeamsConnected,
            p => p.TeamsChannelType,
            p => p.Owner,
            p => p.OwnerEmail,
            p => p.OwnerLoginName,
            p => p.OwnerName,

            p => p.IsGroupOwnerSiteAdmin,
        ];

        private static readonly Expression<Func<Web, object>>[] _webExpressions =
        [
            w => w.HasUniqueRoleAssignments,
            w => w.Id,
            w => w.Title,
            w => w.Url,
            w => w.WebTemplate,
        ];

        private readonly List<DirectoryGroupUserEmails>? _listKnownGroups = new();

        private MembershipReport(LoggerSolution logger, Commands.Authentication.AppInfo appInfo, MembershipReportParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(MembershipReportParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            parameters.SiteParam.SitePropertiesExpressions = _sitePropertiesExpressions;
            parameters.SiteParam.WebExpressions = _webExpressions;

            LoggerSolution logger = new(uiAddLog, typeof(MembershipReport).Name, parameters);
            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new MembershipReport(logger, appInfo, parameters).RunScriptAsync();

                logger.SolutionFinish();

            }
            catch (Exception ex)
            {
                logger.SolutionFinish(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();

            var emptyGuid = Guid.Empty;
            _logger.Debug(GetType().Name, $"Blank Guid: {emptyGuid}");

            await foreach (var siteRecord in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param.SiteAccParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (siteRecord.Ex != null)
                {
                    AddRecord(new(siteRecord.SiteUrl, siteRecord.Ex.Message));
                    continue;
                }

                try
                {
                    await ProcessSite(siteRecord);
                }
                catch (Exception ex)
                {
                    _logger.Error(GetType().Name, "Site", siteRecord.SiteUrl, ex);
                    AddRecord(new(siteRecord.SiteUrl, ex.Message));
                }
            }
        }
        private async Task ProcessSite(SPOTenantSiteUrlsRecord siteRecord)
        {
            _appInfo.IsCancelled();

            if (siteRecord.SiteProperties != null)
            {
                await ProcessSiteCollection(siteRecord.SiteProperties);
            }

            else if (siteRecord.Web != null)
            {
                await ProcessSubsite(siteRecord.Web);
            }

            else
            {
                Web web = await new SPOWebCSOM(_logger, _appInfo).GetAsync(siteRecord.SiteUrl, _webExpressions);

                if (web.IsSubSite())
                {
                    await ProcessSubsite(web);
                }
                else
                {
                    var oSiteProperties = await new SPOSiteCollectionCSOM(_logger, _appInfo).GetAsync(web.Url, _sitePropertiesExpressions);
                    await ProcessSiteCollection(oSiteProperties);
                }
            }

        }

        private async Task ProcessSiteCollection(SiteProperties siteProperties)
        {
            string template = siteProperties.Template;
            _logger.Debug(GetType().Name, $"Site Template: {siteProperties.Template}");

            MembershipReportRecord record = new(siteProperties.Title, siteProperties.Url, SPOWeb.GetSiteTemplateName(siteProperties.Template, siteProperties.IsTeamsConnected), "False");

            await GetMembershipUsersAsync(GetSiteCollectionAdminsAsync, record, "Site Admins");

            if (siteProperties.Template.Contains("SPSPERS", StringComparison.OrdinalIgnoreCase)) { return; }

            if (siteProperties.GroupId != Guid.Empty)
            {
                await GetMS365GroupOwnersAdminsAsync(record, siteProperties.GroupId.ToString(), "Owners");

                await GetMS365GroupMembersAdminsAsync(record, siteProperties.GroupId.ToString(), "Members");
            }

            await GetSiteMembershipUsersAsync(record);
        }

        private async Task ProcessSubsite(Web web)
        {
            MembershipReportRecord record = new(web.Title, web.Url, SPOWeb.GetSiteTemplateName(web.WebTemplate, false), "True");

            if (!web.HasUniqueRoleAssignments)
            {
                string m = "Inherits Site Membership";
                AddRecord(record.ReportUsers(m, m, m));
            }
            else
            {
                await GetSiteMembershipUsersAsync(record);
            }
        }

        private async Task GetSiteMembershipUsersAsync(MembershipReportRecord record)
        {
            await GetMembershipUsersAsync(GetSiteOwnersAsync, record, "Site Owners");

            await GetMembershipUsersAsync(GetSiteMembersAsync, record, "Site Members");

            await GetMembershipUsersAsync(GetSiteVisitorsAsync, record, "Site Visitors");
        }

        private async Task GetMembershipUsersAsync(Func<MembershipReportRecord, string, Task> getMembershipUsers, MembershipReportRecord record, string membership)
        {
            _appInfo.IsCancelled();

            try
            {
                await getMembershipUsers(record, membership);
            }
            catch (Exception ex)
            {
                _logger.Error(GetType().Name, "Site", record.SiteUrl, ex);
                AddRecord(record.ReportError(membership, ex));
            }
        }

        private async Task GetSiteCollectionAdminsAsync(MembershipReportRecord record, string membership)
        {
            _appInfo.IsCancelled();

            if (!_param.MembershipParam.SiteAdmins) { return; }

            IEnumerable<User> collUsers = await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).GetAsync(record.SiteUrl);

            await ProcessUsersAsync(record, membership, collUsers);
        }

        private async Task GetMS365GroupOwnersAdminsAsync(MembershipReportRecord record, string groupId, string membership)
        {
            _appInfo.IsCancelled();

            if (!_param.MembershipParam.Owners) { return; }

            try
            {
                var listSecGroupUsers = await new DirectoryGroupUser(_logger, _appInfo).GetUsersAsync($"{record.SiteTitle} Owners", Guid.Parse(groupId), true, _listKnownGroups);

                AddRecord(record.ReportAadGroupUsers(membership, listSecGroupUsers));
            }
            catch (Exception ex)
            {
                _logger.Error(GetType().Name, "Site", record.SiteUrl, ex);
                AddRecord(record.ReportError(membership, ex));
            }
        }

        private async Task GetMS365GroupMembersAdminsAsync(MembershipReportRecord record, string groupId, string membership)
        {
            _appInfo.IsCancelled();
            
            if (!_param.MembershipParam.Members) { return; }

            try
            {
                var listSecGroupUsers = await new DirectoryGroupUser(_logger, _appInfo).GetUsersAsync($"{record.SiteTitle} Members", Guid.Parse(groupId), false, _listKnownGroups);

                AddRecord(record.ReportAadGroupUsers(membership, listSecGroupUsers));
            }
            catch (Exception ex)
            {
                _logger.Error(GetType().Name, "Site", record.SiteUrl, ex);
                AddRecord(record.ReportError(membership, ex));
            }
        }

        private async Task GetSiteOwnersAsync(MembershipReportRecord record, string membership)
        {
            if (!_param.MembershipParam.SiteOwners) { return; }

            var userCollection = await new SPOAssociatedGroup(_logger, _appInfo).GetSiteOwnersAsync(record.SiteUrl);

            await ProcessUsersAsync(record, membership, userCollection.Users.ToList());
        }

        private async Task GetSiteMembersAsync(MembershipReportRecord record, string membership)
        {
            if (!_param.MembershipParam.SiteMembers) { return; }

            var userCollection = await new SPOAssociatedGroup(_logger, _appInfo).GetSiteMembersAsync(record.SiteUrl);

            await ProcessUsersAsync(record, membership, userCollection.Users.ToList());
        }

        private async Task GetSiteVisitorsAsync(MembershipReportRecord record, string membership)
        {
            if (!_param.MembershipParam.SiteVisitors) { return; }

            var userCollection = await new SPOAssociatedGroup(_logger, _appInfo).GetSiteVisitorsAsync(record.SiteUrl);

            await ProcessUsersAsync(record, membership, userCollection.Users.ToList());
        }

        private async Task ProcessUsersAsync(MembershipReportRecord record, string membership, IEnumerable<User> collUsers)
        {
            _appInfo.IsCancelled();

            if (collUsers.Any())
            {
                string users = String.Join(" ", collUsers.Where(sca => sca.PrincipalType.ToString() == "User").Select(sca => sca.UserPrincipalName).ToList());

                if (!string.IsNullOrWhiteSpace(users))
                {
                    AddRecord(record.ReportUsers(membership, "User", users));
                }

                var collSecurityGroups = collUsers.Where(gm => gm.PrincipalType.ToString() == "SecurityGroup").ToList();

                foreach (var secGroup in collSecurityGroups)
                {
                    _appInfo.IsCancelled();

                    if (secGroup.Title.Contains("SLinkClaim")) { continue; }

                    var listSecGroupUsers = await new DirectoryGroupUser(_logger, _appInfo).GetUsersAsync(secGroup, _listKnownGroups);

                    AddRecord(record.ReportAadGroupUsers(membership, listSecGroupUsers));
                }
            }
            else
            {
                AddRecord(record.ReportUsers(membership, "No users found", "No users found"));
            }
        }

        private void AddRecord(MembershipReportRecord record)
        {
            _logger.RecordCSV(record);
        }
    }

    public class MembershipReportRecord : ISolutionRecord
    {
        internal string SiteTitle { get; set; } = String.Empty;
        internal string SiteUrl { get; set; } = String.Empty;
        internal string SiteTemplate { get; set; } = String.Empty;
        internal string IsSubsite { get; set; } = String.Empty;
        internal string Membership { get; set; } = String.Empty;
        internal string AccountType { get; set; } = String.Empty;
        internal string Users { get; set; } = String.Empty;
        internal string Remarks { get; set; } = String.Empty;

        internal MembershipReportRecord(string siteUrl, string errorMessage)
        {
            SiteUrl = siteUrl;
            Remarks = errorMessage;
        }

        internal MembershipReportRecord(string siteTitle, string siteUrl, string siteTemplate, string isSubsite)
        {
            SiteTitle = siteTitle;
            SiteUrl = siteUrl;
            SiteTemplate = siteTemplate;
            IsSubsite = isSubsite;
        }

        internal MembershipReportRecord ReportError(string membership, Exception ex)
        {
            MembershipReportRecord r = new(SiteTitle, SiteUrl, SiteTemplate, IsSubsite)
            {
                Membership = membership,
                Remarks = ex.Message,
            };

            return r;
        }

        internal MembershipReportRecord ReportUsers(string membership, string accountType, string users)
        {
            MembershipReportRecord r = new(SiteTitle, SiteUrl, SiteTemplate, IsSubsite)
            {
                Membership = membership,
                AccountType = accountType,
                Users = users,
            };

            return r;
        }

        internal MembershipReportRecord ReportAadGroupUsers(string membership, DirectoryGroupUserEmails aadGroupUsers)
        {
            MembershipReportRecord r = new(SiteTitle, SiteUrl, SiteTemplate, IsSubsite)
            {
                Membership = membership,
                AccountType = aadGroupUsers.AccountType,
                Users = aadGroupUsers.Users,
                Remarks = aadGroupUsers.Remarks,
            };

            return r;
        }
    }

    public class MembershipParameters : ISolutionParameters
    {
        public bool SiteAdmins { get; set; } = false;
        public bool Owners { get; set; } = false;
        public bool Members { get; set; } = false;
        public bool SiteOwners { get; set; } = false;
        public bool SiteMembers { get; set; } = false;
        public bool SiteVisitors { get; set; } = false;
    }

    public class MembershipReportParameters : ISolutionParameters
    {
        public MembershipParameters MembershipParam { get; set; }

        internal readonly SPOAdminAccessParameters AdminAccess;
        internal readonly SPOTenantSiteUrlsParameters SiteParam;
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam
        {
            get
            {
                return new(AdminAccess, SiteParam);
            }
        }

        public MembershipReportParameters(MembershipParameters membership, SPOAdminAccessParameters adminAccess, SPOTenantSiteUrlsParameters siteParam)
        {
            MembershipParam = membership;
            AdminAccess = adminAccess;
            SiteParam = siteParam;
        }
    }
    
}