using NovaPointLibrary.Commands.SharePoint.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.List
{
    public class SPOTenantListsParameters : SPOTenantSiteUrlsParameters
    {
        internal Expression<Func<Microsoft.SharePoint.Client.List, object>>[] ListExpresions = new Expression<Func<Microsoft.SharePoint.Client.List, object>>[] {};
        internal bool ListAll { get; set; } = true;
        internal bool IncludeHiddenLists { get; set; } = false;
        internal bool IncludeSystemLists { get; set; } = false;
        internal string ListTitle { get; set; } = String.Empty;

        internal SPOTenantSiteUrlsParameters GetSiteParameters()
        {
            return this;
        }
        //internal SPOSiteURLsEnumarableParameters GetSiteParameters()
        //{
        //    SPOSiteURLsEnumarableParameters p = new()
        //    {
        //        AdminUPN = AdminUPN,
        //        RemoveAdmin = RemoveAdmin,

        //        SiteAll = SiteAll,
        //        IncludePersonalSite = IncludePersonalSite,
        //        IncludeShareSite = IncludeShareSite,
        //        OnlyGroupIdDefined = OnlyGroupIdDefined,
        //        SiteUrl = SiteUrl,
        //        IncludeSubsites = IncludeSubsites,
        //    };

        //    SPOSiteURLsEnumarableParameters x = (SPOSiteURLsEnumarableParameters)this;


        //    return p;
        //}
    }
}
