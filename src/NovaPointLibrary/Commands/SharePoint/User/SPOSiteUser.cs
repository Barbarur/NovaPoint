using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.User
{
    internal class SPOSiteUser
    {
        private NPLogger _logger;
        private readonly AppInfo _appInfo;

        internal SPOSiteUser(NPLogger logger, AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<Microsoft.SharePoint.Client.User?> GetAsync(string siteUrl, string userUPN)
        {
            _appInfo.IsCancelled();

            string userLoginName = "i:0#.f|membership|" + userUPN;

            _logger.LogTxt(GetType().Name, $"Getting '{userUPN}', LoginName '{userLoginName}' from Site '{siteUrl}'");

            var clientContext = await _appInfo.GetContext(siteUrl);

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

            try
            {
                Microsoft.SharePoint.Client.User user = clientContext.Web.SiteUsers.GetByLoginName(userLoginName);

                clientContext.Load(user, retrievalExpressions);
                clientContext.ExecuteQueryRetry();

                return user;
            }
            catch
            {
                _logger.AddLogToTxt($"User '{userUPN}' no found in Site '{siteUrl}'");
                return null;
            }
        }

        internal async Task Register(string siteUrl, string userUPN)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Start registering '{userUPN}' from Site '{siteUrl}'");

            var clientContext = await _appInfo.GetContext(siteUrl);

            Microsoft.SharePoint.Client.User user = clientContext.Web.EnsureUser(userUPN);
            user.Update();
            clientContext.Load(user);
            clientContext.ExecuteQueryRetry();
        }

        // Reference:
        // https://pnp.github.io/powershell/cmdlets/Remove-PnPUser.html
        // https://github.com/pnp/powershell/blob/dev/src/Commands/Principals/RemoveUser.cs
        internal async Task RemoveAsync(string siteUrl, string userUPN)
        {
            _appInfo.IsCancelled();

            string userLoginName = "i:0#.f|membership|" + userUPN;

            _logger.LogTxt(GetType().Name, $"Removing '{userUPN}', LoginName '{userLoginName}' from Site '{siteUrl}'");

            var siteContext = await _appInfo.GetContext(siteUrl);

            siteContext.Web.SiteUsers.RemoveByLoginName(userLoginName);
            siteContext.ExecuteQueryRetry();
        }
    }
}
