﻿using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Core.Logging;

namespace NovaPointLibrary.Commands.SharePoint.RecycleBin
{
    internal class SPORecycleBinItemREST
    {
        private readonly LoggerSolution _logger;
        private readonly Authentication.AppInfo _appInfo;
        internal SPORecycleBinItemREST(LoggerSolution logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task RemoveAsync(string siteUrl, RecycleBinItem oRecycleBinItem)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Removing item {oRecycleBinItem.Title} using REST API");

            string api = siteUrl + "/_api/site/RecycleBin/DeleteByIds";

            string content = $"{{'ids':['{oRecycleBinItem.Id}']}}";

            await new RESTAPIHandler(_logger, _appInfo).PostAsync(api, content);
        }

        internal async Task RestoreAsync(string siteUrl, RecycleBinItem oRecycleBinItem)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Restoring item '{oRecycleBinItem.Title}' with id '{oRecycleBinItem.Id}' using REST API");

            string api = siteUrl + "/_api/site/RecycleBin/RestoreByIds";

            string content = $"{{'ids':['{oRecycleBinItem.Id}']}}";

            await new RESTAPIHandler(_logger, _appInfo).PostAsync(api, content);
        }
    }
}
