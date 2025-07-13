
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Core.Logging;
using System.Linq.Expressions;
using System.Text;
using System.Xml.Linq;


namespace NovaPointLibrary.Commands.SharePoint.Item
{
    internal class SPOListItemCSOM
    {
        private readonly LoggerSolution _logger;
        private readonly Authentication.AppInfo _appInfo;

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

        internal SPOListItemCSOM(LoggerSolution logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        private async IAsyncEnumerable<ListItemCollection> GetBatchAsync(string siteUrl,
                                                                         Microsoft.SharePoint.Client.List list,
                                                                         SPOItemsParameters parameters)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Start getting Items by batch");

            CamlQuery camlQuery = GetCamlQuery(siteUrl, parameters);

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

            string folderServerRelativeUrl = parameters.GetFolderServerRelativeURL(siteUrl);

            await foreach (var listItemCollection in GetBatchAsync(siteUrl, oList, parameters))
            {
                foreach (var oItem in listItemCollection)
                {
                    if (parameters.MatchParameters(oItem))
                    {
                        yield return oItem;
                    }
                }
            }
        }

        internal CamlQuery GetCamlQuery(string siteUrl, SPOItemsParameters parameters)
        {
            StringBuilder sbQuery = new();
            if (parameters.CreatedAfter > DateTime.MinValue)
            {
                sbQuery.Append($"<Gt><FieldRef Name='Created'/><Value IncludeTimeValue='TRUE' Type='DateTime'>{parameters.CreatedAfter}</Value></Gt>");
            }
            if (parameters.CreatedBefore < DateTime.MaxValue)
            {
                sbQuery.Append($"<Lt><FieldRef Name='Created'/><Value IncludeTimeValue='TRUE' Type='DateTime'>{parameters.CreatedBefore}</Value></Lt>");
            }
            if (parameters.ModifiedAfter > DateTime.MinValue)
            {
                sbQuery.Append($"<Gt><FieldRef Name='Modified'/><Value IncludeTimeValue='TRUE' Type='DateTime'>{parameters.ModifiedAfter}</Value></Gt>");
            }
            if (parameters.ModifiedBefore < DateTime.MaxValue)
            {
                sbQuery.Append($"<Lt><FieldRef Name='Modified'/><Value IncludeTimeValue='TRUE' Type='DateTime'>{parameters.ModifiedBefore}</Value></Lt>");
            }

            string viewXml;
            if (sbQuery.Length > 0)
            {
                viewXml = $"<View Scope='RecursiveAll'><Query><Where>{sbQuery}</Where></Query></View>";
                _logger.Debug(GetType().Name, $"ViewXml = {viewXml}");
            }
            else
            {
                viewXml = "";
            }

            return GetCamlQuery(viewXml, parameters.GetFolderServerRelativeURL(siteUrl));
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
