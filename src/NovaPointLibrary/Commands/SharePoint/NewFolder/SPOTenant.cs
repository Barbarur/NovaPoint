using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Core.Logging;
using System.Linq.Expressions;

namespace NovaPointLibrary.Commands.SharePoint.Admin
{
    internal class SPOTenant
    {
        private readonly LoggerSolution _logger;
        private readonly Authentication.AppInfo _appInfo;

        internal SPOTenant(LoggerSolution logger, Authentication.AppInfo appInfo)
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
