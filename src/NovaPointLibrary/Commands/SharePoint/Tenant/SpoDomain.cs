using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Logging;

namespace NovaPointLibrary.Commands.SharePoint.Tenant
{
    internal class SpoDomain
    {
        private readonly ILogger _logger;
        private readonly IAppClient _appInfo;

        internal SpoDomain(ILogger logger, IAppClient appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<string> GetAsync()
        {
            string url = $"/sites/root";
            var graphSiteRoot = await new GraphAPIHandler(_logger, _appInfo).GetObjectAsync<GraphSitesRoot>(url);
            _logger.Info("AppClient", $"Hostname: {graphSiteRoot.SiteCollection.Hostname}");

            string domain = graphSiteRoot.SiteCollection.Hostname.Remove(graphSiteRoot.SiteCollection.Hostname.IndexOf(".SharePoint.com", StringComparison.OrdinalIgnoreCase));
            _logger.Info("AppClient", $"Domain: {domain}");

            return domain;

        }
    }
}
