using Microsoft.Graph;
using Microsoft.Online.SharePoint.TenantAdministration;
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

namespace NovaPointLibrary.Commands.SharePoint.Site
{

    internal class SPOSubsiteCSOM
    {
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;

        internal SPOSubsiteCSOM(NPLogger logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<List<Web>> GetAsync(string siteUrl, Expression<Func<Web, object>>[]? retrievalExpressions = null)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Start getting all Subsites");

            var defaultExpressions = new Expression<Func<Web, object>>[]
            {
                w => w.Id,
                w => w.Title,
                w => w.Url,
                w => w.RootFolder,
                w => w.RootFolder.ServerRelativeUrl,
            };
            if (retrievalExpressions != null) { defaultExpressions = retrievalExpressions.Union(defaultExpressions).ToArray(); }

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);

            var subsites = clientContext.Web.Webs;

            clientContext.Load(subsites);
            clientContext.ExecuteQueryRetry();

            List<Web> collSubsites = new();
            collSubsites.AddRange(GetSubWebsInternal(subsites, defaultExpressions));

            return FilterAddInSites(collSubsites);
        }

        private List<Web> GetSubWebsInternal(WebCollection subsites, Expression<Func<Web, object>>[] retrievalExpressions)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Start getting Subsites internals");

            var collSubsites = new List<Web>();

            subsites.EnsureProperties(new Expression<Func<WebCollection, object>>[] { wc => wc.Include(w => w.Id) });

            foreach (var subsite in subsites)
            {
                subsite.EnsureProperties(retrievalExpressions);
                collSubsites.Add(subsite);

                collSubsites.AddRange(GetSubWebsInternal(subsite.Webs, retrievalExpressions));
            }

            return collSubsites;
        }


        private List<Web> FilterAddInSites(List<Web> collSubsites)
        {
            collSubsites.RemoveAll(w => (!w.Url.Contains(_appInfo.RootPersonalUrl, StringComparison.OrdinalIgnoreCase) && !w.Url.Contains(_appInfo.RootSharedUrl, StringComparison.OrdinalIgnoreCase)));

            _logger.LogTxt(GetType().Name, $"Subsites count: {collSubsites.Count}");

            return collSubsites;
        }
    }
}
