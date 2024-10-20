using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.SiteGroup;
using NovaPointLibrary.Core.Logging;
using System.Linq.Expressions;


namespace NovaPointLibrary.Solutions.Report
{
    public class SiteReport : ISolution
    {
        public static readonly string s_SolutionName = "Site Collections & Subsites report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-SiteReport";

        private SiteReportParameters _param;
        private readonly LoggerSolution _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private readonly Expression<Func<Web, object>>[] _webExpressions = new Expression<Func<Web, object>>[]
        {
            w => w.Id,
            w => w.LastItemModifiedDate,
            w => w.ServerRelativeUrl,
            w => w.Title,
            w => w.Url,
            w => w.WebTemplate,
            w => w.LastItemUserModifiedDate,

        };

        private readonly Expression<Func<Microsoft.SharePoint.Client.Site, object>>[] _siteExpressions = new Expression<Func<Microsoft.SharePoint.Client.Site, object>>[]
        {
            s => s.IsHubSite,
            s => s.HubSiteId,
            s => s.Classification,
        };

        private SiteReport(LoggerSolution logger, Commands.Authentication.AppInfo appInfo, SiteReportParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(SiteReportParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            LoggerSolution logger = new(uiAddLog, "SiteReport", parameters);

            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new SiteReport(logger, appInfo, parameters).RunScriptAsync();

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

            await foreach (var siteRecord in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param.SiteAccParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (siteRecord.Ex != null)
                {
                    SiteReportRecord siteReportRecord = new(siteRecord.SiteUrl, siteRecord.Ex.Message);
                    RecordCSV(siteReportRecord);
                    continue;
                }

                try
                {
                    await ProcessSite(siteRecord);
                }
                catch (Exception ex)
                {
                    _logger.Error(GetType().Name, "Site", siteRecord.SiteUrl, ex);
                    SiteReportRecord siteReportRecord = new(siteRecord.SiteUrl, ex.Message);
                    RecordCSV(siteReportRecord);
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
                Web oWeb = await new SPOWebCSOM(_logger, _appInfo).GetAsync(siteRecord.SiteUrl, _webExpressions);
                
                if (oWeb.IsSubSite())
                {
                    await ProcessSubsite(oWeb);
                }
                else
                {
                    await ProcessSiteCollection(oWeb.Url);
                }
            }

        }

        private async Task ProcessSubsite(Web web)
        {
            var storageMetricsResponse = await new SPOFolderCSOM(_logger, _appInfo).GetFolderStorageMetricAsync(web.Url, web.RootFolder);

            double storageUsedGb = Math.Round(storageMetricsResponse.StorageMetrics.TotalSize / Math.Pow(1024, 3), 2);

            SiteReportRecord siteReportRecord = new(web,storageUsedGb);
            RecordCSV(siteReportRecord);
        }

        private async Task ProcessSiteCollection(string siteUrl)
        {
            _appInfo.IsCancelled(); 
            
            var oSiteProperties = await new SPOSiteCollectionCSOM(_logger, _appInfo).GetAsync(siteUrl);

            await ProcessSiteCollection(oSiteProperties);
        }

        private async Task ProcessSiteCollection(SiteProperties oSiteCollection)
        {
            _appInfo.IsCancelled();
            SiteReportRecord siteRecord = new(oSiteCollection);

            if (_param.IncludeHubInfo || _param.IncludeClassification)
            {
                var site = await new SPOSiteCSOM(_logger, _appInfo).GetAsync(siteRecord.SiteUrl, _siteExpressions);

                if (_param.IncludeHubInfo) { await AddHubinfoAsync(siteRecord, site); }

                if (_param.IncludeClassification) { siteRecord.AddSiteClassification(site.Classification); }
            }


            if (_param.IncludeSharingLinks) { await  AddSharingLinksAsync(siteRecord); }

            RecordCSV(siteRecord);
        }

        private async Task AddHubinfoAsync(SiteReportRecord siteRecord, Site site)
        {
            try
            {                
                string parentHubSiteId = string.Empty;
                if (site.IsHubSite)
                {
                    Tenant tenantContext = new(await _appInfo.GetContext(_appInfo.AdminUrl));
                    HubSiteProperties hubSiteProperties = tenantContext.GetHubSitePropertiesById(site.Id);

                    tenantContext.Context.Load(hubSiteProperties);
                    tenantContext.Context.ExecuteQueryRetry();

                    parentHubSiteId = hubSiteProperties.ParentHubSiteId.ToString();
                }
                siteRecord.AddHubInfo(site, parentHubSiteId);
            }
            catch (Exception ex)
            {
                _logger.Error(GetType().Name, "Site", siteRecord.SiteUrl, ex);
            }
        }

        private async Task AddSharingLinksAsync(SiteReportRecord siteRecord)
        {
            string countSharingLinks;
            try
            {
                List<Group> collGroups = await new SPOSiteGroupCSOM(_logger, _appInfo).GetSharingLinksAsync(siteRecord.SiteUrl);
                countSharingLinks = collGroups.Count.ToString();
            }
            catch (Exception ex)
            {
                _logger.Error(GetType().Name, "Site", siteRecord.SiteUrl, ex);
                countSharingLinks = ex.Message;
            }
            siteRecord.AddSharingLinks(countSharingLinks);
        }

        private void RecordCSV(SiteReportRecord record)
        {
            _logger.RecordCSV(record);
        }
    }


    internal class SiteReportRecord : ISolutionRecord
    {
        internal string Title { get; set; } = String.Empty;
        internal string SiteUrl { get; set; }
        internal string GroupId { get; set; } = String.Empty;
        internal string Template { get; set; } = String.Empty;
        internal string IsSubSite { get; set; } = String.Empty;
        internal string Connected_to_Teams { get; set; } = String.Empty;
        internal string Teams_Channel { get; set; } = String.Empty;

        internal string StorageQuotaGB { get; set; } = String.Empty;
        internal string StorageUsedGB { get; set; } = String.Empty;
        internal string StorageWarningPercentageLevel { get; set; } = String.Empty;

        internal string LastContentModifiedDate { get; set; } = String.Empty;
        internal string LockState { get; set; } = String.Empty;

        internal string IsHubSite { get; set; } = String.Empty;
        internal string HubSiteId { get; set; } = String.Empty;
        internal string ParentHubSiteId { get; set; } = String.Empty;

        internal string Site_Classification { get; set; } = String.Empty;

        internal string Sharing_Links { get; set; }  = String.Empty;

        internal string Remarks { get; set; } = String.Empty;

        internal SiteReportRecord(SiteProperties oSiteCollection)
        {
            Title = oSiteCollection.Title;
            SiteUrl = oSiteCollection.Url;
            GroupId = oSiteCollection.GroupId.ToString();
            Template = GetSiteTemplateName(oSiteCollection.Template, oSiteCollection.IsTeamsConnected);
            IsSubSite = "FALSE";
            Connected_to_Teams = oSiteCollection.IsTeamsConnected.ToString();
            if(!oSiteCollection.TeamsChannelType.ToString().Contains("None", StringComparison.OrdinalIgnoreCase))
            {
                Teams_Channel = oSiteCollection.TeamsChannelType.ToString();
            }

            StorageQuotaGB = Math.Round((float)oSiteCollection.StorageMaximumLevel / 1024, 2).ToString();
            StorageUsedGB = Math.Round((float)oSiteCollection.StorageUsage / 1024, 2).ToString();
            StorageWarningPercentageLevel = Math.Round((float)oSiteCollection.StorageWarningLevel / (float)oSiteCollection.StorageMaximumLevel * 100, 2).ToString();

            LastContentModifiedDate = oSiteCollection.LastContentModifiedDate.ToString();
            LockState = oSiteCollection.LockState.ToString();

        }
        internal SiteReportRecord(Web web, double storageUsedGb)
        {
            Title = web.Title;
            SiteUrl = web.Url;
            GroupId = web.Id.ToString();
            Template = GetSiteTemplateName(web.WebTemplate, false);
            IsSubSite = web.IsSubSite().ToString();

            StorageUsedGB = storageUsedGb.ToString();

            LastContentModifiedDate = web.LastItemUserModifiedDate.ToString();
        }
        internal SiteReportRecord(string siteUrl, string errorMessage)
        {
            SiteUrl = siteUrl;
            Remarks = errorMessage;
        }

        internal void AddHubInfo(Site site, string parentHubSiteId)
        {
            IsHubSite = site.IsHubSite.ToString();
            if (site.HubSiteId.ToString() != "00000000-0000-0000-0000-000000000000") { HubSiteId = site.HubSiteId.ToString(); }
            if (parentHubSiteId != "00000000-0000-0000-0000-000000000000") { ParentHubSiteId = parentHubSiteId; }
        }

        internal void AddSiteClassification(string classification)
        {
            Site_Classification = classification;
        }

        internal void AddSharingLinks(string sharingLinksCount)
        {
            Sharing_Links = sharingLinksCount;
        }

        private string GetSiteTemplateName(string template, bool isTeamsConnected)
        {
            string templateName = template;
            if (template.Contains("SPSPERS", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "OneDrive";
            }
            else if (template.Contains("SITEPAGEPUBLISHING#0", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "Communication site";
            }
            else if (template.Contains("GROUP#0", StringComparison.OrdinalIgnoreCase))
            {
                if (isTeamsConnected) { templateName = "Team site connected to MS Teams"; }
                else { templateName = "Team site"; }
            }
            else if (template.Contains("STS#3", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "Team site (no Microsoft 365 group)";
            }
            else if (template.Contains("STS#0", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "Team site (classic experience)";
            }
            else if (template.Contains("TEAMCHANNEL", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "Channel site";
            }
            else if (template.Contains("APPCATALOG", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "App Catalog Site";
            }
            else if (template.Contains("STS", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "Team site (Subsite)";
            }
            else if (template.Contains("PROJECTSITE", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "Project site (Subsite)";
            }
            else if (template.Contains("SRCHCENTERLITE", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "Basic Search Center (Subsite)";
            }
            else if (template.Contains("BDR", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "Document Center (Subsite)";
            }
            else if (template.Contains("SAPWORKFLOWSITE", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "SAP Workflow site (Subsite)";
            }
            else if (template.Contains("VISPRUS", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "Visio Process Repository (Subsite)";
            }

            return templateName;
        }
    }

    public class SiteReportParameters : ISolutionParameters
    {
        public bool IncludeHubInfo { get; set; }
        public bool IncludeClassification { get; set; }
        public bool IncludeSharingLinks { get; set; }

        internal readonly SPOAdminAccessParameters AdminAccess;
        internal readonly SPOTenantSiteUrlsParameters SiteParam;
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam
        {
            get
            {
                return new(AdminAccess, SiteParam);
            }
        }

        public SiteReportParameters(SPOAdminAccessParameters adminAccess, SPOTenantSiteUrlsParameters siteParam, bool includeHubInfo, bool includeClassification, bool includeSharingLinks)
        {
            AdminAccess = adminAccess;
            SiteParam = siteParam;
            IncludeHubInfo = includeHubInfo;
            IncludeClassification = includeClassification;
            IncludeSharingLinks = includeSharingLinks;

            if (!SiteParam.IncludeSubsites && string.IsNullOrWhiteSpace(SiteParam.SiteUrl) && string.IsNullOrWhiteSpace(SiteParam.ListOfSitesPath) && !IncludeSharingLinks)
            {
                AdminAccess.AddAdmin = false;
                AdminAccess.RemoveAdmin = false;
            }
        }

    }
}
