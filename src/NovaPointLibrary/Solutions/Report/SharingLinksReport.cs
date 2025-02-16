using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.SharingLinks;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.SiteGroup;
using NovaPointLibrary.Commands.Utilities.RESTModel;
using NovaPointLibrary.Core.Logging;


namespace NovaPointLibrary.Solutions.Report
{
    public class SharingLinksReport
    {
        public static readonly string s_SolutionName = "Sharing Links report";
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
                    SharingLinksReportRecord record = new(siteRecord.SiteUrl, siteRecord.Ex);
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
                    SharingLinksReportRecord record = new(siteRecord.SiteUrl, ex);
                    RecordCSV(record);
                }
            }
        }

        private async Task ProcessSite(SPOTenantSiteUrlsRecord siteRecord)
        {
            _appInfo.IsCancelled();

            var collGroups = await new SPOSiteGroupCSOM(_logger, _appInfo).GetSharingLinksAsync(siteRecord.SiteUrl);

            SpoSharingLinksRest spoLinks = new(_logger, _appInfo);
            ProgressTracker groupProgress = new(siteRecord.Progress, collGroups.Count);
            foreach (Group oGroup in collGroups)
            {
                _appInfo.IsCancelled();

                var linkInfo = await spoLinks.GetFromGroupAsync(siteRecord.SiteUrl, oGroup);
                if (_param.SharingLinks.MatchFilters(linkInfo))
                {
                    SharingLinksReportRecord recordSharingLink = new(linkInfo);

                    if (_param.BreakdownInvitations && !linkInfo.LinkDetailsAnonymous && !linkInfo.LinkDetailsOrganization && !String.IsNullOrWhiteSpace(linkInfo.Users) && recordSharingLink.Link != null)
                    {
                        foreach (var invitation in recordSharingLink.Link.linkDetails.Invitations)
                        {
                            var recordInvitationBreakdown = recordSharingLink.CopyRecord();
                            recordInvitationBreakdown.InvitedBy = invitation.InvitedBy.Email;
                            recordInvitationBreakdown.InvitedOn = invitation.InvitedOn.ToString();
                            recordInvitationBreakdown.InvitedTo = invitation.Invitee.Email;

                            RecordCSV(recordInvitationBreakdown);
                        }
                    }
                    else
                    {
                        RecordCSV(recordSharingLink);
                    }
                }

                groupProgress.ProgressUpdateReport();
            }

        }

        private void RecordCSV(SharingLinksReportRecord record)
        {
            _logger.RecordCSV(record);
        }
    }

    public class SharingLinksReportRecord : ISolutionRecord
    {
        internal string SiteTitle { get; set; } = String.Empty;
        internal string SiteUrl { get; set; } = String.Empty;

        internal string ListTitle { get; set; } = String.Empty;
        internal Guid ListId { get; set; } = Guid.Empty;

        internal int ItemId { get; set; } = -1;
        internal Guid ItemUniqueId { get; set; } = Guid.Empty;
        internal string ItemPath { get; set; } = String.Empty;

        internal string SharingLink { get; set; } = String.Empty;
        internal string SharingLinkRequiresPassword { get; set; } = String.Empty;
        internal string SharingLinkExpiration { get; set; } = String.Empty;

        internal string SharingLinkIsActive { get; set; } = String.Empty;
        internal DateTime SharingLinkCreated { get; set; } = DateTime.MinValue;
        internal string SharingLinkCreatedBy { get; set; } = String.Empty;
        internal DateTime SharingLinkModified { get; set; } = DateTime.MinValue;
        internal string SharingLinkModifiedBy { get; set; } = String.Empty;
        internal string SharingLinkUrl { get; set; } = String.Empty;
        internal string SharingLinkShareId { get; set; } = String.Empty;

        internal string InvitedBy { get; set; } = String.Empty;
        internal string InvitedOn { get; set; } = String.Empty;
        internal string InvitedTo { get; set; } = String.Empty;

        internal string GroupId { get; set; } = String.Empty;
        internal string GroupTitle { get; set; } = String.Empty;
        internal string Users { get; set; } = String.Empty;

        //internal string GroupDescription { get; init; } = String.Empty;

        internal string Remarks { get; set; } = String.Empty;

        public Link? Link { get; set; } = null;

        internal SharingLinksReportRecord(string siteUrl, Exception ex)
        {
            SiteUrl = siteUrl;
            Remarks = ex.Message;
        }
        internal SharingLinksReportRecord(SpoSharingLinksRecord record)
        {
            SiteTitle = record.SiteTitle;
            SiteUrl = record.SiteUrl;

            ListTitle = record.ListTitle;
            ListId = record.ListId;
            
            ItemId = record.ItemId;
            ItemUniqueId = record.ItemUniqueId;
            ItemPath = record.ItemPath;
            
            SharingLink = record.SharingLink;
            SharingLinkRequiresPassword = record.SharingLinkRequiresPassword;
            SharingLinkExpiration = record.SharingLinkExpiration;
            SharingLinkIsActive = record.SharingLinkIsActive;
            SharingLinkCreated = record.SharingLinkCreated;
            SharingLinkCreatedBy = record.SharingLinkCreatedBy;
            SharingLinkModified = record.SharingLinkModified;
            SharingLinkModifiedBy = record.SharingLinkModifiedBy;
            SharingLinkUrl = record.SharingLinkUrl;
            SharingLinkShareId = record.ShareId;

            GroupId = record.GroupId;
            GroupTitle = record.GroupTitle;
            Users = record.Users;
            
            Remarks = record.Remarks;

            Link = record.Link;
        }
        internal SharingLinksReportRecord CopyRecord()
        {
            return (SharingLinksReportRecord)this.MemberwiseClone();
        }

    }

    public class SharingLinksReportParameters : ISolutionParameters
    {
        public bool BreakdownInvitations { get; set; } = false;
        public SpoSharingLinksFilter SharingLinks { get; init; }
        public SPOAdminAccessParameters AdminAccess { get; init; }
        public SPOTenantSiteUrlsParameters SiteParam { get; init; }
        internal SPOTenantSiteUrlsWithAccessParameters SiteAccParam
        {
            get
            {
                return new(AdminAccess, SiteParam);
            }
        }

        public SharingLinksReportParameters(bool breakdownInvites, SpoSharingLinksFilter sharingLinks, SPOTenantSiteUrlsParameters siteParam, SPOAdminAccessParameters adminAccess)
        {
            BreakdownInvitations = breakdownInvites;
            SharingLinks = sharingLinks;
            SiteParam = siteParam;
            AdminAccess = adminAccess;
        }

    }

}
