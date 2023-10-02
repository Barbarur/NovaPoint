using Microsoft.Graph;
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

    internal class SPOSubsiteCSOM
    {
        private readonly Main _main;

        internal SPOSubsiteCSOM(Main main)
        {
            _main = main;
        }

        internal async Task<List<Web>> Get(string siteUrl)
        {
            _main.IsCancelled();

            var expresions = new Expression<Func<Web, object>>[]
            {
            };

            return await Get(siteUrl, expresions);
        }

        internal async Task<List<Web>> Get(string siteUrl, Expression<Func<Web, object>>[] retrievalExpressions)
        {
            _main.IsCancelled();
            string methodName = $"{GetType().Name}.Get";
            _main.AddLogToTxt(methodName, $"Start getting all Subsites");

            var defaultExpressions = new Expression<Func<Web, object>>[]
            {
                w => w.Id,
                w => w.Title,
                w => w.Url,
            };

            var expressions = retrievalExpressions.Union(defaultExpressions).ToArray();

            ClientContext clientContext = await _main.GetContext(siteUrl);

            var subsites = clientContext.Web.Webs;

            clientContext.Load(subsites);
            clientContext.ExecuteQueryRetry();

            List<Web> collSubsites = new();
            collSubsites.AddRange(GetSubWebsInternal(subsites, retrievalExpressions));

            _main.AddLogToTxt(methodName, $"Start getting all Subsites");

            return FilterAddInSites( collSubsites );
        }

        private List<Web> GetSubWebsInternal(WebCollection subsites, Expression<Func<Web, object>>[] retrievalExpressions)
        {
            _main.IsCancelled();
            string methodName = $"{GetType().Name}.Get";
            _main.AddLogToTxt(methodName, $"Start getting Subsites internals");

            var collSubsites = new List<Web>();

            subsites.EnsureProperties(new Expression<Func<WebCollection, object>>[] { wc => wc.Include(w => w.Id) });

            foreach (var subsite in subsites)
            {
                subsite.EnsureProperties(retrievalExpressions);
                collSubsites.Add(subsite);

                collSubsites.AddRange(GetSubWebsInternal(subsite.Webs, retrievalExpressions));
            }

            _main.AddLogToTxt(methodName, $"Finish getting Subsites internals");
            return collSubsites;
        }

        private List<Web> FilterAddInSites(List<Web> collSubsites)
        {
            collSubsites.RemoveAll(w => (!w.Url.Contains(_main._rootPersonalUrl, StringComparison.OrdinalIgnoreCase) && !w.Url.Contains(_main._rootSharedUrl, StringComparison.OrdinalIgnoreCase)));
            return collSubsites;
        }
    }
}
