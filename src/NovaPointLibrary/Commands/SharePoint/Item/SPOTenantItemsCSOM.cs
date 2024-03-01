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

        //internal async IAsyncEnumerable<SPOTenantResults> GetItemsAsync()
        //{
        //    _appInfo.IsCancelled();
        //    string methodName = $"{GetType().Name}.GetLists";

        //    await foreach (SPOTenantResults listResult in new SPOTenantListsCSOM(_logger, _appInfo, _param).GetListsAsync())
        //    {
        //        _appInfo.IsCancelled();

        //        if (!String.IsNullOrWhiteSpace(listResult.ErrorMessage))
        //        {
        //            yield return listResult;
        //            continue;
        //        }
        //    }
        //}
    }
}
