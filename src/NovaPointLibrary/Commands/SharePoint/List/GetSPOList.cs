using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.List
{
    internal class GetSPOList
    {
        private readonly LogHelper _logHelper;
        private readonly AppInfo _appInfo;
        private readonly string AccessToken;

        private readonly List<string> SystemLists = new() { "appdata", "appfiles", "Composed Looks", "Content type publishing error log", "Converted Forms", "List Template Gallery", "Maintenance Log Library", "Master Page Gallery", "Preservation Hold Library", "Project Policy Item List", "Solution Gallery", "TaxonomyHiddenList", "Theme Gallery", "User Information List", "Web Template Extensions", "Web Part Gallery" };
        private readonly List<string> ResourceLists = new() { "Form Templates", "Site Assets", "Site Pages", "Style Library" };

        internal GetSPOList(LogHelper logHelper, AppInfo appInfo, string accessToken)
        {
            _logHelper = logHelper;
            _appInfo = appInfo;
            AccessToken = accessToken;
        }

        // Reference:
        // https://github.com/pnp/powershell/blob/dev/src/Commands/Base/PipeBinds/ListPipeBind.cs
        internal Microsoft.SharePoint.Client.List? CSOM_Single(string siteUrl, string listName)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.CSOM_Single";
            _logHelper.AddLogToTxt(methodName, $"Start getting List '{listName}' from Site '{siteUrl}'");

            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };


            try
            {

                Microsoft.SharePoint.Client.List list = clientContext.Web.Lists.GetByTitle(listName);

                clientContext.Load(list);
                clientContext.ExecuteQueryRetry();

                _logHelper.AddLogToTxt(methodName, $"Finish getting List '{listName}' from Site '{siteUrl}'");
                return list;
            }
            catch
            {
                _logHelper.AddLogToTxt(methodName, $"Start getting List '{listName}' from Site '{siteUrl}'. List no found!");
                return null;
            }

        }

        //References:
        //https://pnp.github.io/powershell/cmdlets/Get-PnPList.html
        //https://github.com/pnp/powershell/blob/dev/src/Commands/Lists/GetList.cs
        //https://www.sharepointdiary.com/2018/03/sharepoint-online-get-all-lists-using-powershell.html
        internal List<Microsoft.SharePoint.Client.List> CSOM_All(string siteUrl, bool includeSystemLists = false, bool includeResourceLists = false, bool includeHidden = false)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.CSOM_All";
            _logHelper.AddLogToTxt(methodName, $"Start getting all Lists for '{siteUrl}'");

            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            Web oWebSite = clientContext.Web;
            ListCollection collList = oWebSite.Lists;

            clientContext.Load(collList);
            clientContext.ExecuteQuery();
            _logHelper.AddLogToTxt(methodName, $"Collected Lists for site '{siteUrl}'. Gross Total: '{collList.Count}'");

            // Define potential pre-filters to the return collection of list
            List<string> systemLists = new() { "appdata", "appfiles", "Composed Looks", "Content type publishing error log", "Converted Forms", "List Template Gallery", "Maintenance Log Library", "Master Page Gallery", "Preservation Hold Library", "Project Policy Item List", "Solution Gallery", "TaxonomyHiddenList", "Theme Gallery", "User Information List", "Web Template Extensions", "Web Part Gallery" };
            List<string> resourceLists = new() { "Form Templates", "Site Assets", "Site Pages", "Style Library" };

            // Filter the collection of Lists
            List<Microsoft.SharePoint.Client.List> finalCollList = new();
            foreach (Microsoft.SharePoint.Client.List oList in collList)
            {

                if (oList.Hidden == true && !includeHidden) { continue; }
                if (systemLists.Contains(oList.Title) == true && !includeSystemLists) { continue; }
                if (resourceLists.Contains(oList.Title) == true && !includeResourceLists) { continue; }

                finalCollList.Add(oList);

            }

            _logHelper.AddLogToTxt(methodName, $"Finish getting all Lists for '{siteUrl}'. Final Count: {finalCollList.Count}");
            return finalCollList;
        }

        internal List<Microsoft.SharePoint.Client.List> CSOMAllListsWithRoles(string siteUrl, bool includeSystemLists = false, bool includeResourceLists = false, bool includeHidden = false)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.CSOMAllListsWithRoles";
            _logHelper.AddLogToTxt(methodName, $"Start getting all Lists for '{siteUrl}' with roles");

            var retrievalExpressions = new Expression<Func<Microsoft.SharePoint.Client.List, object>>[]
            {
                w => w.BaseType,
                w => w.DefaultViewUrl,
                w => w.HasUniqueRoleAssignments,
                w => w.Hidden,
                w => w.Id,
                w => w.RoleAssignments.Include(
                    ra => ra.RoleDefinitionBindings,
                    ra => ra.Member),
                w => w.Title,
            };

            var collList = CSOMAllListsRetrievalExpressions(siteUrl, retrievalExpressions);

            _logHelper.AddLogToTxt(methodName, $"Finish getting all Lists for '{siteUrl}' with roles");

            return CSOMFilterLists(collList, includeSystemLists, includeResourceLists, includeHidden);

        }

        internal ListCollection CSOMAllListsRetrievalExpressions(string siteUrl, Expression<Func<Microsoft.SharePoint.Client.List, object>>[] retrievalExpressions)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.CSOMAllListsRetrievalExpressions";
            _logHelper.AddLogToTxt(methodName, $"Start getting all Lists for '{siteUrl}'");

            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            Web oWebSite = clientContext.Web;
            ListCollection collList = oWebSite.Lists;

            clientContext.Load( collList, l => l.Include(retrievalExpressions) );
            clientContext.ExecuteQuery();

            _logHelper.AddLogToTxt(methodName, $"Finish getting all Lists for '{siteUrl}' final count: {collList.Count}");
            return collList;
        }


        internal List<Microsoft.SharePoint.Client.List> CSOMFilterLists(ListCollection collList, bool includeSystemLists, bool includeResourceLists, bool includeHidden)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.CSOMFilterLists";
            _logHelper.AddLogToTxt(methodName, $"Start filtering lists");

            List<Microsoft.SharePoint.Client.List> finalCollList = new();
            foreach (Microsoft.SharePoint.Client.List oList in collList)
            {
                if (oList.Hidden == true && !includeHidden) { continue; }
                if (SystemLists.Contains(oList.Title) == true && !includeSystemLists) { continue; }
                if (ResourceLists.Contains(oList.Title) == true && !includeResourceLists) { continue; }

                finalCollList.Add(oList);
            }

            _logHelper.AddLogToTxt(methodName, $"Finish filtering lists");
            
            return finalCollList;
        }   
    }
}
