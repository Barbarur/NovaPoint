
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.AzureAD;
using NovaPointLibrary.Commands.SharePoint.Permision.Utilities;
using NovaPointLibrary.Commands.SharePoint.User;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Logging;
using System.Data;
using System.Text;
using static NovaPointLibrary.Commands.SharePoint.Permision.SPOSharingLinksREST;

namespace NovaPointLibrary.Commands.SharePoint.Permision
{
    internal class SPORoleAssignmentUsersCSOM
    {
        private readonly LoggerSolution _logger;
        private readonly Authentication.AppInfo _appInfo;
        private readonly SPOKnownRoleAssignmentGroups _knownGroups;
        private readonly SPOSharingLinksREST _restSharingLinks;


        internal SPORoleAssignmentUsersCSOM(
            LoggerSolution logger,
            Authentication.AppInfo appInfo,
            SPOKnownRoleAssignmentGroups knownGroups)
        {
            _logger = logger;
            _appInfo = appInfo;
            _knownGroups = knownGroups;
            _restSharingLinks = new SPOSharingLinksREST(_logger, _appInfo);
        }


        internal async IAsyncEnumerable<SPORoleAssignmentUserRecord> GetAsync(string siteUrl, RoleAssignmentCollection roleAssignmentCollection)
        {
            _appInfo.IsCancelled();

            _logger.Info(GetType().Name, $"Iterating role assignments '{roleAssignmentCollection.Count}'");

            int skippedGroupsCounter = 0;
            foreach (var role in roleAssignmentCollection)
            {
                _logger.Info(GetType().Name, $"Gettig Permissions for '{role.Member.PrincipalType}' '{role.Member.Title}'");

                string accessType = "Direct Permissions";
                var permissionLevels = GetPermissionLevels(role.RoleDefinitionBindings);

                if (String.IsNullOrWhiteSpace(permissionLevels))
                {
                    skippedGroupsCounter++;
                    _logger.Info(GetType().Name, $"No permissions found, skipping group");
                    continue;
                }
                else if (IsSystemGroup(role.Member.Title.ToString()) )
                {
                    SPORoleAssignmentUserRecord record = new(accessType, "NA", permissionLevels);
                    yield return GetSystemGroup(record, "", role.Member.Title.ToString());
                }
                else if (role.Member.PrincipalType.ToString() == "User")
                {
                    string userUPN = role.Member.LoginName.Substring(role.Member.LoginName.IndexOf("i:0#.f|membership|") + 18);

                    yield return SPORoleAssignmentUserRecord.GetRecordUserDirectPermissions(userUPN, permissionLevels);
                }
                else if (role.Member.PrincipalType.ToString() == "SharePointGroup")
                {
                    if (role.Member.Title.Contains("SharingLinks"))
                    {
                        var record = await ProcessSharingLinkAsync(siteUrl, role.Member, permissionLevels);

                        yield return record;
                    }
                    else
                    {
                        await foreach (var record in ProcessSiteGroupUsersAsync(siteUrl, role.Member, permissionLevels))
                        {
                            yield return record;
                        }
                    }
                }
                else if (role.Member.PrincipalType.ToString() == "SecurityGroup")
                {
                    SPOKnownRoleAssignmentGroupHeaders headers = new();
                    SPORoleAssignmentUserRecord record = new(accessType, "NA", permissionLevels);
                    await foreach (var sgRecord in GetSecurityGroupUsersAsync(role.Member.Title, role.Member.LoginName, record, headers))
                    {
                        yield return sgRecord;
                    }
                }
            }

            if(roleAssignmentCollection.Count == skippedGroupsCounter)
            {
                yield return SPORoleAssignmentUserRecord.GetRecordNoAccess();
            }
        }

        internal async IAsyncEnumerable<SPORoleAssignmentUserRecord> ProcessSiteGroupUsersAsync(string siteUrl, Principal spGroup, string permissionLevels)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Processing SharePoint Group '{spGroup.Title}' ({spGroup.Id})");

            string accessType = $"SharePoint Group '{spGroup.Title}'";

            SPORoleAssignmentUserRecord record = new($"SharePoint Group '{spGroup.Title}'", spGroup.Id.ToString(), permissionLevels);

            List<SPOKnownSharePointGroupUsers> collKnownGroups = _knownGroups.FindSharePointGroups(siteUrl, spGroup.Title);
            if (collKnownGroups.Any())
            {
                foreach (var oKnowngroup in collKnownGroups)
                {
                    yield return record.GetRecordWithUsers(oKnowngroup.AccountType, oKnowngroup.Users, oKnowngroup.Remarks);
                }
                yield break;
            }

