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
        public Expression<Func<Microsoft.SharePoint.Client.List, object>>[] ListExpresions = new Expression<Func<Microsoft.SharePoint.Client.List, object>>[] {};
        public bool ListAll { get; set; } = true;
        public bool IncludeHiddenLists { get; set; } = false;
        public bool IncludeSystemLists { get; set; } = false;

        private string _listTitle = string.Empty;
        public string ListTitle
        {
            get { return _listTitle; }
            set { _listTitle = value.Trim(); }
        }

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
