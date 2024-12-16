using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
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

        internal async Task<List<AADGroupUserEmails>> GetUsersAsync(Microsoft.SharePoint.Client.User secGroup, List<AADGroupUserEmails>? listKnownGroups = null)
        {
            return await GetUsersAsync(secGroup.Title, secGroup.AadObjectId.NameId, listKnownGroups);
        }

        internal async Task<List<AADGroupUserEmails>> GetUsersAsync(Microsoft365User secGroup, List<AADGroupUserEmails>? listKnownGroups = null)
        {
            return await GetUsersAsync(secGroup.DisplayName, secGroup.Id, listKnownGroups);
        }

        internal async Task<List<AADGroupUserEmails>> GetUsersAsync(string secGroupTitle, string secGroupId, List<AADGroupUserEmails>? listKnownGroups = null)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Getting users from Security Group '{secGroupTitle}'");

            List<AADGroupUserEmails> listOfUsers = new();

            if (IsSystemGroup(secGroupTitle))
            {
                listOfUsers.Add(new("", secGroupTitle, secGroupTitle));
                return listOfUsers;
            }

            if (listKnownGroups != null)
            {
                List<AADGroupUserEmails> knownGroups = listKnownGroups.Where(sg => sg.GroupID == secGroupId).ToList();

                if (knownGroups.Any()) { return knownGroups; }
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


                IEnumerable<Microsoft365User> groupUsers;
                if (needOwners) { groupUsers = await GetOwnersAsync(secGroupId); }
                else { groupUsers = await GetMembersAsync(secGroupId); }


                if (!groupUsers.Any())
                {
                    listOfUsers.Add(new(secGroupId, secGroupTitle, "Empty Group"));
                }
                else
                {
                    string users = string.Join(" ", groupUsers.Where(com => com.Type.ToString() == "user").Select(com => com.UserPrincipalName).ToList());

                    AADGroupUserEmails usersRecord;
                    if (users.Any()) { usersRecord = new(secGroupId, secGroupTitle, users); }
                    else { usersRecord = new(secGroupId, secGroupTitle, "Group has no direct users"); }
                    listKnownGroups?.Add(usersRecord);
                    listOfUsers.Add(usersRecord);

                    var collSecurityGroups = groupUsers.Where(gm => gm.Type.ToString() == "SecurityGroup").ToList();
                    foreach (var securityGroup in collSecurityGroups)
                    {
                        List<AADGroupUserEmails> listSubgroupUsers = await GetUsersAsync(securityGroup, listKnownGroups);

                        foreach (var subgroupUsers in listSubgroupUsers)
                        {
                            AADGroupUserEmails subgroupUsersRecord = new(secGroupId, secGroupTitle, subgroupUsers);
                            if (listKnownGroups != null && string.IsNullOrWhiteSpace(subgroupUsersRecord.Remarks)) { listKnownGroups.Add(subgroupUsersRecord); }
                            listOfUsers.Add(subgroupUsersRecord);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(GetType().Name, "Security Group", secGroupTitle, ex);
                listOfUsers.Add(new(secGroupId, secGroupTitle, "", ex.Message));
            }

            return listOfUsers;
        }

        internal static bool IsSystemGroup(string secGroupTitle)
        {
            if (secGroupTitle == "Everyone"
                || secGroupTitle == "Everyone except external users"
                || secGroupTitle == "Global Administrator"
                || secGroupTitle == "SharePoint Administrator")
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

    }
}
