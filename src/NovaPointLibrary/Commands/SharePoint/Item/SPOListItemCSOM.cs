
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Logging;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;


namespace NovaPointLibrary.Commands.SharePoint.Item
{
    internal class SPOListItemCSOM
    {
        private readonly ILogger _logger;
        private readonly IAppClient _appInfo;

        private readonly Expression<Func<ListItem, object>>[] _defaultExpressions = new Expression<Func<ListItem, object>>[]
        {
            i => i.Id,
            i => i["FileRef"],
            i => i["FileLeafRef"],
            i => i.FileSystemObjectType,
            i => i.ParentList.Title,
            i => i.ParentList.BaseType,
            i => i.ParentList.RootFolder.ServerRelativeUrl,
            i => i.ParentList.ParentWeb.Url,
            i => i.ParentList.Id,
        };

        internal SPOListItemCSOM(ILogger logger, IAppClient appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        // Above this, resolving a wildcard folder path one matched folder at a time
        // (one query per folder) costs more round-trips than collecting every item of
        // the list once and filtering them by the pattern instead.
        private const int MaxWildcardMatchedFolders = 500;

        private async IAsyncEnumerable<ListItemCollection> GetBatchAsync(string siteUrl,
                                                                         Microsoft.SharePoint.Client.List list,
                                                                         SPOItemsParameters parameters,
                                                                         string? folderServerRelativeUrl)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Start getting Items by batch");

            CamlQuery camlQuery = GetCamlQuery(parameters, folderServerRelativeUrl);

            Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] expressions;
            if (list.BaseType == BaseType.DocumentLibrary)
            {
                expressions = _defaultExpressions.Union(parameters.FileExpressions).ToArray();
            }
            else if (list.BaseType == BaseType.GenericList)
            {
                expressions = _defaultExpressions.Union(parameters.ItemExpressions).ToArray();
            }
            else
            {
                throw new Exception("This is not an Item List neither a Document Library");
            }

            int counter = 0;
            ClientContext clientContext;
            Microsoft.SharePoint.Client.List oList;
            _logger.Info(GetType().Name, $"Start Loop");
            bool shouldContinue = false;
            bool firstTry = true;
            do
            {
                _appInfo.IsCancelled();

                clientContext = await _appInfo.GetContext(siteUrl);
                oList = clientContext.Web.Lists.GetById(list.Id);
                ListItemCollection subcollListItem = oList.GetItems(camlQuery);

                Exception? exception = null;
                try
                {
                    clientContext.Load(subcollListItem,
                        sci => sci.ListItemCollectionPosition,
                        sci => sci.Include(expressions));
                    clientContext.ExecuteQueryRetry();
                }
                catch (Exception ex) { exception = ex; }

                if (exception != null)
                {
                    if (parameters.AllItems == false && firstTry)
                    {
                        LongListNotification(list);
                        shouldContinue = true;
                        firstTry = false;
                        camlQuery = GetCamlQuery("", "");
                    }
                    else
                    {
                        throw exception;
                    }
                }
                else
                {
                    counter += subcollListItem.Count;
                    if (counter >= 5000) { _logger.UI(GetType().Name, $"Collected from '{list.Title}' {counter} items..."); }
                    else { _logger.Info(GetType().Name, $"Collected from '{list.Title}' {counter} items."); }

                    yield return subcollListItem;

                    if (subcollListItem.ListItemCollectionPosition != null)
                    {
                        camlQuery.ListItemCollectionPosition = subcollListItem.ListItemCollectionPosition;
                        shouldContinue = true;
                    }
                    else
                    {
                        shouldContinue = false;
                    }
                }

            }
            while (shouldContinue);

        }