            UserCollection? groupMembers = null;
            Exception? exception = null;
            try
            {
                groupMembers = await new SPOSiteGroupUsersCSOM(_logger, _appInfo).GetAsync(siteUrl, spGroup.Title);

                if (!groupMembers.Any())
                {
                    exception = new("SharePoint group with no users");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(GetType().Name, "SharePoint Group", spGroup.Title, ex);

                exception = ex;
            }


            if (exception != null)
            {
                _knownGroups._groupsSharePoint.Add(new(siteUrl, spGroup.Title, "", "", exception.Message));

                yield return record.GetRecordWithUsers("", "", exception.Message);
                yield break;
            }
            else if (groupMembers != null)
            {
                var users = String.Join(" ", groupMembers.Where(gm => gm.PrincipalType.ToString() == "User").Select(m => m.UserPrincipalName).ToList());
                if (!string.IsNullOrWhiteSpace(users))
                {
                    yield return record.GetRecordWithUsers("User", users);

                    _knownGroups._groupsSharePoint.Add(new(siteUrl, spGroup.Title, "Users", users, ""));
                }

                var collSecurityGroups = groupMembers.Where(gm => gm.PrincipalType.ToString() == "SecurityGroup").ToList();
                foreach (var securityGroup in collSecurityGroups)
                {
                    if (IsSystemGroup(securityGroup.Title))
                    {
                        var sysGroup = GetSystemGroup(record, "", securityGroup.Title);

                        _knownGroups._groupsSharePoint.Add(new(siteUrl, spGroup.Title, sysGroup.AccountType, sysGroup.Users, ""));

                        yield return sysGroup;
                        continue;
                    }

                    SPOKnownRoleAssignmentGroupHeaders headers = new();
                    headers._groupsSharePoint.Add(new(siteUrl, spGroup.Title, "", "", ""));

                    await foreach (var recordSecGroup in GetSecurityGroupUsersAsync(securityGroup.Title, securityGroup.AadObjectId.NameId, record, headers))
                    {
                        yield return recordSecGroup;
                    }
                }
            }
            else
            {
                Exception e = new("Group is null");
                _logger.Error(GetType().Name, "SharePoint Group", spGroup.Title, e);

                yield return record.GetRecordWithUsers("", "", e.Message);
            }

        }

        internal async Task<SPORoleAssignmentUserRecord> ProcessSharingLinkAsync(string siteUrl, Principal spGroup, string permissionLevels)
        {
            _appInfo.IsCancelled();

            SPOSharingLinksRecord recordSharingLink = await _restSharingLinks.GetFromPrincipalAsync(siteUrl, spGroup);

            SPORoleAssignmentUserRecord record;
            if (string.IsNullOrWhiteSpace(recordSharingLink.Remarks))
            {
                record = new($"Sharing link '{recordSharingLink.SharingLink}' ({spGroup.Title})", spGroup.Id.ToString(), "User", recordSharingLink.Users, permissionLevels, $"RequiresPassword: {recordSharingLink.SharingLinkRequiresPassword}, Expiration: {recordSharingLink.SharingLinkExpiration}");
            }
            else
            {
                record = new($"Sharing link 'Unknown' ({spGroup.Title})", spGroup.Id.ToString(), "User", recordSharingLink.Users, permissionLevels, recordSharingLink.Remarks);
            }

            return record;
        }


        internal static bool IsSystemGroup(string groupName)
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

        private SPORoleAssignmentUserRecord GetSystemGroup(SPORoleAssignmentUserRecord record, string accountType, string groupName)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Getting system group users");

            string thisAccountType = accountType + groupName;

            if (groupName.ToString() == "Everyone")
            {
                return record.GetRecordWithUsers(thisAccountType, "All internal and external users");
            }
            else if (groupName.ToString() == "Everyone except external users")
            {
                return record.GetRecordWithUsers(thisAccountType, "All internal users");
            }
            else if (groupName.ToString() == "Global Administrator")
            {
                return record.GetRecordWithUsers(thisAccountType, "Users with Global Admin role");
            }
            else if (groupName.ToString() == "SharePoint Administrator")
            {
                return record.GetRecordWithUsers(thisAccountType, "Users with SharePoint Admin role");
            }
            else
            {
                return record.GetRecordWithUsers(thisAccountType, "Unknown users on this group");
            }

        }
        
        internal async IAsyncEnumerable<SPORoleAssignmentUserRecord> GetSecurityGroupUsersAsync(List<Microsoft.SharePoint.Client.User> listSecurityGroup, string accessType, string permissionLevels)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Getting users from List of Security Groups");

