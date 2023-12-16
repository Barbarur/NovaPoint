using Microsoft.Graph;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
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
        private readonly Main _main;
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;

        internal SPOSiteCollectionCSOM(Main main)
        {
            _main = main;
        }

        internal SPOSiteCollectionCSOM(NPLogger logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<List<SiteProperties>> GetDeprecated(string siteUrl, bool includeShareSite, bool includePersonalSite, bool onlyGroupIdDefined)
        {
            _main.IsCancelled();
            string methodName = $"{GetType().Name}.Get";
            _main.AddLogToTxt(methodName, $"Start getting Site Collections; IncludePersonalSite '{includePersonalSite}', Group ID Defined '{onlyGroupIdDefined}'");

            ClientContext clientContext = await _main.GetContext(_main._adminUrl);

            var tenant = new Tenant(clientContext);
            var collSites = new List<SiteProperties>();

            if (!String.IsNullOrWhiteSpace(siteUrl))
            {
                _main.AddLogToTxt(methodName, $"Getting single site {siteUrl}");

                SiteProperties oSiteCollection = tenant.GetSitePropertiesByUrl(siteUrl, true);
                clientContext.Load(oSiteCollection);
                clientContext.ExecuteQuery();

                collSites.Add(oSiteCollection);
            }
            else
            {
                SPOSitePropertiesEnumerableFilter filter = new()
                {
                    IncludePersonalSite = includePersonalSite ? PersonalSiteFilter.Include : PersonalSiteFilter.Exclude,
                    IncludeDetail = true,
                };
                if (onlyGroupIdDefined) { filter.GroupIdDefined = 1; }

                do
                {
                    SPOSitePropertiesEnumerable subcollSiteCollections = tenant.GetSitePropertiesFromSharePointByFilters(filter);
                    clientContext.Load(subcollSiteCollections);
                    clientContext.ExecuteQuery();
                    collSites.AddRange(subcollSiteCollections);
                    filter.StartIndex = subcollSiteCollections.NextStartIndexFromSharePoint;
                    _main.AddLogToUI(methodName, $"Getting Site Collections... {collSites.Count}");

                    tenant = new Tenant(await _main.GetContext(_main._adminUrl));

                } while (!string.IsNullOrWhiteSpace(filter.StartIndex));
            }

            _main.AddLogToTxt(methodName, $"Finish getting Site Collections. Total: {collSites.Count}");
            return FilterAddInSitesDEPRECATED(collSites, includeShareSite);
        }

        private List<SiteProperties> FilterAddInSitesDEPRECATED(List<SiteProperties> collSiteCollections, bool includeShareSite)
        {
            string methodName = $"{GetType().Name}.FilterAddInSites";
            collSiteCollections.RemoveAll(w => (!w.Url.Contains(_main._rootPersonalUrl, StringComparison.OrdinalIgnoreCase) && !w.Url.Contains(_main._rootSharedUrl, StringComparison.OrdinalIgnoreCase)));
            collSiteCollections.RemoveAll(w => w.Title == "" || w.Template.Contains("Redirect"));

            if (!includeShareSite) { collSiteCollections.RemoveAll(s => !s.Template.Contains("SPSPERS")); }

            _main.AddLogToTxt(methodName, $"Filtered Site Collections. Total: {collSiteCollections.Count}");

            return collSiteCollections;
        }

        internal async Task<List<SiteProperties>> Get(string siteUrl, bool includeShareSite, bool includePersonalSite, bool onlyGroupIdDefined)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.Get";
            _logger.LogTxt(methodName, $"Start getting Site Collections; IncludePersonalSite '{includePersonalSite}', Group ID Defined '{onlyGroupIdDefined}'");

            ClientContext clientContext = await _appInfo.GetContext(_logger, _appInfo.AdminUrl);

            var tenant = new Tenant(clientContext);
            var collSites = new List<SiteProperties>();

            if (!String.IsNullOrWhiteSpace(siteUrl))
            {
                _logger.LogTxt(methodName, $"Getting single site {siteUrl}");

                SiteProperties oSiteCollection = tenant.GetSitePropertiesByUrl(siteUrl, true);
                clientContext.Load(oSiteCollection);
                clientContext.ExecuteQuery();

                collSites.Add(oSiteCollection);
            }
            else
            {
                SPOSitePropertiesEnumerableFilter filter = new()
                {
                    IncludePersonalSite = includePersonalSite ? PersonalSiteFilter.Include : PersonalSiteFilter.Exclude,
                    IncludeDetail = true,
                };
                if (onlyGroupIdDefined) { filter.GroupIdDefined = 1; }

                do
                {
                    SPOSitePropertiesEnumerable subcollSiteCollections = tenant.GetSitePropertiesFromSharePointByFilters(filter);
                    clientContext.Load(subcollSiteCollections);
                    clientContext.ExecuteQuery();
                    collSites.AddRange(subcollSiteCollections);
                    filter.StartIndex = subcollSiteCollections.NextStartIndexFromSharePoint;
                    _logger.LogTxt(methodName, $"Got Site Collections gross: {collSites.Count}");

                    tenant = new Tenant(await _appInfo.GetContext(_logger, _appInfo.AdminUrl));

                } while (!string.IsNullOrWhiteSpace(filter.StartIndex));
            }

            _logger.LogTxt(methodName, $"Finish getting Site Collections. Total: {collSites.Count}");
            return FilterAddInSites(collSites, includeShareSite);
        }

        private List<SiteProperties> FilterAddInSites(List<SiteProperties> collSiteCollections, bool includeShareSite)
        {
            string methodName = $"{GetType().Name}.FilterAddInSites";

            collSiteCollections.RemoveAll(w => (!w.Url.Contains(_appInfo.RootPersonalUrl, StringComparison.OrdinalIgnoreCase) && !w.Url.Contains(_appInfo.RootSharedUrl, StringComparison.OrdinalIgnoreCase)));

            collSiteCollections.RemoveAll(w => w.Title == "" || w.Template.Contains("Redirect"));

            if (!includeShareSite) { collSiteCollections.RemoveAll(s => !s.Template.Contains("SPSPERS")); }

            _logger.LogUI(methodName, $"Got Site Collections: {collSiteCollections.Count}");

            return collSiteCollections;
        }
    }
}
