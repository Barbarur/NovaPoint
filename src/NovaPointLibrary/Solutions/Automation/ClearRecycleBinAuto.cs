using Microsoft.Extensions.Logging;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.News.DataModel;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.RecycleBin;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions.Report;
using PnP.Core.Model.SharePoint;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Automation
{
    public class ClearRecycleBinAuto : ISolution
    {
        public static readonly string s_SolutionName = "Delete items from recycle bin";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-ClearRecycleBinAuto";

        private ClearRecycleBinAutoParameters _param;
        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;

        private ClearRecycleBinAuto(NPLogger logger, AppInfo appInfo, ClearRecycleBinAutoParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(ClearRecycleBinAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            NPLogger logger = new(uiAddLog, "ClearRecycleBinAuto", parameters);
            try
            {
                AppInfo appInfo = await AppInfo.BuildAsync(logger, cancelTokenSource);

                await new ClearRecycleBinAuto(logger, appInfo, parameters).RunScriptAsync();

                logger.ScriptFinish();

            }
            catch (Exception ex)
            {
                logger.ScriptFinish(ex);
            }
        }

        //public ClearRecycleBinAuto(ClearRecycleBinAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        //{
        //    _param = parameters;
        //    _logger = new(uiAddLog, this.GetType().Name, _param);
        //    _appInfo = new(_logger, cancelTokenSource);
        //}

        //public async Task RunAsync()
        //{
        //    try
        //    {
        //        await RunScriptAsync();

        //        _logger.ScriptFinish();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.ScriptFinish(ex);
        //    }
        //}

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();

            await foreach (var siteResults in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param.SiteAccParam).GetAsync())
            {
                _appInfo.IsCancelled();
                
                if (siteResults.Ex != null)
                {
                    _logger.ReportError(GetType().Name, "Site", siteResults.SiteUrl, siteResults.Ex);
                    AddRecord(siteResults.SiteUrl, remarks: siteResults.Ex.Message);
                    continue;
                }

                if (_param.RecycleBinParam.AllItems)
                {
                    try
                    {
                        await DeleteAllRecycleBinItemsAsync(siteResults.SiteUrl);
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("The attempted operation is prohibited because it exceeds the list view threshold"))
                        {
                            _param.RecycleBinParam.AllItems = false;
                            _logger.LogUI(GetType().Name, "Recycle bin cannot be cleared in bulk due view threshold limitation. Recycle bin items will be deleted individually.");
                        }
                        else
                        {
                            _logger.ReportError(GetType().Name, "Site", siteResults.SiteUrl, ex);
                            AddRecord(siteResults.SiteUrl, remarks: ex.Message);
                        }
                    }
                }

                if (!_param.RecycleBinParam.AllItems)
                {
                    try
                    {
                        await ProcessRecycleBinItemsAsync(siteResults.SiteUrl, siteResults.Progress);
                    }
                    catch (Exception ex)
                    {
                        _logger.ReportError(GetType().Name, "Site", siteResults.SiteUrl, ex);
                        AddRecord(siteResults.SiteUrl, remarks: ex.Message);
                    }
                }
            }
        }

        private async Task DeleteAllRecycleBinItemsAsync(string siteUrl)
        {
            _appInfo.IsCancelled();

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);
            clientContext.Web.RecycleBin.DeleteAll();
            clientContext.ExecuteQueryRetry();
            AddRecord(siteUrl, remarks: "All recycle bin items have been deleted") ;
        }

        private async Task ProcessRecycleBinItemsAsync(string siteUrl, ProgressTracker parentProgress)
        {
            _appInfo.IsCancelled();

            ProgressTracker progress = new(parentProgress, 5000);
            int itemCounter = 0;
            int itemExpectedCount = 5000;
            var spoRecycleBinItem = new SPORecycleBinItemCSOM(_logger, _appInfo);
            await foreach (RecycleBinItem oRecycleBinItem in spoRecycleBinItem.GetAsync(siteUrl, _param.RecycleBinParam))
            {
                _appInfo.IsCancelled();

                string remarks = string.Empty;

                try
                {
                    await new SPORecycleBinItemREST(_logger, _appInfo).RemoveAsync(siteUrl, oRecycleBinItem);
                    remarks = "Item removed from Recycle bin";
                }
                catch (Exception ex)
                {
                    _logger.ReportError(GetType().Name, "Recycle bin item", oRecycleBinItem.Title, ex);
                    remarks = ex.Message;
                }

                AddRecord(siteUrl, oRecycleBinItem, remarks);

                progress.ProgressUpdateReport();
                itemCounter++;
                if (itemCounter == itemExpectedCount)
                {
                    progress.IncreaseTotalCount(6000);
                    itemExpectedCount += 5000;
                }
            }

            _logger.LogTxt(GetType().Name, $"Finish processing recycle bin items for '{siteUrl}'");
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

            _logger.DynamicCSV(recordItem);
        }
    }

    public class ClearRecycleBinAutoParameters : ISolutionParameters
    {
        public SPORecycleBinItemParameters RecycleBinParam { get; set; }
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam { get; set; }
        public ClearRecycleBinAutoParameters(SPORecycleBinItemParameters recycleBinParam,
                                             SPOTenantSiteUrlsWithAccessParameters siteAccParam)
        {
            RecycleBinParam = recycleBinParam;
            SiteAccParam = siteAccParam;
        }
    }
}
