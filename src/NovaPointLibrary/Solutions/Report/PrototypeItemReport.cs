using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.News.DataModel;
using Newtonsoft.Json;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Report
{
    public class PrototypeItemReport : ISolution
    {
        public static readonly string s_SolutionName = "Files and Items report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-ItemReport";

        private PrototypeItemReportParameters _param = new();
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (PrototypeItemReportParameters)value; }
        }

        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;

        public PrototypeItemReport(AppInfo appInfo, Action<LogInfo> uiAddLog, PrototypeItemReportParameters parameters)
        {
            Parameters = parameters;
            _appInfo = appInfo;
            _logger = new(uiAddLog, this);
        }

        public async Task RunAsync()
        {
            try
            {
                if (String.IsNullOrWhiteSpace(_param.AdminUPN))
                {
                    throw new Exception("FORM INCOMPLETED: Admin UPN cannot be empty.");
                }
                else if (string.IsNullOrWhiteSpace(_param.SiteUrl) && !_param.SiteAll)
                {
                    throw new Exception($"FORM INCOMPLETED: Site URL cannot be empty when no processing all sites");
                }
                else if (!_param.ListAll && String.IsNullOrWhiteSpace(_param.ListTitle))
                {
                    throw new Exception($"FORM INCOMPLETED: Library name cannot be empty when not processing all Libraries");
                }
                else if (_param.ListAll && !String.IsNullOrWhiteSpace(_param.FolderRelativeUrl))
                {
                    throw new Exception($"FORM ERROR: You cannot target specific Relative URL when running the solution across all Libraries");
                }
                else
                {
                    await RunScriptAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.ScriptFinish(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.RunScriptAsync";

            await foreach (var results in new SPOTenantListsCSOM(_logger, _appInfo, _param).GetListsAsync())
            {
                _appInfo.IsCancelled();

                if (!String.IsNullOrWhiteSpace(results.Remarks))
                {
                    AddRecord(results.SiteUrl, results.List, remarks: results.Remarks);
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

            _logger.ScriptFinish();
        }

        private async Task ProcessItems(string siteUrl, List oList, ProgressTracker parentProgress)
        {
            Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] fileExpressions = new Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[]
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

            Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] itemExpressions = new Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[]
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

            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.ProcessItems";
            _logger.LogTxt(methodName, $"Start getting Items for {oList.BaseType} '{oList.Title}'");

            Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] currentExpressions;

            if (oList.BaseType == BaseType.DocumentLibrary)
            {
                currentExpressions = fileExpressions;
            }
            else if (oList.BaseType == BaseType.GenericList)
            {
                currentExpressions = itemExpressions;
            }
            else
            {
                AddRecord(siteUrl, oList, remarks: "This is not a List neither a Library");

                return;
            }

            ProgressTracker progress = new(parentProgress, oList.ItemCount);
            var spoItem = new SPOListItemCSOM(_logger, _appInfo);
            await foreach (ListItem oItem in spoItem.Get(siteUrl, oList.Title, _param.GetItemParameters(), currentExpressions))
            {
                _appInfo.IsCancelled();

                try
                {
                    if (oItem.FileSystemObjectType.ToString() == "Folder")
                    {
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
                            var oFileAttachment = await spoItem.GetAttachmentFile(siteUrl, oAttachment.ServerRelativeUrl);

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

            _logger.RecordCSV(recordItem);
        }



    }

    public class PrototypeItemReportParameters : SPOTenantItemsParameters, ISolutionParameters
    {
        internal SPOTenantItemsParameters GetItemParameters()
        {
            return this;
        }
    }
}
