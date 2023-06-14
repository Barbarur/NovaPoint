using CamlBuilder;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using NovaPointLibrary.Commands;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NovaPoint.Commands.Site
{
    internal class GetSiteCollection
    {
        private readonly LogHelper _logHelper;
        private readonly string AccessToken;
        internal GetSiteCollection(LogHelper logHelper, string accessToken)
        {
            _logHelper = logHelper;
            AccessToken = accessToken;
        }

        //private static string graphUrl = "https://graph.microsoft.com/v1.0/";
        //public static async Task<List<GraphAdminListSite>> Graph_AdminListAll(Action<string> addRecord, string accessToken)
        //{
        //    string message = graphUrl + "sites/m365x29094319-admin.sharepoint.com/lists/bd34467c-d7ce-4074-b9ab-acc392392342/items?$top=5000&$expand=fields";

        //    List<GraphAdminListSite> siteList = new();

        //    int batch = 1;
        //    int totalSites = 0;
        //    try
        //    {
        //        while (message != null)
        //        {
        //            string consoleWrite = "AllAdminListBatch: " + batch;
        //            addRecord(consoleWrite);
        //            var jsonResponse = await HttpHandler.Graph_Get(message, accessToken);

        //            ContentResponseGraphAdminList responseContent = JsonConvert.DeserializeObject<ContentResponseGraphAdminList>(jsonResponse);

        //            siteList.AddRange(responseContent.value);
        //            message = responseContent.odatanextLink;

        //            addRecord(responseContent.odatanextLink);

        //            totalSites += responseContent.value.Count;
        //            string numberSites = "Number Sites collected:" + totalSites;
        //            addRecord(numberSites);

        //            batch++;
        //        }
        //        return siteList;
        //    }
        //    catch
        //    {
        //        throw;
        //    }
        //}

        //internal async Task<List<SPOAdminListSite>> SPO_AdminListAll(string domain, bool oneDrive = false)
        //{
        //    string message = "https://" + domain + "-admin.sharepoint.com" + "/_api/web/lists/getbytitle('DO_NOT_DELETE_SPLIST_TENANTADMIN_ALL_SITES_AGGREGATED_SITECOLLECTIONS')/items?$top=5000";
        //    if ( !oneDrive ) 
        //    {
        //        message = "https://" + domain + "-admin.sharepoint.com" + "/_api/web/lists/getbytitle('DO_NOT_DELETE_SPLIST_TENANTADMIN_AGGREGATED_SITECOLLECTIONS')/items?$top=5000";
        //    }

        //    List<SPOAdminListSite> siteList = new();

        //    int totalSites = 0;

        //    HttpHandler httpHandler = new(_logHelper, AccessToken);

        //    while (message is not null)
        //    {
        //        try
        //        {
        //            _logHelper.LogDetailInfo($"[{GetType().Name}.SPOAdminListAll]", $"Http request using message: { message }");
        //            var jsonResponse = await httpHandler.SPO_Get(message);

        //            ContentResponseSPOAdminList? responseContent = JsonConvert.DeserializeObject<ContentResponseSPOAdminList>(jsonResponse);

        //            if (responseContent != null)
        //            {
        //                siteList.AddRange(responseContent.value);
        //                message = responseContent.odatanextLink;
        //                totalSites += responseContent.value.Count;
        //                _logHelper.LogDetailInfo($"[{GetType().Name}.SPOAdminListAll]", $"Number Sites collected: { totalSites}");
        //            }
        //        }
        //        catch
        //        {
        //            throw;
        //        }
        //    }
        //    return siteList;
        //}

        internal List<SiteProperties> CSOM_AdminAll(string adminUrl, bool includePersonalSite = false, bool groupIdDefined = false)
        {
            _logHelper.AddLogToTxt($"[{GetType().Name}.CSOM_AdminAll] - Start getting Site Collections; IncludePersonalSite '{includePersonalSite}', Group ID Defined '{groupIdDefined}'");
            
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
                _logHelper.AddLogToTxt($"[{GetType().Name}.CSOM_AdminAll] - getting Site Collections... {collSites.Count}");

            } while (!string.IsNullOrWhiteSpace(filter.StartIndex));

            _logHelper.AddLogToTxt($"({GetType().Name}.CSOM_AdminAll] - Finish getting Site Collections. Total: {collSites.Count}");
            return collSites;
        }


        /// <summary>
        /// No recommended to use. UNFINISHED method
        /// </summary>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public async Task<string> SPO_SearchAll(string accessToken)
        {
            string message = "https://m365x29094319.sharepoint.com/_api/search/query?querytext='contentclass:STS_Site'&rowlimit=50";

            HttpHandler httpHandler = new(_logHelper, AccessToken);
            try
            {
                var jsonResponse = await httpHandler.SPO_Get(message);
                return jsonResponse;
            }
            catch
            {
                throw;
            }
        }
    }
}
