using Microsoft.Graph;
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

    // TO BE DEPRECATED WHEN SPOListCSOM IS TESTED FOR PRODUCTION
    internal class GetSPOList
    {
        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;
        private readonly string AccessToken;

        internal static readonly List<string> SystemLists = new() { "appdata", "appfiles", "Composed Looks", "Content and Structure Reports", "Content type publishing error log", "Converted Forms", "List Template Gallery", "Maintenance Log Library", "Master Page Gallery", "Preservation Hold Library", "Project Policy Item List", "Reusable Content", "Solution Gallery", "TaxonomyHiddenList", "Theme Gallery", "User Information List", "Web Template Extensions", "Web Part Gallery" };
        internal static readonly List<string> ResourceLists = new() { "Form Templates", "Site Assets", "Site Collection Documents", "Site Collection Images", "Site Pages", "Style Library" };

        internal GetSPOList(NPLogger logger, AppInfo appInfo, string accessToken)
        {
            _logger = logger;
            _appInfo = appInfo;
            AccessToken = accessToken;
        }

        internal Microsoft.SharePoint.Client.List CSOMSingleStandard(string siteUrl, string listTitle)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.CSOMSingleStandard";
            _logger.LogTxt(methodName, $"Start getting List '{listTitle}' from Site '{siteUrl}'");

            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            Microsoft.SharePoint.Client.List? oList = null;

            oList = clientContext.Web.GetListByTitle(listTitle);

            if (oList == null)
            {
                throw new Exception($"List '{listTitle}' from Site '{siteUrl}'");
            }
            else
            {
                _logger.LogTxt(methodName, $"Finish getting List '{listTitle}' from Site '{siteUrl}'");
                return oList;
            }
        }

        internal Microsoft.SharePoint.Client.List CSOMSingleWithExpresions(string siteUrl, string listTitle, params System.Linq.Expressions.Expression<Func<Microsoft.SharePoint.Client.List, object>>[] retrievals)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.CSOMSingleWithExpresions";
            _logger.LogTxt(methodName, $"Start getting List '{listTitle}' from Site '{siteUrl}'");

            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            Microsoft.SharePoint.Client.List? oList = null;

            oList = clientContext.Web.GetListByTitle(listTitle, retrievals);

            if (oList == null)
            {
                throw new Exception($"List '{listTitle}' from Site '{siteUrl}'");
            }
            else
            {
                _logger.LogTxt(methodName, $"Finish getting List '{listTitle}' from Site '{siteUrl}'");
                return oList;
            }
        }

        //References:
        //https://pnp.github.io/powershell/cmdlets/Get-PnPList.html
        //https://github.com/pnp/powershell/blob/dev/src/Commands/Lists/GetList.cs
        //https://www.sharepointdiary.com/2018/03/sharepoint-online-get-all-lists-using-powershell.html
        internal List<Microsoft.SharePoint.Client.List> CSOMAll(string siteUrl, bool includeSystemLists = false, bool includeResourceLists = false, bool includeHidden = false)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.CSOMAll";
            _logger.LogTxt(methodName, $"Start getting all Lists for '{siteUrl}'");

            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            ListCollection collList = clientContext.Web.Lists;

            clientContext.Load(collList);
            clientContext.ExecuteQuery();

            _logger.LogTxt(methodName, $"Finish getting all Lists for '{siteUrl}'. Count: {collList.Count}");
            return CSOMFilterLists(collList, includeSystemLists, includeResourceLists, includeHidden);
        }

        // TO BE REMOVED IN THE FUTURE WHEN MODIFIED THE SOLUTION USING IT
        internal List<Microsoft.SharePoint.Client.List> CSOMAllListsWithRoles(string siteUrl,
                                                                              bool includeSystemLists = false,
                                                                              bool includeResourceLists = false,
                                                                              bool includeHidden = false)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.CSOMAllListsWithRoles";
            _logger.LogTxt(methodName, $"Start getting all Lists for '{siteUrl}' with roles");

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

            var collList = CSOMAllWithExpressions(siteUrl, retrievalExpressions, includeSystemLists, includeResourceLists, includeHidden);

            _logger.LogTxt(methodName, $"Finish getting all Lists for '{siteUrl}' with roles");

            return collList;
        }

        internal List<Microsoft.SharePoint.Client.List> CSOMAllWithExpressions(string siteUrl,
                                                                 Expression<Func<Microsoft.SharePoint.Client.List, object>>[] retrievalExpressions,
                                                                 bool includeSystemLists = false,
                                                                 bool includeResourceLists = false,
                                                                 bool includeHidden = false)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.CSOMAllWithExpressions";
            _logger.LogTxt(methodName, $"Start getting all Lists for '{siteUrl}'");

            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            ListCollection collList = clientContext.Web.Lists;

            clientContext.Load( collList, l => l.Include(retrievalExpressions) );
            clientContext.ExecuteQuery();

            _logger.LogTxt(methodName, $"Finish getting all Lists for '{siteUrl}'. Count: {collList.Count}");
            return CSOMFilterLists(collList, includeSystemLists, includeResourceLists, includeHidden); ;
        }


        private List<Microsoft.SharePoint.Client.List> CSOMFilterLists(ListCollection collList, bool includeSystemLists, bool includeResourceLists, bool includeHidden)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.CSOMFilterLists";
            _logger.LogTxt(methodName, $"Start filtering lists. Count: {collList.Count}");

            List<Microsoft.SharePoint.Client.List> finalCollList = new();
            foreach (Microsoft.SharePoint.Client.List oList in collList)
            {
                if (oList.Hidden == true && !includeHidden) { continue; }
                if (SystemLists.Contains(oList.Title) == true && !includeSystemLists) { continue; }
                if (ResourceLists.Contains(oList.Title) == true && !includeResourceLists) { continue; }

                finalCollList.Add(oList);
            }

            _logger.LogTxt(methodName, $"Finish filtering lists. Count: {finalCollList.Count}");
            
            return finalCollList;
        }   
    }
}
