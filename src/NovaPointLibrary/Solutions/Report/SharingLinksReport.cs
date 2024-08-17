using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.Permision;
using NovaPointLibrary.Commands.SharePoint.Permision.Utilities;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.SiteGroup;
using NovaPointLibrary.Solutions.Automation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Report
{
    public class SharingLinksReport
    {
        public static readonly string s_SolutionName = "Report Sharing Links";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-ReportSharingLinks";

        private SharingLinksReportParameters _param;
        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private SharingLinksReport(NPLogger logger, Commands.Authentication.AppInfo appInfo, SharingLinksReportParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(SharingLinksReportParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            NPLogger logger = new(uiAddLog, "SharingLinksReport", parameters);
            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new SharingLinksReport(logger, appInfo, parameters).RunScriptAsync();

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

            await foreach (var siteRecord in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param.SiteAccParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (siteRecord.Ex != null)
                {
                    RecordCSV(new(siteRecord));
                    continue;
                }

                try
                {
                    await ProcessSite(siteRecord);
                }
                catch (Exception ex)
                {
                    RemoveSharingLinksAutoRecord record = new(siteRecord, ex.Message);
                    _logger.RecordCSV(record);
                    _logger.ReportError(GetType().Name, "Site", siteRecord.SiteUrl, ex);
                }
            }
        }

        private async Task ProcessSite(SPOTenantSiteUrlsRecord siteRecord)
        {
            _appInfo.IsCancelled();

            var collGroups = await new SPOSiteGroupCSOM(_logger, _appInfo).GetAsync(siteRecord.SiteUrl);

            ProgressTracker progress = new(siteRecord.Progress, collGroups.Count());
            foreach (Group group in collGroups)
            {
                if (group.Title.Contains("SharingLinks"))
                {
                    try
                    {
                        StringBuilder sbUsers = new StringBuilder();
                        foreach (var user in group.Users)
                        {
                            sbUsers.Append($"{user.Email} ");
                        }
                        SharingLinksReportRecord record = new(siteRecord);
                        record.AddDetails(group, sbUsers.ToString());
                        RecordCSV(record);
                    }
                    catch (Exception ex)
                    {
                        _logger.ReportError(GetType().Name, "Sharing Link", $"{group.Id}", ex);
                        RecordCSV(new(siteRecord, ex.Message));
                    }
                }
                progress.ProgressUpdateReport();
            }

        }

        private void RecordCSV(SharingLinksReportRecord record)
        {
            _logger.RecordCSV(record);
        }
    }

    

    public class SharingLinksReportRecord : ISolutionRecord
    {
        internal string SiteUrl { get; set; } = String.Empty;

        internal string ID { get; set; } = String.Empty;
        internal string Title { get; set; } = String.Empty;
        internal string Description { get; set; } = String.Empty;
        internal string IsHiddenInUI { get; set; } = String.Empty;

        internal string Users { get; set; } = String.Empty;

        internal string Remarks { get; set; } = String.Empty;

        internal SharingLinksReportRecord(SPOTenantSiteUrlsRecord siteRecord, string remarks = "")
        {
            SiteUrl = siteRecord.SiteUrl;
            if (siteRecord.Ex != null) { Remarks = siteRecord.Ex.Message; }
            else { Remarks = remarks; }
        }

        internal void AddDetails(Group groupSharedLink, string users)
        {
            ID = groupSharedLink.Id.ToString();
            Title = groupSharedLink.Title;
            Description = groupSharedLink.Description;
            IsHiddenInUI = groupSharedLink.IsHiddenInUI.ToString();

            Users = users;
        }

    }

    public class SharingLinksReportParameters : ISolutionParameters
    {
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam { get; set; }
        public SharingLinksReportParameters(SPOTenantSiteUrlsWithAccessParameters siteParam)
        {
            SiteAccParam = siteParam;
        }
    }
}
