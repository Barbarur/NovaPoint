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

            await foreach (var tenantListRecord in new SPOTenantListsCSOM(_logger, _appInfo, _param.TListsParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (tenantListRecord.Ex != null || tenantListRecord.List == null)
                {
                    SPOTenantItemRecord recordItem = new(tenantListRecord);
                    yield return recordItem;
                    continue;
                }

                if (tenantListRecord.List.ItemCount == 0)
                {
                    Exception ex = new($"'{tenantListRecord.List.BaseType}' is empty");
                    SPOTenantItemRecord recordItem = new(tenantListRecord, ex);

                    yield return recordItem;
                    continue;
                }


                CamlQuery camlQuery = CamlQuery.CreateAllItemsQuery();

                if (_param.ItemsParam.AllItems)
                {
                    LongListNotification(tenantListRecord.List);
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

                if (tenantListRecord.List.BaseType == BaseType.DocumentLibrary)
                {
                    expressions = _defaultExpressions.Union(_param.ItemsParam.FileExpresions).ToArray();
                }
                else if (tenantListRecord.List.BaseType == BaseType.GenericList)
                {
                    expressions = _defaultExpressions.Union(_param.ItemsParam.ItemExpresions).ToArray();
                }
                else
                {
                    Exception ex = new("This is not an Item List neither a Document Library");
                    SPOTenantItemRecord recordItem = new(tenantListRecord, ex);

                    yield return recordItem;
                    continue;
                }

                int counter = 0;
                ClientContext clientContext;
                Microsoft.SharePoint.Client.List oList;
                _logger.LogTxt(GetType().Name, $"Start Loop");
                ProgressTracker progress = new(tenantListRecord.Progress, tenantListRecord.List.ItemCount);
                bool shouldContinue = false;
                do
                {
                    _appInfo.IsCancelled();

                    clientContext = await _appInfo.GetContext(tenantListRecord.SiteUrl);
                    oList = clientContext.Web.Lists.GetById(tenantListRecord.List.Id);
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

                    if ( exception != null)
                    {
                        if (exception.Message.Contains("exceeds the list view threshold"))
                        {
                            _logger.LogUI(GetType().Name, $"The number of files in the target location exceeds the list view threshold. The Soution will collect all the items and then filter.");
                            camlQuery.FolderServerRelativeUrl = null;
                            LongListNotification(tenantListRecord.List);
                            shouldContinue = true;
                        }
                        else
                        {
                            SPOTenantItemRecord recordItem = new(tenantListRecord, exception);

                            yield return recordItem;
                            break;
                        }
                    }
                    else
                    {
                        counter += subcollListItem.Count;
                        if (counter >= 5000) { _logger.LogUI(GetType().Name, $"Collected from '{tenantListRecord.List.Title}' {counter} items..."); }
                        else { _logger.LogTxt(GetType().Name, $"Collected from '{tenantListRecord.List.Title}' {counter} items."); }


                        foreach (var oItem in subcollListItem)
                        {
                            _appInfo.IsCancelled();

                            if (_param.ItemsParam.AllItems)
                            {
                                SPOTenantItemRecord recordItem = new(tenantListRecord, oItem);
                                yield return recordItem;
                            }
                            else if (!String.IsNullOrWhiteSpace(_param.ItemsParam.FolderRelativeUrl) && oItem["FileRef"].ToString() != null && oItem["FileRef"].ToString().Contains(_param.ItemsParam.FolderRelativeUrl))
                            {
                                SPOTenantItemRecord recordItem = new(tenantListRecord, oItem);
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

            await foreach (var tenantListRecord in new SPOTenantListsCSOM(_logger, _appInfo, _param.TListsParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (tenantListRecord.Ex != null || tenantListRecord.List == null)
                {
                    SPOTenantItemRecord recordItem = new(tenantListRecord);
                    yield return recordItem;
                    continue;
                }

                if (tenantListRecord.List.ItemCount == 0)
                {
                    Exception ex = new($"'{tenantListRecord.List.BaseType}' is empty");
                    SPOTenantItemRecord recordItem = new(tenantListRecord, ex);

                    yield return recordItem;
                    continue;
                }

                var collListItems = new SPOListItemCSOM(_logger, _appInfo).GetAsync(tenantListRecord.SiteUrl, tenantListRecord.List, _param.ItemsParam).GetAsyncEnumerator();
                while (true)
                {
                    ListItem? oItem = null;
                    Exception? exception = null;
                    try
                    {
                        if (!await collListItems.MoveNextAsync()) { break; }
                        oItem = collListItems.Current;
                    }
                    catch (Exception ex) { exception = ex; }

                    if (exception != null)
                    {
                        SPOTenantItemRecord recordItem = new(tenantListRecord, exception);

                        yield return recordItem;
                        break;
                    }
                    else
                    {
                        SPOTenantItemRecord recordItem = new(tenantListRecord, oItem);
                        yield return recordItem;
                    }
                }
            }
        }

    }
}
