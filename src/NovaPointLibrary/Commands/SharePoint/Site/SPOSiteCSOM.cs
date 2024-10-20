using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Core.Logging;
using System.Linq.Expressions;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    internal class SPOSiteCSOM
    {
        private readonly LoggerSolution _logger;
        private readonly AppInfo _appInfo;

        internal SPOSiteCSOM(LoggerSolution logger, AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<Microsoft.SharePoint.Client.Site> GetAsync(string siteUrl, Expression<Func<Microsoft.SharePoint.Client.Site, object>>[]? retrievalExpressions = null)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Getting Site '{siteUrl}'");

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
