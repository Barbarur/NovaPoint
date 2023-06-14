using Microsoft.Graph;
using Microsoft.IdentityModel.Logging;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.News.DataModel;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Item;
using NovaPointLibrary.Commands.List;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Permision;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.Site;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Permision
{
    internal class GetSPOSitePermissions
    {
        private Solutions.LogHelper _logHelper { get; set; }
        private Authentication.AppInfo AppInfo { get; set; }
        private string SPOAccessToken { get; set; }
        private List<SPOLocationPermissionsRecord> LocationPermissionsRecordsList { get; set; } = new() { };
        private GetSPOPermissionUsers _getSPOPermissionUsers { get; set; }

        internal GetSPOSitePermissions(Solutions.LogHelper logHelper,
                                       Authentication.AppInfo appInfo,
                                       string spoAccessToken,
                                       string aadAccessToken,
                                       List<SPORoleAssignmentKnownGroup> knownGroups)
        {
            _getSPOPermissionUsers = new(logHelper, appInfo, spoAccessToken, aadAccessToken, knownGroups);

            _logHelper = logHelper;
            AppInfo = appInfo;
            SPOAccessToken = spoAccessToken;
        }
        
        internal async Task<List<SPOLocationPermissionsRecord>> CSOMSiteAsync(Web oSiteWithRoles,
                                                                     bool includeAdmins,
                                                                     bool includeSiteAccess,
                                                                     bool includeUniquePermissions,
                                                                     bool includeSystemLists,
                                                                     bool includeResourceLists)
        {
            if (includeAdmins) { await GetAdminsAsync(oSiteWithRoles); }

            if(includeSiteAccess) { await GetSiteAccessAsync(oSiteWithRoles); }
            
            if(includeUniquePermissions) { await GetUniquePermissions(oSiteWithRoles, includeSystemLists, includeResourceLists); }

            return ReturnValues();
        }

        internal async Task<List<SPOLocationPermissionsRecord>> CSOMSubsiteAsync(Web oSubsite,
                                                                        bool includeSiteAccess,
                                                                        bool includeUniquePermissions,
                                                                        bool includeSystemLists,
                                                                        bool includeResourceLists)
        {
            if (oSubsite.HasUniqueRoleAssignments)
            {
                _logHelper.AddLogToUI($"SubSite '{oSubsite.Title}' has unique permissions");
                if (includeSiteAccess) { await GetSiteAccessAsync(oSubsite); }
            }
            else
            {
                _logHelper.AddLogToUI($"SubSite '{oSubsite.Title}' inherits permissions");

                List<SPORoleAssignmentRecord> assignmentUsers = new()
                        {
                            new("", "", "", "", "Inheriting Permissions"),
                        };

                LocationPermissionsRecordsList.Add(new SPOLocationPermissionsRecord("Web", oSubsite.Title, oSubsite.Url, assignmentUsers) );
            }

            if (includeUniquePermissions) { await GetUniquePermissions(oSubsite, includeSystemLists, includeResourceLists); }

            return ReturnValues();
        }

        internal async Task GetAdminsAsync(Web oSite)
        {
            AppInfo.IsCancelled();
            _logHelper.AddLogToTxt($"[{GetType().Name}.GetAdminsAsync] - Start getting Admins for Site '{oSite.Url}'");

            string accessType = "Direct Permissions";
            string permissionLevels = "Site Collection Administrator";

            IEnumerable<Microsoft.SharePoint.Client.User> collSiteCollAdmins;
            try { collSiteCollAdmins = new GetSiteCollectionAdmin(_logHelper, SPOAccessToken).Csom(oSite.Url); }
            catch(Exception ex)
            {
                ErrorHandler(ex, "Web", oSite.Title, oSite.Url);
                return;
            }


            List<SPORoleAssignmentRecord> assignmentUsers = new() { };


            _logHelper.AddLogToTxt($"[{GetType().Name}.GetAdminsAsync] - Processing users '{oSite.Url}'");
            string users = String.Join(" ", collSiteCollAdmins.Where(sca => sca.PrincipalType.ToString() == "User").Select(sca => sca.UserPrincipalName).ToList());
            if (users.Count() > 0) { assignmentUsers.Add( new(accessType, "User", users, permissionLevels, "") ); }


            _logHelper.AddLogToTxt($"[{GetType().Name}.GetAdminsAsync] - Processing Security Groups '{oSite.Url}'");
            var collSecurityGroups = collSiteCollAdmins.Where(sca => sca.PrincipalType.ToString() == "SecurityGroup").ToList();
            assignmentUsers.AddRange( await _getSPOPermissionUsers.GetSecurityGroupUsersAsync(oSite.Url, collSecurityGroups, accessType, permissionLevels) );


            LocationPermissionsRecordsList.Add( new SPOLocationPermissionsRecord("Web", oSite.Title, oSite.Url, assignmentUsers) );

            _logHelper.AddLogToTxt($"[{GetType().Name}.GetAdminsAsync] - Finish getting Admins for Site '{oSite.Url}'");
        }

        internal async Task GetSiteAccessAsync(Web oSite)
        {
            AppInfo.IsCancelled();
            _logHelper.AddLogToTxt($"[{GetType().Name}.GetSiteAccessAsync] - Start getting Site access for Site '{oSite.Url}'");

            List<SPORoleAssignmentRecord> assignmentUsers = await _getSPOPermissionUsers.GetRoleAssigmentUsersAsync(oSite.Url, oSite.RoleAssignments);

            LocationPermissionsRecordsList.Add( new SPOLocationPermissionsRecord("Web", oSite.Title, oSite.Url, assignmentUsers) );

            _logHelper.AddLogToTxt($"[{GetType().Name}.GetSiteAccessAsync] - Finish getting Site access for Site '{oSite.Url}'");
        }

        internal async Task GetUniquePermissions(Web oSite, bool includeSystemLists, bool includeResourceLists)
        {
            AppInfo.IsCancelled();
            _logHelper.AddLogToTxt($"[{GetType().Name}.GetSiteAccessAsync] - Start getting unique permissions for Site '{oSite.Url}'");

            try
            {
                var collList = new GetSPOList(_logHelper, AppInfo, SPOAccessToken).CSOMAllListsWithRoles(oSite.Url, includeSystemLists, includeResourceLists);
                foreach (var oList in collList)
                {
                    AppInfo.IsCancelled();

                    List<SPORoleAssignmentRecord> listAssignmentUsers = new() { };
                    if (oList.HasUniqueRoleAssignments)
                    {
                        _logHelper.AddLogToUI($"'{oList.BaseType}' '{oList.Title}' has unique permissions");

                        listAssignmentUsers = await _getSPOPermissionUsers.GetRoleAssigmentUsersAsync(oSite.Url, oList.RoleAssignments);
                    }
                    else
                    {
                        _logHelper.AddLogToUI($"'{oList.BaseType}' '{oList.Title}' inherits permissions");

                        listAssignmentUsers = new()
                        {
                            new("", "", "", "", "Inheriting Permissions"),
                        };
                    }
                    LocationPermissionsRecordsList.Add( new SPOLocationPermissionsRecord( oList.BaseType.ToString(), oList.Title, oList.DefaultViewUrl, listAssignmentUsers) );

                    try
                    {
                        var collItems = new GetSPOItem(_logHelper, AppInfo, SPOAccessToken).CSOMAllItemsWithRoles(oSite.Url, oList.Title);
                        foreach (var oItem in collItems)
                        {
                            AppInfo.IsCancelled();

                            _logHelper.AddLogToTxt($"Processing '{oItem.FileSystemObjectType}' '{oItem["FileLeafRef"]}'");

                            if (oItem.HasUniqueRoleAssignments)
                            {
                                _logHelper.AddLogToUI($"'{oItem.FileSystemObjectType}' '{oItem["FileLeafRef"]}' has unique permissions");

                                List<SPORoleAssignmentRecord> assignmentUsers = await _getSPOPermissionUsers.GetRoleAssigmentUsersAsync(oSite.Url, oItem.RoleAssignments);

                                LocationPermissionsRecordsList.Add( new SPOLocationPermissionsRecord(oItem.FileSystemObjectType.ToString(), oItem["FileLeafRef"].ToString(), oItem["FileRef"].ToString(), assignmentUsers) );
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorHandler(ex, oList.BaseType.ToString(), oList.Title, oList.DefaultViewUrl);

                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler(ex, "Web", oSite.Title, oSite.Url);

                return;
            }

            _logHelper.AddLogToTxt($"[{GetType().Name}.GetSiteAccessAsync] - Finish getting unique permissions for Site '{oSite.Url}'");
        }

        private List<SPOLocationPermissionsRecord> ReturnValues()
        {
            AppInfo.IsCancelled();
            _logHelper.AddLogToTxt($"[{GetType().Name}.GetSiteAccessAsync] - Returning values");

            List<SPOLocationPermissionsRecord> valuesToReturn = LocationPermissionsRecordsList.ToList();

            LocationPermissionsRecordsList.Clear();

            return valuesToReturn;

        }

        private void ErrorHandler(Exception ex, string locationType, string locationName, string locationUrl)
        {
            _logHelper.AddLogToUI($"Error processing '{locationName}' - '{locationUrl}'");
            _logHelper.AddLogToTxt($"Exception: {ex.Message}");
            _logHelper.AddLogToTxt($"Trace: {ex.StackTrace}");

            List<SPORoleAssignmentRecord> assignmentUsers = new()
                        {
                            new("", "", "", "", ex.Message),
                        };

            LocationPermissionsRecordsList.Add( new SPOLocationPermissionsRecord(locationType, locationName, locationUrl, assignmentUsers) );
        }
    }
}
