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
    internal class SPOSiteGroupUsersCSOM
    {
        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;

        internal SPOSiteGroupUsersCSOM(NPLogger logger, AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<UserCollection> GetAsync(string siteUrl, string groupName)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Getting users from SharePoint Group '{groupName}'in site '{siteUrl}'");

            var clientContext = await _appInfo.GetContext(siteUrl);

            var retrievalExpressions = new Expression<Func<Microsoft.SharePoint.Client.User, object>>[]
            {
                u => u.AadObjectId,
                u => u.Alerts.Include(
                    a => a.Title,
                    a => a.Status),
                u => u.Id,
                u => u.Email,
                u => u.Groups.Include(
                    g => g.Id,
                    g => g.Title,
                    g => g.LoginName),
                u => u.IsHiddenInUI,
                u => u.IsShareByEmailGuestUser,
                u => u.IsSiteAdmin,
                u => u.LoginName,
                u => u.PrincipalType,
                u => u.Title,
                u => u.UserId,
                u => u.UserPrincipalName
            };

            var group = clientContext.Web.SiteGroups.GetByName(groupName);
            UserCollection members = group.Users;

            clientContext.Load(group);
            clientContext.Load(members, m => m.Include(retrievalExpressions));
            clientContext.ExecuteQuery();

            return members;
        }
    }
}
