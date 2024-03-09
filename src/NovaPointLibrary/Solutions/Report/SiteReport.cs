using AngleSharp.Css.Dom;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.AzureAD;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Permision;
using NovaPointLibrary.Commands.SharePoint.RecycleBin;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.Utilities;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using PnP.Core.Model.SharePoint;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Report
{
    public class SiteReport : ISolution
    {
        public static readonly string s_SolutionName = "Site Collections & Subsites report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-SiteReport";

        private SiteReportParameters _param;
        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private readonly SPOSitePermissionsCSOM _sitePermissions;

        private readonly Expression<Func<Web, object>>[] _webExpressions = new Expression<Func<Web, object>>[]
        {
            w => w.Id,
            w => w.LastItemModifiedDate,
            w => w.ServerRelativeUrl,
            w => w.Title,
            w => w.Url,
            w => w.WebTemplate,
            w => w.LastItemUserModifiedDate,

        };

        private readonly Expression<Func<Microsoft.SharePoint.Client.Site, object>>[] _siteExpressions = new Expression<Func<Microsoft.SharePoint.Client.Site, object>>[]
        {
            s => s.IsHubSite,
            s => s.HubSiteId,
        };

        public SiteReport(SiteReportParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            _param = parameters;
            _param.PermissionsParam.IncludeSiteAccess = false;
            _param.PermissionsParam.IncludeUniquePermissions = false;

            _logger = new(uiAddLog, this.GetType().Name, _param);
            _appInfo = new(_logger, cancelTokenSource);
            _sitePermissions = new(_logger, _appInfo, _param.PermissionsParam);
        }

        public async Task RunAsync()
        {
            try
            {
                await RunScriptAsync();

                _logger.ScriptFinish();
            }
            catch (Exception ex)
            {
                _logger.ScriptFinish(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();

            if (NeedAccess() || !_param.SitesAccParam.SiteParam.AllSiteCollections)
            {
                await ProcessComplexAsync();
            }
            else if (_param.SitesAccParam.SiteParam.AllSiteCollections)
            {
                await ProcessSimpleReportAsync();
            }
            else
            {
                throw new Exception("No matching requirements for the report.");
            }
        }

        private async Task ProcessSimpleReportAsync()
        {
            _appInfo.IsCancelled();

            await foreach (var recordSite in new SPOTenantSiteUrlsCSOM(_logger, _appInfo, _param.SitesAccParam.SiteParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (recordSite.SiteProperties != null)
                {
                    SiteReportRecord siteRecord = new(recordSite.SiteProperties);
                    _logger.RecordCSV(siteRecord);
                }
                else
                {
                    throw new Exception("Site properties is empty");
                }
            }
        }

        private async Task ProcessComplexAsync()
        {
            _appInfo.IsCancelled();

            await foreach (var recordSite in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param.SitesAccParam).GetAsync())
            {
                _appInfo.IsCancelled();


                if (!string.IsNullOrEmpty(recordSite.ErrorMessage))
                {
                    SiteReportRecord siteRecord = new(recordSite.SiteUrl, recordSite.ErrorMessage);
                    _logger.RecordCSV(siteRecord);
                }

                
                else if (recordSite.SiteProperties != null)
                {
                    await ProcessSiteCollectionRecord(recordSite.SiteProperties, recordSite.Progress);
                }
                
                
                else if (recordSite.Web != null)
                {
                    SiteReportRecord siteRecord = new(recordSite.Web);
                    _logger.RecordCSV(siteRecord);
                }
                

                else
                {
                    Web? oWeb = null;
                    try
                    {
                        oWeb = await new SPOWebCSOM(_logger, _appInfo).GetAsync(recordSite.SiteUrl, _webExpressions);
                    }
                    catch (Exception ex)
                    {
                        SiteReportRecord siteRecord = new(recordSite.SiteUrl, ex.Message);
                        _logger.RecordCSV(siteRecord);
                    }


                    if (oWeb == null) { continue;  }


                    if (oWeb.IsSubSite())
                    {
                        SiteReportRecord siteRecord = new(oWeb);
                        _logger.RecordCSV(siteRecord);
                    }
                    else
                    {
                        try
                        {
                            await ProcessSiteProperties(recordSite.SiteUrl, recordSite.Progress);
                        }
                        catch (Exception ex)
                        {
                            SiteReportRecord siteRecord = new(recordSite.SiteUrl, ex.Message);
                            _logger.RecordCSV(siteRecord);
                        }
                    }
                }
            }
        }

        private async Task ProcessSiteProperties(string siteUrl, ProgressTracker progress)
        {
            _appInfo.IsCancelled(); 
            
            var oSiteProperties = await new SPOSiteCollectionCSOM(_logger, _appInfo).GetAsync(siteUrl);

            await ProcessSiteCollectionRecord(oSiteProperties, progress);
        }

        private async Task ProcessSiteCollectionRecord(SiteProperties oSiteCollection, ProgressTracker progress)
        {
            _appInfo.IsCancelled();
            SiteReportRecord siteRecord = new(oSiteCollection);

            if (_param.Detailed)
            {
                var site = await new SPOSiteCSOM(_logger, _appInfo).GetAsync(oSiteCollection.Url, _siteExpressions);

                siteRecord.AddSiteDetails(site);
            }

            if (_param.PermissionsParam.IncludeAdmins)
            {
                await foreach (var admins in _sitePermissions.GetAsync(oSiteCollection.Url, progress))
                {
                    _appInfo.IsCancelled();

                    siteRecord.AddAdmins(admins);
                    _logger.RecordCSV(siteRecord);
                }
            }
            else
            {
                _logger.RecordCSV(siteRecord);
            }
        }




        //private async Task RunScriptAsyncOLD()
        //{
        //    _appInfo.IsCancelled();

        //    GraphUser signedInUser = await new GetAADUser(_logger, _appInfo).GetSignedInUserAsync();
        //    string adminUPN = signedInUser.UserPrincipalName;

        //    List<SiteProperties> collSiteCollections = await new SPOSiteCollectionCSOM(_logger, _appInfo).GetAllAsync(_param.SitesAccParam.SiteParam.IncludeShareSite, _param.SitesAccParam.SiteParam.IncludePersonalSite, _param.SitesAccParam.SiteParam.OnlyGroupIdDefined);

        //    ProgressTracker progress = new(_logger, collSiteCollections.Count);
        //    foreach (var oSiteCollection in collSiteCollections)
        //    {
        //        _appInfo.IsCancelled();
        //        _logger.LogUI(GetType().Name, $"Processing Site Collection '{oSiteCollection.Url}'");

        //        try
        //        {
        //            await AddAdmin(oSiteCollection.Url, adminUPN);
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.ReportError("Site", oSiteCollection.Url, ex);

        //            SiteReportRecord record = new(oSiteCollection.Url, ex.Message);
        //            _logger.RecordCSV(record);

        //            progress.ProgressUpdateReport();
        //            continue;
        //        }

        //        var recordSiteCollection = await GetSiteCollectionRecord(oSiteCollection);
        //        if (_param.PermissionsParam.IncludeAdmins)
        //        {
        //            await foreach (var admins in _sitePermissions.GetAsync(oSiteCollection.Url, progress))
        //            {
        //                _appInfo.IsCancelled();

        //                recordSiteCollection.AddAdmins(admins);
        //                _logger.RecordCSV(recordSiteCollection);
        //            }
        //        }
        //        else
        //        {
        //            _logger.RecordCSV(recordSiteCollection);
        //        }

        //        if (_param.SitesAccParam.SiteParam.IncludeSubsites)
        //        {
        //            try
        //            {
        //                await GetSubsitesAsync(oSiteCollection.Url, progress);
        //            }
        //            catch (Exception ex)
        //            {
        //                _logger.ReportError("Site Collection", oSiteCollection.Url, ex);

        //                SiteReportRecord record = new(oSiteCollection.Url, ex.Message);
        //                _logger.RecordCSV(record);
        //            }
        //        }

        //        try
        //        {
        //            await RemoveAdmin(oSiteCollection.Url, adminUPN);
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.ReportError("Site Collection", oSiteCollection.Url, ex);

        //            SiteReportRecord record = new(oSiteCollection.Url, ex.Message);
        //            _logger.RecordCSV(record);
        //        }

        //        progress.ProgressUpdateReport();
        //    }
        //}
        //private async Task<SiteReportRecord> GetSiteCollectionRecord(SiteProperties oSiteCollection)
        //{
        //    _appInfo.IsCancelled();
        //    SiteReportRecord siteRecord = new(oSiteCollection);

        //    if (_param.Detailed)
        //    {
        //        var site = await new SPOSiteCSOM(_logger, _appInfo).GetAsync(oSiteCollection.Url, _siteExpressions);

        //        siteRecord.AddSiteDetails(site);

        //    }

        //    return siteRecord;
        //}

        //private async Task GetSubsitesAsync(string siteUrl, ProgressTracker parentProgress)
        //{
        //    _appInfo.IsCancelled();
        //    _logger.LogTxt(GetType().Name, $"Getting Subsites for '{siteUrl}'");

        //    List<Web> collSubsites = await new SPOSubsiteCSOM(_logger, _appInfo).GetAsync(siteUrl, _webExpressions);

        //    ProgressTracker progress = new(parentProgress, collSubsites.Count);
        //    foreach (var oSubsite in collSubsites)
        //    {
        //        _appInfo.IsCancelled();
        //        _logger.LogUI(GetType().Name, $"Processing Subsite '{oSubsite.Url}'");

        //        SiteReportRecord siteRecord = new(oSubsite);

        //        _logger.RecordCSV(siteRecord);
                    
        //        progress.ProgressUpdateReport();
        //    }
        //}

        //private async Task AddAdmin(string siteUrl, string adminUPN)
        //{
        //    if (NeedAccess())
        //    {
        //        await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).SetAsync(siteUrl, adminUPN);
        //    }
        //}

        //private async Task RemoveAdmin(string siteUrl, string adminUPN)
        //{
        //    if (NeedAccess() && _param.SitesAccParam.RemoveAdmin)
        //    {
        //        await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).RemoveAsync(siteUrl, adminUPN);
        //    }
        //}

        private bool NeedAccess()
        {
            if (_param.Detailed || _param.PermissionsParam.IncludeAdmins || _param.SitesAccParam.SiteParam.IncludeSubsites)
            {
                return true;
            }
            else { return false; }
        }
    
    }


    internal class SiteReportRecord : ISolutionRecord
    {
        internal string Title { get; set; } = String.Empty;
        internal string SiteUrl { get; set; } = String.Empty;
        internal string GroupId { get; set; } = String.Empty;
        internal string Tempalte { get; set; } = String.Empty;
        internal string IsSubSite { get; set; } = String.Empty;

        internal string StorageQuotaGB { get; set; } = String.Empty;
        internal string StorageUsedGB { get; set; } = String.Empty;
        internal string StorageWarningPercentageLevel { get; set; } = String.Empty;

        internal string LastContentModifiedDate { get; set; } = String.Empty;
        internal string LockState { get; set; } = String.Empty;

        internal string IsHubSite { get; set; } = String.Empty;
        internal string HubSiteId { get; set; } = String.Empty;

        internal string AccessType { get; set; } = String.Empty;
        internal string AccountType { get; set; } = String.Empty;
        internal string Users { get; set; } = String.Empty;
        internal string PermissionLevels { get; set; } = String.Empty;
        
        internal string Remarks { get; set; } = String.Empty;

        internal SiteReportRecord(SiteProperties oSiteCollection)
        {
            Title = oSiteCollection.Title;
            SiteUrl = oSiteCollection.Url;
            GroupId = oSiteCollection.GroupId.ToString();
            Tempalte = oSiteCollection.Template;
            IsSubSite = "FALSE";

            StorageQuotaGB = Math.Round((float)oSiteCollection.StorageMaximumLevel / 1024, 2).ToString();
            StorageUsedGB = Math.Round((float)oSiteCollection.StorageUsage / 1024, 2).ToString();
            StorageWarningPercentageLevel = Math.Round((float)oSiteCollection.StorageWarningLevel / (float)oSiteCollection.StorageMaximumLevel * 100, 2).ToString();

            LastContentModifiedDate = oSiteCollection.LastContentModifiedDate.ToString();
            LockState = oSiteCollection.LockState.ToString();

        }
        internal SiteReportRecord(Web web)
        {
            Title = web.Title;
            SiteUrl = web.Url;
            GroupId = web.Id.ToString();
            Tempalte = web.WebTemplate;
            IsSubSite = web.IsSubSite().ToString();

            LastContentModifiedDate = web.LastItemUserModifiedDate.ToString();
        }
        internal SiteReportRecord(string siteUrl, string errorMessage)
        {
            SiteUrl = siteUrl;
            Remarks = errorMessage;
        }

        internal void AddSiteDetails(Microsoft.SharePoint.Client.Site site)
        {
            IsHubSite = site.IsHubSite.ToString();
            HubSiteId = site.IsHubSite ? site.HubSiteId.ToString() : string.Empty;
        }


        internal void AddAdmins(SPOLocationPermissionsRecord admins)
        {
            AccessType = admins._role.AccessType;
            AccountType = admins._role.AccountType;
            Users = admins._role.Users;
            PermissionLevels = admins._role.PermissionLevels;

            Remarks = admins._role.Remarks;
        }

    }

    public class SiteReportParameters : ISolutionParameters
    {
        public bool Detailed { get; set; } = false;
        public SPOTenantSiteUrlsWithAccessParameters SitesAccParam {  get; set; }
        public SPOSitePermissionsCSOMParameters PermissionsParam {  get; set; }

        public SiteReportParameters(SPOTenantSiteUrlsWithAccessParameters tenantSitesParam, 
                                    SPOSitePermissionsCSOMParameters permissionsParam)
        {
            SitesAccParam = tenantSitesParam;
            PermissionsParam = permissionsParam;
        }
    }
}
