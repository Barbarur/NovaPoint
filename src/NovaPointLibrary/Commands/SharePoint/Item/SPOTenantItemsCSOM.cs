using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Solutions;
using System.Linq.Expressions;
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
            i => i.ParentList.Title,
            i => i.ParentList.BaseType,
            i => i.ParentList.ParentWeb.Url,
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


                CamlQuery camlQuery = CamlQuery.CreateAllItemsQuery();

                if (_param.ItemsParam.AllItems)
                {
                    LongListNotification(recordList.List);
                }
                else
                {
                    camlQuery.FolderServerRelativeUrl = _param.ItemsParam.FolderRelativeUrl;
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

                if (recordList.List.BaseType == BaseType.DocumentLibrary)
                {
                    expressions = _defaultExpressions.Union(_param.ItemsParam.FileExpresions).ToArray();
                }
                else if (recordList.List.BaseType == BaseType.GenericList)
                {
                    expressions = _defaultExpressions.Union(_param.ItemsParam.ItemExpresions).ToArray();
                }
                else
                {
                    SPOTenantItemRecord recordItem = new(recordList, null)
                    {
                        ErrorMessage = "This is not an Item List neither a Document Library",
                    };

                    yield return recordItem;
                    continue;
                }

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
                            LongListNotification(recordList.List);
                            shouldContinue = true;
                        }
                        else
                        {
                            SPOTenantItemRecord recordItem = new(recordList, null)
                            {
                                ErrorMessage = exceptionMessage,
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

                            if (_param.ItemsParam.AllItems)
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


        internal async IAsyncEnumerable<SPOTenantItemRecord> GetNEWAsync()
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

                var collListItems = new SPOListItemCSOM(_logger, _appInfo).GetAsync(recordList.SiteUrl, recordList.List, _param.ItemsParam).GetAsyncEnumerator();
                while (true)
                {
                    ListItem? oItem = null;
                    string exceptionMessage = string.Empty;
                    try
                    {
                        if (!await collListItems.MoveNextAsync()) { break; }
                        oItem = collListItems.Current;
                    }
                    catch (Exception ex) { exceptionMessage = ex.Message; }

                    if (!string.IsNullOrWhiteSpace(exceptionMessage))
                    {
                        SPOTenantItemRecord recordItem = new(recordList, null)
                        {
                            ErrorMessage = exceptionMessage,
                        };

                        yield return recordItem;
                        break;
                    }
                    else
                    {
                        SPOTenantItemRecord recordItem = new(recordList, oItem);
                        yield return recordItem;
                    }
                }
            }
        }

    }
}
