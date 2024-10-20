using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Logging;

namespace NovaPointLibrary.Commands.AzureAD
{
    internal class GetAADUser
    {
        private readonly LoggerSolution _logger;
        private readonly Authentication.AppInfo _appInfo;

        internal GetAADUser(LoggerSolution logger, Authentication.AppInfo appInfo)
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

        internal async Task<GraphUser> GetUserAsync(string userUPN)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Getting Azure AD user {userUPN}");

            string url = $"/users/{userUPN}";

            GraphUser graphUser = await new GraphAPIHandler(_logger, _appInfo).GetObjectAsync<GraphUser>(url);

            return graphUser;
        }

    }
}
