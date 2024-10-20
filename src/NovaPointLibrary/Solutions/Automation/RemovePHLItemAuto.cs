using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.PreservationHoldLibrary;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Logging;
using System.Dynamic;
using System.Linq.Expressions;

namespace NovaPointLibrary.Solutions.Automation
{
    public class RemovePHLItemAuto
    {
        public static readonly string s_SolutionName = "Remove Files from Preservation";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-RemovePHLItemAuto";

        private RemovePHLItemAutoParameters _param;
        private readonly LoggerSolution _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private static readonly Expression<Func<ListItem, object>>[] _fileExpressions = new Expression<Func<ListItem, object>>[]
        {
            f => f.HasUniqueRoleAssignments,
            f => f["Author"],
            f => f["Created"],
            f => f["Editor"],
            f => f.Id,
            f => f["ID"],
            f => f.File.CheckOutType,
            f => f.FileSystemObjectType,
            f => f["FileLeafRef"],
            f => f["FileRef"],
            f => f["File_x0020_Size"],
            f => f["Modified"],
            f => f["PreservationDatePreserved"],
            f => f["PreservationOriginalDocumentName"],
            f => f["PreservationOriginalURL"],
            f => f["SMTotalSize"],
            f => f.Versions,
            f => f["_UIVersionString"],

        };

        private RemovePHLItemAuto(LoggerSolution logger, Commands.Authentication.AppInfo appInfo, RemovePHLItemAutoParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(RemovePHLItemAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            //parameters.TListsParam.SiteAccParam.SiteParam.IncludeSubsites = false;
            parameters.ListsParam.AllLists = false;
            parameters.ListsParam.IncludeLists = false;
            parameters.ListsParam.IncludeLibraries = false;
            parameters.ListsParam.ListTitle = "Preservation Hold Library";
            parameters.ItemsParam.FileExpresions = _fileExpressions;

            LoggerSolution logger = new(uiAddLog, "RemovePHLItemAuto", parameters);
            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new RemovePHLItemAuto(logger, appInfo, parameters).RunScriptAsync();

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

            await foreach (var tenantItemRecord in new SPOTenantItemsCSOM(_logger, _appInfo, _param.TItemsParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (tenantItemRecord.Ex != null)
                {
                    AddRecord(tenantItemRecord.ListRecord.SiteUrl, tenantItemRecord.ListRecord.List, remarks: tenantItemRecord.Ex.Message);
                    continue;
                }

                if (tenantItemRecord.Item == null || tenantItemRecord.ListRecord.List == null) { continue; }

                try
                {
                    if (tenantItemRecord.Item.FileSystemObjectType.ToString() == "Folder") { continue; }

                    if (_param.PHLParam.MatchParameters(tenantItemRecord.Item))
                    {
                        await new SPOListItemCSOM(_logger, _appInfo).RemoveAsync(tenantItemRecord.ListRecord.SiteUrl, tenantItemRecord.ListRecord.List, tenantItemRecord.Item, _param.Recycle);
                        AddRecord(tenantItemRecord.ListRecord.SiteUrl, tenantItemRecord.ListRecord.List, tenantItemRecord.Item, "Deleted");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(GetType().Name, "Item", (string)tenantItemRecord.Item["FileRef"], ex);

                    AddRecord(tenantItemRecord.ListRecord.SiteUrl, tenantItemRecord.ListRecord.List, tenantItemRecord.Item, remarks: ex.Message);
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
            recordItem.ListServerRelativeUrl = oList != null ? oList.RootFolder.ServerRelativeUrl : string.Empty;

            recordItem.ItemID = oItem != null ? oItem["ID"] : string.Empty;

            recordItem.ItemName = oItem != null ? oItem["FileLeafRef"] : string.Empty;
            recordItem.ItemOriginalName = oItem != null ? oItem["PreservationOriginalDocumentName"] : string.Empty;

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


            float? itemSizeMb = oItem != null ? (float)Math.Round(Convert.ToDouble(oItem["File_x0020_Size"]) / Math.Pow(1024, 2), 2) : null;
            recordItem.ItemSizeMb = itemSizeMb != null ? itemSizeMb.ToString() : string.Empty;

            FieldLookupValue? FileSizeTotalBytes = oItem != null ? (FieldLookupValue)oItem["SMTotalSize"] : null;
            float? itemSizeTotalMb = FileSizeTotalBytes != null ? (float)Math.Round(FileSizeTotalBytes.LookupId / Math.Pow(1024, 2), 2) : null;
            recordItem.ItemSizeTotalMB = itemSizeTotalMb != null ? itemSizeTotalMb.ToString() : string.Empty;

            recordItem.Remarks = remarks;

            _logger.DynamicCSV(recordItem);
        }
    }

    public class RemovePHLItemAutoParameters : ISolutionParameters
    {
        public bool Recycle { get; set; } = true;
        public SPOPreservationHoldLibraryParameters PHLParam { get; set; }
        internal SPOAdminAccessParameters AdminAccess;
        internal SPOTenantSiteUrlsParameters SiteParam;
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam
        {
            get
            {
                return new(AdminAccess, SiteParam);
            }
        }
        internal SPOListsParameters ListsParam { get; set; }
        internal SPOItemsParameters ItemsParam { get; set; }
        public SPOTenantItemsParameters TItemsParam
        {
            get { return new(SiteAccParam, ListsParam, ItemsParam); }
        }

        public RemovePHLItemAutoParameters(SPOPreservationHoldLibraryParameters phlParam,
                                           SPOAdminAccessParameters adminAccess,
                                           SPOTenantSiteUrlsParameters siteParam,
                                           SPOListsParameters listsParam,
                                           SPOItemsParameters itemsParam)
        {
            PHLParam = phlParam;
            AdminAccess = adminAccess;
            SiteParam = siteParam;
            ListsParam = listsParam;
            ItemsParam = itemsParam;
        }
    }
}
