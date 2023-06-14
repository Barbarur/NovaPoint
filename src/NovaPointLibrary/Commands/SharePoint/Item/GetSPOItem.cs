using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NovaPointLibrary.Commands.SharePoint.Item
{
    internal class GetSPOItem
    {
        private readonly LogHelper _logHelper;
        private readonly AppInfo _appInfo;
        private readonly string AccessToken;
        private readonly int PageSize = 3000;
        internal GetSPOItem(LogHelper logHelper, AppInfo appInfo, string accessToken)
        {
            _logHelper = logHelper;
            _appInfo = appInfo;
            AccessToken = accessToken;
        }
        internal void CsomSingleItem(string siteUrl, string listName, int id = -1)
        {

        }
        //Reference:
        // "https://learn.microsoft.com/en-us/previous-versions/office/developer/sharepoint-2010/ee534956(v=office.14)"
        // https://pnp.github.io/powershell/cmdlets/Get-PnPListItem.html
        // https://github.com/pnp/powershell/blob/dev/src/Commands/Lists/GetListItem.cs
        // https://www.sharepointdiary.com/2015/09/sharepoint-online-get-list-items-using-powershell.html
        internal List<Microsoft.SharePoint.Client.ListItem> CsomAllItems(string siteUrl, string listName)
        {
            _appInfo.IsCancelled();
            string methodName = $"[{GetType().Name}.CsomAllItems]";
            _logHelper.AddLogToTxt(methodName, $"Start getting all items in List '{listName}' at Site '{siteUrl}'");

            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            CamlQuery query = CamlQuery.CreateAllItemsQuery();

            if (HasPageSize())
            {
                var queryElement = XElement.Parse(query.ViewXml);

                var rowLimit = queryElement.Descendants("RowLimit").FirstOrDefault();
                if (rowLimit != null)
                {
                    rowLimit.RemoveAll();
                }
                else
                {
                    rowLimit = new XElement("RowLimit");
                    queryElement.Add(rowLimit);
                }

                rowLimit.SetAttributeValue("Paged", "TRUE");
                rowLimit.SetValue(3000);

                query.ViewXml = queryElement.ToString();
            }


            Microsoft.SharePoint.Client.List oList = clientContext.Web.Lists.GetByTitle(listName);
            List<Microsoft.SharePoint.Client.ListItem> collListItem = new();
            do
            {
                ListItemCollection subcollListItem = oList.GetItems(query);
                clientContext.Load(subcollListItem);
                clientContext.ExecuteQuery();

                collListItem.AddRange(subcollListItem);
                _logHelper.AddLogToUI(methodName, $"Subtotal number of items collected: {collListItem.Count}");
                query.ListItemCollectionPosition = subcollListItem.ListItemCollectionPosition;
            }
            while (query.ListItemCollectionPosition != null);

            _logHelper.AddLogToTxt(methodName, $"Finish getting all items in List '{listName}' at Site '{siteUrl}'. Total: {collListItem.Count} items");
            return collListItem;
        }

        private bool HasPageSize()
        {
            return PageSize > 0;
        }


        internal List<Microsoft.SharePoint.Client.ListItem> CSOMAllItemsWithRoles(string siteUrl, string listName)
        {
            _appInfo.IsCancelled();
            string methodName = $"[{GetType().Name}.CSOMAllItemsWithRoles]";
            _logHelper.AddLogToTxt(methodName, $"Start getting all items in List '{listName}' at Site '{siteUrl}'");

            var retrievalExpressions = new Expression<Func<ListItem, object>>[]
            {
                i => i.FileSystemObjectType,
                i => i.HasUniqueRoleAssignments,
                i => i["FileLeafRef"],
                i => i["FileRef"],
                i => i.RoleAssignments.Include(
                    ra => ra.RoleDefinitionBindings,
                    ra => ra.Member),
            };

            var collList = CSOMAllItemsWithRetrievalExpressions(siteUrl, listName, retrievalExpressions);

            _logHelper.AddLogToTxt(methodName, $"Finish getting all items in List '{listName}' at Site '{siteUrl}'");

            return collList;
        }


        internal List<Microsoft.SharePoint.Client.ListItem> CSOMAllItemsWithRetrievalExpressions(string siteUrl, string listName, Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] retrievalExpressions)
        {
            _appInfo.IsCancelled();
            string methodName = $"[{GetType().Name}.CSOMAllItemsWithRetrievalExpressions]";
            _logHelper.AddLogToTxt(methodName, $"Start getting all items in List '{listName}' at Site '{siteUrl}'");

            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            CamlQuery camlQuery = CamlQuery.CreateAllItemsQuery();

            var queryElement = XElement.Parse(camlQuery.ViewXml);

            var rowLimit = queryElement.Descendants("RowLimit").FirstOrDefault();
            if (rowLimit != null)
            {
                rowLimit.RemoveAll();
            }
            else
            {
                rowLimit = new XElement("RowLimit");
                queryElement.Add(rowLimit);
            }

            rowLimit.SetAttributeValue("Paged", "TRUE");
            rowLimit.SetValue(3000);

            camlQuery.ViewXml = queryElement.ToString();

            Microsoft.SharePoint.Client.List oList = clientContext.Web.Lists.GetByTitle(listName);
            List<Microsoft.SharePoint.Client.ListItem> collListItem = new();

            do
            {
                ListItemCollection subcollListItem = oList.GetItems(camlQuery);
                clientContext.Load(subcollListItem,
                    sci => sci.ListItemCollectionPosition,
                    sci => sci.Include(
                        retrievalExpressions));
                clientContext.ExecuteQuery();

                collListItem.AddRange(subcollListItem);
                _logHelper.AddLogToUI(methodName, $"Subtotal number of items collected: {collListItem.Count}");
                camlQuery.ListItemCollectionPosition = subcollListItem.ListItemCollectionPosition;
            }
            while (camlQuery.ListItemCollectionPosition != null);

            _logHelper.AddLogToTxt(methodName, $"Finish getting all items in List '{listName}' at Site '{siteUrl}'. Total: {collListItem.Count} items");

            return collListItem;
        }
    }
}
