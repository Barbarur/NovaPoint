using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Utilities
{
    internal class SPOTenantResults
    {
        internal string SiteUrl { get; set; }
        internal Microsoft.SharePoint.Client.List? List { get; set; } = null;
        internal ListItem? ListItem { get; set; } = null;
        internal string ErrorMessage { get; set; } = string.Empty;

        internal ProgressTracker Progress { get; set; }

        internal SPOTenantResults(ProgressTracker progress, string siteUrl)
        {
            Progress = progress;
            SiteUrl = siteUrl;
        }

        internal SPOTenantResults(ProgressTracker progress, string siteUrl, Microsoft.SharePoint.Client.List list)
        {
            Progress = progress;
            SiteUrl = siteUrl;
            List = list;
        }

        internal SPOTenantResults(ProgressTracker progress, string siteUrl, Microsoft.SharePoint.Client.List list, ListItem listItem)
        {
            Progress = progress;
            SiteUrl = siteUrl;
            List = list;
            ListItem = listItem;
        }
    }
}
