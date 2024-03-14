using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using System.Dynamic;
using System.Linq.Expressions;

namespace NovaPointLibrary.Solutions.Report
{
    public class ShortcutODReport : ISolution
    {
        public static readonly string s_SolutionName = "OneDrive shortcut report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-ShortcutODReport";

        private ShortcutODReportParameters _param;
        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;

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

        private ShortcutODReport(NPLogger logger, AppInfo appInfo, ShortcutODReportParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(ShortcutODReportParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            parameters.TListsParam.SiteAccParam.SiteParam.IncludePersonalSite = true;
            parameters.TListsParam.SiteAccParam.SiteParam.IncludeShareSite = false;
            parameters.TListsParam.SiteAccParam.SiteParam.OnlyGroupIdDefined = false;
            parameters.TListsParam.SiteAccParam.SiteParam.IncludeSubsites = false;
            parameters.TListsParam.ListParam.AllLists = false;
            parameters.TListsParam.ListParam.IncludeLists = false;
            parameters.TListsParam.ListParam.IncludeLibraries = false;
            parameters.TListsParam.ListParam.ListTitle = "Documents";
            parameters.ItemsParam.FileExpresions = _fileExpressions;

            NPLogger logger = new(uiAddLog, "ShortcutODReport", parameters);
            try
            {
                AppInfo appInfo = await AppInfo.BuildAsync(logger, cancelTokenSource);

                await new ShortcutODReport(logger, appInfo, parameters).RunScriptAsync();

                logger.ScriptFinish();

            }
            catch (Exception ex)
            {
                logger.ScriptFinish(ex);
            }
        }
        //public ShortcutODReport(ShortcutODReportParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        //{
        //    _param.TListsParam.SiteAccParam.SiteParam.IncludePersonalSite = true;
        //    _param.TListsParam.SiteAccParam.SiteParam.IncludeShareSite = false;
        //    _param.TListsParam.SiteAccParam.SiteParam.OnlyGroupIdDefined = false;
        //    _param.TListsParam.SiteAccParam.SiteParam.IncludeSubsites = false;
        //    _param.TListsParam.ListParam.AllLists= false;
        //    _param.TListsParam.ListParam.IncludeLists = false;
        //    _param.TListsParam.ListParam.IncludeLibraries = false;
        //    _param.TListsParam.ListParam.ListTitle = "Documents";
        //    _param.ItemsParam.FileExpresions = _fileExpressions;

        //    _logger = new(uiAddLog, this.GetType().Name, _param);
        //    _appInfo = new(_logger, cancelTokenSource);
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

                if ( !results.SiteUrl.Contains(_appInfo.RootPersonalUrl, StringComparison.OrdinalIgnoreCase) ) { continue; }

                if (!String.IsNullOrWhiteSpace(results.ErrorMessage) || results.List == null)
                {
                    AddRecord(results.SiteUrl, results.List, remarks: results.ErrorMessage);
                    continue;
                }

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

        private async Task ProcessItems(string siteUrl, List oList, ProgressTracker parentProgress)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Start getting Items for {oList.BaseType} '{oList.Title}' in '{siteUrl}'");

            if (oList.BaseType != BaseType.DocumentLibrary) { return; }

            ProgressTracker progress = new(parentProgress, oList.ItemCount);
            await foreach (ListItem oItem in new SPOListItemCSOM(_logger, _appInfo).GetAsync(siteUrl, oList, _param.ItemsParam))
            {
                _appInfo.IsCancelled();

                if (!String.IsNullOrWhiteSpace((string)oItem["A2ODExtendedMetadata"]))
                {
                    try
                    {
                        var shortcutData = JsonConvert.DeserializeObject<OneDriveShortcutProperties>((string)oItem["A2ODExtendedMetadata"]);

                        AddRecord(siteUrl, oList, oItem, shortcutData.riwu);
                    }
                    catch (Exception ex)
                    {
                        _logger.ReportError("Item", $"{oItem["FileRef"]}", ex);
                        AddRecord(siteUrl, oList, remarks: ex.Message);
                    }
                }

                progress.ProgressUpdateReport();
            }
        }

        private void AddRecord(string siteUrl,
                               Microsoft.SharePoint.Client.List? oList = null,
                               Microsoft.SharePoint.Client.ListItem? oItem = null,
                               string targetSite = "",
                               string remarks = "")
        {

            dynamic recordItem = new ExpandoObject();
            recordItem.SiteUrl = siteUrl;
            recordItem.ListTitle = oList != null ? oList.Title : String.Empty;
            recordItem.ListType = oList != null ? oList.BaseType.ToString() : String.Empty;

            recordItem.ID = oItem != null ? oItem["ID"] : string.Empty;
            recordItem.ShortcutName = oItem != null ? oItem["FileLeafRef"] : string.Empty;
            recordItem.ShortcutPath = oItem != null ? oItem["FileRef"] : string.Empty;

            recordItem.TargetSite = targetSite;

            recordItem.Remarks = remarks;

            _logger.DynamicCSV(recordItem);
        }
    }

    public class ShortcutODReportParameters : ISolutionParameters
    {
        public SPOTenantListsParameters TListsParam {  get; set; }
        public SPOItemsParameters ItemsParam {  get; set; }

        public ShortcutODReportParameters(SPOTenantListsParameters listsParameters,
                                          SPOItemsParameters itemsParameters)
        {
            TListsParam = listsParameters;
            ItemsParam = itemsParameters;
        }
    }
}
