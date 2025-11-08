using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Logging;
using System.Reflection.Metadata.Ecma335;


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
                    // Manage this here
                    //if (sgTitle.Contains("SLinkClaim")) { return collSgUserEmails; }
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

        // CHANGE TO PRIVATE
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

            //DirectoryGroupUserEmails collSgUserEmails;

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

            if (groupTitle == "Everyone"
                || groupTitle == "Everyone except external users"
                || groupTitle == "Global Administrator"
                || groupTitle == "SharePoint Administrator"
                || groupTitle == "All Company Members"
                || groupTitle == "All Users (windows)"
                || groupTitle == "ReadOnlyAccessToTenantAdminSite")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static List<DirectoryGroupUserEmails> GetSystemGroup(string groupTitle)
        {
            List<DirectoryGroupUserEmails> listOfUsers = [];
            if (IsSystemGroup(groupTitle))
            {
                listOfUsers.Add(new(Guid.Empty, groupTitle, false, groupTitle));
            }
            return listOfUsers;
        }

        internal static bool IsSystemGroup(string secGroupTitle)
        {

            if (secGroupTitle == "Everyone"
                || secGroupTitle == "Everyone except external users"
                || secGroupTitle == "Global Administrator"
                || secGroupTitle == "SharePoint Administrator"
                || secGroupTitle == "All Company Members"
                || secGroupTitle == "All Users (windows)"
                || secGroupTitle == "ReadOnlyAccessToTenantAdminSite")
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


    }
}
