using Microsoft.Graph;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    internal class SPOSiteCollectionCSOM
    {
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;

        internal SPOSiteCollectionCSOM(NPLogger logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<SiteProperties> GetAsync(string siteUrl)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Getting single site {siteUrl}");

            ClientContext clientContext = await _appInfo.GetContext(_appInfo.AdminUrl);
            var tenant = new Tenant(clientContext);

            SiteProperties oSiteCollection = tenant.GetSitePropertiesByUrl(siteUrl, true);
            clientContext.Load(oSiteCollection);
            clientContext.ExecuteQuery();

            return oSiteCollection;
        }

        internal async Task<List<SiteProperties>> GetAllAsync(bool includeShareSite, bool includePersonalSite, bool onlyGroupIdDefined)
        {
            _appInfo.IsCancelled();

            _logger.LogTxt(GetType().Name, $"Getting Site Collections; IncludePersonalSite '{includePersonalSite}', Group ID Defined '{onlyGroupIdDefined}'");

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

                _logger.LogTxt(GetType().Name, $"Collected {collSites.Count} Site Collections...");

            } while (!string.IsNullOrWhiteSpace(filter.StartIndex));

            return FilterAddInSites(collSites, includeShareSite);
        }

        private List<SiteProperties> FilterAddInSites(List<SiteProperties> collSiteCollections, bool includeShareSite)
        {
            string methodName = $"{GetType().Name}.FilterAddInSites";

            collSiteCollections.RemoveAll(w => (!w.Url.Contains(_appInfo.RootPersonalUrl, StringComparison.OrdinalIgnoreCase) && !w.Url.Contains(_appInfo.RootSharedUrl, StringComparison.OrdinalIgnoreCase)));

            collSiteCollections.RemoveAll(w => w.Title == "" || w.Template.Contains("Redirect"));

            if (!includeShareSite) { collSiteCollections.RemoveAll(s => !s.Template.Contains("SPSPERS")); }

            _logger.LogUI(GetType().Name, $"Collected {collSiteCollections.Count} Site Collections");

            return collSiteCollections;
        }
    }
}
