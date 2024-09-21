using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions.Report;
using System.Linq.Expressions;

namespace NovaPointLibrary.Solutions.Automation
{
    public class CheckInFileAuto
    {
        public readonly static String s_SolutionName = "Check-In files";
        public readonly static String s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-CheckInFileAuto";

        private CheckInFileAutoParameters _param;
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (CheckInFileAutoParameters)value; }
        }

        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

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

        private readonly CheckinType _checkinType;

        private CheckInFileAuto(NPLogger logger, Commands.Authentication.AppInfo appInfo, CheckInFileAutoParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;

            if (_param.CheckinType == "Major") { _checkinType = CheckinType.MajorCheckIn; }
            else if (_param.CheckinType == "Minor") { _checkinType = CheckinType.MinorCheckIn; }
            else if (_param.CheckinType == "Discard") { _checkinType = CheckinType.OverwriteCheckIn; }
            else { throw new("Check in type is incorrect."); }
        }

        public static async Task RunAsync(CheckInFileAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            parameters.ItemsParam.FileExpresions = _fileExpressions;
            parameters.ListsParam.IncludeLibraries = true;
            parameters.ListsParam.IncludeLists = false;
            parameters.ListsParam.IncludeHiddenLists = false;
            parameters.ListsParam.IncludeSystemLists = false;


            NPLogger logger = new(uiAddLog, "CheckInFileAuto", parameters);
            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new CheckInFileAuto(logger, appInfo, parameters).RunScriptAsync();

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

            await foreach (var tenantItemRecord in new SPOTenantItemsCSOM(_logger, _appInfo, _param.TItemsParam).GetAsync())
            {
                _appInfo.IsCancelled();

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
                    _logger.ReportError(GetType().Name, "Item", (string)tenantItemRecord.Item["FileRef"], ex);

                    CheckInFileAutoRecord record = new(tenantItemRecord, ex.Message);
                    RecordCSV(record);
                }
            }
        }

        private async Task ProcessItem(SPOTenantItemRecord resultItem)
        {
            _appInfo.IsCancelled();

            try
            {
                if (!_param.ReportMode)
                {
                    await new SPOFileCSOM(_logger, _appInfo).CheckInAsync(resultItem.SiteUrl, resultItem.Item, _checkinType, _param.Comment);
                }

                CheckInFileAutoRecord record = new(resultItem)
                {
                    CheckedOutByUser = resultItem.Item.File.CheckedOutByUser.UserPrincipalName,
                    CheckinType = _param.CheckinType,
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
            _logger.RecordCSV(record);
        }

    }

    public class CheckInFileAutoRecord : ISolutionRecord
    {
        internal string SiteUrl { get; set; } = String.Empty;
        internal string ListTitle { get; set; } = String.Empty;
        internal string ListType { get; set; } = String.Empty;

        internal string ItemID { get; set; } = String.Empty;
        internal string ItemTitle { get; set; } = String.Empty;
        internal string ItemPath { get; set; } = String.Empty;

        internal string CheckedOutByUser { get; set; } = String.Empty;
        internal string CheckinType { get; set; } = String.Empty;
        internal string Comment { get; set; } = String.Empty;

        internal string Remarks { get; set; } = String.Empty;

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
        public string CheckinType { get; set; } = string.Empty;
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
            CheckinType = checkinType;
            Comment = comment;

            AdminAccess = adminAccess;
            SiteParam = siteParam;

            ListsParam = listsParam;
            ItemsParam = itemsParameters;
        }
    }
}
