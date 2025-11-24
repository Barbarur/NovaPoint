using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Logging;


namespace NovaPointLibrary.Commands.Directory
{
    internal class DirectoryGroupUser
    {
        private readonly LoggerSolution _logger;
        private readonly AppInfo _appInfo;


        internal DirectoryGroupUser(LoggerSolution logger, AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<DirectoryGroupUserEmails> GetUsersAsync(Microsoft.SharePoint.Client.Principal secGroup, List<DirectoryGroupUserEmails>? listKnownGroups = null)
        {
            if (IsSystemGroup(out DirectoryGroupUserEmails groupUserEmails, secGroup.Title))
            {
                return groupUserEmails;
            }
            else
            {
                DirectoryGroupUserEmails sgUserEmails;
                try
                {
                    bool isOwner = IsOwnerAndPurgeGroupId(out Guid sgGuid, secGroup.LoginName);

                    sgUserEmails = await GetUsersAsync(secGroup.Title, sgGuid, isOwner, listKnownGroups);
                }
                catch (Exception ex)
                {
                    sgUserEmails = new(Guid.Empty, secGroup.Title, false, $"{secGroup.Title} ({secGroup.LoginName})", ex.Message);
                }

                return sgUserEmails;
            }

        }

        private static bool IsOwnerAndPurgeGroupId(out Guid groupId, string secGroupId)
        {
            bool isOwners = false;
            if (secGroupId.Contains("c:0t.c|tenant|", StringComparison.OrdinalIgnoreCase)) { secGroupId = secGroupId.Substring(secGroupId.IndexOf("c:0t.c|tenant|") + 14); }
            if (secGroupId.Contains("c:0u.c|tenant|", StringComparison.OrdinalIgnoreCase)) { secGroupId = secGroupId[(secGroupId.IndexOf("c:0u.c|tenant|") + 14)..]; }
            if (secGroupId.Contains("c:0o.c|federateddirectoryclaimprovider|", StringComparison.OrdinalIgnoreCase)) { secGroupId = secGroupId.Substring(secGroupId.IndexOf("c:0o.c|federateddirectoryclaimprovider|") + 39); }
            if (secGroupId.Contains("_o"))
            {
                secGroupId = secGroupId.Substring(0, secGroupId.IndexOf("_o"));
                isOwners = true;
            }

            groupId = Guid.Parse(secGroupId);

            return isOwners;
        }

        internal async Task<DirectoryGroupUserEmails> GetUsersAsync(string sgTitle, Guid sgId, bool isOwner, List<DirectoryGroupUserEmails>? listKnownGroups = null)
        {
            _logger.Info(GetType().Name, $"Getting users from Security Group '{sgTitle}' ID '{sgId}'");

            if (listKnownGroups != null)
            {
                DirectoryGroupUserEmails? knownGroup = listKnownGroups.SingleOrDefault(sg => sg.GroupID == sgId && sg.IsOwners == isOwner);
                if (knownGroup != null) { return knownGroup; }
            }

            DirectoryGroupUserEmails groupUserEmails;
            try
            {
                IEnumerable<GraphUser> sgMembers;
                if (isOwner) { sgMembers = await GetOwnersAsync(sgId); }
                else { sgMembers = await GetMembersTransitiveAsync(sgId); }


                if (!sgMembers.Any())
                {
                    groupUserEmails = new(sgId, sgTitle, isOwner, "Security group is empty");
                }
                else
                {
                    string users = string.Join(" ", sgMembers.Where(com => com.Type.ToString() == "user").Select(com => com.UserPrincipalName).ToList());
                    users += " " + string.Join(" ", sgMembers.Where(com => com.Type.ToString() == "SecurityGroup").Select(com => $"{com.DisplayName} ({com.Id})"));

                    groupUserEmails = new(sgId, sgTitle, isOwner, users);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(GetType().Name, "Security Group", sgTitle, ex);
                groupUserEmails = new(sgId, sgTitle, isOwner, "", ex.Message);
            }

            listKnownGroups?.Add(groupUserEmails);
            return groupUserEmails;
        }

        internal static bool IsSystemGroup(out DirectoryGroupUserEmails groupUserEmails, string groupTitle)
        {
            groupUserEmails = new(Guid.Empty, groupTitle, false, groupTitle);

            if (groupTitle.Equals("Everyone", StringComparison.OrdinalIgnoreCase)
                || groupTitle.Equals("Everyone except external users", StringComparison.OrdinalIgnoreCase)
                || groupTitle.Equals("Global Administrator", StringComparison.OrdinalIgnoreCase)
                || groupTitle.Equals("SharePoint Administrator", StringComparison.OrdinalIgnoreCase)
                || groupTitle.Equals("All Company Members", StringComparison.OrdinalIgnoreCase)
                || groupTitle.Equals("All Users (windows)", StringComparison.OrdinalIgnoreCase)
                || groupTitle.Equals("ReadOnlyAccessToTenantAdminSite", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal async Task<IEnumerable<GraphUser>> GetOwnersAsync(Guid groupId)
        {
            string endpointPath = $"/groups/{groupId}/owners?$select=*";

            var collOwners = await new GraphAPIHandler(_logger, _appInfo).GetCollectionAsync<GraphUser>(endpointPath);

            return collOwners;
        }

        internal async Task<IEnumerable<GraphUser>> GetMembersAsync(Guid groupId)
        {
            string endpointPath = $"/groups/{groupId}/members?$select=*";

            var collMembers = await new GraphAPIHandler(_logger, _appInfo).GetCollectionAsync<GraphUser>(endpointPath);

            return collMembers;
        }

        internal async Task<IEnumerable<GraphUser>> GetMembersTransitiveAsync(Guid groupId)
        {
            string endpointPath = $"/groups/{groupId}/transitiveMembers?$select=*";

            var collMembers = await new GraphAPIHandler(_logger, _appInfo).GetCollectionAsync<GraphUser>(endpointPath);

            return collMembers;
        }

        internal async Task<string> GetMembersTotalCountAsync(Guid groupId)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Getting total count of members from Group '{groupId}'");

            string endpointPath = $"/groups/{groupId}/transitiveMembers/$count";

            // THIS IS NOT CORRECT BECAUSE IT NEEDS BELOW HEADERS. IT NEEDS A NEW HEADER
            // ConsistencyLevel: eventual
            // Accept: text / plain

            Dictionary<string, string> additionalHeader = new()
            {
                {"ConsistencyLevel", "eventual" }
            };

            //var uriString = new GraphAPIHandler(_logger, _appInfo).GetUriString(endpointPath);
            //HttpMessageWriter messageWriter = new(_appInfo, HttpMethod.Get, uriString, "text/plain", additionalHeaders: additionalHeader);

            //string response = await HttpClientService.SendHttpRequestMessageAsync(_logger, messageWriter, _appInfo.CancelToken);

            var response = await new GraphAPIHandler(_logger, _appInfo).GetAsync(endpointPath, "text/plain", additionalHeader);

            return response;

        }

    }
}
