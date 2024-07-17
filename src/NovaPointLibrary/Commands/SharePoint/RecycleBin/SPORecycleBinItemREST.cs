using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.RecycleBin
{
    internal class SPORecycleBinItemREST
    {
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;
        internal SPORecycleBinItemREST(NPLogger logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task RemoveAsync(string siteUrl, RecycleBinItem oRecycleBinItem)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Removing item {oRecycleBinItem.Title} using REST API");

            string api = siteUrl + "/_api/site/RecycleBin/DeleteByIds";

            string content = $"{{'ids':['{oRecycleBinItem.Id}']}}";

            await new RESTAPIHandler(_logger, _appInfo).PostAsync(api, content);
        }

        internal async Task RestoreAsync(string siteUrl, RecycleBinItem oRecycleBinItem)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Restoring item '{oRecycleBinItem.Title}' with id '{oRecycleBinItem.Id}' using REST API");

            string api = siteUrl + "/_api/site/RecycleBin/RestoreByIds";

            string content = $"{{'ids':['{oRecycleBinItem.Id}']}}";

            await new RESTAPIHandler(_logger, _appInfo).PostAsync(api, content);
        }
    }
}
