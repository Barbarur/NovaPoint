using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    // TO BE DEPRECATED ONCE SPOSiteCollectionCSOM IS ON PRODUCTION
    internal class GetSPOSiteCollection
    {
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;
        private readonly string AccessToken;
        internal GetSPOSiteCollection(NPLogger logger, Authentication.AppInfo appInfo, string accessToken)
        {
            _logger = logger;
            _appInfo = appInfo;
            AccessToken = accessToken;
        }

        internal List<SiteProperties> CSOM_AdminAll(string adminUrl, bool includePersonalSite = false, bool groupIdDefined = false)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.CSOM_AdminAll";
            _logger.LogTxt(methodName, $"Start getting Site Collections; IncludePersonalSite '{includePersonalSite}', Group ID Defined '{groupIdDefined}'");

            using var clientContext = new ClientContext(adminUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            SPOSitePropertiesEnumerableFilter filter = new()
            {
                IncludePersonalSite = includePersonalSite ? PersonalSiteFilter.Include : PersonalSiteFilter.UseServerDefault,
                IncludeDetail = true,
            };
            if (groupIdDefined) { filter.GroupIdDefined = 1; }

            var tenant = new Tenant(clientContext);
            var collSites = new List<SiteProperties>();

            do
            {
                SPOSitePropertiesEnumerable subcollSiteCollections = tenant.GetSitePropertiesFromSharePointByFilters(filter);
                clientContext.Load(subcollSiteCollections);
                clientContext.ExecuteQuery();
                collSites.AddRange(subcollSiteCollections);
                filter.StartIndex = subcollSiteCollections.NextStartIndexFromSharePoint;
                _logger.LogUI(methodName,$"getting Site Collections... {collSites.Count}");

            } while (!string.IsNullOrWhiteSpace(filter.StartIndex));

            _logger.LogTxt(methodName,$"Finish getting Site Collections. Total: {collSites.Count}");
            return collSites;
        }
    }
}
