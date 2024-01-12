//using Microsoft.Online.SharePoint.TenantAdministration;
//using Microsoft.SharePoint.Client;
//using NovaPointLibrary.Commands.Authentication;
//using NovaPointLibrary.Commands.SharePoint.List;
//using NovaPointLibrary.Commands.SharePoint.Site;
//using NovaPointLibrary.Solutions;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace NovaPointLibrary.Commands.SharePoint.Utilities
//{
//    // TO BE DEPRECATED
//    internal class SPOSiteProcessor
//    {
//        private readonly NPLogger _logger;
//        private readonly Authentication.AppInfo _appInfo;
//        private readonly SPOProcessorParameters _param;

//        internal SPOSiteProcessor(NPLogger logger, Authentication.AppInfo appInfo, SPOProcessorParameters parameters)
//        {
//            _logger = logger;
//            _appInfo = appInfo;
//            _param = parameters;
//        }
        
        
//        internal async IAsyncEnumerable<SPOTenantResults> GetSiteCollections()
//        {
//            _appInfo.IsCancelled();
//            string methodName = $"{GetType().Name}.GetSiteCollections";
//            _logger.LogTxt(methodName, $"Start getting Site Collections'");

//            ProgressTracker progress;
//            if (!String.IsNullOrWhiteSpace(_param.SiteUrl))
//            {
//                Web oSite = await new SPOSiteCSOM(_logger, _appInfo).GetAsync(_param.SiteUrl);

//                progress = new(_logger, 1);

//                SPOTenantResults results = new(progress, oSite.Url);

//                yield return results;
//            }
//            else
//            {
//                List<SiteProperties> collSiteCollections = await new SPOSiteCollectionCSOM(_logger, _appInfo).Get(_param.SiteUrl, _param.IncludeShareSite, _param.IncludePersonalSite, _param.OnlyGroupIdDefined);

//                progress = new(_logger, collSiteCollections.Count);
//                foreach (var oSiteCollection in collSiteCollections)
//                {
//                    SPOTenantResults results = new(progress ,oSiteCollection.Url);
//                    yield return results;
//                }
//            }
//            _logger.LogTxt(methodName, $"Finish getting Site Collections'");
//        }

//        internal async IAsyncEnumerable<SPOTenantResults> GetSubsites(SPOTenantResults siteCollectionResult)
//        {
//            _appInfo.IsCancelled();
//            string methodName = $"{GetType().Name}.GetSubsites";
//            _logger.LogTxt(methodName, $"Start getting Subsites for '{siteCollectionResult.SiteUrl}'");

//            SPOTenantResults? errorResults = null;
//            List<Web>? collSubsites = null;
//            try
//            {
//                collSubsites = await new SPOSubsiteCSOM(_logger, _appInfo).Get(siteCollectionResult.SiteUrl);
//            }
//            catch (Exception ex)
//            {
//                _logger.ReportError("Site", siteCollectionResult.SiteUrl, ex);

//                errorResults = new(siteCollectionResult.Progress, siteCollectionResult.SiteUrl);
//                errorResults.Remarks = ex.Message;
//            }

//            if(errorResults != null)
//            {
//                yield return errorResults;
//            }
//            else if(collSubsites != null)
//            {
//                siteCollectionResult.Progress.IncreaseTotalCount(collSubsites.Count);
//                foreach (var oSubsite in collSubsites)
//                {
//                    SPOTenantResults results = new(siteCollectionResult.Progress, oSubsite.Url);
//                    yield return results;

//                    siteCollectionResult.Progress.ProgressUpdateReport();
//                }
//            }

//            _logger.LogTxt(methodName, $"Finish getting Subsites for '{siteCollectionResult}'");
//        }

//        internal async IAsyncEnumerable<SPOTenantResults> GetSites()
//        {
//            _appInfo.IsCancelled();
//            string methodName = $"{GetType().Name}.GetSites";
//            _logger.LogTxt(methodName, $"Start getting Sites");

//            await foreach(SPOTenantResults SiteCollection in GetSiteCollections())
//            {
//                SPOTenantResults? errorResults = null;
//                try
//                {
//                    await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).Set(SiteCollection.SiteUrl, _param.AdminUPN);
//                }
//                catch (Exception ex)
//                {
//                    _logger.ReportError("Site", SiteCollection.SiteUrl, ex);

//                    errorResults = new(SiteCollection.Progress, SiteCollection.SiteUrl);
//                    errorResults.Remarks = ex.Message;
//                }

//                if (errorResults != null)
//                {
//                    yield return errorResults;
//                    continue;
//                }
//                else
//                {
//                    yield return SiteCollection;
                    
//                    if (!_param.IncludeSubsites)
//                    {
//                        await foreach(SPOTenantResults subsite in GetSubsites(SiteCollection))
//                        {
//                            yield return subsite;
//                        }
//                    }
//                }

//                try
//                {
//                    if (_param.RemoveAdmin)
//                    {
//                        await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).Remove(SiteCollection.SiteUrl, _param.AdminUPN);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    _logger.ReportError("Site", SiteCollection.SiteUrl, ex);
//                }
//            }
//        }

//        internal async IAsyncEnumerable<SPOTenantResults> GetLists()
//        {
//            _appInfo.IsCancelled();
//            string methodName = $"{GetType().Name}.GetLists";

//            await foreach(SPOTenantResults siteResults in GetSites())
//            {
//                _logger.LogTxt(methodName, $"Start getting Lists for '{siteResults.SiteUrl}'");

//                if (!String.IsNullOrWhiteSpace(siteResults.Remarks))
//                {
//                    yield return siteResults;
//                    continue;
//                }

//                SPOTenantResults? errorResults = null;
//                List<Microsoft.SharePoint.Client.List>? collList = null;
//                try
//                {
//                    collList = await new SPOListCSOM(_logger, _appInfo).GetAsync(siteResults.SiteUrl, _param.ListTitle, _param.IncludeHiddenLists, _param.IncludeSystemLists);
//                }
//                catch (Exception ex)
//                {
//                    _logger.ReportError("Site", siteResults.SiteUrl, ex);

//                    errorResults = new(siteResults.Progress, siteResults.SiteUrl);
//                    errorResults.Remarks = ex.Message;

//                }

//                if (errorResults != null)
//                {
//                    yield return errorResults;
//                }
//                else if (collList != null)
//                {
//                    ProgressTracker progress = new(siteResults.Progress, collList.Count);
//                    foreach (var oList in collList)
//                    {
//                        SPOTenantResults results = new(progress, siteResults.SiteUrl, oList);
//                        yield return results;

//                        progress.ProgressUpdateReport();
//                    }
//                }
//            }
//        }


//    }
//}
