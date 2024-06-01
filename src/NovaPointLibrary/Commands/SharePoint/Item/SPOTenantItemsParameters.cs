using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.PreservationHoldLibrary;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions;
using System.Linq.Expressions;


namespace NovaPointLibrary.Commands.SharePoint.Item
{
    public class SPOTenantItemsParameters : ISolutionParameters
    {
        public SPOTenantSiteUrlsWithAccessParameters SitesAccParam { get; set; }
        public SPOListsParameters ListsParam { get; set; }
        internal SPOTenantListsParameters TListsParam
        {
            get { return new(SitesAccParam, ListsParam); }
        }

        public SPOItemsParameters ItemsParam { get; set; }

        public SPOTenantItemsParameters(SPOTenantSiteUrlsWithAccessParameters siteParameters,
                                        SPOListsParameters listParameters,
                                        SPOItemsParameters itemsParam)
        {
            SitesAccParam = siteParameters;
            ListsParam = listParameters;
            ItemsParam = itemsParam;
        }

        public void ParametersCheck()
        {
            if (!String.IsNullOrWhiteSpace(ItemsParam.FolderRelativeUrl) && (String.IsNullOrWhiteSpace(ListsParam.ListTitle) || String.IsNullOrWhiteSpace(SitesAccParam.SiteParam.SiteUrl)))
            {
                throw new Exception($"When using Server relative path for filtering the items, you need to add the List name and URL of a single site");
            }
        }
    }
}
