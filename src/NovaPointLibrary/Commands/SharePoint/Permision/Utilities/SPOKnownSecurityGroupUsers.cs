using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Permision.Utilities
{
    internal class SPOKnownSecurityGroupUsers
    {
        internal string GroupID { get; set; }
        internal string GroupName { get; set; }
        internal string AccountType { get; set; }
        internal string Users { get; set; }
        internal string Remarks { get; set; }

        internal SPOKnownSecurityGroupUsers(string groupID, string groupName, string accountType, string user, string remarks)
        {
            GroupID = groupID;
            GroupName = groupName;
            AccountType = accountType;
            Users = user;
            Remarks = remarks;
        }
    }
}
