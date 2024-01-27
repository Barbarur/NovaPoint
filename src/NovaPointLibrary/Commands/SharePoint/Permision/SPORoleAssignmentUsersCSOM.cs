using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Sharing;
using NovaPointLibrary.Commands.AzureAD;
using NovaPointLibrary.Commands.SharePoint.Permision.Utilities;
using NovaPointLibrary.Commands.SharePoint.User;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Permision
{
    internal class SPORoleAssignmentUsersCSOM
    {
        private readonly Solutions.NPLogger _logger;
        private readonly Authentication.AppInfo _aAppInfo;
        private readonly SPOKnownRoleAssignmentGroups _knownGroups;
        
        internal SPORoleAssignmentUsersCSOM(Solutions.NPLogger logger,
                                       Authentication.AppInfo appInfo,
                                       SPOKnownRoleAssignmentGroups knownGroups)
        {
            _logger = logger;
            _aAppInfo = appInfo;
            _knownGroups = knownGroups;
        }


        internal async IAsyncEnumerable<SPORoleAssignmentUserRecord> GetAsync(string siteUrl, RoleAssignmentCollection roleAssignmentCollection)
        {
            _aAppInfo.IsCancelled();

            _logger.LogTxt(GetType().Name, $"Iterating role assignments '{roleAssignmentCollection.Count}'");

            int skippedGroupsCounter = 0;
            foreach (var role in roleAssignmentCollection)
            {
                _logger.LogTxt(GetType().Name, $"Gettig Permissions for '{role.Member.PrincipalType}' '{role.Member.Title}'");

                string accessType = "Direct Permissions";
                var permissionLevels = GetPermissionLevels(role.RoleDefinitionBindings);

                if (String.IsNullOrWhiteSpace(permissionLevels))
                {
                    skippedGroupsCounter++;
                    _logger.LogTxt(GetType().Name, $"No permissions found, skipping group");
                    continue;
                }
                else if ( IsSystemGroup(role.Member.Title.ToString()) )
                {
                    yield return GetSystemGroup(accessType, "", role.Member.Title.ToString(), permissionLevels);
                }
                else if (role.Member.PrincipalType.ToString() == "User")
                {
                    string userUPN = role.Member.LoginName.Substring(role.Member.LoginName.IndexOf("i:0#.f|membership|") + 18);
                    yield return new(accessType, "User", userUPN, permissionLevels, "");
                }
                else if (role.Member.PrincipalType.ToString() == "SharePointGroup")
                {
                    await foreach (var record in GetSharePointGroupUsersAsync(siteUrl, role.Member.Title, permissionLevels))
                    {
                        yield return record;
                    }
                }
                else if (role.Member.PrincipalType.ToString() == "SecurityGroup")
                {
                    SPOKnownRoleAssignmentGroupHeaders headers = new();
                    await foreach (var record in GetSecurityGroupUsersAsync(role.Member.Title, role.Member.LoginName, accessType, permissionLevels, headers))
                    {
                        yield return record;
                    }
                }
            }

            if(roleAssignmentCollection.Count == skippedGroupsCounter)
            {
                yield return new("No user access", "No user access", "No user access", "No user access", "No user access");
            }
        }

        internal async IAsyncEnumerable<SPORoleAssignmentUserRecord> GetSharePointGroupUsersAsync(string siteUrl, string groupName, string permissionLevels)
        {
            _aAppInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Start getting users from SharePoint Group '{groupName}'");

            string accessType = SharePointGroupName(groupName);

            List<SPOKnownSharePointGroupUsers> collKnownGroups = _knownGroups.FindSharePointGroups(siteUrl, groupName);
            if (collKnownGroups.Any())
            {
                foreach (var oKnowngroup in collKnownGroups)
                {
                    yield return new(accessType, oKnowngroup.AccountType, oKnowngroup.Users, permissionLevels, oKnowngroup.Remarks);
                }
                yield break;
            }


            UserCollection? groupMembers = null;
            string failedTry = string.Empty;
            try
            {
                groupMembers = await new SPOSiteGroupUsersCSOM(_logger, _aAppInfo).GetAsync(siteUrl, groupName);

                if (!groupMembers.Any())
                {
                    groupMembers = null;
                    failedTry = "SharePoint group with no users";
                }
            }
            catch (Exception ex)
            {
                _logger.ReportError("SharePoint Group", groupName, ex);

                failedTry = ex.Message;
            }

            if(string.IsNullOrWhiteSpace(failedTry) && groupMembers != null)
            {
                var users = String.Join(" ", groupMembers.Where(gm => gm.PrincipalType.ToString() == "User").Select(m => m.UserPrincipalName).ToList());
                if (!string.IsNullOrWhiteSpace(users))
                {
                    yield return new(accessType, "User", users, permissionLevels, "");

                    _knownGroups._groupsSharePoint.Add(new(siteUrl, groupName, "Users", users, ""));
                }


                var collSecurityGroups = groupMembers.Where(gm => gm.PrincipalType.ToString() == "SecurityGroup").ToList();
                foreach (var securityGroup in collSecurityGroups)
                {
                    if (IsSystemGroup(securityGroup.Title))
                    {
                        var sysGroup = GetSystemGroup(accessType, "", securityGroup.Title, permissionLevels);

                        _knownGroups._groupsSharePoint.Add(new(siteUrl, groupName, sysGroup.AccountType, sysGroup.Users, ""));

                        yield return sysGroup;
                        continue;
                    }

                    SPOKnownRoleAssignmentGroupHeaders headers = new();
                    headers._groupsSharePoint.Add(new(siteUrl, groupName, "", "", ""));

                    await foreach (var record in GetSecurityGroupUsersAsync(securityGroup.Title, securityGroup.AadObjectId.NameId, accessType, permissionLevels, headers))
                    {
                        yield return record;
                    }
                }
            }
            else
            {
                _knownGroups._groupsSharePoint.Add(new(siteUrl, groupName, "", "", failedTry));
                yield return new(accessType, "", "", permissionLevels, failedTry);
                yield break;
            }
        }

        internal string SharePointGroupName(string groupName)
        {
            if (groupName.Contains("SharingLinks") && groupName.Contains("Anonymous"))
            {
                return $"Sharing Link 'Anyone'";
            }
            else if (groupName.Contains("SharingLinks") && groupName.Contains("Flexible"))
            {
                return $"Sharing Link 'Specific People'";
            }
            else if (groupName.Contains("SharingLinks") && groupName.Contains("Organization"))
            {
                return $"Sharing Link 'People in your organization'";
            }
            else
            {
                return $"SharePoint Group '{groupName}'";
            }
        }

        internal bool IsSystemGroup(string groupName)
        {
            if (groupName.ToString() == "Everyone"
                || groupName.ToString() == "Everyone except external users"
                || groupName.ToString() == "Global Administrator"
                || groupName.ToString() == "SharePoint Administrator")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private SPORoleAssignmentUserRecord GetSystemGroup(string accessType, string accountType, string groupName, string permissionLevels)
        {
            _aAppInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Getting system group users");

            string thisAccountType = accountType + groupName;

            if (groupName.ToString() == "Everyone")
            {
                return new(accessType, thisAccountType, "All internal and external users", permissionLevels, "");
            }
            else if (groupName.ToString() == "Everyone except external users")
            {
                return new(accessType, thisAccountType, "All internal users", permissionLevels, "");
            }
            else if (groupName.ToString() == "Global Administrator")
            {
                return new(accessType, thisAccountType, "Users with Global Admin role", permissionLevels, "");
            }
            else if (groupName.ToString() == "SharePoint Administrator")
            {
                return new(accessType, thisAccountType, "Users with SharePoint Admin role", permissionLevels, "");
            }
            else
            {
                return new(accessType, thisAccountType, "Unknown users on this group", permissionLevels, "");
            }
        }
        
        internal async IAsyncEnumerable<SPORoleAssignmentUserRecord> GetSecurityGroupUsersAsync(List<Microsoft.SharePoint.Client.User> listSecurityGroup, string accessType, string permissionLevels)
        {
            _aAppInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Getting users from List of Security Groups");

            foreach (var securityGroup in listSecurityGroup)
            {
                if (IsSystemGroup(securityGroup.Title))
                {
                    var sysGroup = GetSystemGroup(accessType, "", securityGroup.Title, permissionLevels);

                    yield return sysGroup;
                    continue;
                }

                SPOKnownRoleAssignmentGroupHeaders headers = new();
                await foreach (var record in GetSecurityGroupUsersAsync(securityGroup.Title, securityGroup.AadObjectId.NameId, accessType, permissionLevels, headers))
                {
                    yield return record;
                }
            }
        }

        private async IAsyncEnumerable<SPORoleAssignmentUserRecord> GetSecurityGroupUsersAsync(string groupName, string groupID, string accessType, string permissionLevels, SPOKnownRoleAssignmentGroupHeaders groupHeaders)
        {
            _aAppInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Getting users from Security Group '{groupName}' with ID '{groupID}'");

            string groupUsersToCollect = "Members";
            if (groupID.Contains("c:0t.c|tenant|")) { groupID = groupID.Substring(groupID.IndexOf("c:0t.c|tenant|") + 14); }
            if (groupID.Contains("c:0u.c|tenant|")) { groupID = groupID.Substring(groupID.IndexOf("c:0u.c|tenant|") + 14); }
            if (groupID.Contains("c:0o.c|federateddirectoryclaimprovider|")) { groupID = groupID.Substring(groupID.IndexOf("c:0o.c|federateddirectoryclaimprovider|") + 39); }
            if (groupID.Contains("_o"))
            {
                groupID = groupID.Substring(0, groupID.IndexOf("_o"));
                groupUsersToCollect = "Owners";
            }

            groupHeaders._accountType += $"Security Group '{groupName}' holds ";

            List<SPOKnownSecurityGroupUsers> collKnownGroups = _knownGroups.FindSecurityGroups(groupID, groupName);
            if (collKnownGroups.Any())
            {
                foreach (var oKnowngroup in collKnownGroups)
                {
                    _knownGroups.AddNewGroupsFromHeaders(groupHeaders, oKnowngroup.Users, oKnowngroup.Remarks);
                    yield return new(accessType, groupHeaders._accountType, oKnowngroup.Users, permissionLevels, oKnowngroup.Remarks);

                }
                yield break;
            }

            groupHeaders._groupsSecurity.Add( new(groupID, groupName, "", "", "") );


            IEnumerable<Microsoft365User>? groupUsers = null;
            string failedTry = string.Empty;
            try
            {
                if (groupUsersToCollect == "Owners") { groupUsers = await new AADGroup(_logger, _aAppInfo).GetOwnersAsync(groupID); }
                else { groupUsers = await new AADGroup(_logger, _aAppInfo).GetMembersAsync(groupID); }

                if (!groupUsers.Any())
                {
                    groupUsers = null;
                    failedTry = "Security group with no users";
                }
            }
            catch (Exception ex)
            {
                _logger.ReportError("Security Group", $"{groupName}' with ID {groupID}", ex);

                failedTry = ex.Message;
            }

            if (string.IsNullOrWhiteSpace(failedTry) && groupUsers != null)
            {
                string users = string.Join(" ", groupUsers.Where(com => com.Type.ToString() == "user").Select(com => com.UserPrincipalName).ToList());
                _knownGroups.AddNewGroupsFromHeaders(groupHeaders, users, "");
                yield return new(accessType, groupHeaders._accountType, users, permissionLevels, "");


                var collSecurityGroups = groupUsers.Where(gm => gm.Type.ToString() == "SecurityGroup").ToList();
                foreach (var securityGroup in collSecurityGroups)
                {
                    if (IsSystemGroup(securityGroup.DisplayName))
                    {
                        var sysGroup = GetSystemGroup(accessType, groupHeaders._accountType, securityGroup.DisplayName, permissionLevels);

                        _knownGroups.AddNewGroupsFromHeaders(groupHeaders, sysGroup.Users, sysGroup.Remarks);

                        yield return sysGroup;
                        continue;
                    }

                    await foreach (var group in GetSecurityGroupUsersAsync(securityGroup.DisplayName, securityGroup.Id, accessType, permissionLevels, groupHeaders))
                    {
                        yield return group;
                    }
                }
            }
            else
            {
                _knownGroups.AddNewGroupsFromHeaders(groupHeaders, "", failedTry);

                yield return new(accessType, groupHeaders._accountType, "", permissionLevels, failedTry);
                yield break;
            }
        }

        private string GetPermissionLevels(RoleDefinitionBindingCollection roleDefinitionsCollection)
        {
            _aAppInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Concatenating Permission Levels");

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

            _logger.LogTxt(GetType().Name, $"Permission Levels: {permissionLevels}");
            return permissionLevels;

        }
    }
}
