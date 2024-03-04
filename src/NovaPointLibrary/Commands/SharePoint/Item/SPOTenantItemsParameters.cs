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

        public SPOTenantItemsParameters(SPOTenantListsParameters listsParameters, SPOItemsParameters itemsParameters)
        {
            ListsParameters = listsParameters;
            ItemsParameters = itemsParameters;
        }
    }
}
