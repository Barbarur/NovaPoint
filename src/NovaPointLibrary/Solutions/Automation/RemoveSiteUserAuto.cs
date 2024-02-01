using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.RecycleBin;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.User;
using NovaPointLibrary.Commands.Site;
using NovaPointLibrary.Commands.User;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Automation
{
    public class RemoveSiteUserAuto
    {
        public static readonly string s_SolutionName = "Remove user from Site";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-RemoveSiteUserAuto";

        private RemoveUserAutoParameters _param = new();
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (RemoveUserAutoParameters)value; }
        }

        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;

        public RemoveSiteUserAuto(RemoveUserAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
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

            await foreach (var siteResults in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param).GetAsync())
            {

                if (!String.IsNullOrWhiteSpace(siteResults.ErrorMessage))
                {
                    AddRecord(siteResults.SiteUrl, remarks: siteResults.ErrorMessage);
                    continue;
                }

                try
                {
                    await RemoveSiteUserAsync(siteResults.SiteUrl, siteResults.Progress);
                }
                catch (Exception ex)
                {
                    _logger.ReportError("Site", siteResults.SiteUrl, ex);
                    AddRecord(siteResults.SiteUrl, remarks: ex.Message);
                }
            }
        }

        private async Task RemoveSiteUserAsync(string siteUrl, ProgressTracker parentProgress)
        {
            _appInfo.IsCancelled();

            User? user = await new SPOSiteUserCSOM(_logger, _appInfo).GetAsync(siteUrl, _param.DeleteUserUPN);

            if (user != null)
            {
                if (user.IsSiteAdmin) { await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).RemoveForceAsync(siteUrl, user.UserPrincipalName); }

                await new SPOSiteUserCSOM(_logger, _appInfo).RemoveAsync(siteUrl, user.UserPrincipalName);

                AddRecord(siteUrl, "User removed");
            }
            else
            {
                AddRecord(siteUrl, "User not found");
            }
        }


        private void AddRecord(string siteUrl,
                               string remarks = "")
        {
            dynamic recordItem = new ExpandoObject();
            recordItem.SiteUrl = siteUrl;

            recordItem.Remarks = remarks;

            _logger.DynamicCSV(recordItem);
        }
    }

    public class RemoveUserAutoParameters : SPORecycleBinItemParameters, ISolutionParameters
    {
        public string DeleteUserUPN { get; set; } = string.Empty;
    }
}
