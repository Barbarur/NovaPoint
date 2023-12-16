using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.Utilities;
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

        private async IAsyncEnumerable<SPOTenantResults> GetSiteCollectionsAsync()
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.GetSiteCollections";
            _logger.LogTxt(methodName, $"Start getting Site Collections'");

            ProgressTracker progress;
            if (!String.IsNullOrWhiteSpace(_param.SiteUrl))
            {
                Web oSite = await new SPOSiteCSOM(_logger, _appInfo).GetAsync(_param.SiteUrl);

                progress = new(_logger, 1);

                SPOTenantResults results = new(progress, oSite.Url);

                _logger.LogTxt(methodName, $"Finish getting Site Collections'");
                yield return results;
                progress.ProgressUpdateReport();
            }
            else
            {
                List<SiteProperties> collSiteCollections = await new SPOSiteCollectionCSOM(_logger, _appInfo).Get(_param.SiteUrl, _param.IncludeShareSite, _param.IncludePersonalSite, _param.OnlyGroupIdDefined);

                progress = new(_logger, collSiteCollections.Count);
                _logger.LogTxt(methodName, $"Finish getting Site Collections'");
                foreach (var oSiteCollection in collSiteCollections)
                {
                    SPOTenantResults results = new(progress, oSiteCollection.Url);
                    yield return results;
                    progress.ProgressUpdateReport();
                }
            }
        }

        private async IAsyncEnumerable<SPOTenantResults> GetSubsitesAsync(SPOTenantResults siteCollectionResult)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.GetSubsites";
            _logger.LogTxt(methodName, $"Start getting Subsites for '{siteCollectionResult.SiteUrl}'");

            SPOTenantResults? errorResults = null;
            List<Web>? collSubsites = null;
            try
            {
                collSubsites = await new SPOSubsiteCSOM(_logger, _appInfo).Get(siteCollectionResult.SiteUrl);
            }
            catch (Exception ex)
            {
                _logger.ReportError("Site", siteCollectionResult.SiteUrl, ex);

                errorResults = new(siteCollectionResult.Progress, siteCollectionResult.SiteUrl);
                errorResults.Remarks = ex.Message;
            }

            if (errorResults != null)
            {
                yield return errorResults;
            }
            else if (collSubsites != null)
            {
                siteCollectionResult.Progress.IncreaseTotalCount(collSubsites.Count);
                foreach (var oSubsite in collSubsites)
                {
                    SPOTenantResults results = new(siteCollectionResult.Progress, oSubsite.Url);
                    yield return results;

                    siteCollectionResult.Progress.ProgressUpdateReport();
                }
            }

            _logger.LogTxt(methodName, $"Finish getting Subsites for '{siteCollectionResult}'");
        }

        internal async IAsyncEnumerable<SPOTenantResults> GetAsync()
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.GetSites";
            _logger.LogTxt(methodName, $"Start getting Sites");

            await foreach (SPOTenantResults SiteCollection in GetSiteCollectionsAsync())
            {
                _appInfo.IsCancelled();

                SPOTenantResults? errorResults = null;
                try
                {
                    await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).Set(SiteCollection.SiteUrl, _param.AdminUPN);
                }
                catch (Exception ex)
                {
                    _logger.ReportError("Site", SiteCollection.SiteUrl, ex);

                    errorResults = new(SiteCollection.Progress, SiteCollection.SiteUrl);
                    errorResults.Remarks = ex.Message;
                }

                if (errorResults != null)
                {
                    yield return errorResults;
                    continue;
                }
                else
                {
                    yield return SiteCollection;

                    if (_param.IncludeSubsites)
                    {
                        await foreach (SPOTenantResults subsite in GetSubsitesAsync(SiteCollection))
                        {
                            yield return subsite;
                        }
                    }
                }

                try
                {
                    if (_param.RemoveAdmin)
                    {
                        await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).Remove(SiteCollection.SiteUrl, _param.AdminUPN);
                    }
                }
                catch (Exception ex)
                {
                    _logger.ReportError("Site", SiteCollection.SiteUrl, ex);
                }
            }
        }




    }
}
