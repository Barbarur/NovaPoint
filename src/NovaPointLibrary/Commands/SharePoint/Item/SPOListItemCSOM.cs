﻿using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
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
            i => i["FileRef"],
        };

        internal SPOListItemCSOM(NPLogger logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        private async IAsyncEnumerable<ListItemCollection> GetBatchAsync(string siteUrl,
                                                                         Microsoft.SharePoint.Client.List TargetList,
                                                                         SPOItemsParameters parameters)
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
                requestedExpressions = _defaultExpressions.Union(parameters.FileExpresions).ToArray();
            }
            else if (TargetList.BaseType == BaseType.GenericList)
            {
                requestedExpressions = _defaultExpressions.Union(parameters.ItemExpresions).ToArray();
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
                if (counter >= 5000) { _logger.LogUI(GetType().Name, $"Collected from '{TargetList.Title}' {counter} items..."); }
                else { _logger.LogTxt(GetType().Name, $"Collected from '{TargetList.Title}' {counter} items..."); }
                
                camlQuery.ListItemCollectionPosition = subcollListItem.ListItemCollectionPosition;

                yield return subcollListItem;
            }
            while (camlQuery.ListItemCollectionPosition != null);

        }

        internal async IAsyncEnumerable<ListItem> GetAsync(string siteUrl,
                                                           Microsoft.SharePoint.Client.List oList,
                                                           SPOItemsParameters parameters)
        {
            await foreach (var listItemCollection in GetBatchAsync(siteUrl, oList, parameters))
            {
                foreach (var oItem in listItemCollection)
                {
                    if (String.IsNullOrWhiteSpace(parameters.FolderRelativeUrl))
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
