using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.SiteGroup;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.RESTModel;
using PnP.Framework.Utilities;
using System.Text;


namespace NovaPointLibrary.Solutions.Report
{
    public class SharingLinksReport
    {
        public static readonly string s_SolutionName = "Report Sharing Links";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-ReportSharingLinks";

        private SharingLinksReportParameters _param;
        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private SharingLinksReport(NPLogger logger, Commands.Authentication.AppInfo appInfo, SharingLinksReportParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(SharingLinksReportParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            NPLogger logger = new(uiAddLog, "SharingLinksReport", parameters);
            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new SharingLinksReport(logger, appInfo, parameters).RunScriptAsync();

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

            await foreach (var siteRecord in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param.SiteAccParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (siteRecord.Ex != null)
                {
                    RecordCSV(new(siteRecord));
                    continue;
                }

                try
                {
                    await ProcessSite(siteRecord);
                }
                catch (Exception ex)
                {
                    _logger.ReportError(GetType().Name, "Site", siteRecord.SiteUrl, ex);
                    RecordCSV(new(siteRecord, ex.Message));
                }
            }
        }

        private async Task ProcessSite(SPOTenantSiteUrlsRecord siteRecord)
        {
            _appInfo.IsCancelled();

            Dictionary<string, KnownItemGroups> dKnownSharingInfo = new();

            var collGroups = await new SPOSiteGroupCSOM(_logger, _appInfo).GetSharingLinksAsync(siteRecord.SiteUrl);

            ProgressTracker groupProgress = new(siteRecord.Progress, collGroups.Count);
            foreach (Group oGroup in collGroups)
            {
                _logger.LogTxt(GetType().Name, $"Processing sharing link {oGroup.Title} ({oGroup.Id})");
                SharingLinksReportRecord record = new(siteRecord);

                try
                {
                    record.AddGroup(oGroup);
                    await ProcessGroup(record, dKnownSharingInfo);
                }
                catch (Exception ex)
                {
                    _logger.ReportError(GetType().Name, "SharingLink", record.GroupTitle, ex);
                    record.Remarks = ex.Message;
                    RecordCSV(record);
                }

                groupProgress.ProgressUpdateReport();
            }

        }

        private async Task ProcessGroup(SharingLinksReportRecord reportRecord, Dictionary<string, KnownItemGroups> dKnownSharingInfo)
        {
            _appInfo.IsCancelled();

            dKnownSharingInfo.TryGetValue(reportRecord.ItemUniqueId, out KnownItemGroups? knownGroups);

            RESTSharingInformation restSharingInfo;
            if (knownGroups == null)
            {
                var searchByIdResults = await SearchItemUniqueIdAsync(reportRecord.SiteUrl, reportRecord.ItemUniqueId);
                var idMatchingResult = searchByIdResults.PrimaryQueryResult.RelevantResults.Table.Rows.Find(row => row.Cells.Exists(cell => cell.Key == "UniqueId" && cell.Value.Contains(reportRecord.ItemUniqueId)));
                if (idMatchingResult != null)
                {
                    foreach (var cell in idMatchingResult.Cells)
                    {
                        if (cell.Key == "ListID")
                        {
                            reportRecord.ListId = Guid.Parse(cell.Value);
                        }
                        if (cell.Key == "ListItemID")
                        {
                            reportRecord.ItemID = Int32.Parse(cell.Value);
                        }
                        if (cell.Key == "Path")
                        {
                            reportRecord.ItemPath = cell.Value;
                        }
                    }

                    restSharingInfo = await GetItemSharingInformationAsync(reportRecord.SiteUrl, reportRecord.ListId, reportRecord.ItemID);
                }
                else
                {
                    throw new($"Item with ItemUniqueId '{reportRecord.ItemUniqueId}' not found.");
                }

                dKnownSharingInfo.Add(reportRecord.ItemUniqueId, new(reportRecord.ItemUniqueId, reportRecord.ItemID, reportRecord.ListId, reportRecord.ItemPath, restSharingInfo));
            }
            else
            {
                reportRecord.ListId = knownGroups.ListId;
                reportRecord.ItemID = knownGroups.ItemID;
                reportRecord.ItemPath = knownGroups.ItemPath;

                restSharingInfo = knownGroups.SharingInformation;
            }


            List<Link> collLinks = restSharingInfo.permissionsInformation.links.Where(l => l.linkDetails.ShareId == reportRecord.ShareId).ToList();
            if (!collLinks.Any())
            {
                throw new("Sharing link Id not found on the Item sharing information.");
            }

            foreach (var oLink in collLinks)
            {
                try
                {
                    reportRecord.AddLink(oLink);
                }
                catch (Exception ex)
                {
                    _logger.ReportError(GetType().Name, "SharingLink", reportRecord.GroupTitle, ex);
                    reportRecord.Remarks = ex.Message;
                }
                RecordCSV(reportRecord);
            }

        }

        private async Task<RESTSharingInformation> GetItemSharingInformationAsync(string siteUrl, Guid listId, int listItemId)
        {
            _appInfo.IsCancelled();

            string api = siteUrl + $"/_api/web/Lists('{listId}')/GetItemById('{listItemId}')/GetSharingInformation?$Expand=permissionsInformation,pickerSettings";

            var response = await new RESTAPIHandler(_logger, _appInfo).GetAsync(api);

            var sharingInformation = JsonConvert.DeserializeObject<RESTSharingInformation>(response);

            return sharingInformation;

        }

        private async Task<RESTSearchResults> SearchItemUniqueIdAsync(string siteUrl, string itemUniqueId)
        {
            _appInfo.IsCancelled();

            string api = siteUrl + $"/_api/search/query?querytext='UniqueId:{itemUniqueId}'&selectproperties='ListID,ListItemID,Title,Path'";

            var response = await new RESTAPIHandler(_logger, _appInfo).GetAsync(api);

            var searchResults = JsonConvert.DeserializeObject<RESTSearchResults>(response);

            return searchResults;

        }

        private void RecordCSV(SharingLinksReportRecord record)
        {
            _logger.RecordCSV(record);
        }
    }
    

