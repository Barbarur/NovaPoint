using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPoint.Commands.Site;
using NovaPointLibrary.Commands.AzureAD;
using NovaPointLibrary.Commands.SharePoint.Utilities;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    internal class SPOTenantSiteUrlsWithAccessCSOM
    {
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;
        private readonly SPOTenantSiteUrlsParameters _param;

        internal SPOTenantSiteUrlsWithAccessCSOM(NPLogger logger, Authentication.AppInfo appInfo, SPOTenantSiteUrlsParameters parameters)
        {
            _logger = logger;
            _appInfo = appInfo;
            _param = parameters;
        }

        private async IAsyncEnumerable<SPOTenantResults> GetSiteCollectionsAsync()
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Getting Site Collections");

            ProgressTracker progress;
            if (!String.IsNullOrWhiteSpace(_param.SiteUrl))
            {
                Web oSite = await new SPOWebCSOM(_logger, _appInfo).GetAsync(_param.SiteUrl);

                progress = new(_logger, 1);
                SPOTenantResults results = new(progress, oSite.Url);

                yield return results;
                progress.ProgressUpdateReport();
            }
            else
            {
                List<SiteProperties> collSiteCollections = await new SPOSiteCollectionCSOM(_logger, _appInfo).GetAsync(_param.SiteUrl, _param.IncludeShareSite, _param.IncludePersonalSite, _param.OnlyGroupIdDefined);

                progress = new(_logger, collSiteCollections.Count);
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
            _logger.LogTxt(GetType().Name, $"Getting Subsites from '{siteCollectionResult.SiteUrl}'");

            SPOTenantResults? errorResults = null;
            List<Web>? collSubsites = null;
            try
            {
                collSubsites = await new SPOSubsiteCSOM(_logger, _appInfo).GetAsync(siteCollectionResult.SiteUrl);
            }
            catch (Exception ex)
            {
                _logger.ReportError("Site", siteCollectionResult.SiteUrl, ex);

                errorResults = new(siteCollectionResult.Progress, siteCollectionResult.SiteUrl)
                {
                    ErrorMessage = ex.Message
                };
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
                    SPOTenantResults resultsSubsite = new(siteCollectionResult.Progress, oSubsite.Url);
                    yield return resultsSubsite;

                    siteCollectionResult.Progress.ProgressUpdateReport();
                }
            }
        }

        internal async IAsyncEnumerable<SPOTenantResults> GetAsync()
        {
            _appInfo.IsCancelled();

            GraphUser signedInUser = await new GetAADUser(_logger, _appInfo).GetSignedInUser();
            string adminUPN = signedInUser.UserPrincipalName;

            await foreach (SPOTenantResults resultsSiteCollection in GetSiteCollectionsAsync())
            {
                _appInfo.IsCancelled();

                try
                {
                    await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).SetAsync(resultsSiteCollection.SiteUrl, adminUPN);
                }
                catch (Exception ex)
                {
                    _logger.ReportError("Site", resultsSiteCollection.SiteUrl, ex);

                    resultsSiteCollection.ErrorMessage = ex.Message;
                }

                _logger.LogUI(GetType().Name, $"Processing Site '{resultsSiteCollection.SiteUrl}'");
                yield return resultsSiteCollection;


                if (string.IsNullOrWhiteSpace(resultsSiteCollection.ErrorMessage) && _param.IncludeSubsites)
                {
                    await foreach (SPOTenantResults subsite in GetSubsitesAsync(resultsSiteCollection))
                    {
                        _logger.LogUI(GetType().Name, $"Processing Site '{subsite.SiteUrl}'");
                        yield return subsite;
                    }
                }

                if (string.IsNullOrWhiteSpace(resultsSiteCollection.ErrorMessage) && _param.RemoveAdmin)
                {
                    try
                    {
                        await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).RemoveAsync(resultsSiteCollection.SiteUrl, adminUPN);
                    }
                    catch (Exception ex)
                    {
                        _logger.ReportError("Site", resultsSiteCollection.SiteUrl, ex);
                    }
                }
            }
        }
    }
}
