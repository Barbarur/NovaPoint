using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using System.Linq.Expressions;
using System.Xml.Linq;


namespace NovaPointLibrary.Commands.SharePoint.Item
{
    internal class SPOListItemCSOM
    {
        //private readonly Main _main;
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;

        //internal SPOListItemCSOM(Main main)
        //{
        //    _main = main;
        //}

        internal SPOListItemCSOM(NPLogger logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        //private async IAsyncEnumerable<ListItemCollection> GetBatchDEPRECATED(string siteUrl,
        //                                                            string listTitle,
        //                                                            Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] retrievalExpressions)
        //{
        //    _main.IsCancelled();
        //    string methodName = $"{GetType().Name}.GetBatch";
        //    _main.AddLogToTxt(methodName, $"Start getting Items by batch");

        //    CamlQuery camlQuery = CamlQuery.CreateAllItemsQuery();

        //    var queryElement = XElement.Parse(camlQuery.ViewXml);

        //    var rowLimit = queryElement.Descendants("RowLimit").FirstOrDefault();
        //    if (rowLimit != null)
        //    {
        //        rowLimit.RemoveAll();
        //    }
        //    else
        //    {
        //        rowLimit = new XElement("RowLimit");
        //        queryElement.Add(rowLimit);
        //    }

        //    rowLimit.SetAttributeValue("Paged", "TRUE");
        //    rowLimit.SetValue(5000);

        //    camlQuery.ViewXml = queryElement.ToString();

        //    ClientContext clientContext = await _main.GetContext(siteUrl);

        //    Microsoft.SharePoint.Client.List oList = clientContext.Web.Lists.GetByTitle(listTitle);

        //    var defaultExpressions = new Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[]
        //    {
        //        i => i.ParentList.Title,
        //        i => i.ParentList.ParentWeb.Url,

        //    };

        //    var expressions = retrievalExpressions.Union(defaultExpressions).ToArray();

        //    _main.AddLogToTxt(methodName, $"Start Loop");
        //    int counter = 0;
        //    do
        //    {
        //        _main.IsCancelled();

        //        ListItemCollection subcollListItem = oList.GetItems(camlQuery);

        //        clientContext.Load(subcollListItem,
        //            sci => sci.ListItemCollectionPosition,
        //            sci => sci.Include(expressions));

        //        clientContext.ExecuteQueryRetry();

        //        counter += subcollListItem.Count;
        //        _main.AddLogToUI(methodName, $"Collected {counter} items...");
        //        camlQuery.ListItemCollectionPosition = subcollListItem.ListItemCollectionPosition;

        //        yield return subcollListItem;

        //        clientContext = await _main.GetContext(siteUrl);
        //        oList = clientContext.Web.Lists.GetByTitle(listTitle);

        //    }
        //    while (camlQuery.ListItemCollectionPosition != null);

        //    _main.AddLogToTxt(methodName, $"Finish getting Items by batch");
        //}

        //internal async IAsyncEnumerable<Microsoft.SharePoint.Client.ListItem> GetDEPRECATED(string siteUrl, string listTitle, Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] retrievalExpressions)
        //{
        //    await foreach (var listItemCollection in GetBatchDEPRECATED(siteUrl, listTitle, retrievalExpressions))
        //    {
        //        foreach (var oItem in listItemCollection)
        //        {
        //            yield return oItem;
        //        }
        //    }
        //}

        //internal async Task<Microsoft.SharePoint.Client.File> GetAttachmentFileDEPRECATED(string siteUrl, string attachmentServerRelativeUrl)
        //{
        //    _main.IsCancelled();
        //    string methodName = $"{GetType().Name}.GetAttachmentFile";
        //    _main.AddLogToTxt(methodName, $"Start getting attachment file '{attachmentServerRelativeUrl}'");

        //    ClientContext clientContext = await _main.GetContext(siteUrl);
        //    var file = clientContext.Web.GetFileByServerRelativeUrl(attachmentServerRelativeUrl);
        //    clientContext.Load(file);
        //    clientContext.ExecuteQuery();

        //    _main.AddLogToTxt(methodName, $"Finish getting attachment file '{attachmentServerRelativeUrl}'");
        //    return file;
        //}



        //private async IAsyncEnumerable<ListItemCollection> GetBatchAsync(string siteUrl,
        //                                                    string listTitle,
        //                                                    Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] retrievalExpressions)
        //{
        //    _appInfo.IsCancelled();
        //    string methodName = $"{GetType().Name}.GetBatch";
        //    _logger.LogTxt(methodName, $"Start getting Items by batch");

        //    CamlQuery camlQuery = CamlQuery.CreateAllItemsQuery();

        //    var queryElement = XElement.Parse(camlQuery.ViewXml);

        //    var rowLimit = queryElement.Descendants("RowLimit").FirstOrDefault();
        //    if (rowLimit != null)
        //    {
        //        rowLimit.RemoveAll();
        //    }
        //    else
        //    {
        //        rowLimit = new XElement("RowLimit");
        //        queryElement.Add(rowLimit);
        //    }

        //    rowLimit.SetAttributeValue("Paged", "TRUE");
        //    rowLimit.SetValue(5000);

        //    camlQuery.ViewXml = queryElement.ToString();

        //    ClientContext clientContext = await _appInfo.GetContext(siteUrl);

        //    Microsoft.SharePoint.Client.List oList = clientContext.Web.Lists.GetByTitle(listTitle);

        //    var defaultExpressions = new Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[]
        //    {
        //        i => i.ParentList.Title,
        //        i => i.ParentList.ParentWeb.Url,

        //    };

        //    var expressions = retrievalExpressions.Union(defaultExpressions).ToArray();

        //    _logger.LogTxt(methodName, $"Start Loop");
        //    int counter = 0;
        //    do
        //    {
        //        _appInfo.IsCancelled();

        //        ListItemCollection subcollListItem = oList.GetItems(camlQuery);

        //        clientContext.Load(subcollListItem,
        //            sci => sci.ListItemCollectionPosition,
        //            sci => sci.Include(expressions));

        //        clientContext.ExecuteQueryRetry();

        //        counter += subcollListItem.Count;
        //        _logger.LogUI(methodName, $"Collected {counter} items...");
        //        camlQuery.ListItemCollectionPosition = subcollListItem.ListItemCollectionPosition;

        //        yield return subcollListItem;

        //        clientContext = await _appInfo.GetContext(siteUrl);
        //        oList = clientContext.Web.Lists.GetByTitle(listTitle);

        //    }
        //    while (camlQuery.ListItemCollectionPosition != null);

        //    _logger.LogTxt(methodName, $"Finish getting Items by batch");
        //}

        //internal async IAsyncEnumerable<Microsoft.SharePoint.Client.ListItem> GetAsync(string siteUrl, string listTitle, Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] retrievalExpressions)
        //{
        //    await foreach (var listItemCollection in GetBatchAsync(siteUrl, listTitle, retrievalExpressions))
        //    {
        //        foreach (var oItem in listItemCollection)
        //        {
        //            yield return oItem;
        //        }
        //    }
        //}

        private async IAsyncEnumerable<ListItemCollection> GetBatchAsync(string siteUrl,
                                                                         Microsoft.SharePoint.Client.List TargetList,
                                                                         SPOTenantItemsParameters parameters)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Start getting Items by batch");

            CamlQuery camlQuery = CamlQuery.CreateAllItemsQuery();

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

            Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] requestedExpressions;
            if (TargetList.BaseType == BaseType.DocumentLibrary)
            {
                requestedExpressions = parameters.FileExpresions;
            }
            else if (TargetList.BaseType == BaseType.GenericList)
            {
                requestedExpressions = parameters.ItemExpresions;
            }
            else
            {
                throw new Exception("This is not a List neither a Library");
            }

