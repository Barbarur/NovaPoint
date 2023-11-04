using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.User
{
    internal class RemoveUser
    {
        private readonly NPLogger _logger;
        private readonly string AccessToken;
        internal RemoveUser(NPLogger logger, string accessToken)
        {
            _logger = logger;
            AccessToken = accessToken;
        }

        // Reference:
        // https://pnp.github.io/powershell/cmdlets/Remove-PnPUser.html
        // https://github.com/pnp/powershell/blob/dev/src/Commands/Principals/RemoveUser.cs
        internal void Csom(string siteUrl, string userUPN)
        {
            _logger.AddLogToTxt($"Start obtaining Users for '{siteUrl}'");
            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            var retrievalExpressions = new Expression<Func<Microsoft.SharePoint.Client.User, object>>[]
            {
                u => u.Id,
                u => u.LoginName,
                u => u.Email
            };

            string userLoginName = "i:0#.f|membership|" + userUPN;
            _logger.AddLogToTxt($"User LoginName '{userLoginName}'");

            try
            {
                _logger.AddLogToTxt($"Removing user '{userUPN}' from Site '{siteUrl}'");
                clientContext.Web.SiteUsers.RemoveByLoginName(userLoginName);
                clientContext.ExecuteQueryRetry();
            }
            catch
            {

                string message = $"You cannot remove '{userUPN}' from the site '{siteUrl}'";
                _logger.AddLogToTxt(message);
                Exception exception = new(message);
                throw exception;
            }
        }
    }
}
