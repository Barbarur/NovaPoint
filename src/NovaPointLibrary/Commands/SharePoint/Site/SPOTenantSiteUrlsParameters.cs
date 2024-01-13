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
        public string AdminUPN { get; set; } = String.Empty;
        public bool RemoveAdmin { get; set; } = false;

        public bool SiteAll { get; set; } = true;
        public bool IncludePersonalSite { get; set; } = false;
        public bool IncludeShareSite { get; set; } = true;
        public bool OnlyGroupIdDefined { get; set; } = false;
        public string SiteUrl { get; set; } = String.Empty;
        public bool IncludeSubsites { get; set; } = false;

        internal void ParametersCheck()
        {
            if (string.IsNullOrWhiteSpace(SiteUrl) && !SiteAll)
            {
                throw new Exception($"FORM INCOMPLETED: Site URL cannot be empty when no processing all sites");
            }
        }
    }
}
