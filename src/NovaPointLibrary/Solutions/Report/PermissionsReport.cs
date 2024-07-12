using NovaPointLibrary.Commands.SharePoint.Permision;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.User;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


namespace NovaPointLibrary.Solutions.Report
{
    public class PermissionsReport
    {
        public static readonly string s_SolutionName = "Permissions report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-PermissionsReport";

        private PermissionsReportParameters _param;
        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private readonly Expression<Func<Microsoft.SharePoint.Client.User, object>>[] _userRetrievalExpressions = new Expression<Func<Microsoft.SharePoint.Client.User, object>>[]
        {
            u => u.Id,
            u => u.Title,
            u => u.LoginName,
            u => u.UserPrincipalName,
            u => u.Email,
            u => u.UserId,
        };

        private PermissionsReport(NPLogger logger, Commands.Authentication.AppInfo appInfo, PermissionsReportParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(PermissionsReportParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            NPLogger logger = new(uiAddLog, "PermissionsReport", parameters);
            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new PermissionsReport(logger, appInfo, parameters).RunScriptAsync();

                logger.ScriptFinish();

            }
            catch (Exception ex)
            {
                logger.ScriptFinish(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();

            SPOSitePermissionsCSOM sitePermissions = new(_logger, _appInfo, _param.PermissionsParam);
            await foreach (var siteResults in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param.SiteAccParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (siteResults.Ex != null)
                {
                    AddRecord(new("Site", siteResults.SiteName, siteResults.SiteUrl, new("", "", "", "", siteResults.Ex.Message)));
                    continue;
                }

                if (_param.OnlyUserList)
                {
                    StringBuilder sb = new();
                    
                    await foreach (var oUser in new SPOSiteUserCSOM(_logger, _appInfo).GetAsync(siteResults.SiteUrl, _param.UserParam, _userRetrievalExpressions))
                    {
                        _appInfo.IsCancelled();

                        sb.Append($"{oUser.Title}: {oUser.UserPrincipalName} ");
                    }

                    if (string.IsNullOrWhiteSpace(sb.ToString())) { continue; }
                    AddRecord(new("Site", siteResults.SiteName, siteResults.SiteUrl, new("Site user List", "", sb.ToString(), "", "")));
                
                }
                else
                {
                    if (!await IsTargetSite(siteResults.SiteUrl)) { continue; }

                    try
                    {
                        await foreach(var record in sitePermissions.GetAsync(siteResults.SiteUrl, siteResults.Progress))
                        {
                            _appInfo.IsCancelled();

                            FilterRecord(record);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.ReportError("Site", siteResults.SiteUrl, ex);
                        AddRecord(new("Site", siteResults.SiteName, siteResults.SiteUrl, new("", "", "", "", ex.Message)));
                    }
                }
            }
        }

        private async Task<bool> IsTargetSite(string siteUrl)
        {
            if (_param.UserParam.AllUsers)
            {
                return true;
            }

            await foreach (var oUser in new SPOSiteUserCSOM(_logger, _appInfo).GetAsync(siteUrl, _param.UserParam, _userRetrievalExpressions))
            {
                return true;
            }

            return false;
            
        }

        private void FilterRecord(SPOLocationPermissionsRecord record)
        {

            if (_param.UserParam.AllUsers)
            {
                AddRecord(record);
            }
            else if (!string.IsNullOrWhiteSpace(_param.UserParam.IncludeUserUPN) && record._role.Users.Contains(_param.UserParam.IncludeUserUPN, StringComparison.OrdinalIgnoreCase))
            {
                AddRecord(record);
            }
            else if (_param.UserParam.IncludeExternalUsers && (record._role.AccountType.Contains("#ext#", StringComparison.OrdinalIgnoreCase) || record._role.AccountType.Contains("urn:spo:guest", StringComparison.OrdinalIgnoreCase)))
            {
                AddRecord(record);
            }
            else if (_param.UserParam.IncludeEveryone && record._role.AccountType.Contains("Everyone", StringComparison.OrdinalIgnoreCase))
            {
                AddRecord(record);
            }
            else if (_param.UserParam.IncludeEveryoneExceptExternal && record._role.AccountType.Contains("Everyone except external users", StringComparison.OrdinalIgnoreCase))
            {
                AddRecord(record);
            }
        }

        private void AddRecord(SPOLocationPermissionsRecord record)
        {
            _appInfo.IsCancelled();

            dynamic dynamicRecord = new ExpandoObject();
            dynamicRecord.LocationType = record._locationType;
            dynamicRecord.LocationName = record._locationName;
            dynamicRecord.LocationUrl = record._locationUrl;


            dynamicRecord.AccessType = record._role.AccessType;
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
        public SPOSiteUserParameters UserParam {  get; set; }
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam {  get; set; }
        public SPOSitePermissionsCSOMParameters PermissionsParam {  get; set; }
        public PermissionsReportParameters(SPOSiteUserParameters userParam,
                                           SPOTenantSiteUrlsWithAccessParameters siteParam,
                                           SPOSitePermissionsCSOMParameters permissionParam)
        {
            UserParam = userParam;
            SiteAccParam = siteParam;
            PermissionsParam = permissionParam;
        }
    }
}
