using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    internal class SPOTenantSiteUrlsRecord
    {
        internal string SiteUrl { get; set; }
        internal string SiteName { get; set; }
        internal ProgressTracker Progress { get; set; }
        internal string ErrorMessage { get; set; } = string.Empty;

        internal SPOTenantSiteUrlsRecord(ProgressTracker progress, Web oWeb)
        {
            Progress = progress;
            SiteUrl = oWeb.Url;
            SiteName = oWeb.Title;
        }

        internal SPOTenantSiteUrlsRecord(ProgressTracker progress, SiteProperties oSiteCollection)
        {
            Progress = progress;
            SiteUrl = oSiteCollection.Url;
            SiteName = oSiteCollection.Title;
        }

        internal SPOTenantSiteUrlsRecord(SPOTenantSiteUrlsRecord record)
        {
            Progress = record.Progress;
            SiteUrl = record.SiteUrl;
            SiteName = record.SiteName;
        }
    }
}
