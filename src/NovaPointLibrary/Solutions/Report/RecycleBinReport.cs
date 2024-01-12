using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.RecycleBin;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.Utilities;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Report
{
    public class RecycleBinReport : ISolution
    {
        public static readonly string s_SolutionName = "Recycle bin report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-RecycleBinReport";

        private RecycleBinReportParameters _param = new();
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (RecycleBinReportParameters)value; }
        }

        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;

        public RecycleBinReport(RecycleBinReportParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            Parameters = parameters;
            _logger = new(uiAddLog, this.GetType().Name, parameters);
            _appInfo = new(_logger, cancelTokenSource);
        }

        public async Task RunAsync()
        {
            try
            {
                await RunScriptAsync();
                
                _logger.ScriptFinish();
            }
            catch (Exception ex)
            {
                _logger.ScriptFinish(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();

            await foreach (var siteResults in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param).GetAsync())
            {

                if (!String.IsNullOrWhiteSpace(siteResults.Remarks))
                {
                    AddRecord(siteResults.SiteUrl, remarks: siteResults.Remarks);
                    continue;
                }

                try
                {
                    await ProcessRecycleBinItemsAsync(siteResults.SiteUrl, siteResults.Progress);
                }
                catch (Exception ex)
                {
                    _logger.ReportError("Site", siteResults.SiteUrl, ex);
                    AddRecord(siteResults.SiteUrl, remarks: ex.Message);
                }
            }
        }

        private async Task ProcessRecycleBinItemsAsync(string siteUrl, ProgressTracker parentProgress)
        {
            _appInfo.IsCancelled();

            ProgressTracker progress = new(parentProgress, 5000);
            int itemCounter = 0;
            int itemExpectedCount = 5000;
            var spoRecycleBinItem = new SPORecycleBinItemCSOM(_logger, _appInfo);
            await foreach (RecycleBinItem oRecycleBinItem in spoRecycleBinItem.GetAsync(siteUrl, _param))
            {
                _appInfo.IsCancelled();

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

            _logger.RecordCSV(recordItem);
        }
    }

    public class RecycleBinReportParameters : SPORecycleBinItemParameters, ISolutionParameters
    {
    }
}
