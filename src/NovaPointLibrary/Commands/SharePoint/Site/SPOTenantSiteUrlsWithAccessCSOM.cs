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

            if (_param.AddAdmin)
            {
                try { await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).SetAsync(record.SiteUrl, adminUPN); }
                catch (Exception ex)
                {
                    _logger.ReportError("Site", record.SiteUrl, ex);
                    record.ErrorMessage = ex.Message;
                }
            }

            yield return record;

            if (!string.IsNullOrWhiteSpace(record.ErrorMessage)) { yield break; }

            if (_param.SiteParam.IncludeSubsites)
            {
                await foreach (var recordSubsite in GetSubsitesAsync(record))
                {
                    yield return recordSubsite;
                }
            }

            if (_param.RemoveAdmin)
            {
                string exceptionMessage = string.Empty;
                try { await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).RemoveAsync(record.SiteUrl, adminUPN); }
                catch (Exception ex) { exceptionMessage = ex.Message; }

                if (!string.IsNullOrWhiteSpace(exceptionMessage))
                {
                    _logger.ReportError("Site", record.SiteUrl, exceptionMessage);
                    record.ErrorMessage = exceptionMessage;
                    yield return record;
                }
            }

        }

        private async IAsyncEnumerable<SPOTenantSiteUrlsRecord> GetSubsitesAsync(SPOTenantSiteUrlsRecord recordSite)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Getting Subsites from '{recordSite.SiteUrl}'");

            List<Web>? collSubsites = null;
            try
            {
                collSubsites = await new SPOSubsiteCSOM(_logger, _appInfo).GetAsync(recordSite.SiteUrl);
            }
            catch (Exception ex)
            {
                _logger.ReportError("Site", recordSite.SiteUrl, ex);

                recordSite.ErrorMessage = ex.Message;
            }

            if (!string.IsNullOrWhiteSpace(recordSite.ErrorMessage))
            {
                yield return recordSite;
                yield break;
            }

            else if (collSubsites != null)
            {
                recordSite.Progress.IncreaseTotalCount(collSubsites.Count);
                foreach (var oSubsite in collSubsites)
                {
                    _logger.LogUI(GetType().Name, $"Processing Subsite '{oSubsite.Url}'");

                    SPOTenantSiteUrlsRecord resultsSubsite = new(recordSite.Progress, oSubsite);
                    yield return resultsSubsite;

                    recordSite.Progress.ProgressUpdateReport();
                }
            }
        }

    }
}
