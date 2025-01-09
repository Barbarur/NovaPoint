using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.AzureAD.Groups;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Logging;
using System.Linq.Expressions;

namespace NovaPointLibrary.Solutions.Report
{
    public class PrivacySiteReport
    {
        public static readonly string s_SolutionName = "Public and Private Site Collections report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-PrivacySiteReport";

        private PrivacySiteReportParameters _param;
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

        private readonly Expression<Func<Web, object>>[] _webExpressions = new Expression<Func<Web, object>>[]
        {
            w => w.Title,
            w => w.Url,
            w => w.LastItemModifiedDate,
            w => w.WebTemplate,
        };

        private PrivacySiteReport(LoggerSolution logger, Commands.Authentication.AppInfo appInfo, PrivacySiteReportParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(PrivacySiteReportParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            LoggerSolution logger = new(uiAddLog, "PrivacySiteReport", parameters);

            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new PrivacySiteReport(logger, appInfo, parameters).RunScriptAsync();

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

            await foreach (var siteRecord in new SPOTenantSiteUrlsCSOM(_logger, _appInfo, _param.SiteParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (siteRecord.Ex != null)
                {
                    PrivacySiteReportRecord siteReportRecord = new(siteRecord.SiteUrl, siteRecord.Ex.Message);
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
                    PrivacySiteReportRecord siteReportRecord = new(siteRecord.SiteUrl, ex.Message);
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
                ProcessSubsite(siteRecord.Web);
            }

            else
            {
                Web oWeb = await new SPOWebCSOM(_logger, _appInfo).GetAsync(siteRecord.SiteUrl, _webExpressions);

                if (oWeb.IsSubSite())
                {
                    ProcessSubsite(oWeb);
                }
                else
                {
                    await ProcessSiteCollection(oWeb.Url);
                }
            }

        }

        private void ProcessSubsite(Web web)
        {
            PrivacySiteReportRecord siteReportRecord = new(web);
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
            PrivacySiteReportRecord siteRecord = new(siteProperties);

            string privacy;
            if (siteProperties.GroupId != Guid.Empty)
            {
                try
                {
                    var group = await new AADGroup(_logger, _appInfo).GetInfoAsync(siteProperties.GroupId.ToString(), "?$select=visibility");
                    privacy = group.Visibility;
                }
                catch (Exception ex)
                {
                    _logger.Error(GetType().Name, "Site", siteRecord.SiteUrl, ex);
                    privacy = ex.Message;
                }
            }
            else
            {
                privacy = "NA";
            }

            siteRecord.AddPrivacy(privacy);

            RecordCSV(siteRecord);
        }

        private void RecordCSV(PrivacySiteReportRecord record)
        {
            _logger.RecordCSV(record);
        }
    }


    public class PrivacySiteReportRecord : ISolutionRecord
    {
        internal string SiteTitle { get; set; } = String.Empty;
        internal string SiteUrl { get; set; }
        internal string GroupId { get; set; } = String.Empty;
        internal string Privacy { get; set; } = String.Empty;

        internal string Remarks { get; set; } = String.Empty;

        internal PrivacySiteReportRecord(SiteProperties oSiteCollection)
        {
            SiteTitle = oSiteCollection.Title;
            SiteUrl = oSiteCollection.Url;
            GroupId = oSiteCollection.GroupId.ToString();

            if (oSiteCollection.IsTeamsChannelConnected)
            {
                Remarks = "This is a Teams Channel. Privacy setting is a property of the MS365 group linked to a Site Collection, not applicable to Teams Channels.";
            }
        }

        internal PrivacySiteReportRecord(Web web)
        {
            SiteTitle = web.Title;
            SiteUrl = web.Url;
            GroupId = "NA";

            Remarks = "This is a subsite. Privacy setting is a property of the MS365 group linked to a Site Collection, not applicable to subsites.";
        }

        internal PrivacySiteReportRecord(string siteUrl, string errorMessage)
        {
            SiteUrl = siteUrl;
            Remarks = errorMessage;
        }

        internal void AddPrivacy(string privacy)
        {
            Privacy = privacy;
        }
    }

    public class PrivacySiteReportParameters : ISolutionParameters
    {
        public readonly SPOTenantSiteUrlsParameters SiteParam = new()
        {
            ActiveSites = true,
            IncludeTeamSite = true,
        };

        public PrivacySiteReportParameters() { }
    }
}
