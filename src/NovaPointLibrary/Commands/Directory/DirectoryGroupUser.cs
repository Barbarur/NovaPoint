using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Logging;


namespace NovaPointLibrary.Commands.Directory
{
    internal class DirectoryGroupUser
    {
        private readonly ILogger _logger;
        private readonly IAppClient _appInfo;
        
        private const string _reportedUserProperties = "?$select=id,displayName,userPrincipalName";

        internal DirectoryGroupUser(ILogger logger, IAppClient appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<DirectoryGroupUserEmails> GetUsersAsync(Microsoft.SharePoint.Client.Principal secGroup, List<DirectoryGroupUserEmails>? listKnownGroups = null)
        {
            DirectoryGroupUserEmails sgUserEmails;
            try
            {
                _logger.Debug(GetType().Name, $"Principal '{secGroup.Title}' LoginName '{secGroup.LoginName}'");

                if (!TryGetGroupId(secGroup.LoginName, out Guid sgGuid, out bool isOwner))
                {
                    return GetClaimPrincipal(secGroup.Title);
                }

                sgUserEmails = await GetUsersAsync(secGroup.Title, sgGuid, isOwner, listKnownGroups);
            }
            catch (Exception ex)
            {
                sgUserEmails = new(Guid.Empty, secGroup.Title, false, $"{secGroup.Title} ({secGroup.LoginName})", ex.Message);
            }

            return sgUserEmails;
        }

        // Claims such as 'Everyone except external users' carry no directory object id,
        // so there is nothing to look up in Entra ID.
        internal static bool IsClaimPrincipal(string loginName)
        {
            if (loginName.Contains("i:0#.f|membership|", StringComparison.OrdinalIgnoreCase)) { return false; }

            return !TryGetGroupId(loginName, out _, out _);
        }

        internal static DirectoryGroupUserEmails GetClaimPrincipal(string title)
        {
            return DirectoryGroupUserEmails.GetClaimPrincipal(title, DirectoryWellKnownPrincipal.GetUsersDescription(title));
        }

        private static bool TryGetGroupId(string secGroupId, out Guid groupId, out bool isOwners)
        {
            isOwners = false;
            if (secGroupId.Contains("c:0t.c|tenant|", StringComparison.OrdinalIgnoreCase)) { secGroupId = secGroupId.Substring(secGroupId.IndexOf("c:0t.c|tenant|", StringComparison.OrdinalIgnoreCase) + 14); }
            if (secGroupId.Contains("c:0u.c|tenant|", StringComparison.OrdinalIgnoreCase)) { secGroupId = secGroupId[(secGroupId.IndexOf("c:0u.c|tenant|", StringComparison.OrdinalIgnoreCase) + 14)..]; }
            if (secGroupId.Contains("c:0o.c|federateddirectoryclaimprovider|", StringComparison.OrdinalIgnoreCase)) { secGroupId = secGroupId.Substring(secGroupId.IndexOf("c:0o.c|federateddirectoryclaimprovider|", StringComparison.OrdinalIgnoreCase) + 39); }
            if (secGroupId.Contains("_o"))
            {
                secGroupId = secGroupId.Substring(0, secGroupId.IndexOf("_o", StringComparison.Ordinal));
                isOwners = true;
            }

            return Guid.TryParse(secGroupId, out groupId);
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
                var directoryObject = await GetDirectoryObjectAsync(sgId);

                if (directoryObject != null && directoryObject.IsDirectoryRole)
                {
                    string roleName = string.IsNullOrWhiteSpace(directoryObject.DisplayName) ? sgTitle : directoryObject.DisplayName;

                    groupUserEmails = DirectoryGroupUserEmails.GetDirectoryRole(sgId, roleName, isOwner, DirectoryWellKnownPrincipal.GetUsersDescription(roleName));
                }
                else
                {
                    IEnumerable<GraphUser> sgMembers;
                    if (isOwner) { sgMembers = await GetOwnersAsync(sgId, _reportedUserProperties); }
                    else { sgMembers = await GetMembersTransitiveAsync(sgId, _reportedUserProperties); }


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
            }
            catch (Exception ex)
            {
                _logger.Error(GetType().Name, "Security Group", sgTitle, ex);
                groupUserEmails = new(sgId, sgTitle, isOwner, "", ex.Message);
            }

            listKnownGroups?.Add(groupUserEmails);
            return groupUserEmails;
        }

        // Resolves what the id actually points at. A directory role and a security group
        // are both surfaced by SharePoint as a 'SecurityGroup' principal, but only the
        // latter exists under /groups.
        internal async Task<GraphDirectoryObject> GetDirectoryObjectAsync(Guid objectId)
        {
            string endpointPath = $"/directoryObjects/{objectId}";

            var directoryObject = await new GraphAPIHandler(_logger, _appInfo).GetObjectAsync<GraphDirectoryObject>(endpointPath);

            return directoryObject;
        }

        internal async Task<IEnumerable<GraphUser>> GetOwnersAsync(Guid groupId, string optionalQuery = "")
        {
            string endpointPath = $"/groups/{groupId}/owners" + optionalQuery;

            var collOwners = await new GraphAPIHandler(_logger, _appInfo).GetCollectionAsync<GraphUser>(endpointPath);

            return collOwners;
        }

        internal async Task<IEnumerable<GraphUser>> GetMembersAsync(Guid groupId, string optionalQuery = "")
        {
            string endpointPath = $"/groups/{groupId}/members" + optionalQuery;

            var collMembers = await new GraphAPIHandler(_logger, _appInfo).GetCollectionAsync<GraphUser>(endpointPath);

            return collMembers;
        }

        internal async Task<IEnumerable<GraphUser>> GetMembersTransitiveAsync(Guid groupId, string optionalQuery = "")
        {
            string endpointPath = $"/groups/{groupId}/transitiveMembers" + optionalQuery;

            var collMembers = await new GraphAPIHandler(_logger, _appInfo).GetCollectionAsync<GraphUser>(endpointPath);

            return collMembers;
        }

        // The directory object is referenced by URL, so the body cannot be built from an
        // anonymous object; '@odata.id' is not a legal C# member name.
        internal async Task AddMemberAsync(Guid groupId, string userId)
        {
            string endpointPath = $"/groups/{groupId}/members/$ref";
            string content = $"{{\"@odata.id\":\"https://graph.microsoft.com/v1.0/directoryObjects/{userId}\"}}";

            // Graph answers 204 No Content on success, so there is nothing to deserialize.
            await new GraphAPIHandler(_logger, _appInfo).PostAsync(endpointPath, content);

            _logger.Info(GetType().Name, $"Added user '{userId}' as member of group '{groupId}'");
        }

        internal async Task<string> GetMembersTotalCountAsync(Guid groupId)
        {
            string endpointPath = $"/groups/{groupId}/transitiveMembers/$count";

            Dictionary<string, string> additionalHeader = new()
            {
                {"ConsistencyLevel", "eventual" }
            };

            var response = await new GraphAPIHandler(_logger, _appInfo).GetAsync(endpointPath, "text/plain", additionalHeader);

            return response;

        }

    }

}
