using Microsoft.Graph;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using PnP.Core.Model.SharePoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.List
{
    internal class GetList
    {
        private LogHelper _LogHelper;
        private readonly string AccessToken;
        internal GetList(LogHelper logHelper, string accessToken)
        {
            _LogHelper = logHelper;
            AccessToken = accessToken;
        }

        // Reference:
        // https://github.com/pnp/powershell/blob/dev/src/Commands/Base/PipeBinds/ListPipeBind.cs
        internal Microsoft.SharePoint.Client.List? CSOM_Single(string siteUrl, string listName)
        {
            _LogHelper = new(_LogHelper, $"{GetType().Name}.CSOM_Single");
            _LogHelper.AddLogToTxt($"Getting List '{listName}' from Site '{siteUrl}'");

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

                _LogHelper.AddLogToTxt($"List '{listName}' found in Site '{siteUrl}'");
                return list;

            }
            catch
            {
                
                _LogHelper.AddLogToTxt($"List '{listName}' no found in Site '{siteUrl}'");
                
                return null;

            }

        }

        //References:
        //https://pnp.github.io/powershell/cmdlets/Get-PnPList.html
        //https://github.com/pnp/powershell/blob/dev/src/Commands/Lists/GetList.cs
        //https://www.sharepointdiary.com/2018/03/sharepoint-online-get-all-lists-using-powershell.html
        internal List<Microsoft.SharePoint.Client.List> CSOM_All(string siteUrl, bool includeSystemLists = false ,bool includeResourceLists = false, bool includeHidden = false)
        {
            _LogHelper = new(_LogHelper, $"{GetType().Name}.CSOM_AdminAll");
            _LogHelper.AddLogToTxt($"Getting Lists for '{siteUrl}'");
            
            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            Web oWebSite = clientContext.Web;
            ListCollection collList = oWebSite.Lists;

            clientContext.Load(collList);
            clientContext.ExecuteQuery();
            _LogHelper.AddLogToTxt($"Getting Lists for '{siteUrl}' gross count: '{collList.Count}'");

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

            _LogHelper.AddLogToTxt($"Getting Lists for '{siteUrl}' final count: {finalCollList.Count}");
            _LogHelper.AddLogToTxt($"Getting Lists for '{siteUrl}' COMPLETED");
            return finalCollList;
        }
    }
}
