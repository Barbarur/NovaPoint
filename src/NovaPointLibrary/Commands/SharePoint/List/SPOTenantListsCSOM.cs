using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Logging;
using NovaPointLibrary.Solutions;

namespace NovaPointLibrary.Commands.SharePoint.List
{
    internal class SPOTenantListsCSOM
    {
        private readonly ILogger _logger;
        private readonly IAppClient _appInfo;
        private readonly SPOTenantListsParameters _param;

        internal SPOTenantListsCSOM(ILogger logger, IAppClient appInfo, SPOTenantListsParameters parameters)
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
                List<Microsoft.SharePoint.Client.List> collList = new();
                try
                {
                    collList = await new SPOListCSOM(_logger, _appInfo).GetAsync(siteResults.SiteUrl, _param.ListParam);
                }
                catch (Exception ex) { tryException = ex; }


                if (tryException != null)
                {
                    _logger.Error(GetType().Name, "Site", siteResults.SiteUrl, tryException);

                    SPOTenantListsRecord recordList = new(siteResults, siteResults.Progress, tryException);

                    yield return recordList;
                }
                else if (!collList.Any())
                {
                    Exception ex = new("No lists on this site.");
                    SPOTenantListsRecord recordList = new(siteResults, siteResults.Progress, ex);
                    yield return recordList;
                }
                else
                {
                    ProgressTracker progress = new(siteResults.Progress, collList.Count);
                    foreach (var oList in collList)
                    {
                        _logger.Info(GetType().Name, $"Processing '{oList.BaseType}' '{oList.Title}'");

                        SPOTenantListsRecord record = new(siteResults, progress, oList);
                        yield return record;

                        progress.ProgressUpdateReport();
                    }
                }

            }
        }
    }
}
