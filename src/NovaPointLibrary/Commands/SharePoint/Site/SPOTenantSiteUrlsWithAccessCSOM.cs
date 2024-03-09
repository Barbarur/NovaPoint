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

        //private async IAsyncEnumerable<SPOTenantSiteUrlsRecord> GetSiteCollectionsAsync()
        //{
        //    _appInfo.IsCancelled();
        //    _logger.LogTxt(GetType().Name, $"Getting Site Collections");

        //    ProgressTracker progress;

        //    if (_param.SiteParam.AllSiteCollections)
        //    {
        //        List<SiteProperties> collSiteCollections = await new SPOSiteCollectionCSOM(_logger, _appInfo).GetAsync(_param.SiteParam.SiteUrl, _param.SiteParam.IncludeShareSite, _param.SiteParam.IncludePersonalSite, _param.SiteParam.OnlyGroupIdDefined);

        //        progress = new(_logger, collSiteCollections.Count);
        //        foreach (var oSiteCollection in collSiteCollections)
        //        {
        //            yield return new SPOTenantSiteUrlsRecord(progress, oSiteCollection);

        //            progress.ProgressUpdateReport();
        //        }

        //    }
        //    else if (!String.IsNullOrWhiteSpace(_param.SiteParam.SiteUrl))
        //    {
        //        Web oWeb = await new SPOWebCSOM(_logger, _appInfo).GetAsync(_param.SiteParam.SiteUrl);

        //        progress = new(_logger, 1);

        //        yield return new SPOTenantSiteUrlsRecord(progress, oWeb);

        //        progress.ProgressUpdateReport();
        //    }

        //    else if (!String.IsNullOrWhiteSpace(_param.SiteParam.ListOfSitesPath))
        //    {
        //        if (System.IO.File.Exists(_param.SiteParam.ListOfSitesPath))
        //        {
        //            IEnumerable<string> lines = System.IO.File.ReadLines(@$"{_param.SiteParam.ListOfSitesPath}");
        //            progress = new(_logger, lines.Count());
        //            foreach (string line in lines)
        //            {
        //                Web? oWeb = null;
        //                try
        //                {
        //                    oWeb = await new SPOWebCSOM(_logger, _appInfo).GetAsync(line);
        //                }
        //                catch (Exception ex)
        //                {
        //                    _logger.ReportError("Site", $"{line}", ex.Message);
        //                }

        //                if (oWeb != null) { yield return new SPOTenantSiteUrlsRecord(progress, oWeb); }
        //            }
        //        }
        //        else
        //        {
        //            throw new Exception("The file with the list of sites does not exists");
        //        }
        //    }
        //}

        

        //internal async IAsyncEnumerable<SPOTenantSiteUrlsRecord> GetAsyncOLD()
        //{
        //    _appInfo.IsCancelled();

        //    GraphUser signedInUser = await new GetAADUser(_logger, _appInfo).GetSignedInUser();
        //    string adminUPN = signedInUser.UserPrincipalName;

        //    await foreach (var resultSiteCollection in GetSiteCollectionsAsync())
        //    {
        //        _appInfo.IsCancelled();
        //        _logger.LogUI(GetType().Name, $"Processing Site '{resultSiteCollection.SiteUrl}'");

        //        try
        //        {
        //            await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).SetAsync(resultSiteCollection.SiteUrl, adminUPN);
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.ReportError("Site", resultSiteCollection.SiteUrl, ex);

        //            resultSiteCollection.ErrorMessage = ex.Message;
        //        }

        //        yield return resultSiteCollection;


        //        if (string.IsNullOrWhiteSpace(resultSiteCollection.ErrorMessage)) { continue; }


        //        if (_param.SiteParam.IncludeSubsites)
        //        {
        //            await foreach (var subsite in GetSubsitesAsync(resultSiteCollection))
        //            {
        //                _logger.LogUI(GetType().Name, $"Processing Site '{subsite.SiteUrl}'");
        //                yield return subsite;
        //            }
        //        }


        //        if (_param.RemoveAdmin)
        //        {
        //            try
        //            {
        //                await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).RemoveAsync(resultSiteCollection.SiteUrl, adminUPN);
        //            }
        //            catch (Exception ex)
        //            {
        //                _logger.ReportError("Site", resultSiteCollection.SiteUrl, ex);

        //                resultSiteCollection.ErrorMessage = ex.Message;

        //            }
        //            if (!string.IsNullOrWhiteSpace(resultSiteCollection.ErrorMessage))
        //            {
        //                yield return resultSiteCollection;
        //            }
        //        }
        //    }
        //}











        //internal async IAsyncEnumerable<SPOTenantSiteUrlsRecord> GetAsync()
        //{
        //    _appInfo.IsCancelled();

        //    await foreach (var recordSite in GetSitesAsync())
        //    {
        //        yield return recordSite;

        //        if (_param.SiteParam.IncludeSubsites && string.IsNullOrWhiteSpace(recordSite.ErrorMessage))
        //        {
        //            await foreach (var recordSubsite in GetSubsitesAsync(recordSite))
        //            {
        //                yield return recordSubsite;
        //            }
        //        }
        //    }
        //}

        //internal async IAsyncEnumerable<SPOTenantSiteUrlsRecord> GetSitesAsync()
        //{
        //    _appInfo.IsCancelled();

        //    GraphUser signedInUser = await new GetAADUser(_logger, _appInfo).GetSignedInUser();
        //    string adminUPN = signedInUser.UserPrincipalName;

        //    if (_param.SiteParam.AllSiteCollections)
        //    {
        //        await foreach (var result in GetAllSiteCollectionsAsync(adminUPN))
        //        {
        //            _appInfo.IsCancelled();
        //            yield return result;
        //        }
        //    }

        //    else if (!String.IsNullOrWhiteSpace(_param.SiteParam.SiteUrl))
        //    {
        //        ProgressTracker progress = new(_logger, 1);
        //        await foreach (var record in GetRecordFromUrlAsync(progress, _param.SiteParam.SiteUrl, adminUPN))
        //        {
        //            _appInfo.IsCancelled();
        //            yield return record;
        //        }
        //        progress.ProgressUpdateReport();
        //    }
        //    else if (!String.IsNullOrWhiteSpace(_param.SiteParam.ListOfSitesPath))
        //    {
        //        await foreach (var result in GetAllSitesUrlsFromListAsync(adminUPN))
        //        {
        //            _appInfo.IsCancelled();
        //            yield return result;
        //        }
        //    }
        //}

        //private async IAsyncEnumerable<SPOTenantSiteUrlsRecord> GetAllSiteCollectionsAsync(string adminUPN)
        //{
        //    _appInfo.IsCancelled();
        //    _logger.LogTxt(GetType().Name, $"Getting all site collections");

        //    List<SiteProperties> collSiteCollections = await new SPOSiteCollectionCSOM(_logger, _appInfo).GetAsync(_param.SiteParam.IncludeShareSite, _param.SiteParam.IncludePersonalSite, _param.SiteParam.OnlyGroupIdDefined);

        //    ProgressTracker progress = new(_logger, collSiteCollections.Count);
        //    foreach (var oSiteCollection in collSiteCollections)
        //    {
        //        _appInfo.IsCancelled();
        //        _logger.LogUI(GetType().Name, $"Processing Site '{oSiteCollection.Url}'");

        //        var record = new SPOTenantSiteUrlsRecord(progress, oSiteCollection);

        //        try { await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).SetAsync(oSiteCollection.Url, adminUPN); }
        //        catch (Exception ex)
        //        {
        //            _logger.ReportError("Site", record.SiteUrl, ex);
        //            record.ErrorMessage = ex.Message;
        //        }

        //        yield return record;

        //        if (!string.IsNullOrWhiteSpace(record.ErrorMessage)) { yield break; }

        //        var recordErrorRemovingAdmin = await RemoveSiteCollectionAdminAsync(record, adminUPN);
        //        if (recordErrorRemovingAdmin != null)
        //        {
        //            yield return recordErrorRemovingAdmin;
        //        }

        //        progress.ProgressUpdateReport();
        //    }
        //}

        //private async IAsyncEnumerable<SPOTenantSiteUrlsRecord> GetAllSitesUrlsFromListAsync(string adminUPN)
        //{
        //    _appInfo.IsCancelled();
        //    _logger.LogTxt(GetType().Name, $"Getting sites from list file");

        //    if (System.IO.File.Exists(_param.SiteParam.ListOfSitesPath))
        //    {
        //        IEnumerable<string> lines = System.IO.File.ReadLines(@$"{_param.SiteParam.ListOfSitesPath}");
                
        //        int count = lines.Count();
        //        _logger.LogUI(GetType().Name, $"Collected {count} Site from file");

        //        ProgressTracker progress = new(_logger, count);
        //        foreach (string line in lines)
        //        {
        //            _appInfo.IsCancelled();

        //            await foreach(var record in GetRecordFromUrlAsync(progress, line, adminUPN))
        //            {
        //                yield return record;
        //            }

        //            progress.ProgressUpdateReport();
        //        }
        //    }
        //    else
        //    {
        //        throw new Exception("The file with the list of sites does not exists");
        //    }
        //}

        //private async IAsyncEnumerable<SPOTenantSiteUrlsRecord> GetRecordFromUrlAsync(ProgressTracker progress, string siteUrl, string adminUPN)
        //{
        //    _appInfo.IsCancelled();
        //    _logger.LogUI(GetType().Name, $"Processing Site '{siteUrl}'");

        //    SPOTenantSiteUrlsRecord record = new(progress, siteUrl);

        //    try { await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).SetAsync(siteUrl, adminUPN); }
        //    catch (Exception ex)
        //    {
        //        _logger.ReportError("Site", siteUrl, ex);
        //        record.ErrorMessage = ex.Message;
        //    }
        //    if (!string.IsNullOrWhiteSpace(record.ErrorMessage))
        //    {
        //        yield return record;
        //        yield break;
        //    }

        //    try
        //    {
        //        var oWeb = await new SPOWebCSOM(_logger, _appInfo).GetAsync(siteUrl);
        //        record = new(progress, oWeb);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.ReportError("Site", siteUrl, ex);
        //        record.ErrorMessage = ex.Message;
        //    }
        //    yield return record;

        //    var removeAdmin = await RemoveSiteCollectionAdminAsync(record, adminUPN);
        //    if (removeAdmin != null)
        //    {
        //        yield return record;
        //    }
        //}

        //private async Task<SPOTenantSiteUrlsRecord?> RemoveSiteCollectionAdminAsync(SPOTenantSiteUrlsRecord record, string adminUPN)
        //{
        //    _appInfo.IsCancelled();

        //    string error = string.Empty;
        //    try
        //    {
        //        await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).RemoveAsync(record.SiteUrl, adminUPN);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.ReportError("Site", record.SiteUrl, ex);
        //        error = ex.Message;
        //    }

        //    if (!string.IsNullOrWhiteSpace(error))
        //    {
        //        SPOTenantSiteUrlsRecord recordError = record.ShallowCopy();
        //        recordError.ErrorMessage = error;
        //        return recordError;
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}









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

            try { await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).SetAsync(record.SiteUrl, adminUPN); }
            catch (Exception ex)
            {
                _logger.ReportError("Site", record.SiteUrl, ex);
                record.ErrorMessage = ex.Message;
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

            if (!_param.RemoveAdmin) { yield break; }

            try { await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).RemoveAsync(record.SiteUrl, adminUPN); }
            catch (Exception ex)
            {
                _logger.ReportError("Site", record.SiteUrl, ex);
                record.ErrorMessage = ex.Message;
            }
            if (!string.IsNullOrWhiteSpace(record.ErrorMessage))
            {
                yield return record;
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
