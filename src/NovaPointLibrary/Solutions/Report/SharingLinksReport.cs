using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.Permision;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.SiteGroup;
using NovaPointLibrary.Core.Logging;
using static NovaPointLibrary.Commands.SharePoint.Permision.SPOSharingLinksREST;


namespace NovaPointLibrary.Solutions.Report
{
    public class SharingLinksReport
    {
        public static readonly string s_SolutionName = "Report Sharing Links";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-SharingLinksReport";

        private SharingLinksReportParameters _param;
        private readonly LoggerSolution _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private SharingLinksReport(LoggerSolution logger, Commands.Authentication.AppInfo appInfo, SharingLinksReportParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(SharingLinksReportParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            LoggerSolution logger = new(uiAddLog, "SharingLinksReport", parameters);
            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new SharingLinksReport(logger, appInfo, parameters).RunScriptAsync();

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
                    SPOSharingLinksRecord record = new(siteRecord.SiteUrl);
                    record.Remarks = siteRecord.Ex.Message;
                    RecordCSV(record);
                    continue;
                }

                try
                {
                    await ProcessSite(siteRecord);
                }
                catch (Exception ex)
                {
                    _logger.Error(GetType().Name, "Site", siteRecord.SiteUrl, ex);
                    SPOSharingLinksRecord record = new(siteRecord.SiteUrl);
                    record.Remarks = ex.Message;
                    RecordCSV(record);
                }
            }
        }

        private async Task ProcessSite(SPOTenantSiteUrlsRecord siteRecord)
        {
            _appInfo.IsCancelled();

            var collGroups = await new SPOSiteGroupCSOM(_logger, _appInfo).GetSharingLinksAsync(siteRecord.SiteUrl);

            SPOSharingLinksREST restSharingLinks = new(_logger, _appInfo);
            ProgressTracker groupProgress = new(siteRecord.Progress, collGroups.Count);
            foreach (Group oGroup in collGroups)
            {
                var record = await restSharingLinks.GetFromGroupAsync(siteRecord.SiteUrl, oGroup);
                RecordCSV(record);

                groupProgress.ProgressUpdateReport();
            }

        }

        private void RecordCSV(SPOSharingLinksRecord record)
        {
            _logger.RecordCSV(record);
        }
    }
    

    public class SharingLinksReportParameters : ISolutionParameters
    {
        internal readonly SPOAdminAccessParameters AdminAccess;
        internal readonly SPOTenantSiteUrlsParameters SiteParam;
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam
        {
            get
            {
                return new(AdminAccess, SiteParam);
            }
        }
        public SharingLinksReportParameters(SPOAdminAccessParameters adminAccess, SPOTenantSiteUrlsParameters siteParam)
        {
            AdminAccess = adminAccess;
            SiteParam = siteParam;
        }
    }

}
