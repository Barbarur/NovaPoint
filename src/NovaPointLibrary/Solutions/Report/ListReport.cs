using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.Utilities.RESTModel;
using NovaPointLibrary.Core.Context;
using System.Linq.Expressions;


namespace NovaPointLibrary.Solutions.Report
{
    public class ListReport : ISolution
    {
        public static readonly string s_SolutionName = "Libraries and Lists report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-ListReport";

        private ContextSolution _ctx;
        private ListReportParameters _param;

        private static readonly Expression<Func<Microsoft.SharePoint.Client.List, object>>[] _listExpresions = new Expression<Func<Microsoft.SharePoint.Client.List, object>>[]
        {
            l => l.Title,
            l => l.BaseType,
            l => l.Id,

            l => l.Created,
            l => l.LastItemUserModifiedDate,
            l => l.ItemCount,

            l => l.RootFolder,
            l => l.RootFolder.ServerRelativeUrl,
            l => l.RootFolder.StorageMetrics,
            l => l.RootFolder.StorageMetrics.LastModified,
            l => l.RootFolder.StorageMetrics.TotalFileCount,
            l => l.RootFolder.StorageMetrics.TotalFileStreamSize,
            l => l.RootFolder.StorageMetrics.TotalSize,

            l => l.EnableModeration,
            l => l.EnableVersioning,
            l => l.MajorVersionLimit,
            l => l.EnableMinorVersions,
            l => l.MajorWithMinorVersionsLimit,
            l => l.ForceCheckout,

            l => l.IrmEnabled,

            l => l.Hidden,
        };

        private static readonly Expression<Func<Microsoft.SharePoint.Client.List, object>>[] _libraryExpresions = new Expression<Func<Microsoft.SharePoint.Client.List, object>>[]
        {
            l => l.VersionPolicies,
        };

        private ListReport(ContextSolution context, ListReportParameters parameters)
        {
            _ctx = context;

            parameters.ListsParam.ListExpressions = _listExpresions;
            _param = parameters;

            Dictionary<Type, string> solutionReports = new()
            {
                { typeof(ListReportRecord), "Report" },
            };
            _ctx.DbHandler.AddSolutionReports(solutionReports);
        }

        public static ISolution Create(ContextSolution context, ISolutionParameters parameters)
        {
            return new ListReport(context, (ListReportParameters)parameters);
        }

        public async Task RunAsync()
        {
            _ctx.AppClient.IsCancelled();

            await foreach (var listRecord in new SPOTenantListsCSOM(_ctx.Logger, _ctx.AppClient, _param.TListsParam).GetAsync())
            {
                _ctx.AppClient.IsCancelled();

                if (listRecord.Ex != null)
                {
                    AddRecord(new(listRecord.SiteUrl, listRecord.List, listRecord.Ex.Message));
                    continue;
                }

                if (listRecord.List == null)
                {
                    AddRecord(new(listRecord.SiteUrl, listRecord.List, "List is null"));
                    continue;
                }

                try
                {
                    await ProcessList(listRecord.SiteUrl, listRecord.List);
                }
                catch (Exception ex)
                {
                    _ctx.Logger.Error(GetType().Name, "Site", listRecord.SiteUrl, ex);
                    AddRecord(new(listRecord.SiteUrl, listRecord.List, ex.Message));
                }

            }
        }
        
        private async Task ProcessList(string siteUrl, List list)
        {
            _ctx.AppClient.IsCancelled();

            ListReportRecord record = new(siteUrl, list);

            if (list.BaseType == BaseType.DocumentLibrary)
            {
                list.Context.Load(list, _libraryExpresions);
                list.Context.ExecuteQuery();

                record.AddVersionPolicies(list);
            }

            if (_param.IncludeStorageMetrics)
            {
                var storageMetricsResponse = await new SPOFolderCSOM(_ctx.Logger, _ctx.AppClient).GetFolderStorageMetricAsync(siteUrl, list.RootFolder);
                record.AddStorageMetrics(storageMetricsResponse.StorageMetrics);
            }

            AddRecord(record);
        }

        private void AddRecord(ListReportRecord record)
        {
            _ctx.DbHandler.WriteRecord(record);
        }

    }

