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

        internal SPOSiteCollectionCSOM(Main main)
        {
            _main = main;
        }

        internal async Task<List<SiteProperties>> Get(string siteUrl, bool includeShareSite, bool includePersonalSite, bool onlyGroupIdDefined)
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

                    tenant = new Tenant( await _main.GetContext(_main._adminUrl) );

                } while (!string.IsNullOrWhiteSpace(filter.StartIndex));
            }

            _main.AddLogToTxt(methodName, $"Finish getting Site Collections. Total: {collSites.Count}");
            return FilterAddInSites(collSites, includeShareSite);
        }

        private List<SiteProperties> FilterAddInSites(List<SiteProperties> collSiteCollections, bool includeShareSite)
        {
            string methodName = $"{GetType().Name}.FilterAddInSites";

            collSiteCollections.RemoveAll(w => (!w.Url.Contains(_main._rootPersonalUrl, StringComparison.OrdinalIgnoreCase) && !w.Url.Contains(_main._rootSharedUrl, StringComparison.OrdinalIgnoreCase)));
            
            collSiteCollections.RemoveAll(w => w.Title == "" || w.Template.Contains("Redirect"));

            if (!includeShareSite) { collSiteCollections.RemoveAll(s => !s.Template.Contains("SPSPERS")); }

            _main.AddLogToTxt(methodName, $"Filtered Site Collections. Total: {collSiteCollections.Count}");
            
            return collSiteCollections;
        }
    }
}
