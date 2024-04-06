﻿using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.List
{
    internal class SPOTenantListsRecord
    {
        internal string SiteUrl { get; set; }
        internal string SiteName { get; set; }
        internal ProgressTracker Progress { get; set; }
        internal Microsoft.SharePoint.Client.List? List { get; set; } = null;
        internal string ErrorMessage { get; set; } = string.Empty;

        internal SPOTenantListsRecord(SPOTenantSiteUrlsRecord recordSite, ProgressTracker progress, Microsoft.SharePoint.Client.List? oList)
        {
            Progress = progress;
            SiteUrl = recordSite.SiteUrl;
            SiteName = recordSite.SiteName;
            List = oList;
        }

    }
}
