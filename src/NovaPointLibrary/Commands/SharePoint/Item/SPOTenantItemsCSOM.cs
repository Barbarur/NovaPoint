using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.Utilities;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NovaPointLibrary.Commands.SharePoint.Item
{
    internal class SPOTenantItemsCSOM
    {
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;
        private readonly SPOTenantItemsParameters _param;

        private readonly Expression<Func<ListItem, object>>[] _defaultExpressions = new Expression<Func<ListItem, object>>[]
        {
            i => i.Id,
            i => i["FileRef"],
        };

        internal SPOTenantItemsCSOM(NPLogger logger, Authentication.AppInfo appInfo, SPOTenantItemsParameters parameters)
        {
            _logger = logger;
            _appInfo = appInfo;
            _param = parameters;
        }

        internal async IAsyncEnumerable<SPOTenantItemRecord> GetAsync()
        {
            _appInfo.IsCancelled();

            await foreach (var recordList in new SPOTenantListsCSOM(_logger, _appInfo, _param.TListsParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (!String.IsNullOrWhiteSpace(recordList.ErrorMessage) || recordList.List == null)
                {
                    SPOTenantItemRecord recordItem = new(recordList, null)
                    {
                        ErrorMessage = recordList.ErrorMessage,
                    };

                    yield return recordItem;
                    continue;
                }

                if (recordList.List.ItemCount == 0)
                {
                    SPOTenantItemRecord recordItem = new(recordList, null)
                    {
                        ErrorMessage = $"'{recordList.List.BaseType}' is empty",
                    };

                    yield return recordItem;
                    continue;
                }

                if (String.IsNullOrEmpty(_param.ItemsParam.FolderRelativeUrl))
                {
                    LongListNotification(recordList.List);
                }

                CamlQuery camlQuery = CamlQuery.CreateAllItemsQuery();
                camlQuery.FolderServerRelativeUrl = _param.ItemsParam.FolderRelativeUrl;

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

                if (recordList.List.BaseType == BaseType.DocumentLibrary)
                {
                    requestedExpressions = _defaultExpressions.Union(_param.ItemsParam.FileExpresions).ToArray();
                }
                else if (recordList.List.BaseType == BaseType.GenericList)
                {
                    requestedExpressions = _defaultExpressions.Union(_param.ItemsParam.ItemExpresions).ToArray();
                }
                else
                {
                    throw new Exception("This is not an Item List neither a Document Library");
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
                ProgressTracker progress = new(recordList.Progress, recordList.List.ItemCount);
                bool shouldContinue = false;
                do
                {
                    _appInfo.IsCancelled();

                    clientContext = await _appInfo.GetContext(recordList.SiteUrl);
                    oList = clientContext.Web.Lists.GetById(recordList.List.Id);
                    ListItemCollection subcollListItem = oList.GetItems(camlQuery);

                    string anyError = string.Empty;
                    try
                    {
                        clientContext.Load(subcollListItem,
                            sci => sci.ListItemCollectionPosition,
                            sci => sci.Include(expressions));
                        clientContext.ExecuteQueryRetry();
                    }
                    catch (Exception ex) { anyError = ex.Message; }

                    if (!string.IsNullOrWhiteSpace(anyError))
                    {
                        if (anyError.Contains("exceeds the list view threshold"))
                        {
                            _logger.LogUI(GetType().Name, $"The number of files in the target location exceeds the list view threshold. The Soution will collect all the items and then filter.");
                            camlQuery.FolderServerRelativeUrl = null;
                            LongListNotification(recordList.List);
                            shouldContinue = true;
                        }
                        else
                        {
                            SPOTenantItemRecord recordItem = new(recordList, null)
                            {
                                ErrorMessage = anyError,
                            };

                            yield return recordItem;
                            break;
                        }
                    }
                    else
                    {
                        counter += subcollListItem.Count;
                        if (counter >= 5000) { _logger.LogUI(GetType().Name, $"Collected from '{recordList.List.Title}' {counter} items..."); }
                        else { _logger.LogTxt(GetType().Name, $"Collected from '{recordList.List.Title}' {counter} items."); }


                        foreach (var oItem in subcollListItem)
                        {
                            _appInfo.IsCancelled();

                            if (String.IsNullOrWhiteSpace(_param.ItemsParam.FolderRelativeUrl))
                            {
                                SPOTenantItemRecord recordItem = new(recordList, oItem);
                                yield return recordItem;
                            }
                            else if (!String.IsNullOrWhiteSpace(_param.ItemsParam.FolderRelativeUrl) && oItem["FileRef"].ToString() != null && oItem["FileRef"].ToString().Contains(_param.ItemsParam.FolderRelativeUrl))
                            {
                                SPOTenantItemRecord recordItem = new(recordList, oItem);
                                yield return recordItem;
                            }
                            progress.ProgressUpdateReport();
                        }

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
        }

        internal void LongListNotification(Microsoft.SharePoint.Client.List oList)
        {
            if (oList.ItemCount > 5000)
            {
                _logger.LogUI(GetType().Name, $"'{oList.BaseType}' '{oList.Title}' is a large list with {oList.ItemCount} items. Expect the Solution to take longer to run.");
            }
        }
    }
}
