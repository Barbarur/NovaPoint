using Microsoft.IdentityModel.Logging;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Sharing;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.AzureAD;
using NovaPointLibrary.Commands.SharePoint.User;
using NovaPointLibrary.Commands.Site;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Permision
{
    /// <summary>
    /// TO BE DEPRECATED ONCE GETSPOSITEPERMISSION IS MATURE
    /// </summary>
    internal class GetSPORoleAssigmentUsers
    {
        //private Solutions.LogHelper _logHelper { get; set; }
        //private Authentication.AppInfo AppInfo { get; set; }
        //private string AADAccessToken { get; set; }
        //private string SPOAccessToken { get; set; }
        //private string SiteURL { get; set; }
        //private List<SPORoleAssigmentKnownGroup> KnownGroups { get; set; } = new() { };
        //private List<SPORoleAssignmentUsers> AssignmentUsers { get; set; } = new() { };

        //internal GetSPORoleAssigmentUsers(Solutions.LogHelper logHelper,
        //                                  Authentication.AppInfo appInfo,
        //                                  string aadAccessToken,
        //                                  string spoAccessToken,
        //                                  string siteURL,
        //                                  List<SPORoleAssigmentKnownGroup> knownGroups)
        //{
        //    _logHelper = logHelper;
        //    AppInfo = appInfo;
        //    AADAccessToken = aadAccessToken;
        //    SPOAccessToken = spoAccessToken;
        //    SiteURL = siteURL;
        //    KnownGroups = knownGroups;
        //}

        //internal async Task<List<SPORoleAssignmentUsers>> GetAdminsAsync()
        //{
        //    AppInfo.IsCancelled();
        //    _logHelper.AddLogToTxt($"[{GetType().Name}.GetAdminsAsync] - Start getting Admins for Site '{SiteURL}'");

        //    string accessType = "Direct Permissions";
        //    string permissionLevels = "Site Collection Administrator";


        //    IEnumerable<Microsoft.SharePoint.Client.User> collSiteCollAdmins = new GetSiteCollectionAdmin(_logHelper, SPOAccessToken).Csom(SiteURL);


        //    _logHelper.AddLogToTxt($"[{GetType().Name}.GetAdminsAsync] - Processing users '{SiteURL}'");
        //    string users = String.Join(" ", collSiteCollAdmins.Where(sca => sca.PrincipalType.ToString() == "User").Select(sca => sca.UserPrincipalName).ToList());

        //    SPORoleAssignmentUsers usersPermissionsRecord = new(accessType, "User", users, permissionLevels);
        //    AssignmentUsers.Add(usersPermissionsRecord);


        //    _logHelper.AddLogToTxt($"[{GetType().Name}.GetAdminsAsync] - Processing Security Groups '{SiteURL}'");
        //    var collSecurityGroups = collSiteCollAdmins.Where(sca => sca.PrincipalType.ToString() == "SecurityGroup").ToList();
        //    foreach (var securityGroup in collSecurityGroups)
        //    {
        //        _logHelper.AddLogToTxt($"[{GetType().Name}.GetAdminsAsync] - Processing Security Group '{securityGroup.Title}' - '{securityGroup.AadObjectId.NameId}'");

        //        if (IsSystemGroup(accessType, securityGroup.Title, permissionLevels))
        //        {
        //            continue;
        //        }


        //        List<SPORoleAssigmentHeader> collHeaders = new()
        //        {
        //            new("SharePointGroup", securityGroup.Title, "", SiteURL, "")
        //        };

        //        await GetSecurityGroupUsersAsync(securityGroup.Title, securityGroup.AadObjectId.NameId, accessType, "", permissionLevels, collHeaders);
        //    }

        //    _logHelper.AddLogToTxt($"[{GetType().Name}.GetAdminsAsync] - Finish getting Admins for Site '{SiteURL}'");
        //    return ReturnValues();
        //}


        //internal async Task<List<SPORoleAssignmentUsers>> GetUsersAsync(RoleAssignmentCollection roleAssignmentCollection)
        //{
        //    AppInfo.IsCancelled();
        //    _logHelper.AddLogToTxt($"[{GetType().Name}.GetUsersAsync] - Start getting Site Permissions for Site '{SiteURL}'");

        //    foreach (var role in roleAssignmentCollection)
        //    {
        //        _logHelper.AddLogToTxt($"[{GetType().Name}.GetUsersAsync] - Gettig Site Permissions for '{role.Member.PrincipalType}' '{role.Member.Title}'");

        //        string accessType = "Direct Permissions";
        //        var permissionLevels = GetPermissionLevels(role.RoleDefinitionBindings);

        //        if (String.IsNullOrWhiteSpace(permissionLevels))
        //        {
        //            _logHelper.AddLogToTxt($"[{GetType().Name}.GetUsersAsync] - No permissions found, skipping group");
        //            continue;
        //        }
        //        if ( role.Member.Title.ToString() == "Everyone" || role.Member.Title.ToString() == "Everyone except external users")
        //        {
        //            SPORoleAssignmentUsers usersPermissionsRecord = new(accessType, role.Member.Title.ToString(), "All Users", permissionLevels);
        //            AssignmentUsers.Add(usersPermissionsRecord);
        //        }
        //        else if (role.Member.PrincipalType.ToString() == "User")
        //        {
        //            SPORoleAssignmentUsers usersPermissionsRecord = new(accessType, "User", role.Member.LoginName, permissionLevels);
        //            AssignmentUsers.Add(usersPermissionsRecord);
        //        }
        //        else if (role.Member.PrincipalType.ToString() == "SharePointGroup")
        //        {
        //            await GetSharePointGroupUsers(role.Member.Title, permissionLevels);
        //        }
        //        else if (role.Member.PrincipalType.ToString() == "SecurityGroup")
        //        {
        //            List<SPORoleAssigmentHeader> collHeaders = new() { };
        //            await GetSecurityGroupUsersAsync(role.Member.Title, role.Member.LoginName, accessType, "", permissionLevels, collHeaders);
        //        }
        //    }

        //    _logHelper.AddLogToTxt($"[{GetType().Name}.GetUsersAsync] - Finish Site Permissions for Site '{SiteURL}'. Total {AssignmentUsers.Count}");

        //    return ReturnValues();
        //}


        //private async Task GetSharePointGroupUsers(string groupName, string permissionLevels)
        //{
        //    AppInfo.IsCancelled();
        //    _logHelper.AddLogToTxt($"[{GetType().Name}.GetSharePointGroupUsers] - Start getting users from SharePoint Group '{groupName}'");
            
        //    string accessType = $"SharePoint Group '{groupName}'";

        //    List<SPORoleAssigmentKnownGroup> collKnownGroups = new() { };
        //    collKnownGroups = KnownGroups.Where(kg => kg.PrincipalType == "SharePointGroup" && kg.GroupName == groupName && SiteURL.Contains(kg.SiteURL) ).ToList();
        //    if (collKnownGroups.Count > 0)
        //    {
        //        _logHelper.AddLogToTxt($"[{GetType().Name}.GetSharePointGroupUsers] - SharePoint Group found in Known Groups");
        //        foreach (var oKnowngroup in collKnownGroups)
        //        {
        //            _logHelper.AddLogToTxt($"[{GetType().Name}.GetSharePointGroupUsers] - Adding Assigment Users {oKnowngroup.AccountType}, {oKnowngroup.Users}, {permissionLevels}");
        //            SPORoleAssignmentUsers knownPermissionsRecord = new(accessType, oKnowngroup.AccountType, oKnowngroup.Users, permissionLevels);
        //            AssignmentUsers.Add(knownPermissionsRecord);
        //        }
        //        return;
        //    }


        //    UserCollection groupMembers;
        //    try
        //    {
        //        groupMembers = new GetSPOGroupMember(_logHelper, AppInfo, SPOAccessToken).CSOMAllMembers(SiteURL, groupName);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logHelper.AddLogToUI($"Error processing SharePoint Group '{groupName}'");
        //        _logHelper.AddLogToTxt($"Exception: {ex.Message}");
        //        _logHelper.AddLogToTxt($"Trace: {ex.StackTrace}");

        //        SPORoleAssignmentUsers errorPermissions = new(accessType, "", "", permissionLevels, ex.Message);
        //        AssignmentUsers.Add(errorPermissions);

        //        SPORoleAssigmentKnownGroup newKnownGroup = new("SharePointGroup", groupName, "", SiteURL, accessType, "", "", ex.Message);
        //        KnownGroups.Add(newKnownGroup);
        //        return;
        //    }



        //    var users = String.Join( " ", groupMembers.Where(gm => gm.PrincipalType.ToString() == "User" )
        //        .Select(m => m.UserPrincipalName).ToList());
        //    if (!string.IsNullOrWhiteSpace(users))
        //    { 
        //        SPORoleAssignmentUsers usersPermissionsRecord = new(accessType, "User", users, permissionLevels);
        //        AssignmentUsers.Add(usersPermissionsRecord);

        //        SPORoleAssigmentKnownGroup newKnownGroup = new("SharePointGroup", groupName, "", SiteURL, accessType, "Users", users);
        //        KnownGroups.Add(newKnownGroup);
        //    }



        //    var collSecurityGroups = groupMembers.Where(gm => gm.PrincipalType.ToString() == "SecurityGroup").ToList();
        //    foreach(var securityGroup in collSecurityGroups)
        //    {
        //        if ( IsSystemGroup(accessType, securityGroup.Title, permissionLevels) )
        //        {
        //            SPORoleAssigmentKnownGroup newKnownGroup = new("SharePointGroup", groupName, "", SiteURL, accessType, securityGroup.Title.ToString(), "All Users");
        //            KnownGroups.Add(newKnownGroup);
        //            continue;
        //        }

        //        List<SPORoleAssigmentHeader> collHeaders = new()
        //        {
        //            new("SharePointGroup", groupName, "", SiteURL, "")
        //        };

        //        _logHelper.AddLogToTxt($"[{GetType().Name}.GetSharePointGroupUsers] - Processing Security Group '{securityGroup.Title}'");

        //        await GetSecurityGroupUsersAsync(securityGroup.Title, securityGroup.AadObjectId.NameId, accessType, "", permissionLevels, collHeaders);

        //    }

        //    _logHelper.AddLogToTxt($"[{GetType().Name}.GetSharePointGroupUsers] - Finish getting users from SharePoint Group {groupName}");
        //}

        //private async Task GetSecurityGroupUsersAsync(string groupName, string groupID ,string accessType, string accountType, string permissionLevels, List<SPORoleAssigmentHeader> collHeaders)
        //{
        //    AppInfo.IsCancelled();
        //    _logHelper.AddLogToTxt($"[{GetType().Name}.GetSecurityGroupUsersAsync] - Start getting users from Security Group '{groupName}' with ID '{groupID}'");

        //    string thisAccountType = $"Security Group '{groupName}' holds ";
        //    accountType += thisAccountType;


        //    if (groupID.Contains("c:0t.c|tenant|")) { groupID = groupID.Substring(groupID.IndexOf("c:0t.c|tenant|") + 14); }
        //    if (groupID.Contains("c:0o.c|federateddirectoryclaimprovider|")) { groupID = groupID.Substring(groupID.IndexOf("c:0o.c|federateddirectoryclaimprovider|") + 39); }
        //    if (groupID.Contains("_o")) { groupID = groupID.Substring(0, groupID.IndexOf("_o")); }


        //    foreach (var oHeader in collHeaders)
        //    {
        //        oHeader.AccountType += thisAccountType;
        //    }
        //    SPORoleAssigmentHeader thisGroupHeader = new("SecurityGroup", groupName, groupID, SiteURL, thisAccountType);
        //    collHeaders.Add(thisGroupHeader);


        //    List<SPORoleAssigmentKnownGroup> collKnownGroups = new() { };
        //    collKnownGroups = KnownGroups.Where(kg => kg.PrincipalType == "SecurityGroup" && kg.GroupName == groupName && kg.GroupID == groupID).ToList();
        //    if (collKnownGroups.Count > 0)
        //    {
        //        _logHelper.AddLogToTxt($"[{GetType().Name}.GetSecurityGroupUsersAsync] - SecurityGroup fround in Known Groups");
        //        foreach (var oKnowngroup in collKnownGroups)
        //        {
        //            _logHelper.AddLogToTxt($"[{GetType().Name}.GetSecurityGroupUsersAsync] - Adding Assigment Users {oKnowngroup.AccountType}, {oKnowngroup.Users}, {permissionLevels}");
        //            SPORoleAssignmentUsers knownPermissionsRecord = new(accessType, oKnowngroup.AccountType, oKnowngroup.Users, permissionLevels);
        //            AssignmentUsers.Add(knownPermissionsRecord);
        //        }
        //        return;
        //    }


        //    IEnumerable<Microsoft365User> collOwnersMembers;
        //    try
        //    {
        //        collOwnersMembers = await new GetAzureADGroup(_logHelper, AppInfo, AADAccessToken).GraphOwnersAndMembersAsync(groupID);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logHelper.AddLogToUI($"Error processing Security Group '{groupName}' with ID '{groupID}'");
        //        _logHelper.AddLogToTxt($"Exception: {ex.Message}");
        //        _logHelper.AddLogToTxt($"Trace: {ex.StackTrace}");

        //        SPORoleAssignmentUsers errorPermissions = new(accessType, accountType, "", permissionLevels, ex.Message);
        //        AssignmentUsers.Add(errorPermissions);

        //        foreach (var header in collHeaders)
        //        {
        //            SPORoleAssigmentKnownGroup newKnownGroup = new(header.PrincipalType, header.GroupName, header.GroupID, header.SiteURL, accessType, header.AccountType, "", ex.Message);
        //            KnownGroups.Add(newKnownGroup);
        //        }
        //        return;
        //    }


        //    var users = String.Join(" ", collOwnersMembers.Where(com => com.Type.ToString() == "user").Select(com => com.UserPrincipalName).ToList());
        //    SPORoleAssignmentUsers usersPermissionsRecord = new(accessType, accountType, users, permissionLevels);
        //    AssignmentUsers.Add(usersPermissionsRecord);

        //    foreach(var header in collHeaders)
        //    {
        //        SPORoleAssigmentKnownGroup newKnownGroup = new(header.PrincipalType, header.GroupName, header.GroupID, header.SiteURL, accessType, header.AccountType, users);
        //        KnownGroups.Add(newKnownGroup);
        //    }


        //    var collSecurityGroups = collOwnersMembers.Where(gm => gm.Type.ToString() == "SecurityGroup").ToList();
        //    foreach (var securityGroup in collSecurityGroups)
        //    {
        //        try
        //        {
        //            await GetSecurityGroupUsersAsync(securityGroup.DisplayName, securityGroup.Id, accessType, accountType, permissionLevels, collHeaders);
        //        }
        //        catch (Exception ex)
        //        {
        //            SPORoleAssignmentUsers errorSecurityPermissionsRecord = new(accessType, accountType, users, permissionLevels, ex.Message);
        //            AssignmentUsers.Add(errorSecurityPermissionsRecord);

        //            foreach (var header in collHeaders)
        //            {
        //                SPORoleAssigmentKnownGroup newKnownGroup = new(header.PrincipalType, header.GroupName, header.GroupID, header.SiteURL, accessType, header.AccountType, users, ex.Message);
        //                KnownGroups.Add(newKnownGroup);
        //            }
        //        }
        //    }
        //    _logHelper.AddLogToTxt($"[{GetType().Name}.GetSecurityGroupUsersAsync] - Finish getting users from Security Group {groupName}with ID {groupID}");
        //}


        //internal async Task<List<SPORoleAssignmentUsers>> GetSecurityGroupUsersReturnsAsync(string groupName, string groupID, string accessTyoe, string accountType, string permissionLeves)
        //{
        //    List<SPORoleAssigmentHeader> collHeaders = new() { };

        //    await GetSecurityGroupUsersAsync(groupName, groupID, accessTyoe, "", permissionLeves, collHeaders);

        //    return AssignmentUsers;
        //}


        //private string GetPermissionLevels(RoleDefinitionBindingCollection roleDefinitionsCollection)
        //{
        //    AppInfo.IsCancelled();
        //    _logHelper.AddLogToTxt($"[{GetType().Name}.GetPermissionLevels] - Start getting Permission Levels");

        //    StringBuilder sb = new();
        //    foreach (var roleDefinition in roleDefinitionsCollection)
        //    {
        //        if (roleDefinition.Name == "Limited Access" || roleDefinition.Name == "Web-Only Limited Access") { continue; }
        //        else
        //        {
        //            sb.Append($"{roleDefinition.Name} | ");
        //        }
        //    }

        //    string permissionLevels = "";
        //    if (sb.Length > 0) { permissionLevels = sb.ToString().Remove(sb.Length - 3); }

        //    _logHelper.AddLogToTxt($"[{GetType().Name}.GetPermissionLevels] - Finish getting Permission Levels: {permissionLevels}");
        //    return permissionLevels;

        //}

        //private bool IsSystemGroup(string accessType, string groupName, string permissionLevels)
        //{
        //    if (groupName.ToString() == "Everyone" || groupName.ToString() == "Everyone except external users")
        //    {
        //        SPORoleAssignmentUsers usersPermissionsRecord = new(accessType, groupName, "All Users", permissionLevels);
        //        AssignmentUsers.Add(usersPermissionsRecord);

        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

        //private List<SPORoleAssignmentUsers> ReturnValues()
        //{
        //    List<SPORoleAssignmentUsers> valuesToReturn = AssignmentUsers.ToList();

        //    AssignmentUsers.Clear();

        //    return valuesToReturn;

        //}
    }
}