    internal class ListReportRecord : ISolutionRecord
    {
        public string SiteURL { get; set; }

        public string ListTitle { get; set; } = String.Empty;
        public string ListType { get; set; } = String.Empty;
        public string ListServerRelativeUrl { get; set; } = String.Empty;
        public string ListID { get; set; } = String.Empty;

        public string Created { get; set; } = String.Empty;
        public string LastModified { get; set; } = String.Empty;
        public string TotalFileCount { get; set; } = String.Empty;
        public string TotalSizeGb { get; set; } = String.Empty;

        public string ContentApproval { get; set; } = String.Empty;
        public string EnableVersioning {  get; set; } = String.Empty;
        public string AutomaticExpiration {  get; set; } = "NA";
        public string MajorVersionLimit { get; set; } = String.Empty;
        public string ExpireAfter { get; set; } = "NA";
        public string MinorVersioning { get; set; } = String.Empty;
        public string MinorVersionLimit { get; set; } = String.Empty;
        public string RequireCheckOut { get; set; } = String.Empty;

        public string IRM_Emabled { get; set; } = String.Empty;

        public string Hidden { get; set; } = String.Empty;
        public string IsSystemList { get; set; } = String.Empty;

        public string Remarks { get; set; }

        public ListReportRecord() { }

        internal ListReportRecord(string siteUrl, List? list = null, string remarks = "")
        {
            SiteURL = siteUrl;
            Remarks = remarks;

            if (list != null)
            {
                ListTitle = list.Title;
                ListType = list.BaseType.ToString();
                ListServerRelativeUrl = list.RootFolder.ServerRelativeUrl;
                ListID = list.Id.ToString();

                Created = list.Created.ToString();
                LastModified = list.LastItemUserModifiedDate.ToString();
                TotalFileCount = list.ItemCount.ToString();

                ContentApproval = list.EnableModeration.ToString();
                EnableVersioning = list.EnableVersioning.ToString();
                MajorVersionLimit = list.MajorVersionLimit.ToString();
                MinorVersioning = list.EnableMinorVersions.ToString();
                MinorVersionLimit = list.MajorWithMinorVersionsLimit.ToString();
                RequireCheckOut = list.ForceCheckout.ToString();

                IRM_Emabled = list.IrmEnabled.ToString();

                Hidden = list.Hidden.ToString();
                IsSystemList = list.IsSystemList.ToString();
            }
        }

        internal void AddVersionPolicies(List list)
        {
            if (list.BaseType == BaseType.DocumentLibrary)
            {
                if (list.VersionPolicies.DefaultTrimMode == VersionPolicyTrimMode.AutoExpiration)
                {
                    AutomaticExpiration = "True";
                }
                else if (list.VersionPolicies.DefaultTrimMode == VersionPolicyTrimMode.ExpireAfter)
                {
                    ExpireAfter = list.VersionPolicies.DefaultExpireAfterDays.ToString();
                    AutomaticExpiration = "False";
                }
                else
                {
                    AutomaticExpiration = "False";
                }
            }
        }

        internal void AddStorageMetrics(RESTStorageMetrics storageMetrics)
        {
            TotalSizeGb = Math.Round(storageMetrics.TotalSize / Math.Pow(1024, 3), 2).ToString();
        }

    }

    public class ListReportParameters : ISolutionParameters
    {
        public bool IncludeStorageMetrics { get; set; }
        internal readonly SPOAdminAccessParameters AdminAccess;
        internal readonly SPOTenantSiteUrlsParameters SiteParam;
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam
        {
            get
            {
                return new(AdminAccess, SiteParam);
            }
        }
        internal SPOListsParameters ListsParam { get; set; }
        public SPOTenantListsParameters TListsParam
        {
            get { return new(SiteAccParam, ListsParam); }
        }
        public ListReportParameters(
            bool includeStorageMetrics,
            SPOAdminAccessParameters adminAccess,
            SPOTenantSiteUrlsParameters siteParam,
            SPOListsParameters listsParam)
        {
            IncludeStorageMetrics = includeStorageMetrics;
            AdminAccess = adminAccess;
            SiteParam = siteParam;
            ListsParam = listsParam;
        }
    }
}
