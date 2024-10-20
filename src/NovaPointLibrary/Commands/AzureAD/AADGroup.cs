
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Logging;

namespace NovaPointLibrary.Commands.AzureAD
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

        internal async Task<IEnumerable<Microsoft365User>> GetOwnersAndMembersAsync(string groupId)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.GraphOwnersAndMembersAsync";
            _logger.Info(methodName, $"Getting Owners and Members of Group '{groupId}'");

            List<Microsoft365User> collUsers = new();
            collUsers.AddRange( await GetOwnersAsync(groupId) );
            collUsers.AddRange( await GetMembersAsync(groupId) );

            return collUsers;
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
