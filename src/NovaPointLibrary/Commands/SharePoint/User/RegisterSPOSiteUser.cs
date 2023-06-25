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
        private readonly LogHelper _logHelper;
        private readonly Authentication.AppInfo _appInfo;
        private readonly string AccessToken;
        internal RegisterSPOSiteUser(LogHelper logHelper, Authentication.AppInfo appInfo, string accessToken)
        {
            _logHelper = logHelper;
            _appInfo = appInfo;
            AccessToken = accessToken;
        }

        internal void CSOM(string siteUrl, string userUpn)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.CSOM";
            _logHelper.AddLogToTxt(methodName, $"Start registering user '{userUpn}' in site '{siteUrl}'");

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
                _logHelper.AddLogToTxt(methodName, $"Finish registering user '{userUpn}' in site '{siteUrl}'");
            }
        }
    }
}
