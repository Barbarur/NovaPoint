using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using System.Dynamic;
using System.Linq.Expressions;

namespace NovaPointLibrary.Solutions.Report
{
    public class ShortcutODReport : ISolution
    {
        public static readonly string s_SolutionName = "OneDrive shortcut report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-ShortcutODReport";

        private ShortcutODReportParameters _param = new();
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (ShortcutODReportParameters)value; }
        }

        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;

        public ShortcutODReport(AppInfo appInfo, Action<LogInfo> uiAddLog, ShortcutODReportParameters parameters)
        {
            Parameters = parameters;
            _appInfo = appInfo;

            _param.IncludePersonalSite = true;
            _param.IncludeShareSite = false;
            _param.OnlyGroupIdDefined = false;
            _param.IncludeSubsites = false;
            _param.ListTitle = "Documents";

            _logger = new(uiAddLog, this);
        }

        public async Task RunAsync()
        {
            try
            {
                if (String.IsNullOrWhiteSpace(_param.AdminUPN))
                {
                    throw new Exception("FORM INCOMPLETED: Admin UPN cannot be empty.");
                }
                else if (string.IsNullOrWhiteSpace(_param.SiteUrl) && !_param.SiteAll)
                {
                    throw new Exception($"FORM INCOMPLETED: Site URL cannot be empty when no processing all sites");
                }
                else
                {
                    await RunScriptAsync();
                    _logger.ScriptFinish();
                }
            }
            catch (Exception ex)
            {
                _logger.ScriptFinish(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();

            await foreach (var results in new SPOTenantListsCSOM(_logger, _appInfo, _param.GetListParameters()).GetListsAsync())
            {
                _appInfo.IsCancelled();

                if (!String.IsNullOrWhiteSpace(results.Remarks) || results.List == null)
                {
                    AddRecord(results.SiteUrl, results.List, remarks: results.Remarks);
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
            Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] fileExpressions = new Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[]
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

            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Start getting Items for {oList.BaseType} '{oList.Title}' in '{siteUrl}'");

            if (oList.BaseType != BaseType.DocumentLibrary) { return; }

            ProgressTracker progress = new(parentProgress, oList.ItemCount);
            await foreach (ListItem oItem in new SPOListItemCSOM(_logger, _appInfo).Get(siteUrl, oList.Title, _param.GetItemParameters(), fileExpressions))
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

            _logger.RecordCSV(recordItem);
        }
    }

    public class ShortcutODReportParameters : SPOTenantItemsParameters, ISolutionParameters
    {

        internal SPOTenantItemsParameters GetItemParameters()
        {
            return this;
        }

    }
}
