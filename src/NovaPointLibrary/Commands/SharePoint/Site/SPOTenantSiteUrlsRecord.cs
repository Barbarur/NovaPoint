using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using PnP.Core.Model.SharePoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    internal class SPOTenantSiteUrlsRecord
    {
        internal ProgressTracker Progress { get; set; }
        internal string SiteUrl { get; set; }
        internal string SiteName { get; set; }
        internal SiteProperties? SiteProperties { get; set; } = null;
        internal Web? Web { get; set; } = null;
        internal string ErrorMessage { get; set; } = string.Empty;
        
        internal SPOTenantSiteUrlsRecord(ProgressTracker progress, SiteProperties oSiteCollection)
        {
            Progress = progress;
            SiteUrl = oSiteCollection.Url;
            SiteName = oSiteCollection.Title;
            SiteProperties = oSiteCollection;
        }

        internal SPOTenantSiteUrlsRecord(ProgressTracker progress, Web oWeb)
        {
            Progress = progress;
            SiteUrl = oWeb.Url;
            SiteName = oWeb.Title;
            Web = oWeb;
        }

        internal SPOTenantSiteUrlsRecord(ProgressTracker progress, string siteUrl)
        {
            Progress = progress;
            SiteUrl = siteUrl;
            SiteName = "Unknown";
        }

        internal SPOTenantSiteUrlsRecord ShallowCopy()
        {
            return (SPOTenantSiteUrlsRecord) this.MemberwiseClone();
        }
    }
}
