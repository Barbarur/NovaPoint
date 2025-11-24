using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Directory;
using NovaPointLibrary.Commands.SharePoint.Permission.Utilities;
using NovaPointLibrary.Commands.SharePoint.SharingLinks;
using NovaPointLibrary.Commands.SharePoint.User;
using NovaPointLibrary.Core.Logging;
using System.Data;
using System.Text;

namespace NovaPointLibrary.Commands.SharePoint.Permission
{
    internal class SPORoleAssignmentUsersCSOM
    {
        private readonly LoggerSolution _logger;
        private readonly Authentication.AppInfo _appInfo;
        private SPOKnownRoleAssignmentGroups KnownGroups { get; init; }
        private readonly SpoSharingLinksRest _restSharingLinks;


        internal SPORoleAssignmentUsersCSOM(
            LoggerSolution logger,
            Authentication.AppInfo appInfo,
            SPOKnownRoleAssignmentGroups knownGroups)
        {
            _logger = logger;
            _appInfo = appInfo;
            KnownGroups = knownGroups;
            _restSharingLinks = new SpoSharingLinksRest(_logger, _appInfo);
        }


        internal async IAsyncEnumerable<SPORoleAssignmentUserRecord> GetAsync(string siteUrl, RoleAssignmentCollection roleAssignmentCollection)
        {
            _appInfo.IsCancelled();

            _logger.Info(GetType().Name, $"Iterating role assignments '{roleAssignmentCollection.Count}'");

            int skippedGroupsCounter = 0;
            foreach (var role in roleAssignmentCollection)
            {
                _logger.Info(GetType().Name, $"Getting Permissions for '{role.Member.PrincipalType}' '{role.Member.Title}'");

                string accessType = "Direct Permissions";
                var permissionLevels = GetPermissionLevels(role.RoleDefinitionBindings);

                if (String.IsNullOrWhiteSpace(permissionLevels))
                {
                    skippedGroupsCounter++;
                    _logger.Info(GetType().Name, $"No permissions found, skipping group");
                    continue;
                }
                // CHECK IF IT CAN BE REMOVED
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
                    if (role.Member.Title.Contains("SLinkClaim")) { continue; }

                    var groupUsersEmails = await new DirectoryGroupUser(_logger, _appInfo).GetUsersAsync(role.Member, KnownGroups.SecurityGroups);

                    yield return new(accessType, "NA", groupUsersEmails.AccountType, groupUsersEmails.Users, permissionLevels, groupUsersEmails.Remarks);
                }
            }

            if(roleAssignmentCollection.Count == skippedGroupsCounter)
            {
                yield return SPORoleAssignmentUserRecord.GetRecordNoAccess();
            }
        }

        internal async IAsyncEnumerable<SPORoleAssignmentUserRecord> ProcessSiteGroupUsersAsync(string siteUrl, Principal spGroup, string permissionLevels)
        {
            _logger.Info(GetType().Name, $"Processing SharePoint Group '{spGroup.Title}' ({spGroup.Id})");

            string accessType = $"SharePoint Group '{spGroup.Title}'";

            SPORoleAssignmentUserRecord record = new($"SharePoint Group '{spGroup.Title}'", spGroup.Id.ToString(), permissionLevels);

            List<SPOKnownSharePointGroupUsers> collKnownGroups = KnownGroups.FindSharePointGroups(siteUrl, spGroup.Title);
            if (collKnownGroups.Any())
            {
                foreach (var oKnownGroup in collKnownGroups)
                {
                    yield return record.GetRecordWithUsers(oKnownGroup.AccountType, oKnownGroup.Users, oKnownGroup.Remarks);
                }
                yield break;
            }

            UserCollection? groupMembers = null;
            Exception? exception = null;
            try
            {
                groupMembers = await new SPOSiteGroupUsersCSOM(_logger, _appInfo).GetAsync(siteUrl, spGroup.Title);
            }
            catch (Exception ex)
            {
                _logger.Error(GetType().Name, "SharePoint Group", spGroup.Title, ex);

                exception = ex;
            }


            if (exception != null)
            {
                KnownGroups.SharePointGroup.Add(new(siteUrl, spGroup.Title, "", "", exception.Message));

                yield return record.GetRecordWithUsers("", "", exception.Message);
            }
            else if (groupMembers != null)
            {
                if (!groupMembers.Any())
                {
                    var emptyGroupMessage = "SharePoint group with no users";
                    KnownGroups.SharePointGroup.Add(new(siteUrl, spGroup.Title, emptyGroupMessage, emptyGroupMessage, ""));
                    yield return record.GetRecordWithUsers(emptyGroupMessage, emptyGroupMessage);
                    yield break;
                }


                var users = String.Join(" ", groupMembers.Where(gm => gm.PrincipalType.ToString() == "User").Select(m => m.UserPrincipalName).ToList());
                if (!string.IsNullOrWhiteSpace(users))
                {
                    KnownGroups.SharePointGroup.Add(new(siteUrl, spGroup.Title, "Users", users, ""));

                    yield return record.GetRecordWithUsers("User", users);
                }

                var collSecurityGroups = groupMembers.Where(gm => gm.PrincipalType.ToString() == "SecurityGroup").ToList();
                foreach (var securityGroup in collSecurityGroups)
                {
                    if (securityGroup.Title.Contains("SLinkClaim")) { continue; }

                    var groupUsersEmails = await new DirectoryGroupUser(_logger, _appInfo).GetUsersAsync(securityGroup, KnownGroups.SecurityGroups);

                    yield return new(accessType, "NA", groupUsersEmails.AccountType, groupUsersEmails.Users, permissionLevels, groupUsersEmails.Remarks);
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

            SpoSharingLinksRecord recordSharingLink = await _restSharingLinks.GetFromPrincipalAsync(siteUrl, spGroup);

            SPORoleAssignmentUserRecord record = new($"Sharing link '{recordSharingLink.SharingLink}'", spGroup.Id.ToString(), "User", recordSharingLink.Users, permissionLevels, recordSharingLink.Remarks);

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
