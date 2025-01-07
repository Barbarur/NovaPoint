using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Logging;
using System.Linq.Expressions;

namespace NovaPointLibrary.Solutions.Report
{
    public class PageAssetsReport
    {
        public static readonly string s_SolutionName = "Page assets report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-PageAssetsReport";

        private PageAssetsReportParameters _param;
        private readonly LoggerSolution _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private static readonly Expression<Func<Microsoft.SharePoint.Client.List, object>>[] _listExpressions = new Expression<Func<Microsoft.SharePoint.Client.List, object>>[]
        {
            l => l.Title,
            l => l.BaseType,
            l => l.Id,
            l => l.ItemCount,
            l => l.RootFolder.ServerRelativeUrl,

        };

        private static readonly Expression<Func<ListItem, object>>[] _assetsExpressions = new Expression<Func<ListItem, object>>[]
        {
            f => f["Author"],
            f => f["Created"],
            f => f["Editor"],
            f => f["ID"],
            f => f.FileSystemObjectType,
            f => f["FileLeafRef"],
            f => f["FileRef"],
        };

        private readonly SPOItemsParameters _assetsParam = new()
        {
            FileExpresions = _assetsExpressions,
            AllItems = false,
            FolderSiteRelativeUrl = "SiteAssets/SitePages/"
        };

        private static readonly Expression<Func<ListItem, object>>[] _pageExpressions = new Expression<Func<ListItem, object>>[]
        {
            f => f["CanvasContent1"],


            f => f["Author"],
            f => f["Created"],
            f => f["Editor"],
            f => f["ID"],
            f => f.File.CheckOutType,
            f => f.FileSystemObjectType,
            f => f["FileLeafRef"],
            f => f["FileRef"],
            f => f["File_x0020_Size"],
            f => f["Modified"],
            f => f["SMTotalSize"],
            f => f.Versions,
            f => f["_UIVersionString"],

        };

        private readonly SPOItemsParameters _pagesParam = new()
        {
            FileExpresions = _pageExpressions,
            AllItems = true,
        };

