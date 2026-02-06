using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.AzureAD;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Logging;
using NovaPointLibrary.Solutions;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    internal class SPOTenantSiteUrlsWithAccessCSOM
    {
        private readonly LoggerSolution _logger;
        private readonly IAppClient _appInfo;
        private readonly SPOTenantSiteUrlsWithAccessParameters _param;

        internal SPOTenantSiteUrlsWithAccessCSOM(LoggerSolution logger, IAppClient appInfo, SPOTenantSiteUrlsWithAccessParameters parameters)
        {
            _logger = logger;
            _appInfo = appInfo;
            _param = parameters;
        }

        internal async IAsyncEnumerable<SPOTenantSiteUrlsRecord> GetAsync()
        {
            _appInfo.IsCancelled();

            GraphUser signedInUser = await new GetAADUser(_logger, _appInfo).GetSignedInUserAsync();
            string adminUPN = signedInUser.UserPrincipalName;

            await foreach (var recordSite in new SPOTenantSiteUrlsCSOM(_logger, _appInfo, _param.SiteParam).GetAsync())
            {
                await foreach (var record in ProcessSiteAsync(recordSite, adminUPN)) { yield return  record; }
            }
        }

        internal async IAsyncEnumerable<SPOTenantSiteUrlsRecord> ProcessSiteAsync(SPOTenantSiteUrlsRecord record, string adminUPN)
        {
            _appInfo.IsCancelled();

            if (_param.AdminAccess.AddAdmin)
            {
                try { await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).AddAsync(record.SiteUrl, adminUPN); }
                catch (Exception ex)
                {
                    _logger.Error(GetType().Name, "Site", record.SiteUrl, ex);
                    record.Ex = ex;
                }
            }

            yield return record;

            if ( record.Ex != null ) { yield break; }

            if (_param.SiteParam.IncludeSubsites)
            {
                await foreach (var recordSubsite in GetSubsitesAsync(record))
                {
                    yield return recordSubsite;
                }
            }

            if (_param.AdminAccess.RemoveAdmin)
            {
                try { await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).RemoveAsync(record.SiteUrl, adminUPN); }
                catch (Exception ex)
                { 
                    record.Ex = ex;
                }

                if (record.Ex != null)
                {
                    _logger.Error(GetType().Name, "Site", record.SiteUrl, record.Ex);
                    yield return record;
                }
            }

        }

        private async IAsyncEnumerable<SPOTenantSiteUrlsRecord> GetSubsitesAsync(SPOTenantSiteUrlsRecord recordSite)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Getting Subsites from '{recordSite.SiteUrl}'");

            List<Web>? collSubsites = null;
            try
            {
                collSubsites = await new SPOSubsiteCSOM(_logger, _appInfo).GetAsync(recordSite.SiteUrl, _param.SiteParam.WebExpressions);
            }
            catch (Exception ex)
            {
                _logger.Error(GetType().Name, "Site", recordSite.SiteUrl, ex);

                recordSite.Ex = ex;
            }

            if (recordSite.Ex != null)
            {
                yield return recordSite;
                yield break;
            }

            else if (collSubsites != null)
            {
                ProgressTracker progress = new(recordSite.Progress, collSubsites.Count + 1);
                progress.ProgressUpdateReport();
                foreach (var oSubsite in collSubsites)
                {
                    _logger.Info(GetType().Name, $"Processing Subsite '{oSubsite.Url}'");

                    SPOTenantSiteUrlsRecord resultsSubsite = new(progress, oSubsite);
                    yield return resultsSubsite;

                    progress.ProgressUpdateReport();
                }
            }
        }

    }
}
