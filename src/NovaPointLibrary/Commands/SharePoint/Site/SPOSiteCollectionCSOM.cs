using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Core.Logging;
using System.Linq.Expressions;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    internal class SPOSiteCollectionCSOM
    {
        private readonly LoggerSolution _logger;
        private readonly Authentication.AppInfo _appInfo;

        internal SPOSiteCollectionCSOM(LoggerSolution logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<SiteProperties> GetAsync(string siteUrl, Expression<Func<SiteProperties, object>>[] siteExpressions)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Getting single site {siteUrl}");

            ClientContext clientContext = await _appInfo.GetContext(_appInfo.AdminUrl);
            var tenant = new Tenant(clientContext);

            SiteProperties siteProperties = tenant.GetSitePropertiesByUrl(siteUrl, true);

            clientContext.Load(siteProperties, siteExpressions);
            clientContext.ExecuteQuery();

            return siteProperties;
        }

        internal async Task<List<SiteProperties>> GetAllAsync(bool includeShareSite, bool includePersonalSite, bool onlyGroupIdDefined)
        {
            _appInfo.IsCancelled();

            _logger.Info(GetType().Name, $"Getting Site Collections; IncludePersonalSite '{includePersonalSite}', Group ID Defined '{onlyGroupIdDefined}'");

            SPOSitePropertiesEnumerableFilter filter = new()
            {
                IncludePersonalSite = includePersonalSite ? PersonalSiteFilter.Include : PersonalSiteFilter.Exclude,
                IncludeDetail = true,
            };
            if (onlyGroupIdDefined) { filter.GroupIdDefined = 1; }

            var collSites = new List<SiteProperties>();
            do
            {
                ClientContext clientContext = await _appInfo.GetContext(_appInfo.AdminUrl);
                var tenant = new Tenant(clientContext);

                SPOSitePropertiesEnumerable subcollSiteCollections = tenant.GetSitePropertiesFromSharePointByFilters(filter);
                clientContext.Load(subcollSiteCollections);
                clientContext.ExecuteQuery();
                collSites.AddRange(subcollSiteCollections);
                filter.StartIndex = subcollSiteCollections.NextStartIndexFromSharePoint;

                _logger.Info(GetType().Name, $"Collected {collSites.Count} Site Collections...");

            } while (!string.IsNullOrWhiteSpace(filter.StartIndex));

            return FilterAddInSites(collSites, includeShareSite);
        }

        private List<SiteProperties> FilterAddInSites(List<SiteProperties> collSiteCollections, bool includeShareSite)
        {
            string methodName = $"{GetType().Name}.FilterAddInSites";

            collSiteCollections.RemoveAll(w => (!w.Url.Contains(_appInfo.RootPersonalUrl, StringComparison.OrdinalIgnoreCase) && !w.Url.Contains(_appInfo.RootSharedUrl, StringComparison.OrdinalIgnoreCase)));

            collSiteCollections.RemoveAll(w => w.Title == "" || w.Template.Contains("Redirect"));

            if (!includeShareSite) { collSiteCollections.RemoveAll(s => !s.Template.Contains("SPSPERS")); }

            _logger.UI(GetType().Name, $"Collected {collSiteCollections.Count} Site Collections");

            return collSiteCollections;
        }

        internal async Task<List<SiteProperties>> GetAllAsync(SPOTenantSiteUrlsParameters siteFilters)
        {
            _appInfo.IsCancelled();

            var listSiteProperties = new List<SiteProperties>();

            var allSiteProperties = await GetAllAsync(siteFilters.IncludePersonalSite, false);
            foreach (SiteProperties siteProperties in allSiteProperties)
            {
                if (siteProperties.Template.Contains("SPSPERS", StringComparison.OrdinalIgnoreCase))
                {
                    if (siteFilters.IncludePersonalSite) { listSiteProperties.Add(siteProperties); }
                }
                else if (siteProperties.Template.Contains("SITEPAGEPUBLISHING#0", StringComparison.OrdinalIgnoreCase))
                {
                    if (siteFilters.IncludeCommunication) { listSiteProperties.Add(siteProperties); }
                }
                else if (siteProperties.Template.Contains("GROUP#0", StringComparison.OrdinalIgnoreCase))
                {
                    if (siteFilters.IncludeTeamSite && !siteProperties.IsTeamsConnected) { listSiteProperties.Add(siteProperties); }
                    else if (siteFilters.IncludeTeamSiteWithTeams && siteProperties.IsTeamsConnected) { listSiteProperties.Add(siteProperties); }
                }
                else if (siteProperties.Template.Contains("STS#3", StringComparison.OrdinalIgnoreCase))
                {
                    if (siteFilters.IncludeTeamSiteWithNoGroup) { listSiteProperties.Add(siteProperties); }
                }
                else if (siteProperties.Template.Contains("TEAMCHANNEL", StringComparison.OrdinalIgnoreCase))
                {
                    if (siteFilters.IncludeChannels) { listSiteProperties.Add(siteProperties); }
                }
                else if (siteFilters.IncludeClassic)
                {
                    listSiteProperties.Add(siteProperties);
                }

            }

            _logger.UI(GetType().Name, $"Collected {listSiteProperties.Count} Site Collections");

            return listSiteProperties;
        }

        internal async Task<List<SiteProperties>> GetAllAsync(bool includePersonalSite, bool onlyGroupIdDefined)
        {
            _appInfo.IsCancelled();

            _logger.Info(GetType().Name, $"Getting Site Collections");

            SPOSitePropertiesEnumerableFilter filter = new()
            {
                IncludePersonalSite = includePersonalSite ? PersonalSiteFilter.Include : PersonalSiteFilter.Exclude,
                IncludeDetail = true,
            };
            if (onlyGroupIdDefined) { filter.GroupIdDefined = 1; }

            var collSites = new List<SiteProperties>();
            do
            {
                ClientContext clientContext = await _appInfo.GetContext(_appInfo.AdminUrl);
                var tenant = new Tenant(clientContext);

                SPOSitePropertiesEnumerable subcollSiteCollections = tenant.GetSitePropertiesFromSharePointByFilters(filter);
                clientContext.Load(subcollSiteCollections);
                clientContext.ExecuteQuery();
                collSites.AddRange(subcollSiteCollections);
                filter.StartIndex = subcollSiteCollections.NextStartIndexFromSharePoint;

                _logger.Info(GetType().Name, $"Collected {collSites.Count} Site Collections...");

            } while (!string.IsNullOrWhiteSpace(filter.StartIndex));

            return FilterAddInSites(collSites);
        }

        private List<SiteProperties> FilterAddInSites(List<SiteProperties> collSiteCollections)
        {
            collSiteCollections.RemoveAll(w => (!w.Url.Contains(_appInfo.RootPersonalUrl, StringComparison.OrdinalIgnoreCase) && !w.Url.Contains(_appInfo.RootSharedUrl, StringComparison.OrdinalIgnoreCase)));

            collSiteCollections.RemoveAll(w => w.Title == "" || w.Template.Contains("Redirect"));

            return collSiteCollections;
        }

    }
}
