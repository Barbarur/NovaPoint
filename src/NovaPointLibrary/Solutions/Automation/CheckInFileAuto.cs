using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
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
            parameters.TListsParam.ListParam.IncludeLibraries = true;
            parameters.TListsParam.ListParam.IncludeLists = false;
            parameters.TListsParam.ListParam.IncludeHiddenLists = false;
            parameters.TListsParam.ListParam.IncludeSystemLists = false;


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

        //public CheckInFileAuto(CheckInFileAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        //{
        //    Parameters = parameters;
        //    _param.ItemsParam.FileExpresions = _fileExpressions;
        //    _param.TListsParam.ListParam.IncludeLibraries = true;
        //    _param.TListsParam.ListParam.IncludeLists = false;
        //    _param.TListsParam.ListParam.IncludeHiddenLists = false;
        //    _param.TListsParam.ListParam.IncludeSystemLists = false;

        //    _logger = new(uiAddLog, this.GetType().Name, parameters);
        //    _appInfo = new(_logger, cancelTokenSource);

        //    if (_param.CheckinType == "Major") { _checkinType = CheckinType.MajorCheckIn; }
        //    else if (_param.CheckinType == "Minor") { _checkinType = CheckinType.MinorCheckIn; }
        //    else if (_param.CheckinType == "Discard") { _checkinType = CheckinType.OverwriteCheckIn; }
        //    else { throw new("Check in type is incorrect."); }
        //}

        //public async Task RunAsync()
        //{
        //    try
        //    {
        //        await RunScriptAsync();

        //        _logger.ScriptFinish();

        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.ScriptFinish(ex);
        //    }
        //}

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();

            await foreach (var results in new SPOTenantListsCSOM(_logger, _appInfo, _param.TListsParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (!String.IsNullOrWhiteSpace(results.ErrorMessage) || results.List == null)
                {
                    CheckInFileAutoRecord record = new(results.SiteUrl, results.ErrorMessage);
                    AddRecord(record);
                    continue;
                }

                try
                {
                    await ProcessItems(results.SiteUrl, results.List, results.Progress);
                }
                catch (Exception ex)
                {
                    _logger.ReportError(results.List.BaseType.ToString(), results.List.DefaultViewUrl, ex);

                    CheckInFileAutoRecord record = new(results.SiteUrl, ex.Message);
                    record.AddList(results.List);
                    AddRecord(record);
                }
            }
        }

        private async Task ProcessItems(string siteUrl, List oList, ProgressTracker parentProgress)
        {
            _appInfo.IsCancelled();

            ProgressTracker progress = new(parentProgress, oList.ItemCount);

            var spoItem = new SPOListItemCSOM(_logger, _appInfo);
            await foreach (ListItem oItem in spoItem.GetAsync(siteUrl, oList, _param.ItemsParam))
            {
                _appInfo.IsCancelled();

                if (oItem.FileSystemObjectType.ToString() == "Folder") { continue; }

                if (oItem.File.CheckOutType == CheckOutType.None) { continue; }

                try
                {
                    if (!_param.ReportMode)
                    {
                        await new SPOFileCSOM(_logger, _appInfo).CheckInAsync(siteUrl, oItem, _checkinType, _param.Comment);
                    }

                    CheckInFileAutoRecord record = new(siteUrl);
                    record.AddList(oList);
                    record.AddFile(oItem);
                    AddRecord(record);
                }
                catch (Exception ex)
                {
                    _logger.ReportError("Item", (string)oItem["FileRef"], ex);

                    CheckInFileAutoRecord record = new(siteUrl, ex.Message);
                    record.AddList(oList);
                    record.AddFile(oItem);
                    AddRecord(record);
                }

                progress.ProgressUpdateReport();
            }
        }

        private void AddRecord(CheckInFileAutoRecord record)
        {
            _logger.RecordCSV(record);
        }
    }

    public class CheckInFileAutoRecord : ISolutionRecord
    {
        internal string SiteUrl { get; set; } = String.Empty;
        internal string ListTitle { get; set; } = String.Empty;
        internal string ListType { get; set; } = String.Empty;

        internal string FileID { get; set; } = String.Empty;
        internal string FileTitle { get; set; } = String.Empty;
        internal string ServerRelativeUrl { get; set; } = String.Empty;
        internal string CheckedOutByUser { get; set; } = String.Empty;

        internal string Remarks { get; set; } = String.Empty;

        internal CheckInFileAutoRecord(string siteUrl, string remarks = "")
        {
            SiteUrl = siteUrl;
            Remarks = remarks;
        }

        internal void AddList(List oList)
        {
            ListTitle = oList.Title;
            ListType = oList.BaseType.ToString();
        }
        internal void AddFile(ListItem oItem)
        {
            FileID = oItem.Id.ToString();
            FileTitle = oItem.File.Name;
            ServerRelativeUrl = oItem.File.ServerRelativeUrl;
            CheckedOutByUser = oItem.File.CheckedOutByUser.UserPrincipalName;
        }
    }


    public class CheckInFileAutoParameters : ISolutionParameters
    {
        public bool ReportMode { get; set; } = true;
        public string CheckinType { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;

        public SPOTenantListsParameters TListsParam {  get; set; }
        public SPOItemsParameters ItemsParam { get; set; }

        public CheckInFileAutoParameters(SPOTenantListsParameters listsParameters,
                                         SPOItemsParameters itemsParameters)
        {
            TListsParam = listsParameters;
            ItemsParam = itemsParameters;
        }
    }
}
