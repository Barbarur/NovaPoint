using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.News.DataModel;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.Site;
using System;
using System.Dynamic;
using System.Linq.Expressions;

namespace NovaPointLibrary.Solutions.Report
{
    public class ItemReport : ISolution
    {
        public static readonly string s_SolutionName = "Files and Items report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-ItemReport";

        private ItemReportParameters _param = new();
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (ItemReportParameters)value; }
        }

        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private readonly Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] _fileExpressions = new Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[]
        {
            i => i.HasUniqueRoleAssignments,
            i => i["Author"],
            i => i["Created"],
            i => i["Editor"],
            i => i["ID"],
            i => i.FileSystemObjectType,
            i => i["FileLeafRef"],
            i => i["FileRef"],
            i => i["File_x0020_Size"],
            i => i["Modified"],
            i => i["SMTotalSize"],
            i => i["Title"],
            i => i.Versions,
            i => i["_UIVersionString"],
        };

        private readonly Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] _itemExpressions = new Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[]
        {
            i => i.HasUniqueRoleAssignments,
            i => i.AttachmentFiles,
            i => i["Author"],
            i => i["Created"],
            i => i["Editor"],
            i => i["ID"],
            i => i.FileSystemObjectType,
            i => i["FileLeafRef"],
            i => i["FileRef"],
            i => i["Modified"],
            i => i["SMTotalSize"],
            i => i["Title"],
            i => i.Versions,
            i => i["_UIVersionString"],
        };

        public ItemReport(ItemReportParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            Parameters = parameters;
            _param.FileExpresions = _fileExpressions;
            _param.ItemExpresions = _itemExpressions;
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

            await foreach (var results in new SPOTenantListsCSOM(_logger, _appInfo, _param).GetListsAsync())
            {
                _appInfo.IsCancelled();

                if (!String.IsNullOrWhiteSpace(results.ErrorMessage) || results.List == null)
                {
                    AddRecord(results.SiteUrl, results.List, remarks: results.ErrorMessage);
                    continue;
                }

                try
                {
                    await ProcessItems(results.SiteUrl, results.List, results.Progress);
                }
                catch (Exception ex)
                {
                    _logger.ReportError(results.List.BaseType.ToString(), results.List.DefaultViewUrl, ex);
                    AddRecord(results.SiteUrl, results.List, remarks: ex.Message);
                }
            }
        }

        private async Task ProcessItems(string siteUrl, List oList, ProgressTracker parentProgress)
        {
            _appInfo.IsCancelled();

            ProgressTracker progress = new(parentProgress, oList.ItemCount);

            var spoItem = new SPOListItemCSOM(_logger, _appInfo);
            await foreach (ListItem oItem in spoItem.GetAsync(siteUrl, oList, _param))
            {
                _appInfo.IsCancelled();

                try
                {
                    if (oItem.FileSystemObjectType.ToString() == "Folder")
                    {
                        // NEED TEST; if Folder name change depending on being located in a Library or a List
                        AddRecord(siteUrl, oList, oItem, (string)oItem["FileLeafRef"], "0", "0");

                        continue;
                    }

                    if (oList.BaseType == BaseType.DocumentLibrary)
                    {
                        string itemName = (string)oItem["FileLeafRef"];

                        float itemSizeMb = (float)Math.Round(Convert.ToDouble(oItem["File_x0020_Size"]) / Math.Pow(1024, 2), 2);

                        FieldLookupValue FileSizeTotalBytes = (FieldLookupValue)oItem["SMTotalSize"];
                        float itemSizeTotalMb = (float)Math.Round(FileSizeTotalBytes.LookupId / Math.Pow(1024, 2), 2);

                        AddRecord(siteUrl, oList, oItem, itemName, itemSizeMb.ToString(), itemSizeTotalMb.ToString(), "");
                    }
                    else if (oList.BaseType == BaseType.GenericList)
                    {
                        string itemName = (string)oItem["Title"];

                        int itemSizeTotalBytes = 0;
                        foreach (var oAttachment in oItem.AttachmentFiles)
                        {
                            var oFileAttachment = await spoItem.GetAttachmentFileAsync(siteUrl, oAttachment.ServerRelativeUrl);

                            itemSizeTotalBytes += (int)oFileAttachment.Length;
                        }
                        float itemSizeTotalMb = (float)Math.Round(itemSizeTotalBytes / Math.Pow(1024, 2), 2);

                        AddRecord(siteUrl, oList, oItem, itemName, itemSizeTotalMb.ToString(), itemSizeTotalMb.ToString(), "");
                    }
                }
                catch (Exception ex)
                {
                    _logger.ReportError("Item", (string)oItem["FileRef"], ex);

                    AddRecord(siteUrl, oList, oItem, remarks: ex.Message);
                }

                progress.ProgressUpdateReport();
            }
        }


        private void AddRecord(string siteUrl,
                               Microsoft.SharePoint.Client.List? oList = null,
                               Microsoft.SharePoint.Client.ListItem? oItem = null,
                               string itemName = "",
                               string itemSizeMb = "",
                               string itemSizeTotalMb = "",
                               string remarks = "")
        {

            dynamic recordItem = new ExpandoObject();
            recordItem.SiteUrl = siteUrl;
            recordItem.ListTitle = oList != null ? oList.Title : String.Empty;
            recordItem.ListType = oList != null ? oList.BaseType.ToString() : String.Empty;

            recordItem.ItemID = oItem != null ? oItem["ID"] : string.Empty;
            recordItem.ItemName = oItem != null ? itemName : string.Empty;
            recordItem.ItemPath = oItem != null ? oItem["FileRef"] : string.Empty;
            recordItem.ItemType = oItem != null ? oItem.FileSystemObjectType.ToString() : string.Empty;

            recordItem.ItemCreated = oItem != null ? oItem["Created"] : string.Empty;
            FieldUserValue? author = oItem != null ? (FieldUserValue)oItem["Author"] : null;
            recordItem.ItemCreatedBy = author?.Email;

            recordItem.ItemModified = oItem != null ? oItem["Modified"] : string.Empty;
            FieldUserValue? editor = oItem != null ? (FieldUserValue)oItem["Editor"] : null;
            recordItem.ItemModifiedBy = editor?.Email;

            recordItem.ItemVersion = oItem != null ? oItem["_UIVersionString"] : string.Empty;
            recordItem.ItemVersionsCount = oItem != null ? oItem.Versions.Count.ToString() : string.Empty;

            recordItem.ItemSizeMb = oItem != null ? itemSizeMb : string.Empty;
            recordItem.ItemSizeTotalMB = oItem != null ? itemSizeTotalMb : string.Empty;

            recordItem.Remarks = remarks;

            _logger.DynamicCSV(recordItem);
        }
    }

    public class ItemReportParameters : SPOTenantItemsParameters
    {
    }
}
