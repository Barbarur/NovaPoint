using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    public class SPOTenantSiteUrlsParameters : ISolutionParameters
    {
        public bool RemoveAdmin { get; set; } = false;

        public bool IncludePersonalSite { get; set; } = false;
        public bool IncludeShareSite { get; set; } = true;
        public bool OnlyGroupIdDefined { get; set; } = false;

        private string _siteUrl = string.Empty;
        public string SiteUrl
        {
            get { return _siteUrl; }
            set { _siteUrl = value.Trim(); }
        }

        public bool IncludeSubsites { get; set; } = false;

        internal void ParametersCheck()
        {
        }
    }
}
