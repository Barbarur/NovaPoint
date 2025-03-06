using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Logging;
using System.Linq.Expressions;

namespace NovaPointLibrary.Solutions.Report
{
    public class ItemReport : ISolution
    {
        public static readonly string s_SolutionName = "Files and Items report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-ItemReport";

        private ItemReportParameters _param;
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (ItemReportParameters)value; }
        }

        private readonly LoggerSolution _logger;
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
            f => f["UniqueId"],
            f => f["Modified"],
            f => f["SMTotalSize"],
            f => f.Versions,
            f => f["_UIVersionString"],

        };

        private static readonly Expression<Func<ListItem, object>>[] _itemExpressions = new Expression<Func<ListItem, object>>[]
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
            f => f["UniqueId"],
            i => i["Modified"],
            i => i["SMTotalSize"],
            i => i["Title"],
            i => i.Versions,
            i => i["_UIVersionString"],
        };

        private ItemReport(LoggerSolution logger, Commands.Authentication.AppInfo appInfo, ItemReportParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(ItemReportParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            parameters.ItemsParam.FileExpresions = _fileExpressions;
            parameters.ItemsParam.ItemExpresions = _itemExpressions;

            LoggerSolution logger = new(uiAddLog, "ItemReport", parameters);
            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new ItemReport(logger, appInfo, parameters).RunScriptAsync();

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

                ItemReportRecord record = new(tenantItemRecord);
                if (tenantItemRecord.Ex != null)
                {
                    RecordCSV(record);
                    continue;
                }

                if (tenantItemRecord.Item == null || tenantItemRecord.List == null)
                {
                    record.Remarks = "Item or List is null";
                    RecordCSV(record);
                    continue;
                }


                try
                {
                    await record.AddDetails(_logger, _appInfo, tenantItemRecord.Item);
                }
                catch (Exception ex)
                {
                    _logger.Error(GetType().Name, "Item", (string)tenantItemRecord.Item["FileRef"], ex);
                    record.Remarks = ex.Message;
                }
                finally
                {
                    RecordCSV(record);
                }
            }
        }

        private void RecordCSV(ItemReportRecord record)
        {
            _logger.RecordCSV(record);
        }

    }

    public class ItemReportRecord : ISolutionRecord
    {
        internal string SiteUrl { get; set; } = String.Empty;
        internal string ListTitle { get; set; } = String.Empty;
        internal string ListType { get; set; } = String.Empty;
        internal string ListServerRelativeUrl { get; set; } = String.Empty;

        internal string ItemID { get; set; } = String.Empty;
        internal Guid ItemUniqueID { get; set; } = Guid.Empty;
        internal string ItemTitle { get; set; } = String.Empty;
        internal string ItemPath { get; set; } = String.Empty;
        internal string ItemType { get; set; } = String.Empty;

        internal DateTime ItemCreated { get; set; } = DateTime.MinValue;
        internal string ItemCreatedBy { get; set; } = String.Empty;
        internal DateTime ItemModified { get; set; } = DateTime.MinValue;
        internal string ItemModifiedBy { get; set; } = String.Empty;

        internal string ItemVersion { get; set; } = String.Empty;
        internal string ItemVersionsCount { get; set; } = String.Empty;
        internal string ItemSizeMb { get; set; } = String.Empty;
        internal string ItemSizeTotalMB { get; set; } = String.Empty;

        internal string FileCheckOut { get; set; } = String.Empty;

        internal string Remarks { get; set; } = String.Empty;

        internal ItemReportRecord(SPOTenantItemRecord tenantItemRecord,
                                  string remarks = "")
        {
            SiteUrl = tenantItemRecord.SiteUrl;
            if (tenantItemRecord.Ex != null) { Remarks = tenantItemRecord.Ex.Message; }
            else { Remarks = remarks; }

            if (tenantItemRecord.List != null)
            {
                ListTitle = tenantItemRecord.List.Title;
                ListType = tenantItemRecord.List.BaseType.ToString();
                ListServerRelativeUrl = tenantItemRecord.List.RootFolder.ServerRelativeUrl;
            }

            if (tenantItemRecord.Item != null)
            {
                ItemID = tenantItemRecord.Item.Id.ToString();
                ItemUniqueID = (Guid)tenantItemRecord.Item["UniqueId"];
                ItemPath = (string)tenantItemRecord.Item["FileRef"];
                ItemType = tenantItemRecord.Item.FileSystemObjectType.ToString();

                if (tenantItemRecord.Item.ParentList.BaseType == BaseType.DocumentLibrary || tenantItemRecord.Item.FileSystemObjectType.ToString() == "Folder")
                {
                    ItemTitle = (string)tenantItemRecord.Item["FileLeafRef"];
                }
                else if (tenantItemRecord.Item.ParentList.BaseType == BaseType.GenericList)
                {
                    ItemTitle = (string)tenantItemRecord.Item["Title"];
                }
            }
        }

        internal async Task AddDetails(LoggerSolution logger, AppInfo appInfo, ListItem oItem)
        {
            ItemCreated = (DateTime)oItem["Created"];
            FieldUserValue author = (FieldUserValue)oItem["Author"];
            ItemCreatedBy = author.Email;

            ItemModified = (DateTime)oItem["Modified"];
            FieldUserValue editor = (FieldUserValue)oItem["Editor"];
            ItemModifiedBy = editor.Email;

            ItemVersion = (string)oItem["_UIVersionString"];
            ItemVersionsCount = oItem.Versions.Count.ToString();

            if (oItem.FileSystemObjectType.ToString() == "Folder")
            {
                return;
            }
            else if (oItem.ParentList.BaseType == BaseType.DocumentLibrary)
            {
                ItemSizeMb = Math.Round(Convert.ToDouble(oItem["File_x0020_Size"]) / Math.Pow(1024, 2), 2).ToString();
                try
                {
                    FieldLookupValue FileSizeTotalBytes = (FieldLookupValue)oItem["SMTotalSize"];
                    ItemSizeTotalMB = Math.Round(FileSizeTotalBytes.LookupId / Math.Pow(1024, 2), 2).ToString();
                }
                catch
                {
                    string FileSizeTotalBytes = (string)oItem["SMTotalSize"];
                    ItemSizeTotalMB = Math.Round(long.Parse(FileSizeTotalBytes) / Math.Pow(1024, 2), 2).ToString();
                }

                FileCheckOut = oItem.File.CheckOutType.ToString();
            }
            else if (oItem.ParentList.BaseType == BaseType.GenericList)
            {
                int itemSizeTotalBytes = 0;
                foreach (var oAttachment in oItem.AttachmentFiles)
                {
                    var oFileAttachment = await new SPOListItemCSOM(logger, appInfo).GetAttachmentFileAsync(SiteUrl, oAttachment.ServerRelativeUrl);

                    itemSizeTotalBytes += (int)oFileAttachment.Length;
                }
                float itemSizeTotalMb = (float)Math.Round(itemSizeTotalBytes / Math.Pow(1024, 2), 2);

                ItemSizeMb = itemSizeTotalMb.ToString();
                ItemSizeTotalMB = itemSizeTotalMb.ToString();
            }

        }

    }

    public class ItemReportParameters : ISolutionParameters
    {
        public SPOAdminAccessParameters AdminAccess { get; set; }
        public SPOTenantSiteUrlsParameters SiteParam { get; set; }
        internal SPOTenantSiteUrlsWithAccessParameters SiteAccParam
        {
            get
            {
                return new(AdminAccess, SiteParam);
            }
        }
        public SPOListsParameters ListsParam { get; set; }
        public SPOItemsParameters ItemsParam { get; set; }
        internal SPOTenantItemsParameters TItemsParam
        {
            get { return new(SiteAccParam, ListsParam, ItemsParam); }
        }

        public ItemReportParameters(SPOAdminAccessParameters adminAccess,
                                    SPOTenantSiteUrlsParameters siteParam,
                                    SPOListsParameters listsParam,
                                    SPOItemsParameters itemsParam)
        {
            AdminAccess = adminAccess;
            SiteParam = siteParam;
            ListsParam = listsParam;
            ItemsParam = itemsParam;
        }
    }
}
