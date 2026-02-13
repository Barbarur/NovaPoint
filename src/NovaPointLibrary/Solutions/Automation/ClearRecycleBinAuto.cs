using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.RecycleBin;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Context;
using System.Dynamic;

namespace NovaPointLibrary.Solutions.Automation
{
    public class ClearRecycleBinAuto : ISolution
    {
        public static readonly string s_SolutionName = "Delete items from recycle bin";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-ClearRecycleBinAuto";

        private ContextSolution _ctx;
        private ClearRecycleBinAutoParameters _param;


        private ClearRecycleBinAuto(ContextSolution context, ClearRecycleBinAutoParameters parameters)
        {
            _ctx = context;
            _param = parameters;

            Dictionary<Type, string> solutionReports = new()
            {
                { typeof(ClearRecycleBinAuto), "Report" },
            };
            _ctx.DbHandler.AddSolutionReports(solutionReports);
        }

        public static ISolution Create(ContextSolution context, ISolutionParameters parameters)
        {
            return new ClearRecycleBinAuto(context, (ClearRecycleBinAutoParameters)parameters);
        }

        public async Task RunAsync()
        {
            _ctx.AppClient.IsCancelled();

            await foreach (var siteResults in new SPOTenantSiteUrlsWithAccessCSOM(_ctx.Logger, _ctx.AppClient, _param.SiteAccParam).GetAsync())
            {
                _ctx.AppClient.IsCancelled();
                
                if (siteResults.Ex != null)
                {
                    _ctx.Logger.Error(GetType().Name, "Site", siteResults.SiteUrl, siteResults.Ex);
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
                            _ctx.Logger.UI(GetType().Name, "Recycle bin cannot be cleared in bulk due view threshold limitation. Recycle bin items will be deleted individually.");
                        }
                        else
                        {
                            _ctx.Logger.Error(GetType().Name, "Site", siteResults.SiteUrl, ex);
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
                        _ctx.Logger.Error(GetType().Name, "Site", siteResults.SiteUrl, ex);
                        AddRecord(siteResults.SiteUrl, remarks: ex.Message);
                    }
                }
            }
        }

        private async Task DeleteAllRecycleBinItemsAsync(string siteUrl)
        {
            _ctx.AppClient.IsCancelled();

            ClientContext clientContext = await _ctx.AppClient.GetContext(siteUrl);
            clientContext.Web.RecycleBin.DeleteAll();
            clientContext.ExecuteQueryRetry();
            AddRecord(siteUrl, remarks: "All recycle bin items have been deleted") ;
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

                string remarks = string.Empty;

                try
                {
                    await new SPORecycleBinItemREST(_ctx.Logger, _ctx.AppClient).RemoveAsync(siteUrl, oRecycleBinItem);
                    remarks = "Item removed from Recycle bin";
                }
                catch (Exception ex)
                {
                    _ctx.Logger.Error(GetType().Name, "Recycle bin item", oRecycleBinItem.Title, ex);
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

            _ctx.Logger.Info(GetType().Name, $"Finish processing recycle bin items for '{siteUrl}'");
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

    public class ClearRecycleBinAutoParameters : ISolutionParameters
    {
        public SPORecycleBinItemParameters RecycleBinParam { get; set; }
        internal SPOAdminAccessParameters AdminAccess;
        internal SPOTenantSiteUrlsParameters SiteParam;
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam
        {
            get
            {
                return new(AdminAccess, SiteParam);
            }
        }
        public ClearRecycleBinAutoParameters(
            SPORecycleBinItemParameters recycleBinParam,
            SPOAdminAccessParameters adminAccess,
            SPOTenantSiteUrlsParameters siteParam)
        {
            RecycleBinParam = recycleBinParam;
            AdminAccess = adminAccess;
            SiteParam = siteParam;
        }
    }
}
