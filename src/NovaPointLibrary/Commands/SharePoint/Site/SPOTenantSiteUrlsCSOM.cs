using Microsoft.Online.SharePoint.TenantAdministration;
using NovaPointLibrary.Core.Logging;
using NovaPointLibrary.Solutions;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    internal class SPOTenantSiteUrlsCSOM
    {
        private readonly LoggerSolution _logger;
        private readonly Authentication.AppInfo _appInfo;
        private readonly SPOTenantSiteUrlsParameters _param;

        internal SPOTenantSiteUrlsCSOM(LoggerSolution logger, Authentication.AppInfo appInfo, SPOTenantSiteUrlsParameters parameters)
        {
            _logger = logger;
            _appInfo = appInfo;
            _param = parameters;
        }

        internal async IAsyncEnumerable<SPOTenantSiteUrlsRecord> GetAsync()
        {
            _appInfo.IsCancelled();

            if (_param.ActiveSites)
            {
                List<SiteProperties> collSiteCollections = await new SPOSiteCollectionCSOM(_logger, _appInfo).GetAllAsync(_param);

                ProgressTracker progress = new(_logger, collSiteCollections.Count);
                foreach (var oSiteCollection in collSiteCollections)
                {
                    _appInfo.IsCancelled();
                    _logger.Info(GetType().Name, $"Processing Site '{oSiteCollection.Url}'");

                    var record = new SPOTenantSiteUrlsRecord(progress, oSiteCollection);
                    yield return record;

                    progress.ProgressUpdateReport();
                }
            }

            else if (!String.IsNullOrWhiteSpace(_param.SiteUrl))
            {
                ProgressTracker progress = new(_logger, 1);

                _logger.Info(GetType().Name, $"Processing Site '{_param.SiteUrl}'");

                SPOTenantSiteUrlsRecord record = new(progress, _param.SiteUrl);
                yield return record;

                progress.ProgressUpdateReport();
            }

            else if (!String.IsNullOrWhiteSpace(_param.ListOfSitesPath))
            {
                if (System.IO.File.Exists(_param.ListOfSitesPath))
                {
                    IEnumerable<string> lines = System.IO.File.ReadLines(@$"{_param.ListOfSitesPath}");
                    lines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

                    int count = lines.Count();
                    _logger.UI(GetType().Name, $"Collected {count} Site from file");

                    ProgressTracker progress = new(_logger, count);
                    foreach (string line in lines)
                    {
                        _appInfo.IsCancelled();

                        if (string.IsNullOrEmpty(line)) { continue; }

                        string siteUrl = line.Trim();
                        if (siteUrl.EndsWith("/"))
                        {
                            siteUrl = siteUrl.Remove(siteUrl.LastIndexOf("/"));
                        }

                        _logger.Info(GetType().Name, $"Processing Site '{siteUrl}'");

                        SPOTenantSiteUrlsRecord record = new(progress, siteUrl);
                        yield return record;

                        progress.ProgressUpdateReport();
                    }
                }
                else
                {
                    throw new Exception("The file with the list of sites does not exists");
                }
            }
        }

    }
}
