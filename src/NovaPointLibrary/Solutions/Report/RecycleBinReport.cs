using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.RecycleBin;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Context;
using System.Dynamic;

namespace NovaPointLibrary.Solutions.Report
{
    public class RecycleBinReport : ISolution
    {
        public static readonly string s_SolutionName = "Recycle bin report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-RecycleBinReport";

        private ContextSolution _ctx;
        private RecycleBinReportParameters _param;


        private RecycleBinReport(ContextSolution context, RecycleBinReportParameters parameters)
        {
            _ctx = context;
            _param = parameters;
        }

        public static ISolution Create(ContextSolution context, ISolutionParameters parameters)
        {
            return new RecycleBinReport(context, (RecycleBinReportParameters)parameters);
        }
        public async Task RunAsync()
        {
            _ctx.AppClient.IsCancelled();

            await foreach (var siteResults in new SPOTenantSiteUrlsWithAccessCSOM(_ctx.Logger, _ctx.AppClient, _param.SiteAccParam).GetAsync())
            {
                _ctx.AppClient.IsCancelled();

                if (siteResults.Ex != null)
                {
                    AddRecord(siteResults.SiteUrl, remarks: siteResults.Ex.Message);
                    continue;
                }

                try
                {
                    await ProcessRecycleBinItemsAsync(siteResults.SiteUrl, siteResults.Progress);
                }
                catch (Exception ex)
                {
                    _ctx.Logger.Error(GetType().Name, "Site", siteResults.SiteUrl, ex);
                    AddRecord(siteResults.SiteUrl, remarks: ex.Message);
                }
            }
        }

        private async Task ProcessRecycleBinItemsAsync(string siteUrl, ProgressTracker parentProgress)
        {
            _ctx.AppClient.IsCancelled();

            ProgressTracker progress = new(parentProgress, 5000);
            int itemCounter = 0;
            int itemExpectedCount = 5000;
            var spoRecycleBinItem = new SPORecycleBinItemCSOM(_ctx.Logger, _ctx.AppClient);
            await foreach (RecycleBinItem oRecycleBinItem in spoRecycleBinItem.GetAsync(siteUrl, _param.RecycleBinParam))
            {
                _ctx.AppClient.IsCancelled();

                AddRecord(siteUrl, oRecycleBinItem);

                progress.ProgressUpdateReport();
                itemCounter++;
                if (itemCounter == itemExpectedCount)
                {
                    progress.IncreaseTotalCount(6000);
                    itemExpectedCount += 5000;
                }
            }
        }

        private void AddRecord(string siteUrl,
                               RecycleBinItem? oRecycleBinItem = null,
                               string remarks = "")
        {
            dynamic recordItem = new ExpandoObject();
            recordItem.SiteUrl = siteUrl;

            recordItem.ItemId = oRecycleBinItem != null ? oRecycleBinItem.Id.ToString() : string.Empty;
            recordItem.ItemTitle = oRecycleBinItem != null ? oRecycleBinItem.Title : String.Empty;
            recordItem.ItemType = oRecycleBinItem != null ? oRecycleBinItem.ItemType.ToString() : String.Empty;
            recordItem.ItemState = oRecycleBinItem != null ? oRecycleBinItem.ItemState.ToString() : String.Empty;

            recordItem.DateDeleted = oRecycleBinItem != null ? oRecycleBinItem.DeletedDate.ToString() : String.Empty;
            recordItem.DeletedByName = oRecycleBinItem != null ? oRecycleBinItem.DeletedByName : String.Empty;
            recordItem.DeletedByEmail = oRecycleBinItem != null ? oRecycleBinItem.DeletedByEmail : String.Empty;

            recordItem.CreatedByName = oRecycleBinItem != null ? oRecycleBinItem.AuthorName : String.Empty;
            recordItem.CreatedByEmail = oRecycleBinItem != null ? oRecycleBinItem.AuthorEmail : String.Empty;
            recordItem.OriginalLocation = oRecycleBinItem != null ? oRecycleBinItem.DirName : String.Empty;

            recordItem.SizeMB = oRecycleBinItem != null ? Math.Round(oRecycleBinItem.Size / Math.Pow(1024, 2), 2).ToString() : String.Empty;

            recordItem.Remarks = remarks;

            _ctx.Logger.DynamicCSV(recordItem);
        }
    }

    public class RecycleBinReportParameters : ISolutionParameters
    {
        public SPORecycleBinItemParameters RecycleBinParam { get; set; }
        internal readonly SPOAdminAccessParameters AdminAccess;
        internal readonly SPOTenantSiteUrlsParameters SiteParam;
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam
        {
            get
            {
                return new(AdminAccess, SiteParam);
            }
        }
        public RecycleBinReportParameters(SPORecycleBinItemParameters recycleBinParam,
                                          SPOAdminAccessParameters adminAccess,
                                          SPOTenantSiteUrlsParameters siteParam)
        {
            RecycleBinParam = recycleBinParam;
            AdminAccess = adminAccess;
            SiteParam = siteParam;
        }
    }
}
