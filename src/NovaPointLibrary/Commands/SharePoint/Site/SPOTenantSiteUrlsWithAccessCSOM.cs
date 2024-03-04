using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
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
        private readonly SPOTenantSiteUrlsWithAccessParameters _param;

        internal SPOTenantSiteUrlsWithAccessCSOM(NPLogger logger, Authentication.AppInfo appInfo, SPOTenantSiteUrlsWithAccessParameters parameters)
        {
            _logger = logger;
            _appInfo = appInfo;
            _param = parameters;
        }


        // TO BE DEPRECATED
        private async IAsyncEnumerable<SPOTenantResults> GetSiteCollectionsAsync()
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Getting Site Collections");

            ProgressTracker progress;
            if (!String.IsNullOrWhiteSpace(_param.SiteParam.SiteUrl))
            {
                Web oSite = await new SPOWebCSOM(_logger, _appInfo).GetAsync(_param.SiteParam.SiteUrl);

                progress = new(_logger, 1);
                SPOTenantResults results = new(progress, oSite.Url, oSite.Title);

                yield return results;
                progress.ProgressUpdateReport();
            }
            else
            {
                List<SiteProperties> collSiteCollections = await new SPOSiteCollectionCSOM(_logger, _appInfo).GetAsync(_param.SiteParam.SiteUrl, _param.SiteParam.IncludeShareSite, _param.SiteParam.IncludePersonalSite, _param.SiteParam.OnlyGroupIdDefined);

                progress = new(_logger, collSiteCollections.Count);
                foreach (var oSiteCollection in collSiteCollections)
                {
                    SPOTenantResults results = new(progress, oSiteCollection.Url, oSiteCollection.Title);
                    yield return results;
                    progress.ProgressUpdateReport();
                }
            }
        }

        // TO BE DEPRECATED
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

                errorResults = new(siteCollectionResult.Progress, siteCollectionResult.SiteUrl, siteCollectionResult.SiteName)
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
                    SPOTenantResults resultsSubsite = new(siteCollectionResult.Progress, oSubsite.Url, oSubsite.Title);
                    yield return resultsSubsite;

                    siteCollectionResult.Progress.ProgressUpdateReport();
                }
            }
        }

        // TO BE DEPRECATED
        internal async IAsyncEnumerable<SPOTenantResults> GetAsync()
        {
            _appInfo.IsCancelled();

            GraphUser signedInUser = await new GetAADUser(_logger, _appInfo).GetSignedInUser();
            string adminUPN = signedInUser.UserPrincipalName;

            await foreach (SPOTenantResults resultSiteCollection in GetSiteCollectionsAsync())
            {
                _appInfo.IsCancelled();

                _logger.LogUI(GetType().Name, $"Processing Site '{resultSiteCollection.SiteUrl}'");

                try
                {
                    await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).SetAsync(resultSiteCollection.SiteUrl, adminUPN);
                }
                catch (Exception ex)
                {
                    _logger.ReportError("Site", resultSiteCollection.SiteUrl, ex);

                    resultSiteCollection.ErrorMessage = ex.Message;
                }
                yield return resultSiteCollection;


                if (string.IsNullOrWhiteSpace(resultSiteCollection.ErrorMessage)) { continue; }


                if (_param.SiteParam.IncludeSubsites)
                {
                    await foreach (SPOTenantResults subsite in GetSubsitesAsync(resultSiteCollection))
                    {
                        _logger.LogUI(GetType().Name, $"Processing Site '{subsite.SiteUrl}'");
                        yield return subsite;
                    }
                }


                if (_param.RemoveAdmin)
                {
                    try
                    {
                        await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).RemoveAsync(resultSiteCollection.SiteUrl, adminUPN);
                    }
                    catch (Exception ex)
                    {
                        _logger.ReportError("Site", resultSiteCollection.SiteUrl, ex);

                        resultSiteCollection.ErrorMessage = ex.Message;

                    }
                    if (!string.IsNullOrWhiteSpace(resultSiteCollection.ErrorMessage))
                    {
                        yield return resultSiteCollection;
                    }
                }
            }
        }






        private async IAsyncEnumerable<SPOTenantSiteUrlsRecord> GetSiteCollectionsAsyncNEW()
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Getting Site Collections");

            ProgressTracker progress;

            if (_param.SiteParam.AllSiteCollections)
            {
                List<SiteProperties> collSiteCollections = await new SPOSiteCollectionCSOM(_logger, _appInfo).GetAsync(_param.SiteParam.SiteUrl, _param.SiteParam.IncludeShareSite, _param.SiteParam.IncludePersonalSite, _param.SiteParam.OnlyGroupIdDefined);

                progress = new(_logger, collSiteCollections.Count);
                foreach (var oSiteCollection in collSiteCollections)
                {
                    yield return new SPOTenantSiteUrlsRecord(progress, oSiteCollection);

                    progress.ProgressUpdateReport();
                }

            }
            else if (!String.IsNullOrWhiteSpace(_param.SiteParam.SiteUrl))
            {
                Web oSite = await new SPOWebCSOM(_logger, _appInfo).GetAsync(_param.SiteParam.SiteUrl);

                progress = new(_logger, 1);

                yield return new SPOTenantSiteUrlsRecord(progress, oSite);

                progress.ProgressUpdateReport();
            }

        }

        private async IAsyncEnumerable<SPOTenantSiteUrlsRecord> GetSubsitesAsyncNEW(SPOTenantSiteUrlsRecord siteCollectionResult)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Getting Subsites from '{siteCollectionResult.SiteUrl}'");

            SPOTenantSiteUrlsRecord? errorResults = null;
            List<Web>? collSubsites = null;
            try
            {
                collSubsites = await new SPOSubsiteCSOM(_logger, _appInfo).GetAsync(siteCollectionResult.SiteUrl);
            }
            catch (Exception ex)
            {
                _logger.ReportError("Site", siteCollectionResult.SiteUrl, ex);

                errorResults = new(siteCollectionResult)
                {
                    ErrorMessage = ex.Message,
                };
            }


            if (errorResults != null)
            {
                yield return errorResults;
                yield break;
            }


            else if (collSubsites != null)
            {
                siteCollectionResult.Progress.IncreaseTotalCount(collSubsites.Count);
                foreach (var oSubsite in collSubsites)
                {
                    SPOTenantSiteUrlsRecord resultsSubsite = new(siteCollectionResult.Progress, oSubsite);
                    yield return resultsSubsite;

                    siteCollectionResult.Progress.ProgressUpdateReport();
                }
            }
        }

        internal async IAsyncEnumerable<SPOTenantSiteUrlsRecord> GetAsyncNEW()
        {
            _appInfo.IsCancelled();

            GraphUser signedInUser = await new GetAADUser(_logger, _appInfo).GetSignedInUser();
            string adminUPN = signedInUser.UserPrincipalName;

            await foreach (var resultSiteCollection in GetSiteCollectionsAsyncNEW())
            {
                _appInfo.IsCancelled();
                _logger.LogUI(GetType().Name, $"Processing Site '{resultSiteCollection.SiteUrl}'");

                try
                {
                    await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).SetAsync(resultSiteCollection.SiteUrl, adminUPN);
                }
                catch (Exception ex)
                {
                    _logger.ReportError("Site", resultSiteCollection.SiteUrl, ex);

                    resultSiteCollection.ErrorMessage = ex.Message;
                }

                yield return resultSiteCollection;


                if (string.IsNullOrWhiteSpace(resultSiteCollection.ErrorMessage)) { continue; }


                if (_param.SiteParam.IncludeSubsites)
                {
                    await foreach (var subsite in GetSubsitesAsyncNEW(resultSiteCollection))
                    {
                        _logger.LogUI(GetType().Name, $"Processing Site '{subsite.SiteUrl}'");
                        yield return subsite;
                    }
                }


                if (_param.RemoveAdmin)
                {
                    try
                    {
                        await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).RemoveAsync(resultSiteCollection.SiteUrl, adminUPN);
                    }
                    catch (Exception ex)
                    {
                        _logger.ReportError("Site", resultSiteCollection.SiteUrl, ex);

                        resultSiteCollection.ErrorMessage = ex.Message;

                    }
                    if (!string.IsNullOrWhiteSpace(resultSiteCollection.ErrorMessage))
                    {
                        yield return resultSiteCollection;
                    }
                }
            }
        }
    }
}
