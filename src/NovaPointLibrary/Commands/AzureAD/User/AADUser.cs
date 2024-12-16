using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.AzureAD.User
{
    internal class AADUser
    {
        private readonly LoggerSolution _logger;
        private readonly Authentication.AppInfo _appInfo;

        internal AADUser(LoggerSolution logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<GraphUser> GetSignedInUserAsync()
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, "Getting Signed-in user");

            string url = "/me";

            GraphUser graphUser = await new GraphAPIHandler(_logger, _appInfo).GetObjectAsync<GraphUser>(url);

            return graphUser;
        }

        internal async Task<GraphUser> GetUserAsync(string userUPN, string? select = null)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Getting Azure AD user {userUPN}");

            // UPDATE!!
            //string url = $"/users/{userUPN}?$select=accountEnabled,displayName,mail";
            string url = $"/users/{userUPN}";
            if (select != null)
            {
                url += $"?$select={select}";
            }

            GraphUser graphUser = await new GraphAPIHandler(_logger, _appInfo).GetObjectAsync<GraphUser>(url);

            return graphUser;
        }
    }
}
