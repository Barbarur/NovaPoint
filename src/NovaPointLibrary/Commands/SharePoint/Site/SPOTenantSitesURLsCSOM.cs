//using Microsoft.Online.SharePoint.TenantAdministration;
//using Microsoft.SharePoint.Client;
//using NovaPointLibrary.Commands.AzureAD;
//using NovaPointLibrary.Commands.Utilities.GraphModel;
//using NovaPointLibrary.Solutions;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace NovaPointLibrary.Commands.SharePoint.Site
//{
//    internal class SPOTenantSitesURLsCSOM
//    {
//        private readonly NPLogger _logger;
//        private readonly Authentication.AppInfo _appInfo;
//        private readonly SPOTenantSiteUrlsParameters _param;

//        internal SPOTenantSitesURLsCSOM(NPLogger logger, Authentication.AppInfo appInfo, SPOTenantSiteUrlsParameters parameters)
//        {
//            _logger = logger;
//            _appInfo = appInfo;
//            _param = parameters;
//        }

//        private async IAsyncEnumerable<SPOTenantSiteUrlsRecord> GetSiteCollectionsAsync()
//        {
//            _appInfo.IsCancelled();
//            _logger.LogTxt(GetType().Name, $"Getting Site Collections");

//            ProgressTracker progress;
//            if (!String.IsNullOrWhiteSpace(_param.SiteUrl))
//            {
//                Web oSite = await new SPOWebCSOM(_logger, _appInfo).GetAsync(_param.SiteUrl);

//                progress = new(_logger, 1);

//                yield return new SPOTenantSiteUrlsRecord(progress, oSite);

//                progress.ProgressUpdateReport();
//            }
//            else
//            {
//                List<SiteProperties> collSiteCollections = await new SPOSiteCollectionCSOM(_logger, _appInfo).GetAsync(_param.SiteUrl, _param.IncludeShareSite, _param.IncludePersonalSite, _param.OnlyGroupIdDefined);

//                progress = new(_logger, collSiteCollections.Count);
//                foreach (var oSiteCollection in collSiteCollections)
//                {
//                    yield return new SPOTenantSiteUrlsRecord(progress, oSiteCollection);

//                    progress.ProgressUpdateReport();
//                }
//            }
//        }

//        private async IAsyncEnumerable<SPOTenantSiteUrlsRecord> GetSubsitesAsync(SPOTenantSiteUrlsRecord siteCollectionResult)
//        {
//            _appInfo.IsCancelled();
//            _logger.LogTxt(GetType().Name, $"Getting Subsites from '{siteCollectionResult.SiteUrl}'");

//            SPOTenantSiteUrlsRecord? errorResults = null;
//            List<Web>? collSubsites = null;
//            try
//            {
//                collSubsites = await new SPOSubsiteCSOM(_logger, _appInfo).GetAsync(siteCollectionResult.SiteUrl);
//            }
//            catch (Exception ex)
//            {
//                _logger.ReportError("Site", siteCollectionResult.SiteUrl, ex);

//                errorResults = new(siteCollectionResult)
//                {
//                    ErrorMessage = ex.Message,
//                };
//            }


//            if (errorResults != null)
//            {
//                yield return errorResults;
//                yield break;
//            }


//            else if (collSubsites != null)
//            {
//                siteCollectionResult.Progress.IncreaseTotalCount(collSubsites.Count);
//                foreach (var oSubsite in collSubsites)
//                {
//                    SPOTenantSiteUrlsRecord resultsSubsite = new(siteCollectionResult.Progress, oSubsite);
//                    yield return resultsSubsite;

//                    siteCollectionResult.Progress.ProgressUpdateReport();
//                }
//            }
//        }

//        internal async IAsyncEnumerable<SPOTenantSiteUrlsRecord> GetAsync()
//        {
//            _appInfo.IsCancelled();

//            await foreach (var resultSiteCollection in GetSiteCollectionsAsync())
//            {
//                _appInfo.IsCancelled();
//                _logger.LogUI(GetType().Name, $"Processing Site Collection '{resultSiteCollection.SiteUrl}'");

//                yield return resultSiteCollection;

//                if (_param.IncludeSubsites)
//                {
//                    await foreach (var subsite in GetSubsitesAsync(resultSiteCollection))
//                    {
//                        _logger.LogUI(GetType().Name, $"Processing Subsite '{subsite.SiteUrl}'");
//                        yield return subsite;
//                    }
//                }

//            }
//        }
//    }
//}
