using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Item
{
    internal class SPOTenantItemRecord
    {
        internal string SiteUrl { get; set; }
        internal string SiteName { get; set; }
        internal Microsoft.SharePoint.Client.List? List { get; set; } = null;
        internal SPOTenantListsRecord ListRecord { get; set; }
        internal ListItem? Item { get; set; } = null;
        internal Exception? Ex { get; } = null;

        internal SPOTenantItemRecord(SPOTenantListsRecord listRecord)
        {
            SiteUrl = listRecord.SiteUrl;
            SiteName = listRecord.SiteName;
            List = listRecord.List;
            ListRecord = listRecord;
            Ex = listRecord.Ex;
        }

        internal SPOTenantItemRecord(SPOTenantListsRecord listRecord, ListItem item)
        {
            SiteUrl = listRecord.SiteUrl;
            SiteName = listRecord.SiteName;
            List = listRecord.List;
            ListRecord = listRecord;
            Item = item;
        }

        internal SPOTenantItemRecord(SPOTenantListsRecord listRecord, Exception exception)
        {
            SiteUrl = listRecord.SiteUrl;
            SiteName = listRecord.SiteName;
            List = listRecord.List;
            ListRecord = listRecord;
            Ex = exception;
        }
    }
}
