using Microsoft.Graph;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Solutions;
using PnP.Framework.Modernization.Transform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.AzureAD
{
    internal class GetAzureADGroup
    {
        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;
        private readonly string _graphAccessToken;


        internal GetAzureADGroup(NPLogger logger, AppInfo appInfo, string graphAccessToken)
        {
            _logger = logger;
            _appInfo = appInfo;
            _graphAccessToken = graphAccessToken;
        }

        internal async Task<IEnumerable<Microsoft365User>> GraphOwnersAndMembersAsync(string groupId)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.GraphOwnersAndMembersAsync";
            _logger.LogTxt(methodName, $"Start getting Owners and Members of Group '{groupId}'");

            List<Microsoft365User> collUsers = new();
            collUsers.AddRange( await GraphOwnersAsync(groupId) );
            collUsers.AddRange( await GraphMembersAsync(groupId) );

            _logger.LogTxt(methodName, $"Finish getting Owners and Members of Group '{groupId}'");
            return collUsers;
        }

        internal async Task<IEnumerable<Microsoft365User>> GraphOwnersAsync(string groupId)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.GraphOwnersAsync";
            _logger.LogTxt(methodName, $"Start getting Owners of Group '{groupId}'");

            string url = $"/groups/{groupId}/owners?$select=*";

            var collMembers = await new GraphAPIHandler(_logger, _appInfo, _graphAccessToken).GetCollectionAsync<Microsoft365User>(url);

            _logger.LogTxt(methodName, $"Finish getting Owners of Group '{groupId}'");
            return collMembers;
        }

        internal async Task<IEnumerable<Microsoft365User>> GraphMembersAsync(string groupId)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.GraphMembersAsync";
            _logger.LogTxt(methodName, $"Start getting Members of Group '{groupId}'");

            string url = $"/groups/{groupId}/members?$select=*";

            var collMembers = await new GraphAPIHandler(_logger, _appInfo, _graphAccessToken).GetCollectionAsync<Microsoft365User>(url);

            _logger.LogTxt(methodName, $"Finish getting Members of Group '{groupId}'");
            return collMembers;
        }

    }
}
