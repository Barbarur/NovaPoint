using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Logging;
using System.Linq.Expressions;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    internal class SPOWebCSOM
    {
        private readonly ILogger _logger;
        private readonly IAppClient _appInfo;
        
        internal SPOWebCSOM(ILogger logger, IAppClient appInfo)
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
            _logger.Info(GetType().Name, $"Getting Site '{siteUrl}'");

            var defaultExpressions = new Expression<Func<Web, object>>[]
            {
                w => w.Id,
                w => w.LastItemModifiedDate,
                w => w.ServerRelativeUrl,
                w => w.Title,
                w => w.Url,
                w => w.WebTemplate,
                w => w.LastItemUserModifiedDate,
                w => w.RootFolder,
                w => w.RootFolder.ServerRelativeUrl,
            };

            var expressions = retrievalExpressions.Union(defaultExpressions).ToArray();

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);

            clientContext.Web.EnsureProperties(expressions);

            return clientContext.Web;
        }
    }
}
