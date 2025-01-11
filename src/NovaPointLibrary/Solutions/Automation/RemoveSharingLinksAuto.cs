using NovaPointLibrary.Commands.SharePoint.SharingLinks;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.SiteGroup;
using NovaPointLibrary.Core.Logging;


namespace NovaPointLibrary.Solutions.Automation
{
    public class RemoveSharingLinksAuto
    {
        public static readonly string s_SolutionName = "Remove Sharing Links";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-RemoveSharingLinksAuto";

        private RemoveSharingLinksAutoParameters _param;
        private readonly LoggerSolution _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private RemoveSharingLinksAuto(LoggerSolution logger, Commands.Authentication.AppInfo appInfo, RemoveSharingLinksAutoParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(RemoveSharingLinksAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            LoggerSolution logger = new(uiAddLog, "RemoveSharingLinksAuto", parameters);

            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new RemoveSharingLinksAuto(logger, appInfo, parameters).RunScriptAsync();

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
                    SpoSharingLinksRecord record = new(siteRecord.SiteUrl, siteRecord.Ex);
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
                    SpoSharingLinksRecord record = new(siteRecord.SiteUrl, ex);
                    RecordCSV(record);
                }

            }
        }

        private async Task ProcessSite(SPOTenantSiteUrlsRecord siteRecord)
        {
            _appInfo.IsCancelled();

            var collGroups = await new SPOSiteGroupCSOM(_logger, _appInfo).GetSharingLinksAsync(siteRecord.SiteUrl);

            SpoSharingLinksRest restSharingLinks = new(_logger, _appInfo);
            ProgressTracker progress = new(siteRecord.Progress, collGroups.Count);
            foreach (var oGroup in collGroups)
            {
                var record = await restSharingLinks.GetFromGroupAsync(siteRecord.SiteUrl, oGroup);

                if (_param.SharingLinks.MatchFilters(record))
                {
                    try
                    {
                        await new SPOSiteGroupCSOM(_logger, _appInfo).RemoveAsync(siteRecord.SiteUrl, oGroup);
                        record.Remarks = "Sharing Link deleted";
                    }
                    catch (Exception ex)
                    {
                        record.Remarks = ex.Message;
                    }
                    finally
                    {
                        RecordCSV(record);
                    }
                }

                progress.ProgressUpdateReport();
            }

        }

        private void RecordCSV(SpoSharingLinksRecord record)
        {
            _logger.RecordCSV(record);
        }
    }


    public class RemoveSharingLinksAutoParameters : ISolutionParameters
    {
        public SpoSharingLinksFilter SharingLinks { get; init; }

        internal SPOAdminAccessParameters AdminAccess;
        internal SPOTenantSiteUrlsParameters SiteParam;
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam
        {
            get
            {
                return new(AdminAccess, SiteParam);
            }
        }
        public RemoveSharingLinksAutoParameters(SpoSharingLinksFilter sharingLinks, SPOAdminAccessParameters adminAccess, SPOTenantSiteUrlsParameters siteParam)
        {
            SharingLinks = sharingLinks;
            AdminAccess = adminAccess;
            SiteParam = siteParam;
        }
    }
}
