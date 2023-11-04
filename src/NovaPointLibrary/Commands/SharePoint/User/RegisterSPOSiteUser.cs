using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.User
{
    internal class RegisterSPOSiteUser
    {
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;
        private readonly string AccessToken;
        internal RegisterSPOSiteUser(NPLogger logger, Authentication.AppInfo appInfo, string accessToken)
        {
            _logger = logger;
            _appInfo = appInfo;
            AccessToken = accessToken;
        }

        internal void CSOM(string siteUrl, string userUpn)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.CSOM";
            _logger.LogTxt(methodName, $"Start registering user '{userUpn}' in site '{siteUrl}'");

            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            if (string.IsNullOrWhiteSpace(userUpn))
            {
                throw new Exception("Admin UPN cannot be empty");
            }
            else
            {
                Microsoft.SharePoint.Client.User user = clientContext.Web.EnsureUser(userUpn);
                user.Update();
                clientContext.Load(user);
                clientContext.ExecuteQueryRetry();
                _logger.LogTxt(methodName, $"Finish registering user '{userUpn}' in site '{siteUrl}'");
            }
        }
    }
}
