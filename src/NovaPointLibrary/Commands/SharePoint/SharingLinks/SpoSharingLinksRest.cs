using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using NovaPointLibrary.Commands.SharePoint.SiteGroup;
using NovaPointLibrary.Commands.Utilities.RESTModel;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Core.Logging;


namespace NovaPointLibrary.Commands.SharePoint.SharingLinks
{
    internal class SpoSharingLinksRest
    {
        private readonly LoggerSolution _logger;
        private readonly Authentication.AppInfo _appInfo;
        private readonly Dictionary<string, KnownItemGroups> _dKnownSharingInfo = new();

        internal SpoSharingLinksRest(LoggerSolution logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<SpoSharingLinksRecord> GetFromPrincipalAsync(string siteUrl, Microsoft.SharePoint.Client.Principal principal)
        {
            SpoSharingLinksRecord record;
            try
            {
                Group oGroup = await new SPOSiteGroupCSOM(_logger, _appInfo).GetAsync(siteUrl, principal.Id);
                record = await GetFromGroupAsync(siteUrl, oGroup);
            }
            catch (Exception ex)
            {
                _logger.Error(GetType().Name, "SharingLink", principal.Title, ex);
                record = new(siteUrl, ex);
            }
            return record;
        }

        internal async Task<SpoSharingLinksRecord> GetFromGroupAsync(string siteUrl, Group oGroup)
        {
            SpoSharingLinksRecord record;
            try
            {
                record = new(siteUrl, oGroup);
                await GetSharingLinkInfoAsync(record);
            }
            catch (Exception ex)
            {
                _logger.Error(GetType().Name, "SharingLink", oGroup.Title, ex);
                record = new(siteUrl, ex);
            }
            return record;
        }

        private async Task GetSharingLinkInfoAsync(SpoSharingLinksRecord reportRecord)
        {
            _appInfo.IsCancelled();

            _logger.Info(GetType().Name, $"Processing sharing link {reportRecord.GroupTitle} '{reportRecord.GroupId}' - {reportRecord.GroupDescription}");

            _dKnownSharingInfo.TryGetValue(reportRecord.ItemUniqueId, out KnownItemGroups? knownGroups);

            RESTSharingInformation restSharingInfo;
            if (knownGroups == null)
            {
                var searchByIdResults = await SearchItemUniqueIdAsync(reportRecord.SiteUrl, reportRecord.ItemUniqueId);
                var itemMatchingIdResult = searchByIdResults.PrimaryQueryResult.RelevantResults.Table.Rows.Find(row => row.Cells.Exists(cell => cell.Key == "UniqueId" && cell.Value.Contains(reportRecord.ItemUniqueId)));
                if (itemMatchingIdResult != null)
                {
                    string webId = string.Empty;
                    foreach (var cell in itemMatchingIdResult.Cells)
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
                        if (cell.Key == "WebId")
                        {
                            webId = cell.Value;
                        }
                    }

                    var ctx = await _appInfo.GetContext(reportRecord.SiteUrl);
                    Web web = ctx.Site.OpenWebById(new Guid(webId));
                    ctx.Load(web);
                    ctx.ExecuteQuery();

                    reportRecord.SiteTitle = web.Title;
                    reportRecord.SiteUrl = web.Url;

                    restSharingInfo = await GetItemSharingInformationAsync(reportRecord.SiteUrl, reportRecord.ListId, reportRecord.ItemID);
                }
                else
                {
                    throw new($"Item with ItemUniqueId '{reportRecord.ItemUniqueId}' not found.");
                }

                _dKnownSharingInfo.Add(reportRecord.ItemUniqueId, new(reportRecord, restSharingInfo));
            }
            else
            {
                reportRecord.SiteTitle = knownGroups.SiteTitle;
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

            // Change to get only the first one, as it is expected to get only one result
            foreach (var oLink in collLinks)
            {
                try
                {
                    reportRecord.AddLink(oLink);
                }
                catch (Exception ex)
                {
                    _logger.Error(GetType().Name, "SharingLink", reportRecord.GroupTitle, ex);
                    reportRecord.Remarks = ex.Message;
                }
            }

        }

        private async Task<RESTSearchResults> SearchItemUniqueIdAsync(string siteUrl, string itemUniqueId)
        {
            _appInfo.IsCancelled();

            string api = siteUrl + $"/_api/search/query?querytext='UniqueId:{itemUniqueId}'&selectproperties='ListID,ListItemID,Title,Path,WebId'";

            var response = await new RESTAPIHandler(_logger, _appInfo).GetAsync(api);

            var searchResults = JsonConvert.DeserializeObject<RESTSearchResults>(response);

            return searchResults;

        }

        private async Task<RESTSharingInformation> GetItemSharingInformationAsync(string siteUrl, Guid listId, int listItemId)
        {
            _appInfo.IsCancelled();

            string api = siteUrl + $"/_api/web/Lists('{listId}')/GetItemById('{listItemId}')/GetSharingInformation?$Expand=permissionsInformation,pickerSettings";

            var response = await new RESTAPIHandler(_logger, _appInfo).GetAsync(api);

            var sharingInformation = JsonConvert.DeserializeObject<RESTSharingInformation>(response);

            return sharingInformation;

        }



        internal class KnownItemGroups
        {
            internal string SiteTitle;
            internal Guid ListId;
            internal int ItemID;
            internal string ItemPath;
            internal RESTSharingInformation SharingInformation;

            internal KnownItemGroups(SpoSharingLinksRecord reportRecord, RESTSharingInformation rest)
            {
                SiteTitle = reportRecord.SiteTitle;
                ListId = reportRecord.ListId;
                ItemID = reportRecord.ItemID;
                ItemPath = reportRecord.ItemPath;
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
}