        internal async IAsyncEnumerable<ListItem> GetAsync(string siteUrl,
                                                           Microsoft.SharePoint.Client.List oList,
                                                           SPOItemsParameters parameters)
        {
            _logger.Info(GetType().Name, $"Getting items from site '{siteUrl}' list '{oList.Title}'");

            if (string.IsNullOrWhiteSpace(parameters.FolderRelativeUrl))
            {
                await foreach (var oItem in GetFromFolderAsync(siteUrl, oList, parameters, null))
                {
                    yield return oItem;
                }
                yield break;
            }

            if (!parameters.FolderPathHasWildcard)
            {
                string requestedFolderUrl = parameters.GetFolderServerRelativeURL(oList);
                _logger.Info(GetType().Name, $"Folder '{parameters.FolderRelativeUrl}' resolved to '{requestedFolderUrl}'");

                var oFolder = await new SPOFolderCSOM(_logger, _appInfo).GetFolderAsync(siteUrl, requestedFolderUrl);

                if (oFolder == null || !oFolder.Exists)
                {
                    _logger.UI(GetType().Name, $"Folder '{requestedFolderUrl}' was not found on '{oList.BaseType}' '{oList.Title}' and no items will be collected. Check the folder path is written as it appears on the address bar and not with the display names.");

                    yield break;
                }

                await foreach (var oItem in GetFromFolderAsync(siteUrl, oList, parameters, oFolder.ServerRelativeUrl))
                {
                    yield return oItem;
                }
                yield break;
            }

            List<string>? matchedFolders = await ResolveWildcardFoldersAsync(siteUrl, oList, parameters.GetFolderPathSegments());

            if (matchedFolders == null)
            {
                _logger.UI(GetType().Name, $"Folder path '{parameters.FolderRelativeUrl}' matched more than {MaxWildcardMatchedFolders} folders on '{oList.BaseType}' '{oList.Title}'. Collecting all the items from the '{oList.BaseType}' and filtering them by the folder path instead, which will take longer.");

                string listRootUrl = oList.RootFolder.ServerRelativeUrl;
                await foreach (var oItem in GetFromFolderAsync(siteUrl, oList, parameters, null))
                {
                    if (parameters.MatchParametersWithFolderPattern(oItem, listRootUrl))
                    {
                        yield return oItem;
                    }
                }
                yield break;
            }

            if (matchedFolders.Count == 0)
            {
                _logger.UI(GetType().Name, $"Folder path '{parameters.FolderRelativeUrl}' did not match any folder on '{oList.BaseType}' '{oList.Title}' and no items will be collected. Check the folder path is written correct.");

                yield break;
            }

            _logger.UI(GetType().Name, $"Folder path '{parameters.FolderRelativeUrl}' matched {matchedFolders.Count} folder(s) on '{oList.BaseType}' '{oList.Title}': {string.Join(", ", matchedFolders)}");

            foreach (string matchedFolder in matchedFolders)
            {
                await foreach (var oItem in GetFromFolderAsync(siteUrl, oList, parameters, matchedFolder))
                {
                    yield return oItem;
                }
            }
        }

        private async IAsyncEnumerable<ListItem> GetFromFolderAsync(string siteUrl,
                                                                     Microsoft.SharePoint.Client.List oList,
                                                                     SPOItemsParameters parameters,
                                                                     string? folderServerRelativeUrl)
        {
            await foreach (var listItemCollection in GetBatchAsync(siteUrl, oList, parameters, folderServerRelativeUrl))
            {
                foreach (var oItem in listItemCollection)
                {
                    if (parameters.MatchParameters(oItem, folderServerRelativeUrl))
                    {
                        yield return oItem;
                    }
                }
            }
        }

        // Expands a folder path containing '*' segments into the concrete matching
        // folders, one level at a time (subfolder listing is the only way to test a
        // wildcard segment). Returns null when more than MaxWildcardMatchedFolders.
        private async Task<List<string>?> ResolveWildcardFoldersAsync(string siteUrl,
                                                                       Microsoft.SharePoint.Client.List oList,
                                                                       string[] patternSegments)
        {
            var folderCommand = new SPOFolderCSOM(_logger, _appInfo);

            List<string> candidates = new() { oList.RootFolder.ServerRelativeUrl };

            foreach (string segment in patternSegments)
            {
                List<string> nextCandidates = new();

                if (segment.Contains('*'))
                {
                    // The exact child name isn't known, so every sibling has to be listed
                    // and tested against the segment pattern.
                    Regex segmentRegex = SPOItemsParameters.SegmentPatternToRegex(segment);

                    foreach (string candidate in candidates)
                    {
                        var subFolders = await folderCommand.GetSubFoldersAsync(siteUrl, candidate);
                        nextCandidates.AddRange(subFolders
                            .Where(f => f.Exists && segmentRegex.IsMatch(f.Name))
                            .Select(f => f.ServerRelativeUrl));
                    }
                }
                else
                {
                    // The child name is already known, so look it up directly instead of
                    // listing every sibling just to find the one with a matching name.
                    foreach (string candidate in candidates)
                    {
                        string candidatePath = candidate.TrimEnd('/') + "/" + segment;
                        var oFolder = await folderCommand.GetFolderAsync(siteUrl, candidatePath);
                        if (oFolder != null && oFolder.Exists)
                        {
                            nextCandidates.Add(oFolder.ServerRelativeUrl);
                        }
                    }
                }

                candidates = nextCandidates;

                if (candidates.Count > MaxWildcardMatchedFolders)
                {
                    return null;
                }
            }

            return candidates;
        }

