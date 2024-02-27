using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.RecycleBin;
using NovaPointLibrary.Commands.SharePoint.Site;
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

        private SetSiteCollectionAdminAutoParameters _param = new();
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (SetSiteCollectionAdminAutoParameters)value; }
        }

        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        public SetSiteCollectionAdminAuto(SetSiteCollectionAdminAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
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

            ProgressTracker progress;
            if (!String.IsNullOrWhiteSpace(_param.SiteUrl))
            {
                progress = new(_logger, 1);

                Web oSite = await new SPOWebCSOM(_logger, _appInfo).GetAsync(_param.SiteUrl);

                await SetAdmin(oSite.Url);

                progress.ProgressUpdateReport();
            }
            else
            {
                List<SiteProperties> collSiteCollections = await new SPOSiteCollectionCSOM(_logger, _appInfo).GetAsync(_param.SiteUrl, _param.IncludeShareSite, _param.IncludePersonalSite, _param.OnlyGroupIdDefined);

                progress = new(_logger, collSiteCollections.Count);
                foreach (var oSiteCollection in collSiteCollections)
                {
                    await SetAdmin(oSiteCollection.Url);

                    progress.ProgressUpdateReport();
                }
            }
        }

        private async Task SetAdmin(string siteUrl)
        {
            _appInfo.IsCancelled();
            _logger.LogUI(GetType().Name, $"Processing '{siteUrl}'");

            try
            {
                if (_param.IsSiteAdmin)
                {
                    await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).SetAsync(siteUrl, _param.TargetUserUPN);
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

    public class SetSiteCollectionAdminAutoParameters : SPOTenantSiteUrlsParameters
    {
        private string _targetUserUPN = string.Empty;
        public string TargetUserUPN
        {
            get { return _targetUserUPN; }
            set { _targetUserUPN = value.Trim();}
        }

        public bool IsSiteAdmin { get; set; } = false;

    }
}
