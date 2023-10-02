using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    internal class SPOSiteCSOM
    {
        private readonly Main _main;

        internal SPOSiteCSOM(Main main)
        {
            _main = main;
        }

        internal async Task<Web> Get(string siteUrl)
        {
            _main.IsCancelled();

            var expresions = new Expression<Func<Web, object>>[]
            {
            };

            return await Get(siteUrl, expresions);
        }

        internal async Task<Web> Get(string siteUrl, Expression<Func<Web, object>>[] retrievalExpressions)
        {
            _main.IsCancelled();
            string methodName = $"{GetType().Name}.Get";
            _main.AddLogToTxt(methodName, $"Start getting Site '{siteUrl}'");

            var defaultExpressions = new Expression<Func<Web, object>>[]
            {
                w => w.Id,
                w => w.Title,
                w => w.Url,
            };

            var expressions = retrievalExpressions.Union(defaultExpressions).ToArray();

            ClientContext clientContext = await _main.GetContext(siteUrl);

            clientContext.Web.EnsureProperties(expressions);

            _main.AddLogToTxt(methodName, $" Finish getting Site '{siteUrl}'");
            return clientContext.Web;
        }
    }
}
