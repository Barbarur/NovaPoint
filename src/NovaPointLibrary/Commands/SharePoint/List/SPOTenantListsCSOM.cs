using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.Utilities;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.List
{
    internal class SPOTenantListsCSOM
    {
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;
        private readonly SPOTenantListsParameters _param;

        internal SPOTenantListsCSOM(NPLogger logger, Authentication.AppInfo appInfo, SPOTenantListsParameters parameters)
        {
            _logger = logger;
            _appInfo = appInfo;
            _param = parameters;
        }

        internal async IAsyncEnumerable<SPOTenantResults> GetListsAsync()
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.GetLists";

            await foreach (SPOTenantResults siteResults in new SPOTenantSiteUrlsCSOM(_logger, _appInfo, _param).GetAsync())
            {
                _logger.LogUI(GetType().Name, $"Processing Site '{siteResults.SiteUrl}'");

                if (!String.IsNullOrWhiteSpace(siteResults.Remarks))
                {
                    yield return siteResults;
                    continue;
                }

                SPOTenantResults? errorResults = null;
                List<Microsoft.SharePoint.Client.List>? collList = null;
                try
                {
                    collList = await new SPOListCSOM(_logger, _appInfo).GetAsync(siteResults.SiteUrl, _param.ListTitle, _param.IncludeHiddenLists, _param.IncludeSystemLists);
                }
                catch (Exception ex)
                {
                    _logger.ReportError("Site", siteResults.SiteUrl, ex);

                    errorResults = new(siteResults.Progress, siteResults.SiteUrl);
                    errorResults.Remarks = ex.Message;
                }

                if (errorResults != null)
                {
                    yield return errorResults;
                }
                else if (collList != null)
                {
                    ProgressTracker progress = new(siteResults.Progress, collList.Count);
                    foreach (var oList in collList)
                    {
                        SPOTenantResults results = new(progress, siteResults.SiteUrl, oList);
                        yield return results;

                        progress.ProgressUpdateReport();
                    }
                }
            }
        }
    }
}
