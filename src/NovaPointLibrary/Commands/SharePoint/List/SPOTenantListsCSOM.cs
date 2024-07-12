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

        internal async IAsyncEnumerable<SPOTenantListsRecord> GetAsync()
        {
            _appInfo.IsCancelled();

            await foreach (var siteResults in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param.SiteAccParam).GetAsync())
            {

                if (siteResults.Ex != null)
                {
                    SPOTenantListsRecord record = new(siteResults, siteResults.Progress, siteResults.Ex);

                    yield return record;
                    continue;
                }


                Exception? tryException = null;
                List<Microsoft.SharePoint.Client.List>? collList = null;
                try
                {
                    collList = await new SPOListCSOM(_logger, _appInfo).GetAsync(siteResults.SiteUrl, _param.ListParam);
                }
                catch (Exception ex) { tryException = ex; }


                if (tryException != null)
                {
                    _logger.ReportError("Site", siteResults.SiteUrl, tryException);

                    SPOTenantListsRecord recordList = new(siteResults, siteResults.Progress, tryException);

                    yield return recordList;
                }
                else if (collList != null)
                {
                    ProgressTracker progress = new(siteResults.Progress, collList.Count);
                    foreach (var oList in collList)
                    {
                        _logger.LogTxt(GetType().Name, $"Processing {oList.BaseType} '{oList.Title}'");

                        SPOTenantListsRecord record = new(siteResults, siteResults.Progress, oList);
                        yield return record;

                        progress.ProgressUpdateReport();
                    }
                }
            }
        }
    }
}
