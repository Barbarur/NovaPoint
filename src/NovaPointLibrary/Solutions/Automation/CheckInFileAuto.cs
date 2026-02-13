using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Context;
using System.Linq.Expressions;

namespace NovaPointLibrary.Solutions.Automation
{
    public class CheckInFileAuto : ISolution
    {
        public readonly static String s_SolutionName = "Check-In files";
        public readonly static String s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-CheckInFileAuto";

        private ContextSolution _ctx;
        private CheckInFileAutoParameters _param;
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (CheckInFileAutoParameters)value; }
        }

        private static readonly Expression<Func<ListItem, object>>[] _fileExpressions = new Expression<Func<ListItem, object>>[]
        {
            f => f.Id,
            f => f["FileRef"],
            f => f.FileSystemObjectType,
            f => f.File.Level,
            f => f.File.CheckOutType,
            f => f.File.CheckedOutByUser,
            f => f.File.ServerRelativeUrl,
            f => f.File.Name,
            f => f.File.Title,
        };

        private readonly CheckinType _checkingType;

        private CheckInFileAuto(ContextSolution context, CheckInFileAutoParameters parameters)
        {
            _ctx = context;

            parameters.ItemsParam.FileExpressions = _fileExpressions;
            parameters.ListsParam.IncludeLibraries = true;
            parameters.ListsParam.IncludeLists = false;
            parameters.ListsParam.IncludeHiddenLists = false;
            parameters.ListsParam.IncludeSystemLists = false;
            _param = parameters;

            if (_param.CheckingType == "Major") { _checkingType = CheckinType.MajorCheckIn; }
            else if (_param.CheckingType == "Minor") { _checkingType = CheckinType.MinorCheckIn; }
            else if (_param.CheckingType == "Discard") { _checkingType = CheckinType.OverwriteCheckIn; }
            else { throw new("Check in type is incorrect."); }

            Dictionary<Type, string> solutionReports = new()
            {
                { typeof(CheckInFileAuto), "Report" },
            };
            _ctx.DbHandler.AddSolutionReports(solutionReports);
        }

        public static ISolution Create(ContextSolution context, ISolutionParameters parameters)
        {
            return new CheckInFileAuto(context, (CheckInFileAutoParameters)parameters);
        }

        public async Task RunAsync()
        {
            _ctx.AppClient.IsCancelled();

            await foreach (var tenantItemRecord in new SPOTenantItemsCSOM(_ctx.Logger, _ctx.AppClient, _param.TItemsParam).GetAsync())
            {
                _ctx.AppClient.IsCancelled();

                if (tenantItemRecord.Ex != null)
                {
                    CheckInFileAutoRecord record = new(tenantItemRecord);
                    RecordCSV(record);
                    continue;
                }

                if (tenantItemRecord.Item == null || tenantItemRecord.List == null)
                {
                    CheckInFileAutoRecord record = new(tenantItemRecord)
                    {
                        Remarks = "Item or List is null",
                    };
                    RecordCSV(record);
                    continue;
                }

                if (tenantItemRecord.Item.FileSystemObjectType.ToString() == "Folder") { continue; }

                if (tenantItemRecord.Item.File.CheckOutType == CheckOutType.None) { continue; }

                try
                {
                    await ProcessItem(tenantItemRecord);
                }
                catch (Exception ex)
                {
                    _ctx.Logger.Error(GetType().Name, "Item", (string)tenantItemRecord.Item["FileRef"], ex);

                    CheckInFileAutoRecord record = new(tenantItemRecord, ex.Message);
                    RecordCSV(record);
                }
            }
        }

        private async Task ProcessItem(SPOTenantItemRecord resultItem)
        {
            _ctx.AppClient.IsCancelled();

            try
            {
                if (!_param.ReportMode)
                {
                    await new SPOFileCSOM(_ctx.Logger, _ctx.AppClient).CheckInAsync(resultItem.SiteUrl, resultItem.Item, _checkingType, _param.Comment);
                }

                CheckInFileAutoRecord record = new(resultItem)
                {
                    CheckedOutByUser = resultItem.Item.File.CheckedOutByUser.UserPrincipalName,
                    CheckinType = _param.CheckingType,
                    Comment = _param.Comment,
                };
                RecordCSV(record);
            }
            catch (Exception ex)
            {
                CheckInFileAutoRecord record = new(resultItem, ex.Message);
                RecordCSV(record);
            }

        }

        private void RecordCSV(CheckInFileAutoRecord record)
        {
            _ctx.DbHandler.WriteRecord(record);
        }

    }

    internal class CheckInFileAutoRecord : ISolutionRecord
    {
        public string SiteUrl { get; set; } = String.Empty;
        public string ListTitle { get; set; } = String.Empty;
        public string ListType { get; set; } = String.Empty;

        public string ItemID { get; set; } = String.Empty;
        public string ItemTitle { get; set; } = String.Empty;
        public string ItemPath { get; set; } = String.Empty;

        public string CheckedOutByUser { get; set; } = String.Empty;
        public string CheckinType { get; set; } = String.Empty;
        public string Comment { get; set; } = String.Empty;

        public string Remarks { get; set; } = String.Empty;

        public CheckInFileAutoRecord() { }

        internal CheckInFileAutoRecord(SPOTenantItemRecord tenantItemRecord, string remarks = "")
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
                ItemTitle = tenantItemRecord.Item.File.Name;
                ItemPath = tenantItemRecord.Item.File.ServerRelativeUrl;
            }
        }

    }


    public class CheckInFileAutoParameters : ISolutionParameters
    {
        public bool ReportMode { get; set; } = true;
        public string CheckingType { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;

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

        public CheckInFileAutoParameters(
            bool reportMode,
            string checkinType,
            string comment,
            SPOAdminAccessParameters adminAccess,
            SPOTenantSiteUrlsParameters siteParam,
            SPOListsParameters listsParam,
            SPOItemsParameters itemsParameters)
        {
            ReportMode = reportMode;
            CheckingType = checkinType;
            Comment = comment;

            AdminAccess = adminAccess;
            SiteParam = siteParam;

            ListsParam = listsParam;
            ItemsParam = itemsParameters;
        }
    }
}
