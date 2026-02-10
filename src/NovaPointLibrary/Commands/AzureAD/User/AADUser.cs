using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Logging;


namespace NovaPointLibrary.Commands.AzureAD.User
{
    internal class AADUser
    {
        private readonly ILogger _logger;
        private readonly IAppClient _appInfo;

        internal AADUser(ILogger logger, IAppClient appInfo)
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