        private PageAssetsReport(LoggerSolution logger, Commands.Authentication.AppInfo appInfo, PageAssetsReportParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(PageAssetsReportParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {

            LoggerSolution logger = new(uiAddLog, "PageAssetsReport", parameters);

            Dictionary<Type, string> solutionReports = new()
            {
                { typeof(PageAssetsReportRecord), "PageAssetsReport" },
                { typeof(UnusedPageAssetsReportRecord), "UnusedAssetsReport" },
            };
            logger.AddSolutionReports(solutionReports);

            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new PageAssetsReport(logger, appInfo, parameters).RunScriptAsync();

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

            await foreach (var siteRecord in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param.SiteAccParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (siteRecord.Ex != null)
                {
                    PageAssetsReportRecord siteReportRecord = new(siteRecord.SiteUrl, siteRecord.Ex.Message);
                    RecordCSV(siteReportRecord);
                    continue;
                }

                try
                {
                    await ProcessSite(siteRecord.SiteUrl);
                }
                catch (Exception ex)
                {
                    _logger.Error(GetType().Name, "Site", siteRecord.SiteUrl, ex);
                    PageAssetsReportRecord siteReportRecord = new(siteRecord.SiteUrl, ex.Message);
                    RecordCSV(siteReportRecord);
                }
            }
        }

        private async Task ProcessSite(string siteUrl)
        {
            _appInfo.IsCancelled();

            var siteAssetsLibrary = await new SPOListCSOM(_logger, _appInfo).GetList(siteUrl, "Site Assets", _listExpressions);

            List<ListItem> collAssets = new();
            await foreach (var sitePageAsset in new SPOListItemCSOM(_logger, _appInfo).GetAsync(siteUrl, siteAssetsLibrary, _assetsParam))
            {
                if (sitePageAsset.FileSystemObjectType == FileSystemObjectType.Folder) { continue; }
                collAssets.Add(sitePageAsset);
            }


            var sitePagesLibrary = await new SPOListCSOM(_logger, _appInfo).GetList(siteUrl, "Site Pages", _listExpressions);

            List<ListItem> collUsedAssets = new();
            await foreach (var page in new SPOListItemCSOM(_logger, _appInfo).GetAsync(siteUrl, sitePagesLibrary, _pagesParam))
            {
                foreach (var asset in collAssets)
                {
                    string pageCanvasContent = (string)page["CanvasContent1"];
                    _logger.Debug(GetType().Name, $"Page canvas: {pageCanvasContent}");
                    string assetUrl = (string)asset["FileLeafRef"];
                    _logger.Debug(GetType().Name, $"Asset URL: {assetUrl}");
                    if (!string.IsNullOrWhiteSpace(pageCanvasContent) && !string.IsNullOrWhiteSpace(assetUrl) && pageCanvasContent.Contains(assetUrl))
                    {
                        collUsedAssets.Add(asset); 
                        
                        PageAssetsReportRecord record = new(siteUrl, page, asset);
                        RecordCSV(record);
                    }

                }

            }

            var collUnusedAssets = collAssets.Except(collUsedAssets).ToList();
            foreach (var asset in collUnusedAssets)
            {
                UnusedPageAssetsReportRecord record = new(siteUrl, asset);
                RecordCSV(record);
            }
        }

        private void RecordCSV(ISolutionRecord record)
        {
            _logger.WriteRecord(record);
        }

    }

    internal class PageAssetsReportRecord : ISolutionRecord
    {
        public string SiteUrl { get; set; } = String.Empty;

        public string PageTitle { get; set; } = String.Empty;
        public string PageUrl { get; set; } = String.Empty;
        public DateTime PageCreated { get; set; } = DateTime.MinValue;
        public string PageCreatedBy { get; set; } = String.Empty;
        public DateTime PageModified { get; set; } = DateTime.MinValue;
        public string PageModifiedBy { get; set; } = String.Empty;

        public string SiteAssetTitle { get; set; } = String.Empty;
        public string SiteAssetId { get; set; } = String.Empty;
        public string SiteAssetUrl { get; set; } = String.Empty;

        public string Remarks { get; set; } = String.Empty;

        internal PageAssetsReportRecord() { }

        internal PageAssetsReportRecord(string siteUrl, string errorMessage)
        {
            SiteUrl = siteUrl;
            Remarks = errorMessage;
        }

        internal PageAssetsReportRecord(string siteUrl, ListItem page, ListItem asset)
        {
            SiteUrl = siteUrl;

            PageTitle = (string)page["FileLeafRef"];
            PageUrl = (string)page["FileRef"];
            PageCreated = (DateTime)page["Created"];
            FieldUserValue author = (FieldUserValue)page["Author"];
            PageCreatedBy = author.Email;
            PageModified = (DateTime)page["Modified"];
            FieldUserValue editor = (FieldUserValue)page["Editor"];
            PageModifiedBy = editor.Email;

            SiteAssetTitle = (string)asset["FileLeafRef"];
            SiteAssetId = asset["ID"].ToString();
            SiteAssetUrl = (string)asset["FileRef"];
        }

    }

    internal class UnusedPageAssetsReportRecord : ISolutionRecord
    {
        public string SiteUrl { get; set; } = String.Empty;

        public string SiteAssetTitle { get; set; } = String.Empty;
        public string SiteAssetId { get; set; } = String.Empty;
        public string SiteAssetUrl { get; set; } = String.Empty;

        public string Remarks { get; set; } = String.Empty;

        internal UnusedPageAssetsReportRecord() { }

        internal UnusedPageAssetsReportRecord(string siteUrl, ListItem asset)
        {
            SiteUrl = siteUrl;

            SiteAssetTitle = (string)asset["FileLeafRef"];
            SiteAssetId = asset["ID"].ToString();
            SiteAssetUrl = (string)asset["FileRef"];
        }

    }

    public class PageAssetsReportParameters : ISolutionParameters
    {
        internal readonly SPOAdminAccessParameters AdminAccess;
        internal readonly SPOTenantSiteUrlsParameters SiteParam;
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam
        {
            get
            {
                return new(AdminAccess, SiteParam);
            }
        }

        public PageAssetsReportParameters(
            SPOAdminAccessParameters adminAccess,
            SPOTenantSiteUrlsParameters siteParam)
        {
            AdminAccess = adminAccess;
            SiteParam = siteParam;
        }
    }
}
