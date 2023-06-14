using Microsoft.SharePoint.Client;
using Microsoft.Online.SharePoint.TenantAdministration;
using PnP.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using NovaPointLibrary.Solutions;

namespace NovaPointLibrary.Commands.Site
{
    //References:
    //https://pnp.github.io/powershell/cmdlets/Get-PnPSiteCollectionAdmin.html
    //https://github.com/pnp/powershell/blob/dev/src/Commands/Site/GetSiteCollectionAdmin.cs
    //https://www.sharepointdiary.com/2016/06/sharepoint-online-powershell-get-site-collection-administrators.html
    internal class GetSiteCollectionAdmin
    {
        private LogHelper _logHelper;
        private readonly string AccessToken;

        internal GetSiteCollectionAdmin(LogHelper logHelper, string accessToken)
        {
            _logHelper = logHelper;
            AccessToken = accessToken;
        }
        internal IEnumerable<Microsoft.SharePoint.Client.User> Csom(string siteUrl)
        {
            _logHelper = new(_logHelper, $"{GetType().Name}.Csom");

            _logHelper.AddLogToTxt($"Start obtaining Site Collection Administrators for '{siteUrl}'");

            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            var retrievalExpressions = new Expression<Func<Microsoft.SharePoint.Client.User, object>>[]
            {
                u => u.Id,
                u => u.AadObjectId,
                u => u.Title,
                u => u.LoginName,
                u => u.Email,
                u => u.UserPrincipalName,
                u => u.IsSiteAdmin,
                u => u.UserId,
                u => u.PrincipalType,
                u => u.Alerts.Include(
                    a => a.Title,
                    a => a.Status),
                u => u.Groups.Include(
                    g => g.Id,
                    g => g.Title,
                    g => g.LoginName)
            };

            var query = clientContext.Web.SiteUsers.Where(u => u.IsSiteAdmin);                
            var siteCollectionAdminUsers = clientContext.LoadQuery(query.Include(retrievalExpressions));
            clientContext.ExecuteQueryRetry();

            _logHelper.AddLogToTxt($"Successfully obtained Site Collection Administrators for '{siteUrl}'");
            return siteCollectionAdminUsers;
        }
    }
}
