using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;


namespace NovaPointLibrary.Commands.SharePoint.Item
{
    internal class SPOListItemCSOM
    {
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;

        private readonly Expression<Func<ListItem, object>>[] _defaultExpressions = new Expression<Func<ListItem, object>>[]
        {
            i => i.Id,
            i => i["FileRef"],
            f => f["FileLeafRef"],
            i => i.ParentList.Title,
            i => i.ParentList.BaseType,
            i => i.ParentList.ParentWeb.Url,
        };

        internal SPOListItemCSOM(NPLogger logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        private async IAsyncEnumerable<ListItemCollection> GetBatchAsync(string siteUrl,
                                                                         Microsoft.SharePoint.Client.List list,
                                                                         SPOItemsParameters parameters)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Start getting Items by batch");

            CamlQuery camlQuery = CamlQuery.CreateAllItemsQuery();

            if (parameters.AllItems)
            {
                LongListNotification(list);
            }
            else
            {
                camlQuery.FolderServerRelativeUrl = parameters.FolderRelativeUrl;
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

            Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] expressions;
            if (list.BaseType == BaseType.DocumentLibrary)
            {
                expressions = _defaultExpressions.Union(parameters.FileExpresions).ToArray();
            }
            else if (list.BaseType == BaseType.GenericList)
            {
                expressions = _defaultExpressions.Union(parameters.ItemExpresions).ToArray();
            }
            else
            {
                throw new Exception("This is not an Item List neither a Document Library");
            }

            int counter = 0;
            ClientContext clientContext;
            Microsoft.SharePoint.Client.List oList;
            _logger.LogTxt(GetType().Name, $"Start Loop");
            bool shouldContinue = false;
            do
            {
                _appInfo.IsCancelled();

                clientContext = await _appInfo.GetContext(siteUrl);
                oList = clientContext.Web.Lists.GetById(list.Id);
                ListItemCollection subcollListItem = oList.GetItems(camlQuery);

                string exceptionMessage = string.Empty;
                try
                {
                    clientContext.Load(subcollListItem,
                        sci => sci.ListItemCollectionPosition,
                        sci => sci.Include(expressions));
                    clientContext.ExecuteQueryRetry();
                }
                catch (Exception ex) { exceptionMessage = ex.Message; }

                if (!string.IsNullOrWhiteSpace(exceptionMessage))
                {
                    if (exceptionMessage.Contains("exceeds the list view threshold"))
                    {
                        _logger.LogUI(GetType().Name, $"The number of files in the target location exceeds the list view threshold. The Soution will collect all the items and then filter.");
                        camlQuery.FolderServerRelativeUrl = null;
                        LongListNotification(list);
                        shouldContinue = true;
                    }
                    else
                    {
                        throw new(exceptionMessage);
                    }
                }
                else
                {
                    counter += subcollListItem.Count;
                    if (counter >= 5000) { _logger.LogUI(GetType().Name, $"Collected from '{list.Title}' {counter} items..."); }
                    else { _logger.LogTxt(GetType().Name, $"Collected from '{list.Title}' {counter} items."); }

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
            await foreach (var listItemCollection in GetBatchAsync(siteUrl, oList, parameters))
            {
                foreach (var oItem in listItemCollection)
                {
                    if (parameters.AllItems)
                    {
                        yield return oItem;
                    }
                    else if (!String.IsNullOrWhiteSpace(parameters.FolderRelativeUrl) && oItem["FileRef"].ToString() != null && oItem["FileRef"].ToString().Contains(parameters.FolderRelativeUrl))
                    {
                        yield return oItem;
                    }
                }
            }
        }

        internal void LongListNotification(Microsoft.SharePoint.Client.List oList)
        {
            if (oList.ItemCount > 5000)
            {
                _logger.LogUI(GetType().Name, $"'{oList.BaseType}' '{oList.Title}' is a large list with {oList.ItemCount} items. Expect the Solution to take longer to run.");
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

        internal async Task RemoveAsync(string siteUrl, Microsoft.SharePoint.Client.List oList, ListItem oItem, bool recycle)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Removing ListItem '{oItem["FileLeafRef"]}'");

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
