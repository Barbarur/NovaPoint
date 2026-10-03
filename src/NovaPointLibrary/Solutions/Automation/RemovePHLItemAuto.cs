using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.PreservationHoldLibrary;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Core.Logging;
using System.Dynamic;
using System.Globalization;
using System.Linq.Expressions;

namespace NovaPointLibrary.Solutions.Automation
{
    public class RemovePHLItemAuto : ISolution
    {
        public static readonly string s_SolutionName = "Remove Files from Preservation";
        public static readonly string s_SolutionDocs = $"https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-{nameof(RemovePHLItemAuto)}";

        private ContextSolution _ctx;
        private RemovePHLItemAutoParameters _param;

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

        private RemovePHLItemAuto(ContextSolution context, RemovePHLItemAutoParameters parameters)
        {
            _ctx = context;

            parameters.ItemsParam.FileExpressions = _fileExpressions;
            _param = parameters;
        }

        public static ISolution Create(ContextSolution context, ISolutionParameters parameters)
        {
            return new RemovePHLItemAuto(context, (RemovePHLItemAutoParameters)parameters);
        }

        public async Task RunAsync()
        {
            _ctx.AppClient.IsCancelled();

            await foreach (var tenantItemRecord in new SPOTenantItemsCSOM(_ctx.Logger, _ctx.AppClient, _param.TItemsParam).GetAsync())
            {
                _ctx.AppClient.IsCancelled();

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
                        await new SPOListItemCSOM(_ctx.Logger, _ctx.AppClient).RemoveAsync(tenantItemRecord.ListRecord.SiteUrl, tenantItemRecord.ListRecord.List, tenantItemRecord.Item, _param.Recycle);
                        AddRecord(tenantItemRecord.ListRecord.SiteUrl, tenantItemRecord.ListRecord.List, tenantItemRecord.Item, "Deleted");
                    }
                }
                catch (Exception ex)
                {
                    _ctx.Logger.Error(GetType().Name, "Item", (string)tenantItemRecord.Item["FileRef"], ex);

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
            recordItem.ItemVersionsCount = oItem != null ? oItem.Versions.Count.ToString(CultureInfo.InvariantCulture) : string.Empty;


            float? itemSizeMb = oItem != null ? (float)Math.Round(Convert.ToDouble(oItem["File_x0020_Size"], CultureInfo.InvariantCulture) / Math.Pow(1024, 2), 2) : null;
            recordItem.ItemSizeMb = itemSizeMb != null ? itemSizeMb.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;

            FieldLookupValue? FileSizeTotalBytes = oItem != null ? (FieldLookupValue)oItem["SMTotalSize"] : null;
            float? itemSizeTotalMb = FileSizeTotalBytes != null ? (float)Math.Round(FileSizeTotalBytes.LookupId / Math.Pow(1024, 2), 2) : null;
            recordItem.ItemSizeTotalMB = itemSizeTotalMb != null ? itemSizeTotalMb.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;

            recordItem.Remarks = remarks;

            _ctx.Logger.DynamicCSV(recordItem);
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
        internal readonly SPOListsParameters ListsParam = new();
        internal readonly SPOItemsParameters ItemsParam = new();
        public SPOTenantItemsParameters TItemsParam
        {
            get { return new(SiteAccParam, ListsParam, ItemsParam); }
        }

        public RemovePHLItemAutoParameters(SPOPreservationHoldLibraryParameters phlParam,
                                           SPOAdminAccessParameters adminAccess,
                                           SPOTenantSiteUrlsParameters siteParam)
        {
            PHLParam = phlParam;
            AdminAccess = adminAccess;
            SiteParam = siteParam;

            // This automation only ever reads the Preservation Hold Library.
            ListsParam.AllLists = false;
            ListsParam.IncludeLists = false;
            ListsParam.IncludeLibraries = false;
            ListsParam.ListTitle = "Preservation Hold Library";
        }
    }
}
