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
        private LogHelper _logHelper;
        private AppInfo _appInfo;
        private readonly string _graphAccessToken;


        internal GetAzureADGroup(LogHelper logHelper, AppInfo appInfo, string graphAccessToken)
        {
            _logHelper = logHelper;
            _appInfo = appInfo;
            _graphAccessToken = graphAccessToken;
        }

        internal async Task<IEnumerable<Microsoft365User>> GraphOwnersAndMembersAsync(string groupId)
        {
            _appInfo.IsCancelled();
            _logHelper.AddLogToTxt($"{GetType().Name}.GraphOwnersAndMembersAsync - Start getting Owners and Members of Group '{groupId}'");

            List<Microsoft365User> collUsers = new();
            collUsers.AddRange( await GraphOwnersAsync(groupId) );
            collUsers.AddRange( await GraphMembersAsync(groupId) );

            _logHelper.AddLogToTxt($"{GetType().Name}.GraphOwnersAndMembersAsync - Finish getting Owners and Members of Group '{groupId}'");
            return collUsers;
        }

        internal async Task<IEnumerable<Microsoft365User>> GraphOwnersAsync(string groupId)
        {
            _appInfo.IsCancelled();
            _logHelper.AddLogToTxt($"{GetType().Name}.GraphOwnersAsync - Start getting Owners of Group '{groupId}'");

            string url = $"/groups/{groupId}/owners?$select=*";

            var collMembers = await new GraphAPIHandler(_logHelper, _appInfo, _graphAccessToken).GetCollectionAsync<Microsoft365User>(url);

            _logHelper.AddLogToTxt($"{GetType().Name}.GraphOwnersAsync - Finish getting Owners of Group '{groupId}'");
            return collMembers;
        }

        internal async Task<IEnumerable<Microsoft365User>> GraphMembersAsync(string groupId)
        {
            _appInfo.IsCancelled();
            _logHelper.AddLogToTxt($"{GetType().Name}.GraphMembersAsync - Start getting Members of Group '{groupId}'");

            string url = $"/groups/{groupId}/members?$select=*";

            var collMembers = await new GraphAPIHandler(_logHelper, _appInfo, _graphAccessToken).GetCollectionAsync<Microsoft365User>(url);

            _logHelper.AddLogToTxt($"{GetType().Name}.GraphMembersAsync - Finish getting Members of Group '{groupId}'");
            return collMembers;
        }

    }
}
