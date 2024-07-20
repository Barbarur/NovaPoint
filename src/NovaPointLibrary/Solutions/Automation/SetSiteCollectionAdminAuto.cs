using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.AzureAD;
using NovaPointLibrary.Commands.SharePoint.RecycleBin;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.User;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Automation
{
    public class SetSiteCollectionAdminAuto
    {
        public static readonly string s_SolutionName = "Add or Remove user as Admin";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-SetSiteCollectionAdminAuto";

        private SetSiteCollectionAdminAutoParameters _param;
        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private SetSiteCollectionAdminAuto(NPLogger logger, Commands.Authentication.AppInfo appInfo, SetSiteCollectionAdminAutoParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(SetSiteCollectionAdminAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            NPLogger logger = new(uiAddLog, "SetSiteCollectionAdminAuto", parameters);
            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new SetSiteCollectionAdminAuto(logger, appInfo, parameters).RunScriptAsync();

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

            GraphUser signedInUser = await new GetAADUser(_logger, _appInfo).GetUserAsync(_param.TargetUserUPN);
            _param.TargetUserUPN = signedInUser.UserPrincipalName;

            await foreach (var recordSite in new SPOTenantSiteUrlsCSOM(_logger, _appInfo, _param.SiteParam).GetAsync())
            {
                await SetAdmin(recordSite.SiteUrl);
            }

        }

        private async Task SetAdmin(string siteUrl)
        {
            _appInfo.IsCancelled();

            try
            {
                if (_param.IsSiteAdmin)
                {
                    await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).AddAsync(siteUrl, _param.TargetUserUPN);
                    AddRecord(siteUrl, $"User '{_param.TargetUserUPN}' added as Site Collection Admin");
                }
                else
                {
                    await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).RemoveAsync(siteUrl, _param.TargetUserUPN);
                    AddRecord(siteUrl, $"User '{_param.TargetUserUPN}' removed as Site Collection Admin");
                }
            }
            catch (Exception ex)
            {
                _logger.ReportError("Site", siteUrl, ex);
                AddRecord(siteUrl, ex.Message);
            }

        }

        private void AddRecord(string siteUrl, string remarks)
        {
            dynamic recordItem = new ExpandoObject();
            recordItem.SiteUrl = siteUrl;

            recordItem.Remarks = remarks;

            _logger.DynamicCSV(recordItem);
        }
    }

    public class SetSiteCollectionAdminAutoParameters : ISolutionParameters
    {
        public string TargetUserUPN { get; set; } = string.Empty;

        public bool IsSiteAdmin { get; set; } = false;

        public SPOTenantSiteUrlsParameters SiteParam { get; set; }
        public SetSiteCollectionAdminAutoParameters(SPOTenantSiteUrlsParameters siteParam)
        {
            SiteParam = siteParam;
        }

        public void ParametersCheck()
        {
            if (String.IsNullOrWhiteSpace(TargetUserUPN))
            {
                throw new Exception($"User Principal Name cannot be empty");
            }
        }
    }
}
