using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Context;
using System.Linq.Expressions;

namespace NovaPointLibrary.Solutions.Report
{
    public class ShortcutODReport : ISolution
    {
        public static readonly string s_SolutionName = "OneDrive shortcut report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-ShortcutODReport";

        private ContextSolution _ctx;
        private ShortcutODReportParameters _param;


        private ShortcutODReport(ContextSolution context, ShortcutODReportParameters parameters)
        {
            _ctx = context;
            _param = parameters;

            Dictionary<Type, string> solutionReports = new()
            {
                { typeof(ShortcutODReportRecord), "Report" },
            };
            _ctx.DbHandler.AddSolutionReports(solutionReports);
        }

        public static ISolution Create(ContextSolution context, ISolutionParameters parameters)
        {
            return new ShortcutODReport(context, (ShortcutODReportParameters)parameters);
        }

        public async Task RunAsync()
        {
            _ctx.AppClient.IsCancelled();

            await foreach (var tenantItemRecord in new SPOTenantItemsCSOM(_ctx.Logger, _ctx.AppClient, _param.TItemsParam).GetAsync())
            {
                _ctx.AppClient.IsCancelled();

                if (tenantItemRecord.Ex != null)
                {
                    ShortcutODReportRecord record = new(tenantItemRecord);
                    RecordCSV(record);
                    continue;
                }

                if (tenantItemRecord.Item == null || tenantItemRecord.List == null)
                {
                    ShortcutODReportRecord record = new(tenantItemRecord)
                    {
                        Remarks = "Item or List is null",
                    };
                    RecordCSV(record);
                    continue;
                }

                if (tenantItemRecord.List.BaseType != BaseType.DocumentLibrary) { continue; }

                if (tenantItemRecord.Item.FileSystemObjectType.ToString() == "Folder") { continue; }

                try
                {
                    var shortcutData = JsonConvert.DeserializeObject<OneDriveShortcutProperties>((string)tenantItemRecord.Item["A2ODExtendedMetadata"]);

                    ShortcutODReportRecord record = new(tenantItemRecord);
                    record.AddTargetSite(shortcutData.riwu);
                    RecordCSV(record);
                }
                catch (Exception ex)
                {
                    _ctx.Logger.Error(GetType().Name, "Item", (string)tenantItemRecord.Item["FileRef"], ex);

                    ShortcutODReportRecord record = new(tenantItemRecord, ex.Message);
                    RecordCSV(record);
                }
            }
        }

        private void RecordCSV(ShortcutODReportRecord record)
        {
            _ctx.DbHandler.WriteRecord(record);
        }
    }

    internal class ShortcutODReportRecord : ISolutionRecord
    {
        public string SiteUrl { get; set; } = String.Empty;
        public string ListTitle { get; set; } = String.Empty;
        public string ListType { get; set; } = String.Empty;

        public string ItemID { get; set; } = String.Empty;
        public string ShortcutName { get; set; } = String.Empty;
        public string ShortcutPath { get; set; } = String.Empty;

        public string TargetSite { get; set; } = String.Empty;

        public string Remarks { get; set; } = String.Empty;

        public ShortcutODReportRecord() { }

        internal ShortcutODReportRecord(SPOTenantItemRecord tenantItemRecord, string remarks = "")
        {
            SiteUrl = tenantItemRecord.SiteUrl;
            if (tenantItemRecord.Ex != null) { Remarks = tenantItemRecord.Ex.Message; }
            else { Remarks = remarks; }

            if (tenantItemRecord.List != null)
            {
                ListTitle = tenantItemRecord.List.Title;
                ListType = tenantItemRecord.List.BaseType.ToString();
            }

            if (tenantItemRecord.Item != null)
            {
                ItemID = tenantItemRecord.Item.Id.ToString();
                ShortcutName = (string)tenantItemRecord.Item["FileLeafRef"];
                ShortcutPath = (string)tenantItemRecord.Item["FileRef"];
            }
        }

        internal void AddTargetSite(string targetSite)
        {
            TargetSite = targetSite;
        }
    }

    public class ShortcutODReportParameters : ISolutionParameters
    {
        private static readonly Expression<Func<ListItem, object>>[] _fileExpressions = new Expression<Func<ListItem, object>>[]
        {
            i => i["A2ODExtendedMetadata"],
            i => i["Author"],
            i => i["Created"],
            i => i["Editor"],
            i => i["ID"],
            i => i.FileSystemObjectType,
            i => i["FileLeafRef"],
            i => i["FileRef"],
        };

        internal readonly SPOAdminAccessParameters AdminAccess;
        internal readonly SPOTenantSiteUrlsParameters SiteParam;
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam
        {
            get
            {
                return new(AdminAccess, SiteParam);
            }
        }
        internal SPOListsParameters ListsParam { get; set; } = new();
        internal SPOItemsParameters ItemsParam { get; set; }
        public SPOTenantItemsParameters TItemsParam
        {
            get { return new(SiteAccParam, ListsParam, ItemsParam); }
        }

        public ShortcutODReportParameters(SPOAdminAccessParameters adminAccess, 
                                          SPOTenantSiteUrlsParameters siteParam,
                                          SPOItemsParameters itemsParameters)
        {
            AdminAccess = adminAccess;
            SiteParam = siteParam;
            ItemsParam = itemsParameters;

            SiteParam.IncludePersonalSite = true;
            SiteParam.IncludeSubsites = false;
            ListsParam.AllLists = false;
            ListsParam.IncludeLists = false;
            ListsParam.IncludeLibraries = false;
            ListsParam.ListTitle = "Documents";
            ItemsParam.FileExpressions = _fileExpressions;
        }
    }
}
