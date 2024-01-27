using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Permision.Utilities
{
    internal class SPOKnownRoleAssignmentGroups
    {
        internal List<SPOKnownSharePointGroupUsers> _groupsSharePoint = new();
        internal List<SPOKnownSecurityGroupUsers> _groupsSecurity = new();

        internal List<SPOKnownSharePointGroupUsers> FindSharePointGroups(string siteUrl, string groupName)
        {
            return _groupsSharePoint.Where(kg => kg.GroupName == groupName && siteUrl.Contains(kg.SiteURL)).ToList();
        }

        internal List<SPOKnownSecurityGroupUsers> FindSecurityGroups(string groupID, string groupName)
        {
            return _groupsSecurity.Where(kg => kg.GroupID == groupID && kg.GroupName == groupName).ToList();
        }

        internal void AddNewGroupsFromHeaders(SPOKnownRoleAssignmentGroupHeaders groupHeaders, string users, string remarks)
        {
            foreach (var header in groupHeaders._groupsSharePoint)
            {
                _groupsSharePoint.Add(new(header.SiteURL, header.GroupName, groupHeaders._accountType, users, remarks));
            }

            foreach (var header in groupHeaders._groupsSecurity)
            {
                _groupsSecurity.Add(new(header.GroupID, header.GroupName, groupHeaders._accountType, users, remarks));
            }
        }
    }
}
