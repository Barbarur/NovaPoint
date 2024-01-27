using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
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
using static NovaPointLibrary.Commands.SharePoint.Permision.SPOSitePermissionsCSOM;

namespace NovaPointLibrary.Solutions.Report
{
    public class PermissionsReport
    {
        public static readonly string s_SolutionName = "Permissions report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-PermissionsReport";

        private PermissionsReportParameters _param = new();
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (PermissionsReportParameters)value; }
        }

        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        public PermissionsReport(PermissionsReportParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            Parameters = parameters;
            _logger = new(uiAddLog, this.GetType().Name, parameters);
            _appInfo = new(_logger, cancelTokenSource);
        }

        public async Task RunAsync()
        {
            try
            {
                await RunScriptAsync();

                _logger.ScriptFinish();
            }
            catch (Exception ex)
            {
                _logger.ScriptFinish(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();

            SPOSitePermissionsCSOM sitePermissions = new(_logger, _appInfo, _param);
            await foreach (var siteResults in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param).GetAsync())
            {

                if (!String.IsNullOrWhiteSpace(siteResults.ErrorMessage))
                {
                    AddRecord(new("Site", siteResults.SiteName, siteResults.SiteUrl, new("", "", "", "", siteResults.ErrorMessage)));
                    continue;
                }

                if(!await IsTargetSite(siteResults.SiteUrl))
                {
                    continue;
                }

                if (_param.UserListOnly)
                {
                    var collUsers = await new SPOSiteUserCSOM(_logger, _appInfo).GetAllAsync(siteResults.SiteUrl);

                    if(collUsers != null)
                    {
                        StringBuilder sb = new();
                        foreach(var oUser in collUsers)
                        {
                            sb.Append($"{oUser.Title}: {oUser.UserPrincipalName} ");
                        }
                        AddRecord(new("Site", siteResults.SiteName, siteResults.SiteUrl, new("Site user List", "", sb.ToString(), "","")));
                    }
                    else
                    {
                        AddRecord(new("Site", siteResults.SiteName, siteResults.SiteUrl, new("", "", "", "", "No users found in this Site")));
                    }
                }
                else
                {
                    try
                    {
                        await foreach(var record in sitePermissions.GetAsync(siteResults.SiteUrl, siteResults.Progress))
                        {
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
            if (!string.IsNullOrWhiteSpace(_param.TargetUPN))
            {
                // confirm use for Security Groups

                var oUser = await new SPOSiteUserCSOM(_logger, _appInfo).GetAsync(siteUrl, _param.TargetUPN);
                
                if (oUser != null) { return true; }
                else { return false; }

            }
            else if (_param.TargetEveryone)
            {
                var retrievalExpressions = new Expression<Func<Microsoft.SharePoint.Client.User, object>>[]
                {
                    u => u.Title,
                };

                var clientContext = await _appInfo.GetContext(siteUrl);
                var collUsers = clientContext.Web.SiteUsers.Where(u => u.Title.Contains("Everyone", StringComparison.OrdinalIgnoreCase) || u.Title.Contains("Everyone except external users", StringComparison.OrdinalIgnoreCase));
                clientContext.Load(clientContext.Web.SiteUsers, u => u.Include(retrievalExpressions));
                clientContext.ExecuteQueryRetry();

                if (collUsers.Any()) { return true; }
                else { return false; }

            }
            else
            {
                return true;
            }
        }

        private void FilterRecord(SPOLocationPermissionsRecord record)
        {
            if (!string.IsNullOrWhiteSpace(_param.TargetUPN) && record._role.Users.Contains(_param.TargetUPN, StringComparison.OrdinalIgnoreCase))
            {
                AddRecord(record);
            }
            if (_param.TargetEveryone && (record._role.AccountType.Contains("Everyone", StringComparison.OrdinalIgnoreCase) || record._role.AccountType.Contains("Everyone except external users", StringComparison.OrdinalIgnoreCase)))
            {
                AddRecord(record);
            }
            if(string.IsNullOrWhiteSpace(_param.TargetUPN) && !_param.TargetEveryone)
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

            _logger.RecordCSV(dynamicRecord);

        }

    }

    public class PermissionsReportParameters : SPOSitePermissionsCSOMParameters
    {
        private string _targetUPN = string.Empty;
        public string TargetUPN
        {
            get { return _targetUPN; }
            set { _targetUPN = value.Trim(); }
        }
        public bool TargetEveryone { get; set; } = false;
        public bool UserListOnly { get; set; } = false;

    }
}
