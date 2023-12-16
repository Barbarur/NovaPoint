using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.Utilities;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Automation
{
    // PROTOTYPE ONLY
    public class RestorePHLAuto : ISolution
    {
        public readonly static String s_SolutionName = "Restore Preservation Hold Library";
        public readonly static String s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-RestorePHL";

        private RestorePHLAutoParameters _param = new();
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (RestorePHLAutoParameters)value; }
        }

        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;

        public RestorePHLAuto(AppInfo appInfo, Action<LogInfo> uiAddLog, RestorePHLAutoParameters parameters)
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
                if (string.IsNullOrWhiteSpace(_param.SiteUrl) && !_param.SiteAll)
                {
                    throw new Exception($"FORM INCOMPLETED: Site URL cannot be empty when no processing all sites");
                }
                else if (!_param.ItemsAll && String.IsNullOrWhiteSpace(_param.FolderRelativeUrl))
                {
                    throw new Exception($"FORM INCOMPLETED: Relative Path cannot be empty when not collecting all Files");
                }
                else
                {
                    await RunScriptAsyncTEST();
                }
            }
            catch (Exception ex)
            {
                _logger.ScriptFinish(ex);
            }
        }


        private async Task RunScriptAsyncNEW()
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.ProcessItems";

            await foreach (var results in new SPOTenantListsCSOM(_logger, _appInfo, _param.GetListParameters()).GetListsAsync())
            {
                _appInfo.IsCancelled();

                if (!String.IsNullOrWhiteSpace(results.Remarks))
                {
                    _logger.LogTxt(methodName, $"Processing Error '{results.Remarks}'");
                    AddRecord(results.SiteUrl, results.List, remarks: results.Remarks);
                    continue;
                }

                _logger.LogTxt(methodName, $"Start getting Items for '{results.List.Title}'");
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


        private async Task RunScriptAsyncTEST()
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.ProcessItems";

            await foreach (var results in new SPOTenantListsCSOM(_logger, _appInfo, _param.GetListParameters()).GetListsAsync())
            {
                _appInfo.IsCancelled();

                if(!String.IsNullOrWhiteSpace(results.Remarks))
                {
                    _logger.LogTxt(methodName, $"Processing Error '{results.Remarks}'");
                    AddRecord(results.SiteUrl, results.List, remarks: results.Remarks);
                    continue;
                }

                _logger.LogTxt(methodName, $"Start getting Items for '{results.List.Title}'");
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
        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();

            SPOProcessorParameters parameters = new()
            {
                AdminUPN = _param.AdminUPN,
                RemoveAdmin = _param.RemoveAdmin,

                SiteAll = _param.SiteAll,
                IncludePersonalSite = _param.IncludePersonalSite,
                IncludeShareSite = _param.IncludeShareSite,
                OnlyGroupIdDefined = _param.OnlyGroupIdDefined,
                SiteUrl = _param.SiteUrl,
                IncludeSubsites = _param.IncludeSubsites,

            };


            await foreach(var results in new SPOSiteProcessor(_logger, _appInfo, parameters).GetLists())
            {
                _appInfo.IsCancelled();
                string methodName = $"{GetType().Name}.ProcessItems";
                _logger.LogTxt(methodName, $"Processing Site URL '{results.SiteUrl}'");
                //_logger.LogTxt(methodName, $"Result Remarks '{results.Remarks}'");
                //_logger.LogTxt(methodName, $"Result List '{results.List.Title}'");

                if (String.IsNullOrWhiteSpace(results.Remarks))
                {
                    _logger.LogTxt(methodName, $"Processing Library '{results.List.Title}'");
                    try
                    {
                        await ProcessItems(results.SiteUrl, results.List, results.Progress);
                    }
                    catch (Exception ex)
                    {
                        AddRecord(results.SiteUrl, results.List, remarks: ex.Message);
                    }
                }
                else
                {
                    _logger.LogTxt(methodName, $"Processing Error '{results.Remarks}'");
                    AddRecord(results.SiteUrl, results.List, remarks: results.Remarks);
                }

            }
        }

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

        private async Task ProcessItems(string siteUrl, List oList, ProgressTracker parentProgress)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.ProcessItems";
            _logger.LogTxt(methodName, $"Start getting Items for '{oList.Title}'");
                //- '{oList.Title}'

            Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] currentExpressions;

            if (oList.BaseType == BaseType.DocumentLibrary)
            {
                currentExpressions = _fileExpressions;
            }
            else if (oList.BaseType == BaseType.GenericList)
            {
                currentExpressions = _itemExpressions;
            }
            else
            {
                AddRecord(siteUrl, oList, remarks: "This is not a List neither a Library");

                return;
            }

            ProgressTracker progress = new(parentProgress, oList.ItemCount);

            var spoItem = new SPOListItemCSOM(_logger, _appInfo);
            await foreach (ListItem oItem in spoItem.Get(siteUrl, oList.Title, currentExpressions))
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

    public class RestorePHLAutoParameters : SPOTenantListsParameters, ISolutionParameters
    {

        public bool ItemsAll { get; set; } = true;
        public string FolderRelativeUrl { get; set; } = String.Empty;

        public bool ReportMode { get; set; } = true;

        internal SPOTenantListsParameters GetListParameters()
        {
            SPOTenantListsParameters p = new()
            {
                AdminUPN = AdminUPN,
                RemoveAdmin = RemoveAdmin,

                SiteAll = SiteAll,
                IncludePersonalSite = IncludePersonalSite,
                IncludeShareSite = IncludeShareSite,
                OnlyGroupIdDefined = OnlyGroupIdDefined,
                SiteUrl = SiteUrl,
                IncludeSubsites = IncludeSubsites,

                ListAll = ListAll,
                IncludeHiddenLists = IncludeHiddenLists,
                IncludeSystemLists = IncludeSystemLists,
                ListTitle = ListTitle,
            };

            return p;
        }
    }
}
