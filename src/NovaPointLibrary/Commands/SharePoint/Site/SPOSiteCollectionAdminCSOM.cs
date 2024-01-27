using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    internal class SPOSiteCollectionAdminCSOM
    {
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;

        internal SPOSiteCollectionAdminCSOM(NPLogger logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }


        internal async Task SetAsync(string siteUrl, string userAdmin)
        {
            await SetAsync(siteUrl, userAdmin, true);
        }

        internal async Task RemoveAsync(string siteUrl, string userAdmin)
        {
            string upnCoded = userAdmin.Trim().Replace("@", "_").Replace(".", "_");

            if (siteUrl.Contains(upnCoded, StringComparison.OrdinalIgnoreCase) && siteUrl.Contains("-my.sharepoint.com", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("This is user's OneDrive. User will not be removed as Site Admin.");
            }
            else
            {
                await SetAsync(siteUrl, userAdmin, false);
            }
        }

        internal async Task RemoveForceAsync(string siteUrl, string userAdmin)
        {
            await SetAsync(siteUrl, userAdmin, false);
        }

        private async Task SetAsync(string siteUrl, string userAdmin, bool isSiteAdmin)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Processing '{siteUrl}' setting '{userAdmin}' IsSiteAdmin '{isSiteAdmin}'");

            try
            {
                var tenantContext = new Tenant(await _appInfo.GetContext(_appInfo.AdminUrl));
                tenantContext.SetSiteAdmin(siteUrl, userAdmin, isSiteAdmin);
                tenantContext.Context.ExecuteQueryRetry();
            }
            catch
            {
                var siteContext = await _appInfo.GetContext(siteUrl);
                var user = siteContext.Web.EnsureUser(userAdmin);
                user.IsSiteAdmin = isSiteAdmin;
                user.Update();
                siteContext.Load(user);
                siteContext.ExecuteQueryRetry();
            }
        }

        internal async Task<IEnumerable<Microsoft.SharePoint.Client.User>> GetAsync(string siteUrl)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Getting Site Collection Administrators for '{siteUrl}'");

            var retrievalExpressions = new Expression<Func<Microsoft.SharePoint.Client.User, object>>[]
            {
                u => u.AadObjectId,
                u => u.Email,
                u => u.Id,
                u => u.IsSiteAdmin,
                u => u.LoginName,
                u => u.PrincipalType,
                u => u.Title,
                u => u.UserId,
                u => u.UserPrincipalName,
            };

            var clientContext = await _appInfo.GetContext(siteUrl);

            var query = clientContext.Web.SiteUsers.Where(u => u.IsSiteAdmin);
            var siteCollectionAdminUsers = clientContext.LoadQuery(query.Include(retrievalExpressions));
            clientContext.ExecuteQueryRetry();

            return siteCollectionAdminUsers;
        }
    }
}
