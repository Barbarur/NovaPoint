using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Context;
using System.Linq.Expressions;


namespace NovaPointLibrary.Solutions.Report
{
    public class PageAssetsReport : ISolution
    {
        public static readonly string s_SolutionName = "Page assets report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-PageAssetsReport";

        private ContextSolution _ctx;
        private PageAssetsReportParameters _param;


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
            FileExpressions = _assetsExpressions,
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
            FileExpressions = _pageExpressions,
            AllItems = true,
        };

        private PageAssetsReport(ContextSolution context, PageAssetsReportParameters parameters)
        {
            _ctx = context;
            _param = parameters;

            Dictionary<Type, string> solutionReports = new()
            {
                { typeof(PageAssetsReportRecord), "PageAssetsReport" },
                { typeof(UnusedPageAssetsReportRecord), "UnusedAssetsReport" },
            };
            _ctx.DbHandler.AddSolutionReports(solutionReports);
        }

        public static ISolution Create(ContextSolution context, ISolutionParameters parameters)
        {
            return new PageAssetsReport(context, (PageAssetsReportParameters)parameters);
        }

        public async Task RunAsync()
        {
            _ctx.AppClient.IsCancelled();

            await foreach (var siteRecord in new SPOTenantSiteUrlsWithAccessCSOM(_ctx.Logger, _ctx.AppClient, _param.SiteAccParam).GetAsync())
            {
                _ctx.AppClient.IsCancelled();

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
                    _ctx.Logger.Error(GetType().Name, "Site", siteRecord.SiteUrl, ex);
                    PageAssetsReportRecord siteReportRecord = new(siteRecord.SiteUrl, ex.Message);
                    RecordCSV(siteReportRecord);
                }
            }
        }

        private async Task ProcessSite(string siteUrl)
        {
            _ctx.AppClient.IsCancelled();

            var siteAssetsLibrary = await new SPOListCSOM(_ctx.Logger, _ctx.AppClient).GetList(siteUrl, "Site Assets", _listExpressions);

            List<ListItem> collAssets = new();
            await foreach (var sitePageAsset in new SPOListItemCSOM(_ctx.Logger, _ctx.AppClient).GetAsync(siteUrl, siteAssetsLibrary, _assetsParam))
            {
                if (sitePageAsset.FileSystemObjectType == FileSystemObjectType.Folder) { continue; }
                collAssets.Add(sitePageAsset);
            }


            var sitePagesLibrary = await new SPOListCSOM(_ctx.Logger, _ctx.AppClient).GetList(siteUrl, "Site Pages", _listExpressions);

            List<ListItem> collUsedAssets = new();
            await foreach (var page in new SPOListItemCSOM(_ctx.Logger, _ctx.AppClient).GetAsync(siteUrl, sitePagesLibrary, _pagesParam))
            {
                foreach (var asset in collAssets)
                {
                    string pageCanvasContent = (string)page["CanvasContent1"];
                    _ctx.Logger.Debug(GetType().Name, $"Page canvas: {pageCanvasContent}");
                    string assetUrl = (string)asset["FileLeafRef"];
                    _ctx.Logger.Debug(GetType().Name, $"Asset URL: {assetUrl}");
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
            _ctx.Logger.WriteRecord(record);
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
