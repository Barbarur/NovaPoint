using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Permision;
using NovaPointLibrary.Commands.SharePoint.RecycleBin;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.Utilities;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Report
{
    public class SiteReport : ISolution
    {
        public static readonly string s_SolutionName = "Site Collections & Subsites report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-SiteReport";

        private SiteReporttParameters _param = new();
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (SiteReporttParameters)value; }
        }

        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        public SiteReport(SiteReporttParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
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
                
                Web oSite = await new SPOSiteCSOM(_logger, _appInfo).GetAsync(_param.SiteUrl);

                AddRecord(null, oSite);

                try
                {
                    await GetSubsitesAsync(oSite.Url, progress);
                }
                catch (Exception ex)
                {
                    _logger.ReportError("Site", oSite.Url, ex);

                    AddRecord(null, oSite, ex.Message);
                }

                progress.ProgressUpdateReport();
            }
            else
            {
                List<SiteProperties> collSiteCollections = await new SPOSiteCollectionCSOM(_logger, _appInfo).GetAsync(_param.SiteUrl, _param.IncludeShareSite, _param.IncludePersonalSite, _param.OnlyGroupIdDefined);

                progress = new(_logger, collSiteCollections.Count);
                
                foreach (var oSiteCollection in collSiteCollections)
                {
                    AddRecord(oSiteCollection, null);

                    if (_param.IncludeSubsites)
                    {
                        try
                        {
                            await GetSubsitesAsync(oSiteCollection.Url, progress);
                        }
                        catch (Exception ex)
                        {
                            _logger.ReportError("Site", oSiteCollection.Url, ex);

                            AddRecord(oSiteCollection, null, ex.Message);
                        }
                    }

                    progress.ProgressUpdateReport();
                }
            }
        }

        private async Task GetSubsitesAsync(string siteUrl, ProgressTracker parentProgress)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Getting Subsites for '{siteUrl}'");

            List<Web> collSubsites = await new SPOSubsiteCSOM(_logger, _appInfo).GetAsync(siteUrl);

            ProgressTracker progress = new(parentProgress, collSubsites.Count);
            foreach (var oSubsite in collSubsites)
            {
                AddRecord(null, oSubsite);
                    
                progress.ProgressUpdateReport();
            }
        }

        private void AddRecord(SiteProperties? siteCollection,
                               Web? subsiteWeb,
                               string remarks = "")
        {
            dynamic record = new ExpandoObject();
            record.Title = siteCollection != null ? siteCollection?.Title : subsiteWeb?.Title;
            record.SiteUrl = siteCollection != null ? siteCollection?.Url : subsiteWeb?.Url;
            record.GroupId = siteCollection != null ? siteCollection?.GroupId.ToString() : string.Empty;
            record.Tempalte = siteCollection != null ? siteCollection?.Template : subsiteWeb?.WebTemplate;

            record.StorageQuotaGB = siteCollection != null ? Math.Round(((float)siteCollection.StorageMaximumLevel / 1024), 2).ToString() : string.Empty;
            record.StorageUsedGB = siteCollection != null ? Math.Round(((float)siteCollection.StorageUsage / 1024), 2).ToString() : string.Empty; // ADD CONSUMPTION FOR SUBSITES
            record.storageWarningPercentageLevelGB = siteCollection != null ? Math.Round((float)siteCollection.StorageWarningLevel / (float)siteCollection.StorageMaximumLevel * 100, 2).ToString() : string.Empty;

            record.LastContentModifiedDate = siteCollection != null ? siteCollection?.LastContentModifiedDate.ToString() : subsiteWeb?.LastItemModifiedDate.ToString();
            record.LockState = siteCollection != null ? siteCollection?.LockState.ToString() : string.Empty;

            record.Remarks = remarks;

            _logger.RecordCSV(record);
        }
    }

    public class SiteReporttParameters : SPOTenantSiteUrlsParameters
    {
    }
}
