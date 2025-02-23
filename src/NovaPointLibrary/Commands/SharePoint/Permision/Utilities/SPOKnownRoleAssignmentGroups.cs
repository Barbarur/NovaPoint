using NovaPointLibrary.Commands.AzureAD.Groups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Permission.Utilities
{
    internal class SPOKnownRoleAssignmentGroups
    {
        internal List<SPOKnownSharePointGroupUsers> GroupsSharePoint { get; init; } = new();
        internal List<AADGroupUserEmails> SecurityGroups { get; init; } = new();

        internal List<SPOKnownSharePointGroupUsers> FindSharePointGroups(string siteUrl, string groupName)
        {
            return GroupsSharePoint.Where(kg => kg.GroupName == groupName && siteUrl.Contains(kg.SiteURL)).ToList();
        }

    }
}