            var defaultExpressions = new Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[]
            {
                i => i.ParentList.Title,
                i => i.ParentList.ParentWeb.Url,
            };

            var expressions = requestedExpressions.Union(defaultExpressions).ToArray();

            int counter = 0;
            ClientContext clientContext;
            Microsoft.SharePoint.Client.List oList;
            _logger.LogTxt(GetType().Name, $"Start Loop");
            do
            {
                _appInfo.IsCancelled();

                clientContext = await _appInfo.GetContext(siteUrl);
                oList = clientContext.Web.Lists.GetById(TargetList.Id);
                ListItemCollection subcollListItem = oList.GetItems(camlQuery);

                clientContext.Load(subcollListItem,
                    sci => sci.ListItemCollectionPosition,
                    sci => sci.Include(expressions));

                clientContext.ExecuteQueryRetry();

                counter += subcollListItem.Count;
                _logger.LogUI(GetType().Name, $"Collected from '{TargetList.Title}' {counter} items...");
                camlQuery.ListItemCollectionPosition = subcollListItem.ListItemCollectionPosition;

                yield return subcollListItem;
            }
            while (camlQuery.ListItemCollectionPosition != null);

        }

        internal async IAsyncEnumerable<ListItem> GetAsync(string siteUrl,
                                                           Microsoft.SharePoint.Client.List oList,
                                                           SPOTenantItemsParameters parameters)
        {
            await foreach (var listItemCollection in GetBatchAsync(siteUrl, oList, parameters))
            {
                foreach (var oItem in listItemCollection)
                {
                    if (String.IsNullOrWhiteSpace(parameters.FolderRelativeUrl))
                    {
                        yield return oItem;
                    }
                    if (!String.IsNullOrWhiteSpace(parameters.FolderRelativeUrl) && oItem["FileRef"].ToString() != null && oItem["FileRef"].ToString().Contains(parameters.FolderRelativeUrl))
                    {
                        yield return oItem;
                    }
                }
            }
        }

        internal async Task<Microsoft.SharePoint.Client.File> GetAttachmentFileAsync(string siteUrl, string attachmentServerRelativeUrl)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Getting attachment file '{attachmentServerRelativeUrl}'");

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);
            var file = clientContext.Web.GetFileByServerRelativeUrl(attachmentServerRelativeUrl);
            clientContext.Load(file);
            clientContext.ExecuteQuery();

            return file;
        }
    }
}