            foreach (var securityGroup in listSecurityGroup)
            {
                SPORoleAssignmentUserRecord record = new(accessType, "NA", permissionLevels);
                
                if (IsSystemGroup(securityGroup.Title))
                {
                    var sysGroup = GetSystemGroup(record, "", securityGroup.Title);

                    yield return sysGroup;
                    continue;
                }

                SPOKnownRoleAssignmentGroupHeaders headers = new();
                await foreach (var sgRecord in GetSecurityGroupUsersAsync(securityGroup.Title, securityGroup.AadObjectId.NameId, record, headers))
                {
                    yield return sgRecord;
                }
            }
        }

        private async IAsyncEnumerable<SPORoleAssignmentUserRecord> GetSecurityGroupUsersAsync(string sgName, string sgID, SPORoleAssignmentUserRecord record, SPOKnownRoleAssignmentGroupHeaders groupHeaders)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Getting users from Security Group '{sgName}' with ID '{sgID}'");

            if (sgName.Contains("SLinkClaim")) { yield break; }

            string groupUsersToCollect = "Members";
            if (sgID.Contains("c:0t.c|tenant|")) { sgID = sgID.Substring(sgID.IndexOf("c:0t.c|tenant|") + 14); }
            if (sgID.Contains("c:0u.c|tenant|")) { sgID = sgID.Substring(sgID.IndexOf("c:0u.c|tenant|") + 14); }
            if (sgID.Contains("c:0o.c|federateddirectoryclaimprovider|")) { sgID = sgID.Substring(sgID.IndexOf("c:0o.c|federateddirectoryclaimprovider|") + 39); }
            if (sgID.Contains("_o"))
            {
                sgID = sgID.Substring(0, sgID.IndexOf("_o"));
                groupUsersToCollect = "Owners";
            }
            if (String.IsNullOrWhiteSpace(groupHeaders._accountType))
            {
                groupHeaders._accountType += $"Security Group '{sgName}' ({sgID})";
            }
            else
            {
                groupHeaders._accountType += $" holds Security Group '{sgName}' ({sgID})";
            }

            List<SPOKnownSecurityGroupUsers> collKnownGroups = _knownGroups.FindSecurityGroups(sgID, sgName);
            if (collKnownGroups.Any())
            {
                foreach (var oKnowngroup in collKnownGroups)
                {
                    _knownGroups.AddNewGroupsFromHeaders(groupHeaders, oKnowngroup.Users, oKnowngroup.Remarks);

                    yield return record.GetRecordWithUsers(groupHeaders._accountType, oKnowngroup.Users, oKnowngroup.Remarks);
                }
                yield break;
            }

            groupHeaders._groupsSecurity.Add( new(sgID, sgName, "", "", "") );


            IEnumerable<Microsoft365User>? groupUsers = null;
            string exceptionMessage = string.Empty;
            try
            {
                if (groupUsersToCollect == "Owners") { groupUsers = await new AADGroup(_logger, _appInfo).GetOwnersAsync(sgID); }
                else { groupUsers = await new AADGroup(_logger, _appInfo).GetMembersAsync(sgID); }

                if (!groupUsers.Any())
                {
                    groupUsers = null;
                    exceptionMessage = "Security group with no users";
                }
            }
            catch (Exception ex)
            {
                _logger.Error(GetType().Name, "Security Group", $"{sgName}' with ID {sgID}", ex);
                groupUsers = null;
                exceptionMessage = ex.Message;
            }

            if (string.IsNullOrWhiteSpace(exceptionMessage) && groupUsers != null)
            {
                string users = string.Join(" ", groupUsers.Where(com => com.Type.ToString() == "user").Select(com => com.UserPrincipalName).ToList());
                _knownGroups.AddNewGroupsFromHeaders(groupHeaders, users, "");
                yield return record.GetRecordWithUsers(groupHeaders._accountType, users);


                var collSecurityGroups = groupUsers.Where(gm => gm.Type.ToString() == "SecurityGroup").ToList();
                foreach (var securityGroup in collSecurityGroups)
                {
                    if (IsSystemGroup(securityGroup.DisplayName))
                    {
                        var sysGroup = GetSystemGroup(record, groupHeaders._accountType, securityGroup.DisplayName);

                        _knownGroups.AddNewGroupsFromHeaders(groupHeaders, sysGroup.Users, sysGroup.Remarks);

                        yield return sysGroup;
                        continue;
                    }

                    await foreach (var group in GetSecurityGroupUsersAsync(securityGroup.DisplayName, securityGroup.Id, record, groupHeaders))
                    {
                        yield return group;
                    }
                }
            }
            else
            {
                _knownGroups.AddNewGroupsFromHeaders(groupHeaders, "", exceptionMessage);

                yield return record.GetRecordWithUsers(groupHeaders._accountType, "", exceptionMessage); ;
                yield break;
            }
        }

        private string GetPermissionLevels(RoleDefinitionBindingCollection roleDefinitionsCollection)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Concatenating Permission Levels");

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

            _logger.Info(GetType().Name, $"Permission Levels: {permissionLevels}");
            return permissionLevels;

        }
    }
}
