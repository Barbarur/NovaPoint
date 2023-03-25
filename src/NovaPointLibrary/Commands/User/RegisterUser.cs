using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.User
{
    internal class RegisterUser
    {
        private LogHelper _LogHelper;
        private readonly string AccessToken;

        internal RegisterUser(LogHelper logHelper, string accessToken)
        {
            _LogHelper = logHelper;
            AccessToken = accessToken;
        }

        internal void Csom(string siteUrl, string userUpn)
        {
            _LogHelper = new(_LogHelper, $"{GetType().Name}.CsomSingle");
            _LogHelper.AddLogToTxt($"Start registering user '{userUpn}' in site '{siteUrl}'");

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
                _LogHelper.AddLogToTxt($"Successfully registered user '{userUpn}' in site '{siteUrl}'");
            }
        }
    }
}
