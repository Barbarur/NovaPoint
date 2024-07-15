using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    public class SPOTenantSiteUrlsWithAccessParameters : ISolutionParameters
    {
        public SPOAdminAccessParameters AdminAccess { get; set; } = new();
        public SPOTenantSiteUrlsParameters SiteParam { get; set; } = new();

    }
}
