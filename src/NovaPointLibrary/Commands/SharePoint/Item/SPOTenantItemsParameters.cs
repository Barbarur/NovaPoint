using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions;
using System.Linq.Expressions;


namespace NovaPointLibrary.Commands.SharePoint.Item
{
    public class SPOTenantItemsParameters : ISolutionParameters
    {
        public SPOTenantListsParameters ListsParameters;
        public SPOItemsParameters ItemsParameters;
        //internal Expression<Func<ListItem, object>>[] ItemExpresions = new Expression<Func<ListItem, object>>[] { };
        //internal Expression<Func<ListItem, object>>[] FileExpresions = new Expression<Func<ListItem, object>>[] { };

        //private string _folderRelativeUrl = String.Empty;
        //public string FolderRelativeUrl
        //{
        //    get { return _folderRelativeUrl; }
        //    set { _folderRelativeUrl = value.Trim(); }
        //}
        public SPOTenantItemsParameters(SPOTenantListsParameters listsParameters, SPOItemsParameters itemsParameters)
        {
            ListsParameters = listsParameters;
            ItemsParameters = itemsParameters;
        }
    }
}
