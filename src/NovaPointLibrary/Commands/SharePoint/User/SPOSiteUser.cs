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
    internal class SPOSiteUser
    {
        private NPLogger _logger;
        private readonly AppInfo _appInfo;

        internal SPOSiteUser(NPLogger logger, AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task Register(string siteUrl, string userUPN)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.Register";
            _logger.LogTxt(methodName, $"Start registering '{userUPN}' from Site '{siteUrl}'");

            var clientContext = await _appInfo.GetContext(_logger, siteUrl);

            Microsoft.SharePoint.Client.User user = clientContext.Web.EnsureUser(userUPN);
            user.Update();
            clientContext.Load(user);
            clientContext.ExecuteQueryRetry();

            _logger.LogTxt(methodName, $"Finish registering '{userUPN}' from Site '{siteUrl}'");
        }

        // Reference:
        // https://pnp.github.io/powershell/cmdlets/Remove-PnPUser.html
        // https://github.com/pnp/powershell/blob/dev/src/Commands/Principals/RemoveUser.cs
        internal async Task Remove(string siteUrl, string userUPN)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.{{";
            _logger.LogTxt(methodName, $"Start removing '{userUPN}' from Site '{siteUrl}'");

            var retrievalExpressions = new Expression<Func<Microsoft.SharePoint.Client.User, object>>[]
            {
                u => u.Id,
                u => u.LoginName,
                u => u.Email
            };

            string userLoginName = "i:0#.f|membership|" + userUPN;
            _logger.LogTxt(methodName, $"User LoginName '{userLoginName}'");

            var siteContext = await _appInfo.GetContext(_logger, siteUrl);

            siteContext.Web.SiteUsers.RemoveByLoginName(userLoginName);
            siteContext.ExecuteQueryRetry();

            _logger.LogTxt(methodName, $"Finish removing '{userUPN}' from Site '{siteUrl}'");
        }
    }
}
