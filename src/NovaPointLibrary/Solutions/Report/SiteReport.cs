using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Directory;
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

        private readonly Expression<Func<SiteProperties, object>>[] _sitePropertiesExpressions = new Expression<Func<SiteProperties, object>>[]
        {
            p => p.Title,
            p => p.Url,
            p => p.GroupId,
            p => p.Template,
            p => p.IsTeamsConnected,
            p => p.TeamsChannelType,
            p => p.StorageMaximumLevel,
            p => p.StorageUsage,
            p => p.StorageWarningLevel,
            p => p.LastContentModifiedDate,
            p => p.LockState,
        };

        private readonly Expression<Func<Site, object>>[] _siteExpressions = new Expression<Func<Site, object>>[]
        {
            s => s.IsHubSite,
            s => s.HubSiteId,
            s => s.Classification,
        };

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
            
            var oSiteProperties = await new SPOSiteCollectionCSOM(_logger, _appInfo).GetAsync(siteUrl, _sitePropertiesExpressions);

            await ProcessSiteCollection(oSiteProperties);
        }

        private async Task ProcessSiteCollection(SiteProperties siteProperties)
        {
            _appInfo.IsCancelled();
            SiteReportRecord siteRecord = new(siteProperties);

            if (_param.SiteInfo.IncludeHubInfo || _param.SiteInfo.IncludeClassification)
            {
                var site = await new SPOSiteCSOM(_logger, _appInfo).GetAsync(siteRecord.SiteUrl, _siteExpressions);

                if (_param.SiteInfo.IncludeHubInfo) { await AddHubInfoAsync(siteRecord, site); }

                if (_param.SiteInfo.IncludeClassification) { siteRecord.AddSiteClassification(site.Classification); }
            }

            if (_param.SiteInfo.IncludeSharingLinks) { await AddSharingLinksAsync(siteRecord); }

            if (_param.SiteInfo.IncludePrivacy) { await AddPrivacyInfoAsync(siteRecord, siteProperties.GroupId); }

            RecordCSV(siteRecord);
        }

        private async Task AddHubInfoAsync(SiteReportRecord siteRecord, Site site)
        {
            try
            {
                if (site.IsHubSite)
                {
                    Tenant tenantContext = new(await _appInfo.GetContext(_appInfo.AdminUrl));
                    HubSiteProperties hubSiteProperties = tenantContext.GetHubSitePropertiesById(site.Id);

                    tenantContext.Context.Load(hubSiteProperties);
                    tenantContext.Context.ExecuteQueryRetry();

                    string parentHubSiteId = hubSiteProperties.ParentHubSiteId.ToString();
                    
                    siteRecord.AddHubInfo(site, parentHubSiteId);
                }
                else
                {
                    siteRecord.AddNoHub();
                }
            }
            catch (Exception ex)
            {
                siteRecord.IsHubSite = "Error";
                siteRecord.HubSiteId = ex.Message;
                siteRecord.Remarks = "Error while processing the site. Check the columns on this for the error message.";
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
                countSharingLinks = ex.Message;
                siteRecord.Remarks = "Error while processing the site. Check the columns on this for the error message.";
                _logger.Error(GetType().Name, "Site", siteRecord.SiteUrl, ex);
            }
            siteRecord.AddSharingLinks(countSharingLinks);
        }

        private async Task AddPrivacyInfoAsync(SiteReportRecord siteRecord, Guid groupId)
        {
            string privacy;
            if (groupId != Guid.Empty)
            {
                try
                {
                    var group = await new DirectoryGroup(_logger, _appInfo).GetAsync(groupId.ToString(), "?$select=visibility");
                    privacy = group.Visibility;
                }
                catch (Exception ex)
                {
                    privacy = ex.Message;
                    siteRecord.Remarks = "Error while processing the site. Check the columns on this for the error message.";
                    _logger.Error(GetType().Name, "Site", siteRecord.SiteUrl, ex);

                }
            }
            else
            {
                privacy = "NA";
            }

            siteRecord.AddPrivacy(privacy);
        }

        private void RecordCSV(SiteReportRecord record)
        {
            _logger.RecordCSV(record);
        }
    }


    internal class SiteReportRecord : ISolutionRecord
    {
        internal string SiteTitle { get; set; } = String.Empty;
        internal string SiteUrl { get; set; }
        internal string GroupId { get; set; } = String.Empty;
        internal string SiteTemplate { get; set; } = String.Empty;
        internal string IsSubsite { get; set; } = String.Empty;
        internal string ConnectedToTeams { get; set; } = String.Empty;
        internal string TeamsChannel { get; set; } = String.Empty;

        internal string StorageQuotaGB { get; set; } = String.Empty;
        internal string StorageUsedGB { get; set; } = String.Empty;
        internal string StorageWarningPercentageLevel { get; set; } = String.Empty;

        internal string LastContentModifiedDate { get; set; } = String.Empty;
        internal string LockState { get; set; } = String.Empty;

        internal string IsHubSite { get; set; } = String.Empty;
        internal string HubSiteId { get; set; } = String.Empty;
        internal string ParentHubSiteId { get; set; } = String.Empty;

        internal string Classification { get; set; } = String.Empty;

        internal string SharingLinks { get; set; }  = String.Empty;

        internal string Privacy { get; set; } = String.Empty;

        internal string Remarks { get; set; } = String.Empty;

        internal SiteReportRecord(SiteProperties oSiteCollection)
        {
            SiteTitle = oSiteCollection.Title;
            SiteUrl = oSiteCollection.Url;
            GroupId = oSiteCollection.GroupId.ToString();
            SiteTemplate = SPOWeb.GetSiteTemplateName(oSiteCollection.Template, oSiteCollection.IsTeamsConnected);
            IsSubsite = "False";
            ConnectedToTeams = oSiteCollection.IsTeamsConnected.ToString();
            if(!oSiteCollection.TeamsChannelType.ToString().Contains("None", StringComparison.OrdinalIgnoreCase))
            {
                TeamsChannel = oSiteCollection.TeamsChannelType.ToString();
            }
            else { TeamsChannel = "NA"; }

            StorageQuotaGB = Math.Round((float)oSiteCollection.StorageMaximumLevel / 1024, 2).ToString();
            StorageUsedGB = Math.Round((float)oSiteCollection.StorageUsage / 1024, 2).ToString();
            StorageWarningPercentageLevel = Math.Round((float)oSiteCollection.StorageWarningLevel / (float)oSiteCollection.StorageMaximumLevel * 100, 2).ToString();

            LastContentModifiedDate = oSiteCollection.LastContentModifiedDate.ToString();
            LockState = oSiteCollection.LockState.ToString();

        }

        internal SiteReportRecord(Web web, double storageUsedGb)
        {
            SiteTitle = web.Title;
            SiteUrl = web.Url;
            GroupId = web.Id.ToString();
            SiteTemplate = SPOWeb.GetSiteTemplateName(web.WebTemplate, false);
            IsSubsite = web.IsSubSite().ToString();

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
            else { HubSiteId = "NA"; }
            if (parentHubSiteId != "00000000-0000-0000-0000-000000000000") { ParentHubSiteId = parentHubSiteId; }
            else { ParentHubSiteId = "NA"; }
        }
        internal void AddNoHub()
        {
            IsHubSite = "NA";
            HubSiteId = "NA";
            ParentHubSiteId = "NA";
        }

        internal void AddSiteClassification(string classification)
        {
            Classification = classification;
        }

        internal void AddSharingLinks(string sharingLinksCount)
        {
            SharingLinks = sharingLinksCount;
        }

        internal void AddPrivacy(string privacy)
        {
            Privacy = privacy;
        }
    }

    public class SiteInformationParameters : ISolutionParameters
    {
        public bool IncludeHubInfo { get; set; } = false;
        public bool IncludeClassification { get; set; } = false;
        public bool IncludeSharingLinks { get; set; } = false;
        public bool IncludePrivacy {  get; set; } = false;
    }

    public class SiteReportParameters : ISolutionParameters
    {
        public SiteInformationParameters SiteInfo { get; set; }

        internal readonly SPOAdminAccessParameters AdminAccess;
        internal readonly SPOTenantSiteUrlsParameters SiteParam;
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam
        {
            get
            {
                return new(AdminAccess, SiteParam);
            }
        }

        public SiteReportParameters(SiteInformationParameters siteInfo, SPOAdminAccessParameters adminAccess, SPOTenantSiteUrlsParameters siteParam)
        {
            SiteInfo = siteInfo;
            AdminAccess = adminAccess;
            SiteParam = siteParam;

            if (!SiteParam.IncludeSubsites && string.IsNullOrWhiteSpace(SiteParam.SiteUrl) && string.IsNullOrWhiteSpace(SiteParam.ListOfSitesPath) && !SiteInfo.IncludeSharingLinks)
            {
                AdminAccess.AddAdmin = false;
                AdminAccess.RemoveAdmin = false;
            }
        }

    }
}
