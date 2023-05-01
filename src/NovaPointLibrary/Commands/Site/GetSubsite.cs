using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.Site
{
    internal class GetSubsite
    {
        private LogHelper _logHelper;
        private readonly AppInfo _appInfo;
        private readonly string AccessToken;
        internal GetSubsite(LogHelper logHelper, AppInfo appInfo, string accessToken)
        {
            _logHelper = logHelper;
            _appInfo = appInfo;
            AccessToken = accessToken;
        }

        internal List<Web> CsomAllSubsitesBasicExpressions(string siteUrl)
        {
            var retrievalExpressions = new Expression<Func<Web, object>>[]
            {
                w => w.Id,
                w => w.Url,
                w => w.Title,
                w => w.ServerRelativeUrl,
                w => w.WebTemplate,
                w => w.LastItemModifiedDate,
            };

            var results = CsomAllSubsites(siteUrl, retrievalExpressions);

            return results;
        }

        internal List<Web> CsomAllSubsitesWithRoles(string siteUrl)
        {
            var retrievalExpressions = new Expression<Func<Web, object>>[]
            {
                w => w.Id,
                w => w.Url,
                w => w.Title,
                w => w.ServerRelativeUrl,
                w => w.HasUniqueRoleAssignments,
                w => w.RoleAssignments.Include(
                    ra => ra.RoleDefinitionBindings,
                    ra => ra.Member),
            };

            var results = CsomAllSubsites(siteUrl, retrievalExpressions);

            return results;

        }

        private List<Web> CsomAllSubsites(string siteUrl, Expression<Func<Web, object>>[] retrievalExpressions)
        {

            if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
            
            _logHelper = new(_logHelper, $"{GetType().Name}.CsomAllSubsites");

            _logHelper.AddLogToTxt($"Getting Subsite");

            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            List<Web> results = new();

            var subsites = clientContext.Web.Webs;

            clientContext.Load(subsites);
            clientContext.ExecuteQueryRetry();

            results.AddRange(GetSubWebsInternal(subsites, retrievalExpressions));

            return results;

        }

        private List<Web> GetSubWebsInternal(WebCollection subsites, Expression<Func<Web, object>>[] retrievalExpressions)
        {
            if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); }

            var subwebs = new List<Web>();

            // Retrieve the subsites in the provided webs collection
            subsites.EnsureProperties(new Expression<Func<WebCollection, object>>[] { wc => wc.Include(w => w.Id) });

            foreach (var subsite in subsites)
            {
                // Retrieve all the properties for this particular subsite
                subsite.EnsureProperties(retrievalExpressions);
                subwebs.Add(subsite);

                subwebs.AddRange(GetSubWebsInternal(subsite.Webs, retrievalExpressions));

            }

            return subwebs;
        }
    }
}
