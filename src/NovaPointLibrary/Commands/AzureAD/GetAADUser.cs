using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.AzureAD
{
    internal class GetAADUser
    {
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;

        internal GetAADUser(NPLogger logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<GraphUser> GetSignedInUser()
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, "Getting Signed-in user");

            string url = "/me";

            GraphUser graphUser = await new GraphAPIHandler(_logger, _appInfo).GetObjectAsync<GraphUser>(url);

            return graphUser;
        }

    }
}
