using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NovaPointLibrary.Commands.Item
{
    internal class GetItem
    {
        private LogHelper _LogHelper;
        private readonly string AccessToken;
        private readonly int PageSize = 3000;
        internal GetItem(LogHelper logHelper, string accessToken)
        {
            _LogHelper = logHelper;
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
            _LogHelper = new(_LogHelper, $"{GetType().Name}.CsomAllItems");
            _LogHelper.AddLogToUI($"Getting all items in List '{listName}' at Site '{siteUrl}'");
            
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
                _LogHelper.AddLogToUI($"Subtotal number of items: {collListItem.Count}");
                query.ListItemCollectionPosition = subcollListItem.ListItemCollectionPosition;
            }
            while (query.ListItemCollectionPosition != null);

            _LogHelper.AddLogToUI($"Successfully obtained {collListItem.Count} items");
            return collListItem;
        }
        private bool HasPageSize()
        {
            return PageSize > 0;
        }
    }
}
