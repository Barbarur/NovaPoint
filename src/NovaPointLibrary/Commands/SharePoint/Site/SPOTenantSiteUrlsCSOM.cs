using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.AzureAD;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    internal class SPOTenantSiteUrlsCSOM
    {
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;
        private readonly SPOTenantSiteUrlsParameters _param;

        internal SPOTenantSiteUrlsCSOM(NPLogger logger, Authentication.AppInfo appInfo, SPOTenantSiteUrlsParameters parameters)
        {
            _logger = logger;
            _appInfo = appInfo;
            _param = parameters;
        }

        internal async IAsyncEnumerable<SPOTenantSiteUrlsRecord> GetAsync()
        {
            _appInfo.IsCancelled();

            if (_param.AllSiteCollections)
            {
                List<SiteProperties> collSiteCollections = await new SPOSiteCollectionCSOM(_logger, _appInfo).GetAllAsync(_param.IncludeShareSite, _param.IncludePersonalSite, _param.OnlyGroupIdDefined);

                ProgressTracker progress = new(_logger, collSiteCollections.Count);
                foreach (var oSiteCollection in collSiteCollections)
                {
                    _appInfo.IsCancelled();
                    _logger.LogUI(GetType().Name, $"Processing Site '{oSiteCollection.Url}'");

                    var record = new SPOTenantSiteUrlsRecord(progress, oSiteCollection);
                    yield return record;

                    progress.ProgressUpdateReport();
                }
            }

            else if (!String.IsNullOrWhiteSpace(_param.SiteUrl))
            {
                ProgressTracker progress = new(_logger, 1);

                _logger.LogUI(GetType().Name, $"Processing Site '{_param.SiteUrl}'");

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
                    _logger.LogUI(GetType().Name, $"Collected {count} Site from file");

                    ProgressTracker progress = new(_logger, count);
                    foreach (string line in lines)
                    {
                        _appInfo.IsCancelled();
                        _logger.LogUI(GetType().Name, $"Processing Site '{line}'");

                        if (string.IsNullOrEmpty(line)) { continue; }

                        SPOTenantSiteUrlsRecord record = new(progress, line.Trim());
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
