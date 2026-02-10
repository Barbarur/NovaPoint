using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Logging;

namespace NovaPointLibrary.Commands.SharePoint.Admin
{
    internal class SPOTenant
    {
        private readonly ILogger _logger;
        private readonly IAppClient _appInfo;

        internal SPOTenant(ILogger logger, IAppClient appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<Tenant> GetAsync()
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Getting Tenant information");

            var adminContext = await _appInfo.GetContext(_appInfo.AdminUrl);
            var tenant = new Tenant(adminContext);
            adminContext.Load(tenant);
            adminContext.Load(tenant, t => t.HideDefaultThemes);
            adminContext.ExecuteQueryRetry();

            return tenant;
        }
    }
}