        internal CamlQuery GetCamlQuery(SPOItemsParameters parameters, string? folderServerRelativeUrl)
        {
            List<string> conditions = [];
            if (parameters.CreatedAfter > DateTime.MinValue)
            {
                conditions.Add(GetDateCondition("Gt", "Created", parameters.CreatedAfter));
            }
            if (parameters.CreatedBefore < DateTime.MaxValue)
            {
                conditions.Add(GetDateCondition("Lt", "Created", parameters.CreatedBefore));
            }
            if (parameters.ModifiedAfter > DateTime.MinValue)
            {
                conditions.Add(GetDateCondition("Gt", "Modified", parameters.ModifiedAfter));
            }
            if (parameters.ModifiedBefore < DateTime.MaxValue)
            {
                conditions.Add(GetDateCondition("Lt", "Modified", parameters.ModifiedBefore));
            }

            string viewXml;
            if (conditions.Count > 0)
            {
                viewXml = $"<View Scope='RecursiveAll'><Query><Where>{CombineWithAnd(conditions)}</Where></Query></View>";
                _logger.Debug(GetType().Name, $"ViewXml = {viewXml}");
            }
            else
            {
                viewXml = "";
            }

            return GetCamlQuery(viewXml, folderServerRelativeUrl ?? string.Empty);
        }

        // Filter dates are UTC (as the wiki documents); StorageTZ stops SharePoint reading them in the site's time zone.
        private static string GetDateCondition(string comparison, string fieldName, DateTime value)
        {
            string isoValue = value.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
            return $"<{comparison}><FieldRef Name='{fieldName}'/><Value IncludeTimeValue='TRUE' StorageTZ='TRUE' Type='DateTime'>{isoValue}</Value></{comparison}>";
        }

        // CAML's <And> takes exactly two conditions, so three or more must be nested.
        private static string CombineWithAnd(IReadOnlyList<string> conditions)
        {
            return conditions.Count == 1
                ? conditions[0]
                : $"<And>{conditions[0]}{CombineWithAnd(conditions.Skip(1).ToList())}</And>";
        }

        internal CamlQuery GetCamlQuery(string viewXml, string folderServerRelativeUrl)
        {
            _logger.Debug(GetType().Name, $"Getting CAML Query: ViewXml {viewXml}, FolderServerRelativeUrl {folderServerRelativeUrl}");

            CamlQuery camlQuery = string.IsNullOrWhiteSpace(viewXml) ? CamlQuery.CreateAllItemsQuery() : new CamlQuery { ViewXml = viewXml };

            if (!string.IsNullOrWhiteSpace(folderServerRelativeUrl))
            {
                _logger.Debug(GetType().Name, $"Folder ServerRelativeUrl {folderServerRelativeUrl}");
                camlQuery.FolderServerRelativeUrl = folderServerRelativeUrl;
            }
            
            var queryElement = XElement.Parse(camlQuery.ViewXml);
            var rowLimit = queryElement.Descendants("RowLimit").FirstOrDefault();
            if (rowLimit != null)
            {
                rowLimit.RemoveAll();
            }
            else
            {
                rowLimit = new XElement("RowLimit");
                queryElement.Add(rowLimit);
            }

            rowLimit.SetAttributeValue("Paged", "TRUE");
            rowLimit.SetValue(5000);

            camlQuery.ViewXml = queryElement.ToString();

            return camlQuery;
        }


        internal void LongListNotification(Microsoft.SharePoint.Client.List oList)
        {
            if (oList.ItemCount > 5000)
            {
                _logger.UI(GetType().Name, $"'{oList.BaseType}' '{oList.Title}' is a large list with {oList.ItemCount} items. Expect the Solution to take longer to run.");
            }
            else
            {
                _logger.Info(GetType().Name, $"'{oList.BaseType}' '{oList.Title}' has {oList.ItemCount} items.");
            }
        }

        internal async Task<ListItem> GetBySiteRelativePath(string siteUrl, string itemSiteRelativeUrl)
        {
            ClientContext clientContext = await _appInfo.GetContext(siteUrl);

            ListItem oListItem = clientContext.Web.GetListItemUsingPath(ResourcePath.FromDecodedUrl(itemSiteRelativeUrl));

            clientContext.Load(oListItem, _defaultExpressions);
            clientContext.ExecuteQueryRetry();

            return oListItem;
        }

        internal async Task<Microsoft.SharePoint.Client.File> GetAttachmentFileAsync(string siteUrl, string attachmentServerRelativeUrl)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Getting attachment file '{attachmentServerRelativeUrl}'");

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);
            var file = clientContext.Web.GetFileByServerRelativeUrl(attachmentServerRelativeUrl);
            clientContext.Load(file);
            clientContext.ExecuteQuery();

            return file;
        }

        internal async Task RemoveAsync(string siteUrl, Microsoft.SharePoint.Client.List oList, ListItem oItem, bool recycle)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Removing ListItem '{oItem["FileLeafRef"]}'");

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);
            Microsoft.SharePoint.Client.List list = clientContext.Web.Lists.GetById(oList.Id);
            ListItem item = list.GetItemById(oItem.Id);

            if (recycle)
            {
                item.Recycle();
            }
            else
            {
                item.DeleteObject();
            }
            clientContext.ExecuteQuery();
        }

    }
}
