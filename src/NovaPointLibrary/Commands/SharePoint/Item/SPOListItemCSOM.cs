using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using System.Linq.Expressions;
using System.Xml.Linq;


namespace NovaPointLibrary.Commands.SharePoint.Item
{
    internal class SPOListItemCSOM
    {
        private readonly Main _main;
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;

        internal SPOListItemCSOM(Main main)
        {
            _main = main;
        }

        internal SPOListItemCSOM(NPLogger logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        private async IAsyncEnumerable<ListItemCollection> GetBatchDEPRECATED(string siteUrl,
                                                                    string listTitle,
                                                                    Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] retrievalExpressions)
        {
            _main.IsCancelled();
            string methodName = $"{GetType().Name}.GetBatch";
            _main.AddLogToTxt(methodName, $"Start getting Items by batch");

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

            ClientContext clientContext = await _main.GetContext(siteUrl);

            Microsoft.SharePoint.Client.List oList = clientContext.Web.Lists.GetByTitle(listTitle);

            var defaultExpressions = new Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[]
            {
                i => i.ParentList.Title,
                i => i.ParentList.ParentWeb.Url,

            };

            var expressions = retrievalExpressions.Union(defaultExpressions).ToArray();

            _main.AddLogToTxt(methodName, $"Start Loop");
            int counter = 0;
            do
            {
                _main.IsCancelled();

                ListItemCollection subcollListItem = oList.GetItems(camlQuery);

                clientContext.Load(subcollListItem,
                    sci => sci.ListItemCollectionPosition,
                    sci => sci.Include(expressions));

                clientContext.ExecuteQueryRetry();

                counter += subcollListItem.Count;
                _main.AddLogToUI(methodName, $"Collected {counter} items...");
                camlQuery.ListItemCollectionPosition = subcollListItem.ListItemCollectionPosition;

                yield return subcollListItem;

                clientContext = await _main.GetContext(siteUrl);
                oList = clientContext.Web.Lists.GetByTitle(listTitle);

            }
            while (camlQuery.ListItemCollectionPosition != null);

            _main.AddLogToTxt(methodName, $"Finish getting Items by batch");
        }

        internal async IAsyncEnumerable<Microsoft.SharePoint.Client.ListItem> GetDEPRECATED(string siteUrl, string listTitle, Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] retrievalExpressions)
        {
            await foreach (var listItemCollection in GetBatchDEPRECATED(siteUrl, listTitle, retrievalExpressions))
            {
                foreach (var oItem in listItemCollection)
                {
                    yield return oItem;
                }
            }
        }

        internal async Task<Microsoft.SharePoint.Client.File> GetAttachmentFileDEPRECATED(string siteUrl, string attachmentServerRelativeUrl)
        {
            _main.IsCancelled();
            string methodName = $"{GetType().Name}.GetAttachmentFile";
            _main.AddLogToTxt(methodName, $"Start getting attachment file '{attachmentServerRelativeUrl}'");

            ClientContext clientContext = await _main.GetContext(siteUrl);
            var file = clientContext.Web.GetFileByServerRelativeUrl(attachmentServerRelativeUrl);
            clientContext.Load(file);
            clientContext.ExecuteQuery();

            _main.AddLogToTxt(methodName, $"Finish getting attachment file '{attachmentServerRelativeUrl}'");
            return file;
        }



