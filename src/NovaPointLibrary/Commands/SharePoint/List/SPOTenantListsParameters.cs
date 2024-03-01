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

        //public Expression<Func<Microsoft.SharePoint.Client.List, object>>[] ListExpresions = new Expression<Func<Microsoft.SharePoint.Client.List, object>>[] {};

        //public bool AllLists { get; set; } = false;
        //public bool IncludeLists { get; set; } = true;
        //public bool IncludeLibraries { get; set; } = true;
        //public bool IncludeHiddenLists { get; set; } = false;
        //public bool IncludeSystemLists { get; set; } = false;


        //private string _listTitle = string.Empty;
        //public string ListTitle
        //{
        //    get { return _listTitle; }
        //    set { _listTitle = value.Trim(); }
        //}

        public SPOTenantListsParameters(SPOTenantSiteUrlsWithAccessParameters siteParameters, SPOListsParameters listParameters)
        {
            SiteAccParam = siteParameters;
            ListParam = listParameters;
        }
    }
}
