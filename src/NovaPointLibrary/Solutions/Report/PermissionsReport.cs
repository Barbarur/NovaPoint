using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.AzureAD.Groups;
using NovaPointLibrary.Commands.SharePoint.Permission;
using NovaPointLibrary.Commands.SharePoint.Permission.Utilities;
using NovaPointLibrary.Commands.SharePoint.SharingLinks;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.SiteGroup;
using NovaPointLibrary.Commands.SharePoint.User;
using NovaPointLibrary.Core.Logging;
using System.Dynamic;
using System.Linq.Expressions;
using System.Text;



namespace NovaPointLibrary.Solutions.Report
{
    public class PermissionsReport
    {
        public static readonly string s_SolutionName = "Permissions report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-PermissionsReport";

        private PermissionsReportParameters _param;
        private readonly LoggerSolution _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private readonly Expression<Func<Microsoft.SharePoint.Client.User, object>>[] _userRetrievalExpressions = new Expression<Func<Microsoft.SharePoint.Client.User, object>>[]
        {
            u => u.Id,
            u => u.Title,
            u => u.LoginName,
            u => u.UserPrincipalName,
            u => u.Email,
            u => u.UserId,
            u => u.PrincipalType,
            u => u.AadObjectId
        };

        private readonly SPOKnownRoleAssignmentGroups _knownGroups = new();

