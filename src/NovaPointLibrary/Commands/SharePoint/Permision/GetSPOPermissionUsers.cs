using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.AzureAD;
using NovaPointLibrary.Commands.SharePoint.User;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Permision
{
    internal class GetSPOPermissionUsers
    {
        private Solutions.LogHelper _logHelper { get; set; }
        private Authentication.AppInfo AppInfo { get; set; }
        private string SPOAccessToken { get; set; }
        private string AADAccessToken { get; set; }
        private List<SPORoleAssignmentKnownGroup> KnownGroups { get; set; } = new() { };
        private List<SPORoleAssignmentRecord> RoleAssignmentUsers { get; set; } = new() { };
        internal GetSPOPermissionUsers(Solutions.LogHelper logHelper,
                                       Authentication.AppInfo appInfo,
                                       string spoAccessToken,
                                       string aadAccessToken,
                                       List<SPORoleAssignmentKnownGroup> knownGroups)
        {
            _logHelper = logHelper;
            AppInfo = appInfo;
            SPOAccessToken = spoAccessToken;
            AADAccessToken = aadAccessToken;
            KnownGroups = knownGroups;
        }


        internal async Task<List<SPORoleAssignmentRecord>> GetRoleAssigmentUsersAsync(string siteUrl, RoleAssignmentCollection roleAssignmentCollection)
        {
            AppInfo.IsCancelled();
            string methodName = $"{GetType().Name}.GetRoleAssigmentUsersAsync";
            _logHelper.AddLogToTxt(methodName, $"Start getting Site Permissions for Site '{siteUrl}'");

            foreach (var role in roleAssignmentCollection)
            {
                _logHelper.AddLogToTxt(methodName, $"Gettig Site Permissions for '{role.Member.PrincipalType}' '{role.Member.Title}'");

                string accessType = "Direct Permissions";
                var permissionLevels = GetPermissionLevels(role.RoleDefinitionBindings);

                if (String.IsNullOrWhiteSpace(permissionLevels))
                {
                    _logHelper.AddLogToTxt(methodName, $"No permissions found, skipping group");
                    continue;
                }
                else if ( IsSystemGroup(accessType, role.Member.Title.ToString(), permissionLevels) )
                {
                    continue;
                }
                else if (role.Member.PrincipalType.ToString() == "User")
                {
                    SPORoleAssignmentRecord usersPermissionsRecord = new(accessType, "User", role.Member.LoginName, permissionLevels, "");
                    RoleAssignmentUsers.Add(usersPermissionsRecord);
                }
                else if (role.Member.PrincipalType.ToString() == "SharePointGroup")
                {
                    await GetSharePointGroupUsersAsync(siteUrl, role.Member.Title, permissionLevels);
                }
                else if (role.Member.PrincipalType.ToString() == "SecurityGroup")
                {
                    List<SPORoleAssignmentKnownGroupHeader> collHeaders = new() { };
                    await GetSecurityGroupUsersAsync(siteUrl, role.Member.Title, role.Member.LoginName, accessType, "", permissionLevels, collHeaders);
                }
            }

            _logHelper.AddLogToTxt(methodName, $"Finish Site Permissions for Site '{siteUrl}'. Total {RoleAssignmentUsers.Count}");

            return ReturnValues();
        }

        private async Task GetSharePointGroupUsersAsync(string siteUrl, string groupName, string permissionLevels)
        {
            AppInfo.IsCancelled();
            string methodName = $"{GetType().Name}.GetSharePointGroupUsers";
            _logHelper.AddLogToTxt(methodName, $"Start getting users from SharePoint Group '{groupName}'");

            string accessType = $"SharePoint Group '{groupName}'";

            List<SPORoleAssignmentKnownGroup> collKnownGroups = new() { };
            collKnownGroups = KnownGroups.Where(kg => kg.PrincipalType == "SharePointGroup" && kg.GroupName == groupName && siteUrl.Contains(kg.SiteURL)).ToList();
            if (collKnownGroups.Count > 0)
            {
                _logHelper.AddLogToTxt(methodName, $"SharePoint Group found in Known Groups");
                foreach (var oKnowngroup in collKnownGroups)
                {
                    _logHelper.AddLogToTxt(methodName, $"Adding Assigment Users {oKnowngroup.AccountType}, {oKnowngroup.Users}, {permissionLevels}");
                    SPORoleAssignmentRecord knownPermissionsRecord = new(accessType, oKnowngroup.AccountType, oKnowngroup.Users, permissionLevels, oKnowngroup.Remarks);
                    RoleAssignmentUsers.Add(knownPermissionsRecord);
                }
                return;
            }


            UserCollection groupMembers;
            try
            {
                groupMembers = new GetSPOGroupMember(_logHelper, AppInfo, SPOAccessToken).CSOMAllMembers(siteUrl, groupName);
            }
            catch (Exception ex)
            {
                _logHelper.AddLogToUI(methodName, $"Error processing SharePoint Group '{groupName}'");
                _logHelper.AddLogToTxt(methodName, $"Exception: {ex.Message}");
                _logHelper.AddLogToTxt(methodName, $"Trace: {ex.StackTrace}");

                SPORoleAssignmentRecord errorPermissions = new(accessType, "", "", permissionLevels, ex.Message);
                RoleAssignmentUsers.Add(errorPermissions);

                SPORoleAssignmentKnownGroup newKnownGroup = new("SharePointGroup", groupName, "", siteUrl, accessType, "", "", ex.Message);
                KnownGroups.Add(newKnownGroup);
                return;
            }


            var users = String.Join(" ", groupMembers.Where(gm => gm.PrincipalType.ToString() == "User")
                .Select(m => m.UserPrincipalName).ToList());
            if (!string.IsNullOrWhiteSpace(users))
            {
                SPORoleAssignmentRecord usersPermissionsRecord = new(accessType, "User", users, permissionLevels, "");
                RoleAssignmentUsers.Add(usersPermissionsRecord);

                SPORoleAssignmentKnownGroup newKnownGroup = new("SharePointGroup", groupName, "", siteUrl, accessType, "Users", users, "");
                KnownGroups.Add(newKnownGroup);
            }



            var collSecurityGroups = groupMembers.Where(gm => gm.PrincipalType.ToString() == "SecurityGroup").ToList();
            foreach (var securityGroup in collSecurityGroups)
            {
                if (IsSystemGroup(accessType, securityGroup.Title, permissionLevels))
                {
                    SPORoleAssignmentKnownGroup newKnownGroup = new("SharePointGroup", groupName, "", siteUrl, accessType, securityGroup.Title.ToString(), "All Users", "");
                    KnownGroups.Add(newKnownGroup);
                    continue;
                }

                List<SPORoleAssignmentKnownGroupHeader> collHeaders = new()
                {
                    new("SharePointGroup", groupName, "", siteUrl, "")
                };

                _logHelper.AddLogToTxt(methodName, $"Processing Security Group '{securityGroup.Title}'");

                await GetSecurityGroupUsersAsync(siteUrl, securityGroup.Title, securityGroup.AadObjectId.NameId, accessType, "", permissionLevels, collHeaders);

            }

            _logHelper.AddLogToTxt(methodName, $"Finish getting users from SharePoint Group {groupName}");
        }

        internal bool IsSystemGroup(string accessType, string groupName, string permissionLevels)
        {
            AppInfo.IsCancelled();
            string methodName = $"{GetType().Name}.IsSystemGroup";
            _logHelper.AddLogToTxt(methodName, $"Start checking if it is a Security Group");

            if (groupName.ToString() == "Everyone" || groupName.ToString() == "Everyone except external users")
            {
                SPORoleAssignmentRecord usersPermissionsRecord = new(accessType, groupName, "All Users", permissionLevels, "");
                RoleAssignmentUsers.Add(usersPermissionsRecord);

                _logHelper.AddLogToTxt(methodName, $"Finish checking if it is a Security Group: '{true}'");
                return true;
            }
            else
            {
                _logHelper.AddLogToTxt(methodName, $"Finish checking if it is a Security Group: '{false}'");
                return false;
            }
        }

        internal async Task<List<SPORoleAssignmentRecord>> GetSecurityGroupUsersAsync(string siteUrl, List<Microsoft.SharePoint.Client.User> listSecurityGroup, string accessType, string permissionLevels)
        {
            AppInfo.IsCancelled();
            string methodName = $"{GetType().Name}.GetSecurityGroupUsersAsync";
            _logHelper.AddLogToTxt(methodName, $"Finish getting users from List of Security Groups");

            foreach (var securityGroup in listSecurityGroup)
            {
                if (IsSystemGroup(accessType, securityGroup.Title, permissionLevels)) { continue; }

                List<SPORoleAssignmentKnownGroupHeader> collHeaders = new() { };

                await GetSecurityGroupUsersAsync(siteUrl, securityGroup.Title, securityGroup.AadObjectId.NameId, accessType, "", permissionLevels, collHeaders);                
            }

            _logHelper.AddLogToTxt(methodName, $"Finish getting users from List of Security Groups");
            return ReturnValues();
        }

        private async Task GetSecurityGroupUsersAsync(string siteUrl, string groupName, string groupID, string accessType, string accountType, string permissionLevels, List<SPORoleAssignmentKnownGroupHeader> collHeaders)
        {
            AppInfo.IsCancelled();
            string methodName = $"{GetType().Name}.GetSecurityGroupUsersAsync";
            _logHelper.AddLogToTxt(methodName, $"Start getting users from Security Group '{groupName}' with ID '{groupID}'");

            string thisAccountType = $"Security Group '{groupName}' holds ";
            accountType += thisAccountType;

            string groupToCollect = "AllUsers";

            if (groupID.Contains("c:0t.c|tenant|")) { groupID = groupID.Substring(groupID.IndexOf("c:0t.c|tenant|") + 14); }
            if (groupID.Contains("c:0o.c|federateddirectoryclaimprovider|")) { groupID = groupID.Substring(groupID.IndexOf("c:0o.c|federateddirectoryclaimprovider|") + 39); }
            if (groupID.Contains("_o")) 
            {
                groupID = groupID.Substring(0, groupID.IndexOf("_o"));
                groupToCollect = "Owners";
            }


            foreach (var oHeader in collHeaders)
            {
                oHeader.AccountType += thisAccountType;
            }
            SPORoleAssignmentKnownGroupHeader thisGroupHeader = new("SecurityGroup", groupName, groupID, siteUrl, thisAccountType);
            collHeaders.Add(thisGroupHeader);


            List<SPORoleAssignmentKnownGroup> collKnownGroups = new() { };
            collKnownGroups = KnownGroups.Where(kg => kg.PrincipalType == "SecurityGroup" && kg.GroupName == groupName && kg.GroupID == groupID).ToList();
            if (collKnownGroups.Count > 0)
            {
                _logHelper.AddLogToTxt(methodName, $"SecurityGroup fround in Known Groups");
                foreach (var oKnowngroup in collKnownGroups)
                {
                    _logHelper.AddLogToTxt(methodName, $"Adding Role Assignment Users {oKnowngroup.AccountType}, {oKnowngroup.Users}, {permissionLevels}");
                    SPORoleAssignmentRecord knownPermissionsRecord = new(accessType, oKnowngroup.AccountType, oKnowngroup.Users, permissionLevels, oKnowngroup.Remarks);
                    RoleAssignmentUsers.Add(knownPermissionsRecord);
                }
                return;
            }


            IEnumerable<Microsoft365User> collOwnersMembers;
            try
            {
                if (groupToCollect == "Owners") { collOwnersMembers = await new GetAzureADGroup(_logHelper, AppInfo, AADAccessToken).GraphOwnersAsync(groupID); }
                else { collOwnersMembers = await new GetAzureADGroup(_logHelper, AppInfo, AADAccessToken).GraphOwnersAndMembersAsync(groupID); }
            }
            catch (Exception ex)
            {
                _logHelper.AddLogToTxt(methodName, $"Error processing Security Group '{groupName}' with ID '{groupID}'");
                _logHelper.AddLogToTxt(methodName, $"Exception: {ex.Message}");
                _logHelper.AddLogToTxt(methodName, $"Trace: {ex.StackTrace}");

                SPORoleAssignmentRecord errorPermissions = new(accessType, accountType, "", permissionLevels, ex.Message);
                RoleAssignmentUsers.Add(errorPermissions);

                foreach (var header in collHeaders)
                {
                    SPORoleAssignmentKnownGroup newKnownGroup = new(header.PrincipalType, header.GroupName, header.GroupID, header.SiteURL, accessType, header.AccountType, "", ex.Message);
                    KnownGroups.Add(newKnownGroup);
                }
                return;
            }


            string users = string.Join(" ", collOwnersMembers.Where(com => com.Type.ToString() == "user").Select(com => com.UserPrincipalName).ToList());
            SPORoleAssignmentRecord usersPermissionsRecord = new(accessType, accountType, users, permissionLevels, "");
            RoleAssignmentUsers.Add(usersPermissionsRecord);

            foreach (SPORoleAssignmentKnownGroupHeader header in collHeaders)
            {
                SPORoleAssignmentKnownGroup newKnownGroup = new(header.PrincipalType, header.GroupName, header.GroupID, header.SiteURL, accessType, header.AccountType, users, "");
                KnownGroups.Add(newKnownGroup);
            }


            var collSecurityGroups = collOwnersMembers.Where(gm => gm.Type.ToString() == "SecurityGroup").ToList();
            foreach (var securityGroup in collSecurityGroups)
            {
                try
                {
                    await GetSecurityGroupUsersAsync(siteUrl, securityGroup.DisplayName, securityGroup.Id, accessType, accountType, permissionLevels, collHeaders);
                }
                catch (Exception ex)
                {
                    SPORoleAssignmentRecord errorSecurityPermissionsRecord = new(accessType, accountType, users, permissionLevels, ex.Message);
                    RoleAssignmentUsers.Add(errorSecurityPermissionsRecord);

                    foreach (var header in collHeaders)
                    {
                        SPORoleAssignmentKnownGroup newKnownGroup = new(header.PrincipalType, header.GroupName, header.GroupID, header.SiteURL, accessType, header.AccountType, users, ex.Message);
                        KnownGroups.Add(newKnownGroup);
                    }
                }
            }

            _logHelper.AddLogToTxt(methodName, $"Finish getting users from Security Group {groupName}with ID {groupID}");
        }

        private string GetPermissionLevels(RoleDefinitionBindingCollection roleDefinitionsCollection)
        {
            AppInfo.IsCancelled();
            string methodName = $"{GetType().Name}.GetSecurityGroupUsersAsync";
            _logHelper.AddLogToTxt(methodName, $"Start getting Permission Levels");

            StringBuilder sb = new();
            foreach (var roleDefinition in roleDefinitionsCollection)
            {
                if (roleDefinition.Name == "Limited Access" || roleDefinition.Name == "Web-Only Limited Access") { continue; }
                else
                {
                    sb.Append($"{roleDefinition.Name} | ");
                }
            }

            string permissionLevels = "";
            if (sb.Length > 0) { permissionLevels = sb.ToString().Remove(sb.Length - 3); }

            _logHelper.AddLogToTxt(methodName, $"Finish getting Permission Levels: {permissionLevels}");
            return permissionLevels;

        }

        private List<SPORoleAssignmentRecord> ReturnValues()
        {
            List<SPORoleAssignmentRecord> valuesToReturn = RoleAssignmentUsers.ToList();

            RoleAssignmentUsers.Clear();

            return valuesToReturn;

        }
    }
}
