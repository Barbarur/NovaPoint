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
    internal class SPOSiteCSOM
    {
        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;

        internal SPOSiteCSOM(NPLogger logger, AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<Microsoft.SharePoint.Client.Site> GetAsync(string siteUrl, Expression<Func<Microsoft.SharePoint.Client.Site, object>>[]? retrievalExpressions = null)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Getting Site '{siteUrl}'");

            var defaultExpressions = new Expression<Func<Microsoft.SharePoint.Client.Site, object>>[]
            {
                s => s.Id,
                s => s.Url,
            };
            if (retrievalExpressions != null) { defaultExpressions = retrievalExpressions.Union(defaultExpressions).ToArray(); }

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);
            clientContext.Site.EnsureProperties(defaultExpressions);

            return clientContext.Site;
        }
    }
}
