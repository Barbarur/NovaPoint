using Newtonsoft.Json;
using NovaPointLibrary.Commands.Utilities.RESTModel;
using NovaPointLibrary.Commands.Utilities;
using System.Text;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using PnP.Framework.Utilities;
using NovaPointLibrary.Commands.SharePoint.SiteGroup;
using NovaPointLibrary.Core.Logging;

namespace NovaPointLibrary.Commands.SharePoint.Permision
{
    internal class SPOSharingLinksREST
    {
        private readonly LoggerSolution _logger;
        private readonly Authentication.AppInfo _appInfo;
        private readonly Dictionary<string, KnownItemGroups> _dKnownSharingInfo = new();

        internal SPOSharingLinksREST(LoggerSolution logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<SPOSharingLinksRecord> GetFromPrincipalAsync(string siteUrl, Microsoft.SharePoint.Client.Principal principal)
        {
            _appInfo.IsCancelled();

            _logger.Info(GetType().Name, $"Processing sharing link {principal.Title} ({principal.Id})");

            SPOSharingLinksRecord record = new(siteUrl);

            try
            {
                Group oGroup = await new SPOSiteGroupCSOM(_logger, _appInfo).GetAsync(siteUrl, principal.Id);
                record.AddGroup(oGroup);
                await GetSharingLinkInfoAsync(record);
            }
            catch (Exception ex)
            {
                _logger.Error(GetType().Name, "SharingLink", record.GroupTitle, ex);
                record.Remarks = ex.Message;
            }
            return record;
        }

        internal async Task<SPOSharingLinksRecord> GetFromGroupAsync(string siteUrl, Group oGroup)
        {
            _appInfo.IsCancelled();

            _logger.Info(GetType().Name, $"Processing sharing link {oGroup.Title} ({oGroup.Id}) - {oGroup.Description}");

            SPOSharingLinksRecord record = new(siteUrl);

            try
            {
                record.AddGroup(oGroup);
                await GetSharingLinkInfoAsync(record);
            }
            catch (Exception ex)
            {
                _logger.Error(GetType().Name, "SharingLink", record.GroupTitle, ex);
                record.Remarks = ex.Message;
            }
            return record;
        }

        private async Task GetSharingLinkInfoAsync(SPOSharingLinksRecord reportRecord)
        {
            _appInfo.IsCancelled();

            _dKnownSharingInfo.TryGetValue(reportRecord.ItemUniqueId, out KnownItemGroups? knownGroups);

            RESTSharingInformation restSharingInfo;
            if (knownGroups == null)
            {
                var searchByIdResults = await SearchItemUniqueIdAsync(reportRecord.SiteUrl, reportRecord.ItemUniqueId);
                var idMatchingResult = searchByIdResults.PrimaryQueryResult.RelevantResults.Table.Rows.Find(row => row.Cells.Exists(cell => cell.Key == "UniqueId" && cell.Value.Contains(reportRecord.ItemUniqueId)));
                if (idMatchingResult != null)
                {
                    string webId = string.Empty;
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

                _dKnownSharingInfo.Add(reportRecord.ItemUniqueId, new(reportRecord.ItemUniqueId, reportRecord.ItemID, reportRecord.ListId, reportRecord.ItemPath, restSharingInfo));
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

        public class SPOSharingLinksRecord : ISolutionRecord
        {
            internal string SiteTitle { get; set; } = String.Empty;
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
            internal string SharingLinkModified { get; set; } = String.Empty;
            internal string SharingLinkModifiedBy { get; set; } = String.Empty;
            internal string SharingLinkUrl { get; set; } = String.Empty;


            internal string GroupId { get; set; } = String.Empty;
            internal string GroupTitle { get; set; } = String.Empty;
            internal string ItemUniqueId = String.Empty;
            internal string ShareId = String.Empty;
            internal string Users { get; set; } = String.Empty;

            internal string Remarks { get; set; } = String.Empty;

            internal SPOSharingLinksRecord(string siteUrl)
            {
                SiteUrl = siteUrl;
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
}