    public class SharingLinksReportRecord : ISolutionRecord
    {
        internal string SiteUrl { get; set; }

        internal Guid ListId = Guid.Empty;
        internal int ItemID { get; set; } = -1;
        internal string ItemPath { get; set; } = String.Empty;


        internal string SharingLink { get; set; } = String.Empty;
        internal string SharingLinkRequiresPassword { get; set; } = String.Empty;
        internal string SharingLinkExpiration { get; set; } = String.Empty;
        

        internal string SharingLinkIsActive { get; set; } = String.Empty;
        internal string SharingLinkCreated { get; set; } = String.Empty;
        internal string SharingLinkCreatedBy { get; set; } = String.Empty;
        internal string SharingLinkModified {  get; set; } = String.Empty;
        internal string SharingLinkModifiedBy { get; set; } = String.Empty;
        internal string SharingLinkUrl { get; set; } = String.Empty;


        internal string GroupId { get; set; } = String.Empty;
        internal string GroupTitle { get; set; } = String.Empty;
        internal string ItemUniqueId = String.Empty;
        internal string ShareId = String.Empty;
        internal string Users { get; set; } = String.Empty;

        internal string Remarks { get; set; }

        internal SharingLinksReportRecord(SPOTenantSiteUrlsRecord siteRecord, string remarks = "")
        {
            SiteUrl = siteRecord.SiteUrl;
            if (siteRecord.Ex != null) { Remarks = siteRecord.Ex.Message; }
            else { Remarks = remarks; }
        }

        internal void AddGroup(Group oGroup)
        {
            GroupId = oGroup.Id.ToString();
            GroupTitle = oGroup.Title;

            var titleComponents = oGroup.Title.Split(".");
            ItemUniqueId = titleComponents[1];
            ShareId = titleComponents[3];

            StringBuilder sbUsers = new();
            foreach (var user in oGroup.Users)
            {
                sbUsers.Append($"{user.Email} ");
            }
            Users = sbUsers.ToString();

            int i = oGroup.Description.IndexOf("'") + 1;
            int l = oGroup.Description.Length - i - 1;
            ItemPath = UrlUtility.Combine(SiteUrl, oGroup.Description.Substring(i, l));
        }

