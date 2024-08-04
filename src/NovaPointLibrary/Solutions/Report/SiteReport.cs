using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.Permision;
using NovaPointLibrary.Commands.SharePoint.Site;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Report
{
    public class SiteReport : ISolution
    {
        public static readonly string s_SolutionName = "Site Collections & Subsites report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-SiteReport";

        private SiteReportParameters _param;
        private readonly NPLogger _logger;
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
        };

        private SiteReport(NPLogger logger, Commands.Authentication.AppInfo appInfo, SiteReportParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(SiteReportParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            NPLogger logger = new(uiAddLog, "SiteReport", parameters);
            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new SiteReport(logger, appInfo, parameters).RunScriptAsync();

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

            if (!NeedAccess() && _param.SitesAccParam.SiteParam.AllSiteCollections)
            {
                await SimpleReportAsync();
            }
            else if (NeedAccess() || !_param.SitesAccParam.SiteParam.AllSiteCollections)
            {
                await ComplexReportAsync();
            }
            else
            {
                throw new Exception("No matching requirements for the report.");
            }
        }

        private async Task SimpleReportAsync()
        {
            _appInfo.IsCancelled();

            await foreach (var recordSite in new SPOTenantSiteUrlsCSOM(_logger, _appInfo, _param.SitesAccParam.SiteParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (recordSite.SiteProperties != null)
                {
                    SiteReportRecord siteRecord = new(recordSite.SiteProperties);
                    RecordCSV(siteRecord);
                }
                else
                {
                    throw new Exception("Site properties is empty");
                }
            }
        }

        private async Task ComplexReportAsync()
        {
            _appInfo.IsCancelled();

            await foreach (var siteRecord in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param.SitesAccParam).GetAsync())
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
                    _logger.ReportError(GetType().Name, "Site", siteRecord.SiteUrl, ex);
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
                SiteReportRecord siteReportRecord = new(siteRecord.Web);
                RecordCSV(siteReportRecord);
            }

            else
            {
                Web? oWeb = await new SPOWebCSOM(_logger, _appInfo).GetAsync(siteRecord.SiteUrl, _webExpressions);

                if (oWeb == null)
                {
                    SiteReportRecord siteReportRecord = new(siteRecord.SiteUrl, "Site wasn't found.");
                    RecordCSV(siteReportRecord);
                }
                else if (oWeb.IsSubSite())
                {
                    SiteReportRecord siteReportRecord = new(oWeb);
                    RecordCSV(siteReportRecord);
                }
                else
                {
                    await ProcessSiteCollection(siteRecord.SiteUrl);
                }
            }

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

            if (_param.Detailed)
            {
                var site = await new SPOSiteCSOM(_logger, _appInfo).GetAsync(oSiteCollection.Url, _siteExpressions);
                
                string parentHubSiteId = string.Empty;
                if (site.IsHubSite)
                {
                    try
                    {
                        Tenant tenantContext = new(await _appInfo.GetContext(_appInfo.AdminUrl));
                        HubSiteProperties hubSiteProperties = tenantContext.GetHubSitePropertiesById(oSiteCollection.SiteId);

                        tenantContext.Context.Load(hubSiteProperties);
                        tenantContext.Context.ExecuteQueryRetry();

                        parentHubSiteId = hubSiteProperties.ParentHubSiteId.ToString();
                    }
                    catch (Exception ex) 
                    {
                        _logger.ReportError(GetType().Name, "Site", oSiteCollection.Url, ex);
                        siteRecord.Remarks = ex.Message;
                    }
                }

                siteRecord.AddHubDetails(site, parentHubSiteId);
            }

            RecordCSV(siteRecord);
        }

        private bool NeedAccess()
        {
            if (_param.Detailed || _param.SitesAccParam.SiteParam.IncludeSubsites)
            {
                return true;
            }
            else { return false; }
        }

        private void RecordCSV(SiteReportRecord record)
        {
            _logger.RecordCSV(record);
        }
    }


    internal class SiteReportRecord : ISolutionRecord
    {
        internal string Title { get; set; } = String.Empty;
        internal string SiteUrl { get; set; } = String.Empty;
        internal string GroupId { get; set; } = String.Empty;
        internal string Tempalte { get; set; } = String.Empty;
        internal string IsSubSite { get; set; } = String.Empty;

        internal string StorageQuotaGB { get; set; } = String.Empty;
        internal string StorageUsedGB { get; set; } = String.Empty;
        internal string StorageWarningPercentageLevel { get; set; } = String.Empty;

        internal string LastContentModifiedDate { get; set; } = String.Empty;
        internal string LockState { get; set; } = String.Empty;

        internal string IsHubSite { get; set; } = String.Empty;
        internal string HubSiteId { get; set; } = String.Empty;
        internal string ParentHubSiteId { get; set; } = String.Empty;

        internal string Remarks { get; set; } = String.Empty;

        internal SiteReportRecord(SiteProperties oSiteCollection)
        {
            Title = oSiteCollection.Title;
            SiteUrl = oSiteCollection.Url;
            GroupId = oSiteCollection.GroupId.ToString();
            Tempalte = oSiteCollection.Template;
            IsSubSite = "FALSE";

            StorageQuotaGB = Math.Round((float)oSiteCollection.StorageMaximumLevel / 1024, 2).ToString();
            StorageUsedGB = Math.Round((float)oSiteCollection.StorageUsage / 1024, 2).ToString();
            StorageWarningPercentageLevel = Math.Round((float)oSiteCollection.StorageWarningLevel / (float)oSiteCollection.StorageMaximumLevel * 100, 2).ToString();

            LastContentModifiedDate = oSiteCollection.LastContentModifiedDate.ToString();
            LockState = oSiteCollection.LockState.ToString();

        }
        internal SiteReportRecord(Web web)
        {
            Title = web.Title;
            SiteUrl = web.Url;
            GroupId = web.Id.ToString();
            Tempalte = web.WebTemplate;
            IsSubSite = web.IsSubSite().ToString();

            LastContentModifiedDate = web.LastItemUserModifiedDate.ToString();
        }
        internal SiteReportRecord(string siteUrl, string errorMessage)
        {
            SiteUrl = siteUrl;
            Remarks = errorMessage;
        }

        internal void AddHubDetails(Site site, string parentHubSiteId)
        {
            IsHubSite = site.IsHubSite.ToString();
            if (site.HubSiteId.ToString() != "00000000-0000-0000-0000-000000000000") { HubSiteId = site.HubSiteId.ToString(); }
            if (parentHubSiteId != "00000000-0000-0000-0000-000000000000") { ParentHubSiteId = parentHubSiteId; }
        }

    }

    public class SiteReportParameters : ISolutionParameters
    {
        public bool Detailed { get; set; } = false;
        public SPOTenantSiteUrlsWithAccessParameters SitesAccParam {  get; set; }
        public SiteReportParameters(SPOTenantSiteUrlsWithAccessParameters tenantSitesParam, 
                                    bool detailed)
        {
            Detailed = detailed;
            SitesAccParam = tenantSitesParam;
        }
    }
}
