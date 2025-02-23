﻿using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Logging;


namespace NovaPointLibrary.Commands.AzureAD.Groups
{
    internal class AADGroup
    {
        private readonly LoggerSolution _logger;
        private readonly AppInfo _appInfo;


        internal AADGroup(LoggerSolution logger, AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<GraphGroup> GetInfoAsync(string groupId, string optionalQuery = "")
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Getting information from Group '{groupId}'");

            string api = $"/groups/{groupId}" + optionalQuery;

            var group = await new GraphAPIHandler(_logger, _appInfo).GetObjectAsync<GraphGroup>(api);

            return group;
        }

        internal async Task<List<AADGroupUserEmails>> GetUsersAsync(Microsoft.SharePoint.Client.Principal principal, List<AADGroupUserEmails>? listKnownGroups = null)
        {
            List<AADGroupUserEmails> listOfUsers = GetSystemGroup(principal.Title);

            if (!listOfUsers.Any())
            {
                listOfUsers = await GetUsersAsync(principal.Title, principal.LoginName, listKnownGroups);
            }

            return listOfUsers;
        }

        internal async Task<List<AADGroupUserEmails>> GetUsersAsync(Microsoft.SharePoint.Client.User secGroup, List<AADGroupUserEmails>? listKnownGroups = null)
        {
            List<AADGroupUserEmails> listOfUsers = GetSystemGroup(secGroup.Title);

            if (!listOfUsers.Any())
            {
                listOfUsers = await GetUsersAsync(secGroup.Title, secGroup.AadObjectId.NameId, listKnownGroups);
            }

            return listOfUsers;
        }

        internal async Task<List<AADGroupUserEmails>> GetUsersAsync(Microsoft365User secGroup, List<AADGroupUserEmails>? listKnownGroups = null)
        {
            List<AADGroupUserEmails> listOfUsers = GetSystemGroup(secGroup.DisplayName);

            if (!listOfUsers.Any())
            {
                listOfUsers = await GetUsersAsync(secGroup.DisplayName, secGroup.Id, listKnownGroups);
            }

            return listOfUsers;
        }

        internal async Task<List<AADGroupUserEmails>> GetUsersAsync(
            string secGroupTitle, 
            string secGroupId, 
            List<AADGroupUserEmails>? listKnownGroups = null)
        {
            _logger.Info(GetType().Name, $"Getting users from Security Group '{secGroupTitle}'");

            List<AADGroupUserEmails> collSgUserEmails = new();

            if (secGroupTitle.Contains("SLinkClaim")) { return collSgUserEmails; }

            if (listKnownGroups != null)
            {
                collSgUserEmails = listKnownGroups.Where(sg => sg.GroupID == secGroupId).ToList();

                if (collSgUserEmails.Any()) { return collSgUserEmails; }
            }

            try
            {
                bool needOwners = false;
                if (secGroupId.Contains("c:0t.c|tenant|")) { secGroupId = secGroupId.Substring(secGroupId.IndexOf("c:0t.c|tenant|") + 14); }
                if (secGroupId.Contains("c:0u.c|tenant|")) { secGroupId = secGroupId[(secGroupId.IndexOf("c:0u.c|tenant|") + 14)..]; }
                if (secGroupId.Contains("c:0o.c|federateddirectoryclaimprovider|")) { secGroupId = secGroupId.Substring(secGroupId.IndexOf("c:0o.c|federateddirectoryclaimprovider|") + 39); }
                if (secGroupId.Contains("_o"))
                {
                    secGroupId = secGroupId.Substring(0, secGroupId.IndexOf("_o"));
                    needOwners = true;
                }


                IEnumerable<Microsoft365User> sgMembers;
                if (needOwners) { sgMembers = await GetOwnersAsync(secGroupId); }
                else { sgMembers = await GetMembersAsync(secGroupId); }


                if (!sgMembers.Any())
                {
                    collSgUserEmails.Add(new(secGroupId, secGroupTitle, "Security group is empty"));
                }
                else
                {
                    string users = string.Join(" ", sgMembers.Where(com => com.Type.ToString() == "user").Select(com => com.UserPrincipalName).ToList());

                    AADGroupUserEmails usersRecord;
                    if (users.Any()) { usersRecord = new(secGroupId, secGroupTitle, users); }
                    else { usersRecord = new(secGroupId, secGroupTitle, "Group has no direct users"); }
                    collSgUserEmails.Add(usersRecord);

                    var collSgGroups = sgMembers.Where(gm => gm.Type.ToString() == "SecurityGroup").ToList();
                    foreach (var securityGroup in collSgGroups)
                    {
                        List<AADGroupUserEmails> collChildSgUsers = await GetUsersAsync(securityGroup, listKnownGroups);

                        foreach (var childSgUsers in collChildSgUsers)
                        {
                            AADGroupUserEmails subgroupUsersRecord = new(secGroupId, secGroupTitle, childSgUsers);
                            collSgUserEmails.Add(subgroupUsersRecord);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(GetType().Name, "Security Group", secGroupTitle, ex);
                collSgUserEmails.Add(new(secGroupId, secGroupTitle, "", ex.Message));
            }

            listKnownGroups?.AddRange(collSgUserEmails);
            return collSgUserEmails;
        }

        private List<AADGroupUserEmails> GetSystemGroup(string groupTitle)
        {
            List<AADGroupUserEmails> listOfUsers = new();
            if (IsSystemGroup(groupTitle))
            {
                listOfUsers.Add(new("", groupTitle, groupTitle));
            }
            return listOfUsers;
        }

        internal static bool IsSystemGroup(string secGroupTitle)
        {
            if (secGroupTitle == "Everyone"
                || secGroupTitle == "Everyone except external users"
                || secGroupTitle == "Global Administrator"
                || secGroupTitle == "SharePoint Administrator"
                || secGroupTitle == "All Company Members")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal async Task<IEnumerable<Microsoft365User>> GetOwnersAsync(string groupId)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Getting Owners of Group '{groupId}'");

            string url = $"/groups/{groupId}/owners?$select=*";

            var collMembers = await new GraphAPIHandler(_logger, _appInfo).GetCollectionAsync<Microsoft365User>(url);

            return collMembers;
        }

        internal async Task<IEnumerable<Microsoft365User>> GetMembersAsync(string groupId)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Getting Members of Group '{groupId}'");

            string url = $"/groups/{groupId}/members?$select=*";

            var collMembers = await new GraphAPIHandler(_logger, _appInfo).GetCollectionAsync<Microsoft365User>(url);

            return collMembers;
        }

        internal async Task RemoveGroupAsync(string groupId)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Removing Group '{groupId}'");

            string url = $"/groups/{groupId}";

            await new GraphAPIHandler(_logger, _appInfo).DeleteAsync(url);
        }

    }
}
