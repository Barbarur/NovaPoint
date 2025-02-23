using NovaPointLibrary.Commands.AzureAD.Groups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NovaPointLibrary.Commands.SharePoint.SharingLinks.SpoSharingLinksRest;

namespace NovaPointLibrary.Commands.SharePoint.Permission.Utilities
{
    internal class SPOKnownRoleAssignmentGroups
    {
        internal List<SPOKnownSharePointGroupUsers> SharePointGroup { get; set; } = new();
        internal Dictionary<string, KnownItemGroups> SharingLinks { get; set; } = new();
        internal List<AADGroupUserEmails> SecurityGroups { get; init; } = new();

        internal List<SPOKnownSharePointGroupUsers> FindSharePointGroups(string siteUrl, string groupName)
        {
            return SharePointGroup.Where(kg => kg.GroupName == groupName && siteUrl.Contains(kg.SiteURL)).ToList();
        }

        internal void ResetSiteGroups()
        {
            SharePointGroup = new();
            SharingLinks = new();
        }
    }
}
