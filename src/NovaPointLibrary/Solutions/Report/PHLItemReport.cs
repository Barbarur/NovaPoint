using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.News.DataModel;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.PreservationHoldLibrary;
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
    public class PHLItemReport : ISolution
    {
        public static readonly string s_SolutionName = "Preservation Hold Library report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-PHLItemReport";

        private PHLItemReportParameters _param;
        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private static readonly Expression<Func<ListItem, object>>[] _fileExpressions = new Expression<Func<ListItem, object>>[]
        {
            f => f.HasUniqueRoleAssignments,
            f => f["Author"],
            f => f["Created"],
            f => f["Editor"],
            f => f["ID"],
            f => f.File.CheckOutType,
            f => f.FileSystemObjectType,
            f => f["FileLeafRef"],
            f => f["FileRef"],
            f => f["File_x0020_Size"],
            f => f["File_x0020_Type"],
            f => f["Modified"],
            f => f["PreservationDatePreserved"],
            f => f["PreservationOriginalDocumentName"],
            f => f["PreservationOriginalURL"],
            f => f["SMTotalSize"],
            f => f.Versions,
            f => f["_UIVersionString"],

        };

        private PHLItemReport(NPLogger logger, Commands.Authentication.AppInfo appInfo, PHLItemReportParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(PHLItemReportParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            //parameters.TListsParam.SiteAccParam.SiteParam.IncludeSubsites = false;
            parameters.ListsParam.AllLists = false;
            parameters.ListsParam.IncludeLists = false;
            parameters.ListsParam.IncludeLibraries = false;
            parameters.ListsParam.ListTitle = "Preservation Hold Library";
            parameters.ItemsParam.FileExpresions = _fileExpressions;

            NPLogger logger = new(uiAddLog, "PHLItemReport", parameters);
            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new PHLItemReport(logger, appInfo, parameters).RunScriptAsync();

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

            await foreach (var resultItem in new SPOTenantItemsCSOM(_logger, _appInfo, _param.TItemsParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (!String.IsNullOrWhiteSpace(resultItem.ErrorMessage))
                {
                    AddRecord(resultItem.ListRecord.SiteUrl, resultItem.ListRecord.List, remarks: resultItem.ErrorMessage);
                    continue;
                }

                if (resultItem.Item == null || resultItem.ListRecord.List == null) { continue; }

                try
                {
                    if (resultItem.Item.FileSystemObjectType.ToString() == "Folder") { continue; }

                    if (_param.PHLParam.MatchParameters(resultItem.Item))
                    {
                        AddRecord(resultItem.ListRecord.SiteUrl, resultItem.ListRecord.List, resultItem.Item);
                    }

                }
                catch (Exception ex)
                {
                    _logger.ReportError("Item", (string)resultItem.Item["FileRef"], ex);

                    AddRecord(resultItem.ListRecord.SiteUrl, resultItem.ListRecord.List, resultItem.Item, remarks: ex.Message);
                }

            }
        }

        private void AddRecord(string siteUrl,
                               Microsoft.SharePoint.Client.List? oList = null,
                               Microsoft.SharePoint.Client.ListItem? oItem = null,
                               string remarks = "")
        {

            dynamic recordItem = new ExpandoObject();
            recordItem.SiteUrl = siteUrl;
            recordItem.ListTitle = oList != null ? oList.Title : String.Empty;
            recordItem.ListType = oList != null ? oList.BaseType.ToString() : String.Empty;
            recordItem.ListDefaultViewUrl = oList != null ? oList.DefaultViewUrl : string.Empty;

            recordItem.ItemID = oItem != null ? oItem["ID"] : string.Empty;

            recordItem.ItemName = oItem != null ? oItem["FileLeafRef"] : string.Empty;
            recordItem.ItemOriginalName = oItem != null ? oItem["PreservationOriginalDocumentName"] : string.Empty;

            recordItem.ItemType = oItem != null ? oItem.FileSystemObjectType.ToString() : string.Empty;
            recordItem.FileType = oItem != null ? oItem["File_x0020_Type"] : string.Empty;

            recordItem.ItemPath = oItem != null ? oItem["FileRef"] : string.Empty;
            recordItem.ItemOriginalPath = oItem != null ? oItem["PreservationOriginalURL"] : string.Empty;


            recordItem.ItemCreated = oItem != null ? oItem["Created"] : string.Empty;
            FieldUserValue? author = oItem != null ? (FieldUserValue)oItem["Author"] : null;
            recordItem.ItemCreatedBy = author?.Email;

            recordItem.ItemModified = oItem != null ? oItem["Modified"] : string.Empty;
            FieldUserValue? editor = oItem != null ? (FieldUserValue)oItem["Editor"] : null;
            recordItem.ItemModifiedBy = editor?.Email;

            recordItem.ItemPreserved = oItem != null ? oItem["PreservationDatePreserved"] : string.Empty;

            recordItem.ItemVersion = oItem != null ? oItem["_UIVersionString"] : string.Empty;
            recordItem.ItemVersionsCount = oItem != null ? oItem.Versions.Count.ToString() : string.Empty;


            float? itemSizeMb = oItem != null ?  (float)Math.Round(Convert.ToDouble(oItem["File_x0020_Size"]) / Math.Pow(1024, 2), 2) : null;
            recordItem.ItemSizeMb = itemSizeMb != null ? itemSizeMb.ToString() : string.Empty;

            FieldLookupValue? FileSizeTotalBytes = oItem != null ? (FieldLookupValue)oItem["SMTotalSize"] : null;
            float? itemSizeTotalMb = FileSizeTotalBytes != null ? (float)Math.Round(FileSizeTotalBytes.LookupId / Math.Pow(1024, 2), 2) : null;
            recordItem.ItemSizeTotalMB = itemSizeTotalMb != null ? itemSizeTotalMb.ToString() : string.Empty;

            recordItem.Remarks = remarks;

            _logger.DynamicCSV(recordItem);
        }
    }

    public class PHLItemReportParameters : ISolutionParameters
    {
        public SPOPreservationHoldLibraryParameters PHLParam { get; set; }
        public SPOTenantSiteUrlsWithAccessParameters SitesAccParam { get; set; }
        public SPOListsParameters ListsParam { get; set; }
        public SPOItemsParameters ItemsParam { get; set; }
        internal SPOTenantItemsParameters TItemsParam
        {
            get { return new(SitesAccParam, ListsParam, ItemsParam); }
        }

        public PHLItemReportParameters(SPOPreservationHoldLibraryParameters phlParam,
                                       SPOTenantSiteUrlsWithAccessParameters sitesParam,
                                       SPOListsParameters listsParam,
                                       SPOItemsParameters itemsParam)
        {
            PHLParam = phlParam;
            SitesAccParam = sitesParam;
            ListsParam = listsParam;
            ItemsParam = itemsParam;
        }
    }
}
