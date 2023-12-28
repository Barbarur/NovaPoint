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

        internal new void ParametersCheck()
        {
            base.ParametersCheck();

            if (!ListAll && String.IsNullOrWhiteSpace(ListTitle))
            {
                throw new Exception($"FORM INCOMPLETED: Library name cannot be empty when not processing all Libraries");
            }
        }
    }
}
