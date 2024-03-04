using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.List
{
    public class SPOTenantListsParameters : ISolutionParameters
    {
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam { get; set; }
        public SPOListsParameters ListParam { get; set; }

        public SPOTenantListsParameters(SPOTenantSiteUrlsWithAccessParameters siteParameters, SPOListsParameters listParameters)
        {
            SiteAccParam = siteParameters;
            ListParam = listParameters;
        }
    }
}
