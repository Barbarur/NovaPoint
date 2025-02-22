using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Permission.Utilities
{
    internal class SPOKnownRoleAssignmentGroupHeaders
    {
        internal List<SPOKnownSharePointGroupUsers> _groupsSharePoint = new();
        internal List<SPOKnownSecurityGroupUsers> _groupsSecurity = new();

        internal string _accountType = string.Empty;

    }
}
