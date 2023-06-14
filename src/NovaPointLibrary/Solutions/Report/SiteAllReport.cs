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
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Report
{
    public class SiteAllReport
    {
        public static string _solutionName = "Report of all Site Collections and Subsites";
        public static string _solutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/";

        private readonly LogHelper _logHelper;
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
            _logHelper = new(uiAddLog, "Reports", GetType().Name);
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
                _logHelper.ScriptFinishErrorNotice(ex);
            }
        }



        private async Task RunScriptAsyncUSINGSITEPERMISIONS()
        {
            _appInfo.IsCancelled();

            _logHelper.ScriptStartNotice();

            string aadAccessToken = await new GetAccessToken(_logHelper, _appInfo).GraphInteractiveAsync();
            string spoAdminAccessToken = await new GetAccessToken(_logHelper, _appInfo).SpoInteractiveAsync(_appInfo._adminUrl);
            string? spoRootPersonalSiteAccessToken = String.Empty;
            string? spoRootShareSiteAccessToken = String.Empty;
            if (IncludeAdmins || IncludeSiteAccess || IncludeSubsites)
            {

                if (IncludePersonalSite) { spoRootPersonalSiteAccessToken = await new GetAccessToken(_logHelper, _appInfo).SpoInteractiveAsync(_appInfo._rootPersonalUrl); }
                if (IncludeShareSite) { spoRootShareSiteAccessToken = await new GetAccessToken(_logHelper, _appInfo).SpoInteractiveAsync(_appInfo._rootSharedUrl); }

            }

            List<SiteProperties> collSiteCollections = new GetSiteCollection(_logHelper, spoAdminAccessToken).CSOM_AdminAll(_appInfo._adminUrl, IncludePersonalSite, GroupIdDefined);
            collSiteCollections.RemoveAll(s => s.Title == "" || s.Template.Contains("Redirect"));
            if (!IncludePersonalSite) { collSiteCollections.RemoveAll(s => s.Template.Contains("SPSPERS")); }
            if (!IncludeShareSite) { collSiteCollections.RemoveAll(s => !s.Template.Contains("SPSPERS")); }

            double counter = 0;
            float counterStep = 1 / collSiteCollections.Count;
            foreach (SiteProperties oSiteCollection in collSiteCollections)
            {
                _appInfo.IsCancelled();

                //if (oSiteCollection.Url != "https://m365x88421522.sharepoint.com/sites/35581560BirendarKumar") { continue; }

                double progress = Math.Round(counter * 100 / collSiteCollections.Count, 2);
                counter++;
                _logHelper.AddProgressToUI(progress);
                _logHelper.AddLogToUI($"Processing Site Collection '{oSiteCollection.Title}'");

                string? currentSiteAccessToken = oSiteCollection.Url.Contains("-my.sharepoint.com") ? spoRootPersonalSiteAccessToken : spoRootShareSiteAccessToken;

                try
                {

                    if (IncludeAdmins || IncludeSiteAccess || IncludeSubsites)
                    {
                        new SetSiteCollectionAdmin(_logHelper, spoAdminAccessToken, _appInfo._domain).Add(AdminUPN, oSiteCollection.Url);

                        GetSPOSitePermissions getPermissions = new(_logHelper, _appInfo, currentSiteAccessToken, aadAccessToken, KnownGroups);

                        if (IncludeAdmins || IncludeSiteAccess)
                        {
                            Web oSite = new GetSPOSite(_logHelper, _appInfo, currentSiteAccessToken).CSOMWithRoles(oSiteCollection.Url);

                            AddSiteListRecordToCSVWITHPERMISSIONS(oSiteCollection, null, await getPermissions.CSOMSiteAsync(oSite, IncludeAdmins, IncludeSiteAccess, false, false, false));
                        }
                        else
                        {
                            SPORoleAssignmentRecord blankPermissions = new();
                            AddSiteRecordToCSVWITHPERMISSIONS(oSiteCollection, null, blankPermissions);
                        }

                        if (IncludeSubsites)
                        {
                            var collSubsites = new GetSubsite(_logHelper, _appInfo, currentSiteAccessToken).CsomAllSubsitesWithRolesAndSiteDetails(oSiteCollection.Url);
                            foreach (var oSubsite in collSubsites)
                            {
                                progress = Math.Round( progress + ( counterStep * 100 / ( collSubsites.Count + 1) ), 2);
                                _logHelper.AddProgressToUI(progress);
                                _logHelper.AddLogToUI($"Processing SubSite '{oSubsite.Title}' Pregress {progress}");
                                _logHelper.AddLogToUI($"Processing SubSite '{oSubsite.Title}' COunterStep {counterStep}");
                                _logHelper.AddLogToUI($"Processing SubSite '{oSubsite.Title}'");

                                if (IncludeSiteAccess)
                                {
                                    AddSiteListRecordToCSVWITHPERMISSIONS(null, oSubsite, await getPermissions.CSOMSubsiteAsync(oSubsite, IncludeSiteAccess, false, false, false));

                                }
                                else
                                {
                                    SPORoleAssignmentRecord blankPermissions = new();
                                    AddSiteRecordToCSVWITHPERMISSIONS(null, oSubsite, blankPermissions);
                                }
                            }
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
                    _logHelper.AddLogToUI($"Error processing Site Collection '{oSiteCollection.Url}'");
                    _logHelper.AddLogToTxt($"Exception: {ex.Message}");
                    _logHelper.AddLogToTxt($"Trace: {ex.StackTrace}");

                    SPORoleAssignmentRecord blankPermissions = new("", "", "", "", ex.Message);
                    AddSiteRecordToCSVWITHPERMISSIONS(oSiteCollection, null, blankPermissions);
                }
            }

            _logHelper.ScriptFinishSuccessfulNotice();
        }



        //private async Task RunScriptAsyncNew()
        //{
        //    _appInfo.IsCancelled();

        //    _logHelper.ScriptStartNotice();

        //    string aadAccessToken = await new GetAccessToken(_logHelper, _appInfo).GraphInteractiveAsync();
        //    string spoAdminAccessToken = await new GetAccessToken(_logHelper, _appInfo).SpoInteractiveAsync(_appInfo._adminUrl);
        //    string? spoRootPersonalSiteAccessToken = String.Empty;
        //    string? spoRootShareSiteAccessToken = String.Empty;
        //    if (IncludeAdmins || IncludeSiteAccess || IncludeSubsites)
        //    {

        //        if (IncludePersonalSite) { spoRootPersonalSiteAccessToken = await new GetAccessToken(_logHelper, _appInfo).SpoInteractiveAsync(_appInfo._rootPersonalUrl); }
        //        if (IncludeShareSite) { spoRootShareSiteAccessToken = await new GetAccessToken(_logHelper, _appInfo).SpoInteractiveAsync(_appInfo._rootSharedUrl); }

        //    }

        //    List<SiteProperties> collSiteCollections = new GetSiteCollection(_logHelper, spoAdminAccessToken).CSOM_AdminAll(_appInfo._adminUrl, IncludePersonalSite, GroupIdDefined);
        //    collSiteCollections.RemoveAll(s => s.Title == "" || s.Template.Contains("Redirect"));
        //    if (!IncludePersonalSite) { collSiteCollections.RemoveAll(s => s.Template.Contains("SPSPERS")); }
        //    if (!IncludeShareSite) { collSiteCollections.RemoveAll(s => !s.Template.Contains("SPSPERS")); }

        //    double counter = 0;
        //    double counterStep = 1 / collSiteCollections.Count;
        //    foreach (SiteProperties oSiteCollection in collSiteCollections)
        //    {
        //        _appInfo.IsCancelled();

        //        //if (oSiteCollection.Url != "https://m365x88421522.sharepoint.com/sites/35581560BirendarKumar") { continue; }

        //        double progress = Math.Round(counter * 100 / collSiteCollections.Count, 2);
        //        counter++;
        //        _logHelper.AddProgressToUI(progress);
        //        _logHelper.AddLogToUI($"Processing Site Collection '{oSiteCollection.Title}'");

        //        string? currentSiteAccessToken = oSiteCollection.Url.Contains("-my.sharepoint.com") ? spoRootPersonalSiteAccessToken : spoRootShareSiteAccessToken;

        //        try
        //        {

        //            if (IncludeAdmins || IncludeSiteAccess || IncludeSubsites)
        //            {
        //                new SetSiteCollectionAdmin(_logHelper, spoAdminAccessToken, _appInfo._domain).Add(AdminUPN, oSiteCollection.Url);
        //            }

        //            if (IncludeAdmins && currentSiteAccessToken != null)
        //            {
        //                await GetAdmins(currentSiteAccessToken, aadAccessToken, oSiteCollection);
        //            }

        //            if (IncludeSiteAccess && currentSiteAccessToken != null)
        //            {
        //                Web siteWeb = new Commands.SharePoint.Site.GetSPOSite(_logHelper, _appInfo, currentSiteAccessToken).CSOMWithRoles(oSiteCollection.Url);

        //                var collRoleAssigmentUsers = await new GetSPORoleAssigmentUsers(_logHelper, _appInfo, aadAccessToken, currentSiteAccessToken, oSiteCollection.Url, KnownGroups)
        //                    .GetUsersAsync(siteWeb.RoleAssignments);

        //                foreach (var roleAssigmentUsers in collRoleAssigmentUsers)
        //                {
        //                    SPOLocationPermissionsRecord usersPermissionsRecord = new(roleAssigmentUsers.AccessType, roleAssigmentUsers.AccountType, roleAssigmentUsers.Users, roleAssigmentUsers.PermissionLevels);
        //                    AddSiteRecordToCSV(oSiteCollection, usersPermissionsRecord, roleAssigmentUsers.Remarks);
        //                }

        //                if (collRoleAssigmentUsers.Count == 0)
        //                {
        //                    SPOLocationPermissionsRecord siteAccessBlankPermissions = new("", "", "", "");
        //                    AddSiteRecordToCSV(oSiteCollection, siteAccessBlankPermissions, "No user has access to the site");
        //                }

        //            }

        //            if (IncludeSubsites)
        //            {
        //                var collSubsites = new GetSubsite(_logHelper, _appInfo, currentSiteAccessToken).CsomAllSubsitesWithRolesAndSiteDetails(oSiteCollection.Url);
        //                foreach (var oSubsite in collSubsites)
        //                {
        //                    _logHelper.AddLogToUI($"Processing SubSite '{oSubsite.Title}'");

        //                    try
        //                    {
        //                        if (oSubsite.HasUniqueRoleAssignments)
        //                        {
        //                            _logHelper.AddLogToUI($"SubSite '{oSubsite.Title}' has unique permissions");

        //                            var collRoleAssigmentUsers = await new GetSPORoleAssigmentUsers(_logHelper, _appInfo, aadAccessToken, currentSiteAccessToken, oSubsite.Url, KnownGroups)
        //                                .GetUsersAsync(oSubsite.RoleAssignments);

        //                            foreach (var roleAssigmentUsers in collRoleAssigmentUsers)
        //                            {
        //                                SPOLocationPermissionsRecord usersPermissionsRecord = new(roleAssigmentUsers.AccessType, roleAssigmentUsers.AccountType, roleAssigmentUsers.Users, roleAssigmentUsers.PermissionLevels);
        //                                AddSiteRecordToCSV(oSubsite, usersPermissionsRecord, roleAssigmentUsers.Remarks);
        //                            }
        //                        }
        //                        else
        //                        {
        //                            _logHelper.AddLogToUI($"SubSite '{oSubsite.Title}' inherits permissions");
        //                            SPOLocationPermissionsRecord subsiteAccessBlankPermissions = new("", "", "", "");
        //                            AddSiteRecordToCSV(oSubsite, subsiteAccessBlankPermissions, "Inheriting Permissions");
        //                        }
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logHelper.AddLogToUI($"Error processing Subsite '{oSubsite.Url}'");
        //                        _logHelper.AddLogToTxt($"Exception: {ex.Message}");
        //                        _logHelper.AddLogToTxt($"Trace: {ex.StackTrace}");
        //                        SPOLocationPermissionsRecord blankPermissions = new("", "", "", "");
        //                        AddSiteRecordToCSV(oSubsite, blankPermissions, ex.Message);
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _logHelper.AddLogToUI($"Error processing Site Collection '{oSiteCollection.Url}'");
        //            _logHelper.AddLogToTxt($"Exception: {ex.Message}");
        //            _logHelper.AddLogToTxt($"Trace: {ex.StackTrace}");
        //            SPOLocationPermissionsRecord blankPermissions = new("", "", "", "");
        //            AddSiteRecordToCSV(oSiteCollection, blankPermissions, ex.Message);
        //        }
        //    }

        //    _logHelper.ScriptFinishSuccessfulNotice();
        //}


        //private async Task GetAdmins(string spoAccessToken, string aadAccessToken, SiteProperties siteCollection)
        //{
        //    _appInfo.IsCancelled();
        //    _logHelper.AddLogToTxt($"[{GetType().Name}.GetAdmins] - Getting Admins for Site Collestion '{siteCollection.Url}'");

        //    string accessType = "Direct Permissions";
        //    string permissionLevels = "Site Collection Administrator";

        //    IEnumerable<Microsoft.SharePoint.Client.User> collSiteCollAdmins = new GetSiteCollectionAdmin(_logHelper, spoAccessToken).Csom(siteCollection.Url);

        //    _logHelper.AddLogToTxt($"[{GetType().Name}.GetAdmins] - Provessing users '{siteCollection.Url}'");
        //    string users = String.Join(" ", collSiteCollAdmins.Where(sca => sca.PrincipalType.ToString() == "User").Select(sca => sca.UserPrincipalName).ToList());
        //    SPOLocationPermissionsRecord usersPermissionsRecord = new(accessType, "User", users, permissionLevels);
        //    AddSiteRecordToCSV(siteCollection, usersPermissionsRecord);

        //    _logHelper.AddLogToTxt($"[{GetType().Name}.GetAdmins] - Provessing Security Groups '{siteCollection.Url}'");
        //    var collSecurityGroups = collSiteCollAdmins.Where(sca => sca.PrincipalType.ToString() == "SecurityGroup").ToList();
        //    foreach (var securityGroup in collSecurityGroups)
        //    {
        //        List<SPORoleAssigmentKnownGroup> collKnownGroups = new() { };
        //        collKnownGroups = KnownGroups.Where(kg => kg.PrincipalType == "SecurityGroup" && kg.GroupName == securityGroup.Title && kg.GroupID == securityGroup.AadObjectId.NameId).ToList();

        //        if (collKnownGroups.Count > 0)
        //        {
        //            foreach (var knownGroup in collKnownGroups)
        //            {
        //                usersPermissionsRecord = new(accessType, knownGroup.AccountType, knownGroup.Users, permissionLevels);
        //                AddSiteRecordToCSV(siteCollection, usersPermissionsRecord);
        //            }
        //        }
        //        else
        //        {

        //            var collRoleAssigmentUsers = await new GetSPORoleAssigmentUsers(_logHelper, _appInfo, aadAccessToken, spoAccessToken, siteCollection.Url, KnownGroups)
        //                .GetSecurityGroupUsersReturnsAsync(securityGroup.Title, securityGroup.LoginName, accessType, "", permissionLevels);

        //            foreach (var roleAssigmentUsers in collRoleAssigmentUsers)
        //            {
        //                usersPermissionsRecord = new(roleAssigmentUsers.AccessType, roleAssigmentUsers.AccountType, roleAssigmentUsers.Users, permissionLevels);
        //                AddSiteRecordToCSV(siteCollection, usersPermissionsRecord, roleAssigmentUsers.Remarks);
        //            }
        //        }
        //    }
        //}

        //private void AddSiteRecordToCSV(SiteProperties siteCollection, SPOLocationPermissionsRecord? permissionRecord = null, string remarks = "")
        //{
        //    AddSiteRecordToCSV(siteCollection, null, permissionRecord, remarks);
        //}

        //private void AddSiteRecordToCSV(Web subsiteWeb, SPOLocationPermissionsRecord? permissionRecord = null, string remarks = "")
        //{
        //    AddSiteRecordToCSV(null, subsiteWeb, permissionRecord, remarks);
        //}

        //private void AddSiteRecordToCSV(SiteProperties? siteCollection, Web? subsiteWeb, SPOLocationPermissionsRecord? permissionRecord = null, string remarks = "")
        //{
        //    _appInfo.IsCancelled();
        //    _logHelper.AddLogToTxt($"[{GetType().Name}.AddSiteRecordToCSV] - Adding Site record");

        //    dynamic record = new ExpandoObject();
        //    record.Title = siteCollection != null ? siteCollection?.Title : subsiteWeb?.Title;
        //    record.SiteUrl = siteCollection != null ? siteCollection?.Url : subsiteWeb?.Url;
        //    record.GroupId = siteCollection != null ? siteCollection?.GroupId.ToString() : string.Empty;
        //    record.Tempalte = siteCollection != null ? siteCollection?.Template : subsiteWeb.WebTemplate;

        //    record.StorageQuotaGB = siteCollection != null ? string.Format("{0:N2}", Math.Round(siteCollection.StorageMaximumLevel / Math.Pow(1024, 3), 2)) : string.Empty;
        //    record.StorageUsedGB = siteCollection != null ? string.Format("{0:N2}", Math.Round(siteCollection.StorageUsage / Math.Pow(1024, 3), 2)) : string.Empty; // ADD CONSUMPTION FOR SUBSITES
        //    record.storageWarningLevelGB = siteCollection != null ? string.Format("{0:N2}", Math.Round(siteCollection.StorageWarningLevel / Math.Pow(1024, 3), 2)) : string.Empty;

        //    record.IsHubSite = siteCollection != null ? siteCollection?.IsHubSite.ToString() : "False";
        //    record.LastContentModifiedDate = siteCollection != null ? siteCollection?.LastContentModifiedDate.ToString() : subsiteWeb?.LastItemModifiedDate.ToString();
        //    record.LockState = siteCollection != null ? siteCollection?.LockState.ToString() : string.Empty;

        //    if (permissionRecord != null)
        //    {
        //        record.AccessType = permissionRecord != null ? permissionRecord.AccessType : string.Empty;
        //        record.AccountType = permissionRecord != null ? permissionRecord.AccountType : string.Empty;
        //        record.User = permissionRecord != null ? permissionRecord.Users : string.Empty;
        //        record.PermissionsLevel = permissionRecord != null ? permissionRecord.PermissionLevels : string.Empty;
        //    }

        //    record.Remarks = remarks;

        //    _logHelper.AddRecordToCSV(record);
        //}




        //private void AddSiteRecordToCSVWITHPERMISSIONS(SiteProperties siteCollection, List<SPOLocationPermissionsRecord>? listRecord, string remarks)
        //{
        //    if (listRecord == null)
        //    {
        //        AddSiteRecordToCSVWITHPERMISSIONS(siteCollection, null, null);
        //    }
        //    else
        //    {
        //        foreach (var record in listRecord)
        //        {
        //            AddSiteRecordToCSVWITHPERMISSIONS(siteCollection, null, record);
        //        }
        //    }
        //}

        //private void AddSiteRecordToCSVWITHPERMISSIONS(Web subsiteWeb, List<SPOLocationPermissionsRecord> listRecord)
        //{
        //    AddSiteRecordToCSVWITHPERMISSIONS(null, subsiteWeb, listRecord);
        //}


        private void AddSiteListRecordToCSVWITHPERMISSIONS(SiteProperties? siteCollection, Web? subsiteWeb, List<SPOLocationPermissionsRecord> recordsList)
        {
            _appInfo.IsCancelled();
            _logHelper.AddLogToTxt($"[{GetType().Name}.AddSiteListRecordToCSVWITHPERMISSIONS] - Adding Site record");

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
            _logHelper.AddLogToTxt($"[{GetType().Name}.AddSiteRecordToCSV] - Adding Site record");

            dynamic record = new ExpandoObject();
            record.Title = siteCollection != null ? siteCollection?.Title : subsiteWeb?.Title;
            record.SiteUrl = siteCollection != null ? siteCollection?.Url : subsiteWeb?.Url;
            record.GroupId = siteCollection != null ? siteCollection?.GroupId.ToString() : string.Empty;
            record.Tempalte = siteCollection != null ? siteCollection?.Template : subsiteWeb?.WebTemplate;

            record.StorageQuotaGB = siteCollection != null ? string.Format("{0:N2}", Math.Round(siteCollection.StorageMaximumLevel / Math.Pow(1024, 3), 2)) : string.Empty;
            record.StorageUsedGB = siteCollection != null ? string.Format("{0:N2}", Math.Round(siteCollection.StorageUsage / Math.Pow(1024, 3), 2)) : string.Empty; // ADD CONSUMPTION FOR SUBSITES
            record.storageWarningLevelGB = siteCollection != null ? string.Format("{0:N2}", Math.Round(siteCollection.StorageWarningLevel / Math.Pow(1024, 3), 2)) : string.Empty;

            record.IsHubSite = siteCollection != null ? siteCollection?.IsHubSite.ToString() : "False";
            record.LastContentModifiedDate = siteCollection != null ? siteCollection?.LastContentModifiedDate.ToString() : subsiteWeb?.LastItemModifiedDate.ToString();
            record.LockState = siteCollection != null ? siteCollection?.LockState.ToString() : string.Empty;

            record.AccessType = permissionRecord.AccessType;
            record.AccountType = permissionRecord.AccountType;
            record.User = permissionRecord.Users;
            record.PermissionsLevel = permissionRecord.PermissionLevels;

            record.Remarks = permissionRecord.Remarks;

            _logHelper.AddRecordToCSV(record);
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
