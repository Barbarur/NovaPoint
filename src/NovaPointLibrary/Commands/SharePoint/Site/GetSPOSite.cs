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
    // TO BE DEPRECATED ONCE SPOSiteCSOM IS ON PRODUCTION
    internal class GetSPOSite
    {
        private readonly LogHelper _logHelper;
        private readonly AppInfo _appInfo;
        private readonly string AccessToken;

        internal GetSPOSite(LogHelper logHelper, AppInfo appInfo, string accessToken)
        {
            _logHelper = logHelper;
            _appInfo = appInfo;
            AccessToken = accessToken;
        }

        internal Web CSOMWithRoles(string siteUrl)
        {
            _appInfo.IsCancelled();
            _logHelper.AddLogToTxt($"[{GetType().Name}.CSOMWithRoles] - Start getting site with roles: '{siteUrl}'");
            var retrievalExpressions = new Expression<Func<Web, object>>[]
            {
                w => w.HasUniqueRoleAssignments,
                w => w.Id,
                w => w.RoleAssignments.Include(
                    ra => ra.RoleDefinitionBindings,
                    ra => ra.Member),
                w => w.ServerRelativeUrl,
                w => w.Title,
                w => w.Url,
            };

            var results = CSOM(siteUrl, retrievalExpressions);

            _logHelper.AddLogToTxt($"[{GetType().Name}.CSOMWithRoles] - Finish getting site with roles: '{siteUrl}'");

            return results;
        }

        private Web CSOM(string siteUrl, Expression<Func<Web, object>>[] retrievalExpressions)
        {
            _appInfo.IsCancelled();
            _logHelper.AddLogToTxt($"[{GetType().Name}.CSOM] - Start getting Site '{siteUrl}'");

            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            clientContext.Web.EnsureProperties(retrievalExpressions);

            _logHelper.AddLogToTxt($"[{GetType().Name}.CSOM] - Finish getting Site '{siteUrl}'");
            return clientContext.Web;
        }
    }
}
