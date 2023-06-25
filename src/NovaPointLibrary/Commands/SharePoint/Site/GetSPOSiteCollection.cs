using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    internal class GetSPOSiteCollection
    {
        private readonly LogHelper _logHelper;
        private readonly Authentication.AppInfo _appInfo;
        private readonly string AccessToken;
        internal GetSPOSiteCollection(LogHelper logHelper, Authentication.AppInfo appInfo, string accessToken)
        {
            _logHelper = logHelper;
            _appInfo = appInfo;
            AccessToken = accessToken;
        }

        internal List<SiteProperties> CSOM_AdminAll(string adminUrl, bool includePersonalSite = false, bool groupIdDefined = false)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.CSOM_AdminAll";
            _logHelper.AddLogToTxt(methodName, $"Start getting Site Collections; IncludePersonalSite '{includePersonalSite}', Group ID Defined '{groupIdDefined}'");

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
                _logHelper.AddLogToUI(methodName,$"getting Site Collections... {collSites.Count}");

            } while (!string.IsNullOrWhiteSpace(filter.StartIndex));

            _logHelper.AddLogToTxt(methodName,$"Finish getting Site Collections. Total: {collSites.Count}");
            return collSites;
        }
    }
}