        private PermissionsReport(LoggerSolution logger, Commands.Authentication.AppInfo appInfo, PermissionsReportParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(PermissionsReportParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            LoggerSolution logger = new(uiAddLog, "PermissionsReport", parameters);
            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new PermissionsReport(logger, appInfo, parameters).RunScriptAsync();

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

            SPOSitePermissionsCSOM sitePermissions = new(_logger, _appInfo, _param.PermissionsParam);
            await foreach (var siteRecord in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param.SiteAccParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (siteRecord.Ex != null)
                {
                    AddRecord(new("Site", siteRecord.SiteName, siteRecord.SiteUrl, SPORoleAssignmentUserRecord.GetRecordBlankException(siteRecord.Ex.Message)));
                    continue;
                }

                try
                {
                    await ProcessSite(siteRecord, sitePermissions);
                }
                catch (Exception ex)
                {
                    _logger.Error(GetType().Name, "Site", siteRecord.SiteUrl, ex);
                    AddRecord(new("Site", siteRecord.SiteName, siteRecord.SiteUrl, SPORoleAssignmentUserRecord.GetRecordBlankException(ex.Message)));
                }
                //if (_param.OnlyUserList)
                //{
                //    StringBuilder sb = new();

                //    await foreach (var oUser in new SPOSiteUserCSOM(_logger, _appInfo).GetAsync(siteRecord.SiteUrl, _param.UserParam, _userRetrievalExpressions))
                //    {
                //        _appInfo.IsCancelled();

                //        sb.Append($"{oUser.Title}: {oUser.UserPrincipalName} ");
                //    }

                //    if (string.IsNullOrWhiteSpace(sb.ToString())) { continue; }

                //    SPORoleAssignmentUserRecord record = new("Site user List", "NA", "");
                //    AddRecord(new("Site", siteRecord.SiteName, siteRecord.SiteUrl, record.GetRecordWithUsers("Site user List", sb.ToString())));
                //}
                //else
                //{
                //    if (!await IsTargetSite(siteRecord.SiteUrl)) { continue; }

                //    try
                //    {
                //        await foreach(var record in sitePermissions.GetAsync(siteRecord.SiteUrl, siteRecord.Progress))
                //        {
                //            _appInfo.IsCancelled();

                //            if (IsTargetRecord(record))
                //            {
                //                AddRecord(record);
                //            }

                //        }
                //    }
                //    catch (Exception ex)
                //    {
                //        _logger.Error(GetType().Name, "Site", siteRecord.SiteUrl, ex);
                //        AddRecord(new("Site", siteRecord.SiteName, siteRecord.SiteUrl, SPORoleAssignmentUserRecord.GetRecordBlankException(ex.Message)));
                //    }
                //}

            }
        }

        private async Task ProcessSite(SPOTenantSiteUrlsRecord siteRecord, SPOSitePermissionsCSOM sitePermissions)
        {
            if (_param.OnlyUserList)
            {
                StringBuilder sb = new();

                await foreach (var oUser in new SPOSiteUserCSOM(_logger, _appInfo).GetAsync(siteRecord.SiteUrl, _param.UserParam, _userRetrievalExpressions))
                {
                    _appInfo.IsCancelled();

                    sb.Append($"{oUser.Title}: {oUser.UserPrincipalName} ");
                }

                if (string.IsNullOrWhiteSpace(sb.ToString())) { return; }

                SPORoleAssignmentUserRecord record = new("Site user List", "NA", "");
                AddRecord(new("Site", siteRecord.SiteName, siteRecord.SiteUrl, record.GetRecordWithUsers("Site user List", sb.ToString())));
            }
            else
            {
                if (!await IsTargetSite(siteRecord.SiteUrl)) { return; }

                await foreach (var record in sitePermissions.GetAsync(siteRecord.SiteUrl, siteRecord.Progress))
                {
                    _appInfo.IsCancelled();

                    if (IsTargetRecord(record))
                    {
                        AddRecord(record);
                    }

                }

            }
        }

        private async Task<bool> IsTargetSite(string siteUrl)
        {
            _logger.Info(GetType().Name, $"Checking if site {siteUrl} is target site");
            
            if (_param.UserParam.AllUsers)
            {
                return true;
            }

            await foreach (var oUser in new SPOSiteUserCSOM(_logger, _appInfo).GetAsync(siteUrl, _param.UserParam, _userRetrievalExpressions))
            {
                return true;
            }

            if (_param.UserParam.Detailed)
            {
                if (await IsTargetSecurityGroup(siteUrl))
                {
                    return true;
                }

                if (await IsTargetInsideSharingLink(siteUrl))
                {
                    return true;
                }


            }

            return false;
            
        }

        private async Task<bool> IsTargetSecurityGroup(string siteUrl)
        {
            _logger.Info(GetType().Name, $"Checking if site {siteUrl} has target Security Groups");

            var collSiteUsers = await new SPOSiteUserCSOM(_logger, _appInfo).GetAsync(siteUrl, _userRetrievalExpressions);

            if (collSiteUsers != null)
            {
                var collSecurityGroups = collSiteUsers.Where(gm => gm.PrincipalType.ToString() == "SecurityGroup").ToList();

                foreach (var securityGroup in collSecurityGroups)
                {
                    var collSgUsersRecord = await new AADGroup(_logger, _appInfo).GetUsersAsync(securityGroup, _knownGroups.SecurityGroups);

                    foreach (var sgUsersRecord in collSgUsersRecord)
                    {
                        SPORoleAssignmentUserRecord role = new("", "", sgUsersRecord.AccountType, sgUsersRecord.Users, "", "");
                        if (IsTargetRole(role))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private async Task<bool> IsTargetInsideSharingLink(string siteUrl)
        {
            _logger.Info(GetType().Name, $"Checking if site {siteUrl} has target Sharing Links");

            var collGroups = await new SPOSiteGroupCSOM(_logger, _appInfo).GetSharingLinksAsync(siteUrl);

            SpoSharingLinksRest spoLinks = new(_logger, _appInfo, _knownGroups.SharingLinks);
            foreach (Group oGroup in collGroups)
            {
                _appInfo.IsCancelled();

                var linkInfo = await spoLinks.GetFromGroupAsync(siteUrl, oGroup);
                SPORoleAssignmentUserRecord role = new($"Sharing link '{linkInfo.SharingLink}'", linkInfo.GroupId, "User", linkInfo.Users, "", "");

                if (IsTargetRole(role))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsTargetRecord(SPOLocationPermissionsRecord record)
        {
            if (!string.IsNullOrWhiteSpace(record._role.Remarks))
            {
                return true;
            }

            if (_param.UserParam.AllUsers)
            {
                return true;
            }

            if (IsTargetRole(record._role))
            {
                return true;
            }

            return false;
        }

        private bool IsTargetRole(SPORoleAssignmentUserRecord role)
        {

            if (!string.IsNullOrWhiteSpace(_param.UserParam.IncludeUserUPN))
            {
                if (role.Users.Contains(_param.UserParam.IncludeUserUPN, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (role.AccessType.Contains("Sharing link", StringComparison.OrdinalIgnoreCase))
                {
                    if (role.AccessType.Contains("organization", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    if (role.AccessType.Contains("Anyone", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }


            if (_param.UserParam.IncludeExternalUsers)
            {
                if (role.Users.Contains("#ext#", StringComparison.OrdinalIgnoreCase) || role.Users.Contains("urn:spo:guest", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                else if (role.AccessType.Contains("Anyone") && role.AccessType.Contains("Sharing link", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }


            if (_param.UserParam.IncludeEveryone && role.AccountType.Contains("Everyone", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (_param.UserParam.IncludeEveryoneExceptExternal && role.AccountType.Contains("Everyone except external users", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }


        private void AddRecord(SPOLocationPermissionsRecord record)
        {
            _appInfo.IsCancelled();

            dynamic dynamicRecord = new ExpandoObject();
            dynamicRecord.LocationType = record._locationType;
            dynamicRecord.LocationName = record._locationName;
            dynamicRecord.LocationUrl = record._locationUrl;

            dynamicRecord.AccessType = record._role.AccessType;
            dynamicRecord.GroupId = record._role.GroupId;
            dynamicRecord.AccountType = record._role.AccountType;
            dynamicRecord.Users = record._role.Users;
            dynamicRecord.PermissionLevels = record._role.PermissionLevels;

            dynamicRecord.Remarks = record._role.Remarks;

            _logger.DynamicCSV(dynamicRecord);

        }

    }

    public class PermissionsReportParameters : ISolutionParameters
    {
        public bool OnlyUserList { get; set; } = false;
        public SPOSiteUserParameters UserParam { get; set; }
        public SPOAdminAccessParameters AdminAccess { get; set; }
        public SPOTenantSiteUrlsParameters SiteParam { get; set; }
        internal SPOTenantSiteUrlsWithAccessParameters SiteAccParam
        {
            get
            {
                return new(AdminAccess, SiteParam);
            }
        }
        public SPOSitePermissionsCSOMParameters PermissionsParam {  get; set; }
        
        public PermissionsReportParameters(
            SPOSiteUserParameters userParam,
            SPOAdminAccessParameters adminAccess, 
            SPOTenantSiteUrlsParameters siteParam, 
            SPOSitePermissionsCSOMParameters permissionParam)
        {
            UserParam = userParam;
            AdminAccess = adminAccess;
            SiteParam = siteParam;
            PermissionsParam = permissionParam;
        }
    }
}
