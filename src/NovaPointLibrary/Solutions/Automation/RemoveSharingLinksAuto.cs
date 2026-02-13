using NovaPointLibrary.Commands.SharePoint.SharingLinks;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.SiteGroup;
using NovaPointLibrary.Core.Context;


namespace NovaPointLibrary.Solutions.Automation
{
    public class RemoveSharingLinksAuto : ISolution
    {
        public static readonly string s_SolutionName = "Remove Sharing Links";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-RemoveSharingLinksAuto";

        private ContextSolution _ctx;
        private RemoveSharingLinksAutoParameters _param;

        private RemoveSharingLinksAuto(ContextSolution context, RemoveSharingLinksAutoParameters parameters)
        {
            _ctx = context;
            _param = parameters;

            Dictionary<Type, string> solutionReports = new()
            {
                { typeof(SpoSharingLinksRecord), "Report" },
            };
            _ctx.DbHandler.AddSolutionReports(solutionReports);
        }

        public static ISolution Create(ContextSolution context, ISolutionParameters parameters)
        {
            return new RemoveSharingLinksAuto(context, (RemoveSharingLinksAutoParameters)parameters);
        }

        public async Task RunAsync()
        {
            _ctx.AppClient.IsCancelled();

            await foreach (var siteRecord in new SPOTenantSiteUrlsWithAccessCSOM(_ctx.Logger, _ctx.AppClient, _param.SiteAccParam).GetAsync())
            {
                _ctx.AppClient.IsCancelled();
                
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
                    _ctx.Logger.Error(GetType().Name, "Site", siteRecord.SiteUrl, ex);
                    SpoSharingLinksRecord record = new(siteRecord.SiteUrl, ex);
                    RecordCSV(record);
                }

            }
        }

        private async Task ProcessSite(SPOTenantSiteUrlsRecord siteRecord)
        {
            _ctx.AppClient.IsCancelled();

            var collGroups = await new SPOSiteGroupCSOM(_ctx.Logger, _ctx.AppClient).GetSharingLinksAsync(siteRecord.SiteUrl);

            SpoSharingLinksRest restSharingLinks = new(_ctx.Logger, _ctx.AppClient);
            ProgressTracker progress = new(siteRecord.Progress, collGroups.Count);
            foreach (var oGroup in collGroups)
            {
                _ctx.AppClient.IsCancelled();

                var record = await restSharingLinks.GetFromGroupAsync(siteRecord.SiteUrl, oGroup);

                if (_param.SharingLinks.MatchFilters(record))
                {
                    try
                    {
                        await new SPOSiteGroupCSOM(_ctx.Logger, _ctx.AppClient).RemoveAsync(siteRecord.SiteUrl, oGroup);
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
            _ctx.DbHandler.WriteRecord(record);
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
