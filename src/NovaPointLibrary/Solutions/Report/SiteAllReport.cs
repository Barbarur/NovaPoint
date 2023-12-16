using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPoint.Commands.Site;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Permision;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.Site;
using NovaPointLibrary.Solutions.Reports;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Report
{
    public class SiteAllReport
    {
        public static string _solutionName = "Site Collections & Subsites report";
        public static string _solutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-SiteAllReport";

        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private readonly string AdminUPN = "";
        private readonly bool RemoveAdmin = false;
 
        private readonly bool IncludePersonalSite = false;
        private readonly bool IncludeShareSite = true;
        private readonly bool GroupIdDefined = false;

        private readonly bool IncludeAdmins = false;
        private readonly bool IncludeSiteAccess = false;
        private readonly bool IncludeSubsites = false;

        List<SPORoleAssignmentKnownGroup> KnownGroups { get; set; } = new() { };

        public SiteAllReport(Action<LogInfo> uiAddLog, Commands.Authentication.AppInfo appInfo, SiteAllReportParameters parameters)
        {
            _logger = new(uiAddLog, "Reports", GetType().Name);
            _appInfo = appInfo;

            IncludeShareSite = parameters.IncludeShareSite;
            IncludePersonalSite = parameters.IncludePersonalSite;
            GroupIdDefined = parameters.GroupIdDefined;

            AdminUPN = parameters.AdminUPN;
            RemoveAdmin = parameters.RemoveAdmin;

            IncludeAdmins = parameters.IncludeAdmins;
            IncludeSiteAccess = parameters.IncludeSiteAccess;
            IncludeSubsites = parameters.IncludeSubsites;

        }

        public async Task RunAsync()
        {
            try
            {
                if ( ( IncludeAdmins || IncludeSiteAccess || IncludeSubsites ) && String.IsNullOrWhiteSpace(AdminUPN) )
                {
                    string message = "FORM INCOMPLETED: Admin UPN cannot be empty if you want to include Site Collection Administrators, Site Access or Subsites";
                    Exception ex = new(message);
                    throw ex;
                }
                else if ( !IncludeShareSite && GroupIdDefined )
                {
                    string message = $"FORM INCOMPLETED: If you want to get GroupIdDefined Sites, you want to include ShareSites";
                    Exception ex = new(message);
                    throw ex;
                }
                else if ( RemoveAdmin && String.IsNullOrWhiteSpace(AdminUPN) )
                {
                    string message = "FORM INCOMPLETED: Admin UPN cannot be empty if you need to remove the user as User Admin as Site Collection Administraitor";
                    Exception ex = new(message);
                    throw ex;
                }
                else
                {
                    await RunScriptAsyncUSINGSITEPERMISIONS();
                }
            }
            catch (Exception ex)
            {
                _logger.ScriptFinish(ex);
            }
        }



        private async Task RunScriptAsyncUSINGSITEPERMISIONS()
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.AllSitesAsync";
            _logger.ScriptStartNotice();

            string spoAdminAccessToken = await new GetAccessToken(_logger, _appInfo).SpoInteractiveAsync(_appInfo.AdminUrl);
            string aadAccessToken = String.Empty;
            string? spoRootPersonalSiteAccessToken = String.Empty;
            string? spoRootShareSiteAccessToken = String.Empty;
            if (IncludeAdmins || IncludeSiteAccess || IncludeSubsites)
            {
                aadAccessToken = await new GetAccessToken(_logger, _appInfo).GraphInteractiveAsync();
                if (IncludePersonalSite) { spoRootPersonalSiteAccessToken = await new GetAccessToken(_logger, _appInfo).SpoInteractiveAsync(_appInfo.RootPersonalUrl); }
                if (IncludeShareSite) { spoRootShareSiteAccessToken = await new GetAccessToken(_logger, _appInfo).SpoInteractiveAsync(_appInfo.RootSharedUrl); }

            }

            List<SiteProperties> collSiteCollections = new GetSiteCollection(_logger, spoAdminAccessToken).CSOM_AdminAll(_appInfo.AdminUrl, IncludePersonalSite, GroupIdDefined);
            collSiteCollections.RemoveAll(s => s.Title == "" || s.Template.Contains("Redirect"));
            if (!IncludePersonalSite) { collSiteCollections.RemoveAll(s => s.Template.Contains("SPSPERS")); }
            if (!IncludeShareSite) { collSiteCollections.RemoveAll(s => !s.Template.Contains("SPSPERS")); }

            ProgressTracker progress = new(_logger, collSiteCollections.Count);
            foreach (SiteProperties oSiteCollection in collSiteCollections)
            {
                _appInfo.IsCancelled();

                _logger.LogTxt(methodName, $"Processing Site '{oSiteCollection.Title}'");

                string? currentSiteAccessToken = oSiteCollection.Url.Contains("-my.sharepoint.com") ? spoRootPersonalSiteAccessToken : spoRootShareSiteAccessToken;

                try
                {

                    if (IncludeAdmins || IncludeSiteAccess || IncludeSubsites)
                    {
                        new SetSiteCollectionAdmin(_logger, spoAdminAccessToken, _appInfo.Domain).Add(AdminUPN, oSiteCollection.Url);

                        GetSPOSitePermissions getPermissions = new(_logger, _appInfo, currentSiteAccessToken, aadAccessToken, KnownGroups);

                        if (IncludeAdmins || IncludeSiteAccess)
                        {
                            Web oSite = new GetSPOSite(_logger, _appInfo, currentSiteAccessToken).CSOMWithRoles(oSiteCollection.Url);

                            AddSiteListRecordToCSVWITHPERMISSIONS(oSiteCollection, null, await getPermissions.CSOMSiteAsync(oSite, IncludeAdmins, IncludeSiteAccess, false, false, false));
                        }
                        else
                        {
                            SPORoleAssignmentRecord blankPermissions = new();
                            AddSiteRecordToCSVWITHPERMISSIONS(oSiteCollection, null, blankPermissions);
                        }

                        if (IncludeSubsites)
                        {
                            var collSubsites = new GetSubsite(_logger, _appInfo, currentSiteAccessToken).CsomAllSubsitesWithRolesAndSiteDetails(oSiteCollection.Url);
                            ProgressTracker progressSubsite = new(progress, collSubsites.Count);
                            foreach (var oSubsite in collSubsites)
                            {
                                _logger.LogTxt(methodName, $"Processing Subsite '{oSubsite.Title}'");

                                if (IncludeSiteAccess)
                                {
                                    AddSiteListRecordToCSVWITHPERMISSIONS(null, oSubsite, await getPermissions.CSOMSubsiteAsync(oSubsite, IncludeSiteAccess, false, false, false));

                                }
                                else
                                {
                                    SPORoleAssignmentRecord blankPermissions = new();
                                    AddSiteRecordToCSVWITHPERMISSIONS(null, oSubsite, blankPermissions);
                                }
                                progressSubsite.ProgressUpdateReport();
                            }
                        }
                        if (RemoveAdmin)
                        {
                            new RemoveSiteCollectionAdmin(_logger, spoAdminAccessToken, _appInfo.Domain).Csom(oSiteCollection.Url, AdminUPN);
                        }
                    }
                    else
                    {
                        SPORoleAssignmentRecord blankPermissions = new();
                        AddSiteRecordToCSVWITHPERMISSIONS(oSiteCollection, null, blankPermissions);
                    }
                }
                catch (Exception ex)
                {
                    _logger.AddLogToUI($"Error processing Site Collection '{oSiteCollection.Url}'");
                    _logger.AddLogToTxt($"Exception: {ex.Message}");
                    _logger.AddLogToTxt($"Trace: {ex.StackTrace}");

                    SPORoleAssignmentRecord blankPermissions = new("", "", "", "", ex.Message);
                    AddSiteRecordToCSVWITHPERMISSIONS(oSiteCollection, null, blankPermissions);
                }
                progress.ProgressUpdateReport();
            }

            _logger.ScriptFinish();
        }

        private void AddSiteListRecordToCSVWITHPERMISSIONS(SiteProperties? siteCollection, Web? subsiteWeb, List<SPOLocationPermissionsRecord> recordsList)
        {
            _appInfo.IsCancelled();
            _logger.AddLogToTxt($"[{GetType().Name}.AddSiteListRecordToCSVWITHPERMISSIONS] - Adding Site record");

            foreach (var record in recordsList)
            {
                foreach (var roleAssigmentUser in record.SPORoleAssignmentUsersList)
                {
                    AddSiteRecordToCSVWITHPERMISSIONS(siteCollection, subsiteWeb, roleAssigmentUser);
                }
            }
        }

        private void AddSiteRecordToCSVWITHPERMISSIONS(SiteProperties? siteCollection, Web? subsiteWeb, SPORoleAssignmentRecord permissionRecord)
        {
            _appInfo.IsCancelled();
            _logger.AddLogToTxt($"[{GetType().Name}.AddSiteRecordToCSV] - Adding Site record");

            dynamic record = new ExpandoObject();
            record.Title = siteCollection != null ? siteCollection?.Title : subsiteWeb?.Title;
            record.SiteUrl = siteCollection != null ? siteCollection?.Url : subsiteWeb?.Url;
            record.GroupId = siteCollection != null ? siteCollection?.GroupId.ToString() : string.Empty;
            record.Tempalte = siteCollection != null ? siteCollection?.Template : subsiteWeb?.WebTemplate;

            record.StorageQuotaGB = siteCollection != null ? Math.Round(((float)siteCollection.StorageMaximumLevel / 1024), 2).ToString() : string.Empty;
            record.StorageUsedGB = siteCollection != null ? Math.Round(((float)siteCollection.StorageUsage / 1024), 2).ToString() : string.Empty; // ADD CONSUMPTION FOR SUBSITES
            record.storageWarningPercentageLevelGB = siteCollection != null ? Math.Round( (float)siteCollection.StorageWarningLevel / (float)siteCollection.StorageMaximumLevel * 100, 2).ToString() : string.Empty;

            record.IsHubSite = siteCollection != null ? siteCollection?.IsHubSite.ToString() : "False";
            record.LastContentModifiedDate = siteCollection != null ? siteCollection?.LastContentModifiedDate.ToString() : subsiteWeb?.LastItemModifiedDate.ToString();
            record.LockState = siteCollection != null ? siteCollection?.LockState.ToString() : string.Empty;

            record.AccessType = permissionRecord.AccessType;
            record.AccountType = permissionRecord.AccountType;
            record.User = permissionRecord.Users;
            record.PermissionsLevel = permissionRecord.PermissionLevels;

            record.Remarks = permissionRecord.Remarks;

            _logger.RecordCSV(record);
        }
    }


    public class SiteAllReportParameters
    {
        public bool IncludePersonalSite { get; set; } = false;
        public bool IncludeShareSite { get; set; } = true;
        public bool GroupIdDefined { get; set; } = false;
        
        public string AdminUPN { get; set; } = "";
        public bool RemoveAdmin { get; set; } = false;
        
        public bool IncludeAdmins { get; set; } = false;
        public bool IncludeSiteAccess { get; set; } = false;
        public bool IncludeSubsites { get; set; } = false;

    }
}
