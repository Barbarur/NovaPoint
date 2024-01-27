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
    internal class SPOWebCSOM
    {
        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;
        
        internal SPOWebCSOM(NPLogger logger, AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<Web> GetAsync(string siteUrl)
        {
            _appInfo.IsCancelled();

            var expresions = new Expression<Func<Web, object>>[]
            {
            };

            return await GetAsync(siteUrl, expresions);
        }

        internal async Task<Web> GetAsync(string siteUrl, Expression<Func<Web, object>>[] retrievalExpressions)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Getting Site '{siteUrl}'");

            var defaultExpressions = new Expression<Func<Web, object>>[]
            {
                w => w.Id,
                w => w.Title,
                w => w.Url,
            };

            var expressions = retrievalExpressions.Union(defaultExpressions).ToArray();

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);

            clientContext.Web.EnsureProperties(expressions);

            return clientContext.Web;
        }
    }
}
