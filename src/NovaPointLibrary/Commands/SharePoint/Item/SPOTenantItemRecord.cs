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
        internal SPOTenantListsRecord ListRecord { get; set; }
        internal ProgressTracker Progress { get; set; }
        internal ListItem? Item { get; set; } = null;
        internal string ErrorMessage { get; set; } = string.Empty;

        internal SPOTenantItemRecord(SPOTenantListsRecord listRecord, ProgressTracker progress, ListItem? item)
        {
            ListRecord = listRecord;
            Progress = progress;
            Item = item;
        }
    }
}