        private async IAsyncEnumerable<ListItemCollection> GetBatch(string siteUrl,
                                                            string listTitle,
                                                            Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] retrievalExpressions)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.GetBatch";
            _logger.LogTxt(methodName, $"Start getting Items by batch");

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

            ClientContext clientContext = await _appInfo.GetContext(_logger, siteUrl);

            Microsoft.SharePoint.Client.List oList = clientContext.Web.Lists.GetByTitle(listTitle);

            var defaultExpressions = new Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[]
            {
                i => i.ParentList.Title,
                i => i.ParentList.ParentWeb.Url,

            };

            var expressions = retrievalExpressions.Union(defaultExpressions).ToArray();

            _logger.LogTxt(methodName, $"Start Loop");
            int counter = 0;
            do
            {
                _appInfo.IsCancelled();

                ListItemCollection subcollListItem = oList.GetItems(camlQuery);

                clientContext.Load(subcollListItem,
                    sci => sci.ListItemCollectionPosition,
                    sci => sci.Include(expressions));

                clientContext.ExecuteQueryRetry();

                counter += subcollListItem.Count;
                _logger.LogUI(methodName, $"Collected {counter} items...");
                camlQuery.ListItemCollectionPosition = subcollListItem.ListItemCollectionPosition;

                yield return subcollListItem;

                clientContext = await _appInfo.GetContext(_logger, siteUrl);
                oList = clientContext.Web.Lists.GetByTitle(listTitle);

            }
            while (camlQuery.ListItemCollectionPosition != null);

            _logger.LogTxt(methodName, $"Finish getting Items by batch");
        }

        internal async IAsyncEnumerable<Microsoft.SharePoint.Client.ListItem> Get(string siteUrl, string listTitle, Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] retrievalExpressions)
        {
            await foreach (var listItemCollection in GetBatch(siteUrl, listTitle, retrievalExpressions))
            {
                foreach (var oItem in listItemCollection)
                {
                    yield return oItem;
                }
            }
        }

        internal async Task<Microsoft.SharePoint.Client.File> GetAttachmentFile(string siteUrl, string attachmentServerRelativeUrl)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.GetAttachmentFile";
            _logger.LogTxt(methodName, $"Start getting attachment file '{attachmentServerRelativeUrl}'");

            ClientContext clientContext = await _appInfo.GetContext(_logger, siteUrl);
            var file = clientContext.Web.GetFileByServerRelativeUrl(attachmentServerRelativeUrl);
            clientContext.Load(file);
            clientContext.ExecuteQuery();

            _logger.LogTxt(methodName, $"Finish getting attachment file '{attachmentServerRelativeUrl}'");
            return file;
        }










        private async IAsyncEnumerable<ListItemCollection> GetBatchPROTOTYPE(string siteUrl,
                                                                             string listTitle,
                                                                             Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] retrievalExpressions)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.GetBatch";
            _logger.LogTxt(methodName, $"Start getting Items by batch");

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

            var defaultExpressions = new Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[]
            {
                i => i.ParentList.Title,
                i => i.ParentList.ParentWeb.Url,
            };

            var expressions = retrievalExpressions.Union(defaultExpressions).ToArray();

            int counter = 0;
            ClientContext clientContext;
            Microsoft.SharePoint.Client.List oList;
            _logger.LogTxt(methodName, $"Start Loop");
            do
            {
                _appInfo.IsCancelled();

                clientContext = await _appInfo.GetContext(_logger, siteUrl);
                oList = clientContext.Web.Lists.GetByTitle(listTitle);
                ListItemCollection subcollListItem = oList.GetItems(camlQuery);

                clientContext.Load(subcollListItem,
                    sci => sci.ListItemCollectionPosition,
                    sci => sci.Include(expressions));

                clientContext.ExecuteQueryRetry();

                counter += subcollListItem.Count;
                _logger.LogUI(methodName, $"Collected {counter} items...");
                camlQuery.ListItemCollectionPosition = subcollListItem.ListItemCollectionPosition;

                yield return subcollListItem;
            }
            while (camlQuery.ListItemCollectionPosition != null);

            _logger.LogTxt(methodName, $"Finish getting Items by batch");
        }


        internal async IAsyncEnumerable<ListItem> Get(string siteUrl,
                                                               string listTitle,
                                                               SPOTenantItemsParameters parameters,
                                                               Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] retrievalExpressions)
        {
            await foreach (var listItemCollection in GetBatchPROTOTYPE(siteUrl, listTitle, retrievalExpressions))
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

    }

}
