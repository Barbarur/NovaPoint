using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Logging;
using NovaPointLibrary.Solutions;
using System.Linq.Expressions;
using System.Xml.Linq;

namespace NovaPointLibrary.Commands.SharePoint.Item
{
    internal class SPOTenantItemsCSOM
    {
        private readonly ILogger _logger;
        private readonly IAppClient _appInfo;
        private readonly SPOTenantItemsParameters _param;

        private readonly Expression<Func<ListItem, object>>[] _defaultExpressions = new Expression<Func<ListItem, object>>[]
        {
            i => i.Id,
            i => i["FileRef"],
            f => f["FileLeafRef"],
            i => i.ParentList.Title,
            i => i.ParentList.BaseType,
            i => i.ParentList.RootFolder.ServerRelativeUrl,
            i => i.ParentList.ParentWeb.Url,
        };

        internal SPOTenantItemsCSOM(ILogger logger, IAppClient appInfo, SPOTenantItemsParameters parameters)
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

                var collListItems = new SPOListItemCSOM(_logger, _appInfo).GetAsync(tenantListRecord.SiteUrl, tenantListRecord.List, _param.ItemsParam).GetAsyncEnumerator();
                ProgressTracker progress = new(tenantListRecord.Progress, tenantListRecord.List.ItemCount);
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

                    if (exception == null)
                    {
                        SPOTenantItemRecord recordItem = new(tenantListRecord, oItem);
                        yield return recordItem;
                    }
                    else
                    {
                        SPOTenantItemRecord recordItem = new(tenantListRecord, exception);
                        _logger.Error(GetType().Name, $"{tenantListRecord.List.BaseType}", $"{tenantListRecord.List.RootFolder.ServerRelativeUrl}", exception);

                        yield return recordItem;
                        break;
                    }
                    progress.ProgressUpdateReport();
                }
            }
        }

    }
}
