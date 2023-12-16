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

namespace NovaPointLibrary.Commands.SharePoint.Item
{
    // ON TESTING ONLY
    internal class SPOTenantItemsCSOM
    {
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;
        private readonly SPOTenantItemsParameters _param;

        internal SPOTenantItemsCSOM(NPLogger logger, Authentication.AppInfo appInfo, SPOTenantItemsParameters parameters)
        {
            _logger = logger;
            _appInfo = appInfo;
            _param = parameters;
        }

        internal async IAsyncEnumerable<SPOTenantResults> GetItemsAsync()
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.GetLists";

            await foreach (SPOTenantResults listResult in new SPOTenantListsCSOM(_logger, _appInfo, _param.GetListParameters()).GetListsAsync())
            {
                _appInfo.IsCancelled();

                if (!String.IsNullOrWhiteSpace(listResult.Remarks))
                {
                    yield return listResult;
                    continue;
                }

                //try
                //{
                //    _logger.LogTxt(methodName, $"Start getting Items for '{listResult.List.Title}'");

                //    Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] currentExpressions;
                //    if (listResult.List.BaseType == BaseType.DocumentLibrary)
                //    {
                //        currentExpressions = _param.FileExpresions;
                //    }
                //    else if (listResult.List.BaseType == BaseType.GenericList)
                //    {
                //        currentExpressions = _param.ItemExpresions;
                //    }
                //    else
                //    {
                //        SPOTenantResults errorResults = new(listResult.Progress, listResult.SiteUrl, listResult.List);
                //        errorResults.Remarks = "This is not a List neither a Library";
                //        yield return errorResults;
                //    }
                    



                //}
                //catch (Exception ex)
                //{
                //    _logger.ReportError(listResult.List.BaseType.ToString(), listResult.List.DefaultViewUrl, ex);

                //    SPOTenantResults errorResults = new(listResult.Progress, listResult.SiteUrl, listResult.List);
                //    errorResults.Remarks = ex.Message;
                //    yield return errorResults;
                //}
                



                //SPOTenantResults? errorResults = null;
                //List<Microsoft.SharePoint.Client.List>? collList = null;
                //try
                //{
                //    collList = await new SPOListCSOM(_logger, _appInfo).GetAsync(listResult.SiteUrl, _param.ListTitle, _param.IncludeHiddenLists, _param.IncludeSystemLists);
                //}
                //catch (Exception ex)
                //{
                //    _logger.ReportError("Site", listResult.SiteUrl, ex);

                //    errorResults = new(listResult.Progress, listResult.SiteUrl);
                //    errorResults.Remarks = ex.Message;
                //}

                //if (errorResults != null)
                //{
                //    yield return errorResults;
                //}
                //else if (collList != null)
                //{
                //    ProgressTracker progress = new(listResult.Progress, collList.Count);
                //    foreach (var oList in collList)
                //    {
                //        SPOTenantResults results = new(progress, listResult.SiteUrl, oList);
                //        yield return results;

                //        progress.ProgressUpdateReport();
                //    }
                //}
            }
        }


    }
}
