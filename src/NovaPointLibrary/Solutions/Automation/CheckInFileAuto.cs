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

            await foreach (var resultItem in new SPOTenantItemsCSOM(_logger, _appInfo, _param.TItemsParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (!String.IsNullOrWhiteSpace(resultItem.ErrorMessage))
                {
                    ItemReportRecord record = new(resultItem);
                    _logger.RecordCSV(record);
                    continue;
                }

                if (resultItem.Item == null || resultItem.List == null)
                {
                    ItemReportRecord record = new(resultItem)
                    {
                        Remarks = "Item or List is null",
                    };
                    _logger.RecordCSV(record);
                    continue;
                }

                if (resultItem.Item.FileSystemObjectType.ToString() == "Folder") { continue; }

                if (resultItem.Item.File.CheckOutType == CheckOutType.None) { continue; }

                try
                {
                    await ProcessItem(resultItem);
                }
                catch (Exception ex)
                {
                    _logger.ReportError("Item", (string)resultItem.Item["FileRef"], ex);

                    CheckInFileAutoRecord record = new(resultItem, ex.Message);
                    _logger.RecordCSV(record);
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
                _logger.RecordCSV(record);
            }
            catch (Exception ex)
            {
                CheckInFileAutoRecord record = new(resultItem, ex.Message);
                _logger.RecordCSV(record);
            }

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

        internal CheckInFileAutoRecord(SPOTenantItemRecord resultItem, string remarks = "")
        {
            SiteUrl = resultItem.SiteUrl;
            if (String.IsNullOrWhiteSpace(remarks)) { Remarks = resultItem.ErrorMessage; }
            else { Remarks = remarks; }

            if (resultItem.List != null)
            {
                ListTitle = resultItem.List.Title;
                ListType = resultItem.List.BaseType.ToString();
            }

            if (resultItem.Item != null)
            {
                ItemID = resultItem.Item.Id.ToString();
                ItemTitle = resultItem.Item.File.Name;
                ItemPath = resultItem.Item.File.ServerRelativeUrl;
            }
        }

    }


    public class CheckInFileAutoParameters : ISolutionParameters
    {
        public bool ReportMode { get; set; } = true;
        public string CheckinType { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;

        internal SPOTenantSiteUrlsWithAccessParameters SitesAccParam { get; set; }
        internal SPOListsParameters ListsParam { get; set; }
        internal SPOItemsParameters ItemsParam { get; set; }
        public SPOTenantItemsParameters TItemsParam
        {
            get { return new(SitesAccParam, ListsParam, ItemsParam); }
        }

        public CheckInFileAutoParameters(SPOTenantSiteUrlsWithAccessParameters sitesParam,
                                         SPOListsParameters listsParam,
                                         SPOItemsParameters itemsParameters)
        {
            SitesAccParam = sitesParam;
            ListsParam = listsParam;
            ItemsParam = itemsParameters;
        }
    }
}
