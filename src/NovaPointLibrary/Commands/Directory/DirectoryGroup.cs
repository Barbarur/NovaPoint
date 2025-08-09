using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Logging;


namespace NovaPointLibrary.Commands.Directory
{
    internal class DirectoryGroup
    {
        private readonly LoggerSolution _logger;
        private readonly AppInfo _appInfo;


        internal DirectoryGroup(LoggerSolution logger, AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<GraphGroup> GetAsync(string groupId, string optionalQuery = "")
        {
            string api = $"/groups/{groupId}" + optionalQuery;

            var group = await new GraphAPIHandler(_logger, _appInfo).GetObjectAsync<GraphGroup>(api);

            return group;
        }

        internal async Task<IEnumerable<GraphGroup>> GetAllAsync(string optionalQuery = "")
        {
            string endpointPath = $"/groups" + optionalQuery;

            var groups = await new GraphAPIHandler(_logger, _appInfo).GetCollectionAsync<GraphGroup>(endpointPath);

            return groups;
        }

        internal async Task<IEnumerable<GraphUser>> GetOwnersAsync(string groupId)
        {
            string endpointPath = $"/groups/{groupId}/owners?$select=*";

            var collOwners = await new GraphAPIHandler(_logger, _appInfo).GetCollectionAsync<GraphUser>(endpointPath);

            return collOwners;
        }

        internal async Task<IEnumerable<GraphUser>> GetMembersAsync(string groupId)
        {
            string endpointPath = $"/groups/{groupId}/members?$select=*";

            var collMembers = await new GraphAPIHandler(_logger, _appInfo).GetCollectionAsync<GraphUser>(endpointPath);

            return collMembers;
        }

        internal async Task<string> GetMembersTotalCountAsync(string groupId)
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

        internal async Task RemoveGroupAsync(string groupId)
        {
            string url = $"/groups/{groupId}";

            await new GraphAPIHandler(_logger, _appInfo).DeleteAsync(url);
        }

    }
}