        internal void AddLink(Link oLink)
        {
            if (oLink.linkDetails.AllowsAnonymousAccess)
            {
                SharingLink = "Anyone with the link";
                Users = "Anyone with the link";
            }
            else if (!oLink.linkDetails.RestrictedShareMembership)
            {
                SharingLink = "People in your organization with the link";
                Users = "People in your organization with the link";
            }
            else
            {
                SharingLink = "Specific People with the link";
            }

            if (oLink.linkDetails.IsEditLink)
            {
                SharingLink += " can edit";
            }
            else if (oLink.linkDetails.IsReviewLink)
            {
                SharingLink += " can review";
            }
            else if (oLink.linkDetails.BlocksDownload)
            {
                SharingLink += " can view but can't download";
            }
            else
            {
                SharingLink += " can view";
            }

            SharingLinkRequiresPassword = oLink.linkDetails.RequiresPassword.ToString();
            SharingLinkExpiration = oLink.linkDetails.Expiration.ToString();

            SharingLinkIsActive = oLink.linkDetails.IsActive.ToString();

            SharingLinkCreated = oLink.linkDetails.Created;
            SharingLinkCreatedBy = oLink.linkDetails.CreatedBy.email;
            SharingLinkModified = oLink.linkDetails.LastModified;
            SharingLinkModifiedBy = oLink.linkDetails.LastModifiedBy.email;
            SharingLinkUrl = oLink.linkDetails.Url;
        }

    }

    public class SharingLinksReportParameters : ISolutionParameters
    {
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam { get; set; }
        public SharingLinksReportParameters(SPOTenantSiteUrlsWithAccessParameters siteParam)
        {
            SiteAccParam = siteParam;
        }
    }

    internal class KnownItemGroups
    {
        internal string ItemUniqueId;
        internal Guid ListId;
        internal int ItemID;
        internal string ItemPath;
        internal RESTSharingInformation SharingInformation;

        internal KnownItemGroups(string itemUniqueId, int itemID, Guid listId, string itemPath, RESTSharingInformation rest)
        {
            ItemUniqueId = itemUniqueId;
            ItemID = itemID;
            ListId = listId;
            ItemPath = itemPath;
            SharingInformation = rest;
        }

    }


    internal class RESTSearchResults
    {
        [JsonProperty("odata.metadata")]
        public string odatametadata { get; set; }
        public int ElapsedTime { get; set; }
        public PrimaryQueryResult PrimaryQueryResult { get; set; }
        public List<Property> Properties { get; set; }
        public List<object> SecondaryQueryResults { get; set; }
        public object SpellingSuggestion { get; set; }
        public List<object> TriggeredRules { get; set; }
    }
    internal class PrimaryQueryResult
    {
        public List<object> CustomResults { get; set; }
        public string QueryId { get; set; }
        public string QueryRuleId { get; set; }
        public object RefinementResults { get; set; }
        public RelevantResults RelevantResults { get; set; }
        public object SpecialTermResults { get; set; }
    }
    internal class RelevantResults
    {
        public object GroupTemplateId { get; set; }
        public object ItemTemplateId { get; set; }
        public List<Property> Properties { get; set; }
        public object ResultTitle { get; set; }
        public object ResultTitleUrl { get; set; }
        public int RowCount { get; set; }
        public Table Table { get; set; }
        public int TotalRows { get; set; }
        public int TotalRowsIncludingDuplicates { get; set; }
    }
    internal class Property
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string ValueType { get; set; }
    }

    internal class Table
    {
        public List<Row> Rows { get; set; }
    }
    internal class Row
    {
        public List<Cell> Cells { get; set; }
    }

    internal class Cell
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string ValueType { get; set; }
    }

}
