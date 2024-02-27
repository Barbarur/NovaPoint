using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.User
{
    internal class GetSPOUser
    {
        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;
        private readonly string AccessToken;
        internal GetSPOUser(NPLogger logger, AppInfo appInfo, string accessToken)
        {
            _logger = logger;
            _appInfo = appInfo;
            AccessToken = accessToken;
        }
        // Reference:
        // https://github.com/pnp/powershell/blob/dev/src/Commands/Base/PipeBinds/UserPipeBind.cs
        internal Microsoft.SharePoint.Client.User? CSOMSingle(string siteUrl, string userUPN)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.CSOMSingle";
            _logger.LogTxt(methodName, $"Start obtaining User '{userUPN}' from Site '{siteUrl}'");

            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            var retrievalExpressions = new Expression<Func<Microsoft.SharePoint.Client.User, object>>[]
            {
                u => u.Id,
                u => u.Title,
                u => u.LoginName,
                u => u.UserPrincipalName,
                u => u.Email,
                u => u.IsShareByEmailGuestUser,
                u => u.IsSiteAdmin,
                u => u.UserId,
                u => u.IsHiddenInUI,
                u => u.PrincipalType,
                u => u.Alerts.Include(
                    a => a.Title,
                    a => a.Status),
                u => u.Groups.Include(
                    g => g.Id,
                    g => g.Title,
                    g => g.LoginName)
            };

            string userLoginName = "i:0#.f|membership|" + userUPN;
            _logger.LogTxt(methodName, $"User LoginName '{userLoginName}'");

            try
            {

                Microsoft.SharePoint.Client.User user = clientContext.Web.SiteUsers.GetByLoginName(userLoginName);

                clientContext.Load(user, retrievalExpressions);
                clientContext.ExecuteQueryRetry();

                _logger.LogTxt(methodName, $"User '{userUPN}' found in Site '{siteUrl}'");
                return user;

            }
            catch
            {
                _logger.LogTxt(methodName, $"User '{userUPN}' no found in Site '{siteUrl}'");
                return null;
            }

        }


        // Reference:
        // https://pnp.github.io/powershell/cmdlets/Get-PnPUser.html
        // https://github.com/pnp/powershell/blob/dev/src/Commands/Principals/GetUser.cs
        // https://www.sharepointdiary.com/2017/02/sharepoint-online-get-all-users-using-powershell.html
        internal List<Microsoft.SharePoint.Client.User> CSOMAll(string siteUrl, bool WithRightsAssigned = false, bool WithRightsAssignedDetailed = false)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.CSOMAll";
            _logger.LogTxt(methodName, $"Start getting Users for Site '{siteUrl}'");

            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            var retrievalExpressions = new Expression<Func<Microsoft.SharePoint.Client.User, object>>[]
            {
                u => u.Id,
                u => u.Title,
                u => u.LoginName,
                u => u.UserPrincipalName,
                u => u.Email,
                u => u.IsShareByEmailGuestUser,
                u => u.IsSiteAdmin,
                u => u.UserId,
                u => u.IsHiddenInUI,
                u => u.PrincipalType,
                u => u.Alerts.Include(
                    a => a.Title,
                    a => a.Status),
                u => u.Groups.Include(
                    g => g.Id,
                    g => g.Title,
                    g => g.LoginName)
            };

            UserCollection collUsers = clientContext.Web.SiteUsers;
            clientContext.Load(collUsers, u => u.Include(retrievalExpressions));

            clientContext.ExecuteQuery();

            List<Microsoft.SharePoint.Client.User> listUsersReturned = new();
            listUsersReturned.AddRange(clientContext.Web.SiteUsers);
            listUsersReturned.RemoveAll(u => u.Title == "System Account" || u.Title == "SharePoint App" || u.Title == "NT Service\\spsearch");
            
            _logger.LogTxt(methodName, $"Finish getting Users for Site '{siteUrl}'");
            return listUsersReturned;
        }
    }
}
